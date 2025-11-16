using Amazon.S3;
using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.S3;
using MatchBy.Settings;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatchBy.Services.Conversations;

public class ConversationService(
    ApplicationDbContext applicationDbContext,
    IS3Service s3Service,
    IOptions<S3Settings> s3Settings,
    IValidator<CreateConversationDto> createConversationValidator,
    IValidator<UpdateConversationDto> updateConversationValidator) : IConversationService
{
    private async Task<CursorPaginationResponse<List<ConversationDto>>> PaginateAsync(
        IQueryable<Conversation> baseQuery,
        int pageSize,
        string? cursorReceived,
        string? query)
    {
        if (!string.IsNullOrWhiteSpace(cursorReceived))
        {
            var cursor = ConversationCursorDto.Decode(cursorReceived);
            if (cursor != null)
            {
                baseQuery = baseQuery
                    .Where(p =>
                        p.LastMessageAtUtc != null
                            ? p.LastMessageAtUtc < cursor.Date ||
                              p.LastMessageAtUtc == cursor.Date &&
                              string.Compare(p.Id, cursor.Id) <= 0
                            : p.CreatedAtUtc < cursor.Date ||
                              p.CreatedAtUtc == cursor.Date &&
                              string.Compare(p.Id, cursor.Id) <= 0);
            }
        }

        if (query != null)
        {
            baseQuery = baseQuery.Where(c => c.Title != null && c.Title.ToLower().Contains(query.ToLower()) ||
                                             c.Participants.Any(p => p.DisplayName.ToLower().Contains(query.ToLower())));
        }

        List<Conversation> items = await baseQuery
            .OrderByDescending(p => p.LastMessageAtUtc)
            .ThenByDescending(p => p.Id)
            .Take(pageSize + 1)
            .ToListAsync();

        bool hasNext = items.Count > pageSize;
        string? nextCursor = null;
        if (hasNext)
        {
            Conversation last = items[^1];
            DateTime orderingDate = last.LastMessageAtUtc ?? last.CreatedAtUtc;
            nextCursor = ConversationCursorDto.Encode(last.Id, orderingDate);
            items.RemoveAt(items.Count - 1);
        }

        foreach (Conversation conv in items)
        {
            await RefreshConversationImagesAsync(conv);
        }

        await applicationDbContext.SaveChangesAsync();

        var conversationDtos = items.Select(conversation => conversation.ToDto()).ToList();

        return new CursorPaginationResponse<List<ConversationDto>>
        {
            Data = conversationDtos,
            NextCursor = nextCursor
        };
    }


    public async Task<Result<CursorPaginationResponse<List<ConversationDto>>>> GetConversationsAsync(
        string creatorUserId,
        int pageSize,
        string? cursor,
        string? query,
        CancellationToken ct = default)
    {
        IQueryable<Conversation> baseQuery = applicationDbContext.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Creator)
            .Include(c => c.Messages)
            .Where(c => c.Participants.Any(p => p.Id == creatorUserId));

        return Result<CursorPaginationResponse<List<ConversationDto>>>.Ok(await PaginateAsync(baseQuery, pageSize,
            cursor, query));
    }

    public async Task<Result<ConversationDto>> GetConversationByIdAsync(string conversationId, string creatorUserId,
        CancellationToken ct = default)
    {
        Conversation? conversation = await applicationDbContext.Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .Where(m => m.Id.Equals(conversationId))
            .Where(m => m.Participants.Any(p => p.Id == creatorUserId))
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
        {
            return Result<ConversationDto>.Fail("No conversation found");
        }
        
        await RefreshConversationImagesAsync(conversation);

        // For private conversations, set the title to the other participant's name
        if (conversation.Type == ConversationType.Private)
        {
            conversation.Title = conversation.Participants.FirstOrDefault(p => !p.Id.Equals(creatorUserId))?.DisplayName;
        }
        
        return Result<ConversationDto>.Ok(conversation.ToDto());
    }


    public async Task<Result<ConversationDto>> CreateConversationAsync(CreateConversationDto createConversationDto,
        CancellationToken ct = default)
    {
        await createConversationValidator.ValidateAndThrowAsync(createConversationDto, cancellationToken: ct);

        Conversation conversation = createConversationDto.ToEntity();

        //if already a private conversation between these users, return null
        if (createConversationDto.ConversationType == ConversationType.Private)
        {
            bool exists = await applicationDbContext.Conversations
                .Include(c => c.Participants)
                .Where(c => c.Type == ConversationType.Private)
                .AnyAsync(c => c.Participants.Count == createConversationDto.ParticipantIds.Count &&
                               c.Participants.All(p => createConversationDto.ParticipantIds.Contains(p.Id)), ct);
            if (exists)
            {
                return Result<ConversationDto>.Fail("Private conversation between these users already exists.");
            }
        }

        List<ApplicationUser> participants = await applicationDbContext.Users
            .Where(u => createConversationDto.ParticipantIds.Contains(u.Id))
            .ToListAsync(ct);

        conversation.Participants = participants;

        await applicationDbContext.Conversations.AddAsync(conversation, ct);
        await applicationDbContext.SaveChangesAsync(ct);

        return await GetConversationByIdAsync(conversation.Id, conversation.CreatorId, ct);
    }

    public async Task<Result<ConversationDto>> UpdateConversationAsync(UpdateConversationDto updateConversationDto,
        CancellationToken ct = default)
    {
        await updateConversationValidator.ValidateAndThrowAsync(updateConversationDto, cancellationToken: ct);

        // only the creator can update participants
        Conversation? convo = await applicationDbContext.Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .Where(c => c.Id == updateConversationDto.ConversationId)
            .Where(c => c.CreatorId == updateConversationDto.CreatorUserId)
            .FirstOrDefaultAsync(ct);
        if (convo is null)
        {
            return Result<ConversationDto>.Fail("Conversation not found or user is not the creator.");
        }

        List<ApplicationUser> participants = await applicationDbContext.Users
            .Where(u => updateConversationDto.ParticipantIds.Contains(u.Id))
            .ToListAsync(ct);
        
        convo.Title = updateConversationDto.Title;
        convo.Participants = participants;
        convo.UpdatedAtUtc = DateTime.UtcNow;

        if (updateConversationDto.File is not null)
        {
            return await UpdateConversationImageAsync(convo, updateConversationDto.CreatorUserId,
                updateConversationDto.File, ct);
        }

        //inside updateConversationImageAsync we already save changes
        //so we only need to save changes here if no image update
        await applicationDbContext.SaveChangesAsync(ct);
        return await GetConversationByIdAsync(convo.Id, updateConversationDto.CreatorUserId, ct);
    }

    public async Task<Result<bool>> DeleteConversationAsync(string conversationId, string userId,
        CancellationToken ct = default)
    {
        Conversation? convo = await applicationDbContext.Conversations
            .Include(m => m.Participants)
            .Where(c => c.Id == conversationId)
            .FirstOrDefaultAsync(ct);

        if (convo is null)
        {
            return Result<bool>.Fail("Conversation not found.");
        }

        bool canDelete = convo.Type == ConversationType.Private
            ? await applicationDbContext.Conversations
                .AnyAsync(c => c.Id == conversationId && c.Participants.Any(p => p.Id == userId), ct)
            : await applicationDbContext.Conversations
                .AnyAsync(c => c.Id == conversationId && c.CreatorId == userId, ct);

        if (!canDelete)
        {
            return Result<bool>.Fail("User does not have permission to delete this conversation.");
        }

        int affected = await applicationDbContext.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);

        return Result<bool>.Ok(affected > 0);
    }

    public async Task<Result<int>> LeaveConversationAsync(string conversationId, string userId,
        CancellationToken ct = default)
    {
        Conversation? convo = await applicationDbContext.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (convo is null)
        {
            return Result<int>.Fail("Conversation not found.");
        }

        ApplicationUser? me = convo.Participants.FirstOrDefault(p => p.Id == userId);
        if (me is null)
        {
            return Result<int>.Fail("User is not a participant of the conversation.");
        }

        convo.Participants.Remove(me);
        convo.UpdatedAtUtc = DateTime.UtcNow;

        // Se não ficou ninguém (ou conversa privada com menos de 2), faz soft-delete
        int remaining = convo.Participants.Count;

        bool mustSoftDelete = remaining == 1;

        if (mustSoftDelete)
        {
            convo.DeletedAtUtc = DateTime.UtcNow;
        }

        await applicationDbContext.SaveChangesAsync(ct);

        return Result<int>.Ok(mustSoftDelete ? 1 : 2);
    }

    private async Task<Result<ConversationDto>> UpdateConversationImageAsync(
        Conversation conversation,
        string userId,
        IBrowserFile file,
        CancellationToken ct = default)
    {
        // upload
        Result<string> uploadedKey = await s3Service.UploadBrowserFileAsync(file, $"conversations/{conversation.Id}/image");
        if (!uploadedKey.Success)
        {
            return Result<ConversationDto>.Fail(uploadedKey.ErrorMessages.ToArray());
        }

        // URL presign
        Result<string> url =
            await s3Service.GetPresignedUrlAsync($"conversations/{conversation.Id}/image/{uploadedKey.Data}", HttpVerb.GET);
        if (!url.Success)
        {
            return Result<ConversationDto>.Fail(url.ErrorMessages.ToArray());
        }

        // delete previous, if it exists
        string? oldKey = conversation.Image?.Key;
        if (!string.IsNullOrWhiteSpace(oldKey) && !oldKey.Equals(uploadedKey.Data, StringComparison.OrdinalIgnoreCase))
        {
            await s3Service.DeleteFileAsync($"conversations/{conversation.Id}/image/{oldKey}");
        }

        // store the image info
        conversation.Image = new FileStore(
            Url: url.Data!,
            ExpireDateTimeUtc: DateTime.UtcNow.AddMinutes(30),
            Key: uploadedKey.Data!,
            FileCategory: FileCategory.ConversationImage,
            FileType: FileType.Image,
            CreatedAtUtc: DateTime.UtcNow
        );
        conversation.UpdatedAtUtc = DateTime.UtcNow;

        await applicationDbContext.SaveChangesAsync(ct);

        return await GetConversationByIdAsync(conversation.Id, userId, ct);
    }

    private async Task RefreshConversationImagesAsync(Conversation c)
    {
        // Imagem da conversa
        if (c.Image is not null)
        {
            if (c.Image.ExpireDateTimeUtc >= DateTime.UtcNow)
            {
                return;
            }

            Result<string> url = await s3Service.GetPresignedUrlAsync(
                $"conversations/{c.Id}/image/{c.Image.Key}", HttpVerb.GET);
            if (url.Success)
            {
                c.Image = c.Image with
                {
                    Url = url.Data!,
                    ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(s3Settings.Value.DefaultUrlExpiry)
                };
            }
        }

        // Refresh Participants profile images
        await Task.WhenAll(c.Participants.Select(RefreshProfileImage));

        await applicationDbContext.SaveChangesAsync();

        // Refresh Creator profile image
        if (c.Creator is not null)
        {
            await RefreshProfileImage(c.Creator);
        }
    }

    private async Task RefreshProfileImage(ApplicationUser user)
    {
        if (user.ProfileImage?.Key is null)
        {
            return;
        }

        if (user.ProfileImage.ExpireDateTimeUtc < DateTime.UtcNow || string.IsNullOrEmpty(user.ProfileImage.Url))
        {
            Result<string> url = await s3Service.GetPresignedUrlAsync(
                $"users/{user.Id}/profile-pictures/{user.ProfileImage.Key}", HttpVerb.GET);

            if (url.Success)
            {
                user.ProfileImage = user.ProfileImage with
                {
                    Url = url.Data!,
                    ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(s3Settings.Value.DefaultUrlExpiry)
                };
            }
        }
    }
}
