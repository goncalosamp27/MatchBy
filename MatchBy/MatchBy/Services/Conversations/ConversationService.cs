using Amazon.S3;
using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.S3;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.Conversations;

public class ConversationService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IS3Service s3Service,
    IValidator<CreateConversationDto> createConversationValidator,
    IValidator<UpdateConversationDto> updateConversationValidator,
    IImageRefreshService imageRefreshService) : IConversationService
{
    /// <summary>
    /// Paginates conversations using cursor-based pagination with optional search filtering.
    /// </summary>
    /// <param name="baseQuery">The base queryable collection of conversations to paginate.</param>
    /// <param name="pageSize">The number of conversations to retrieve per page.</param>
    /// <param name="cursorReceived">Optional cursor for pagination. If provided, returns conversations before this cursor.</param>
    /// <param name="query">Optional search query to filter conversations by title or participant names (case-insensitive).</param>
    /// <param name="dbContext">The database context to save image refresh changes.</param>
    /// <returns>
    /// A cursor-paginated response with a list of conversation DTOs, ordered by last message date or creation date (newest first).
    /// </returns>
    /// <remarks>
    /// Conversations are ordered by last message date (if available) or creation date, then by ID.
    /// Images are refreshed in parallel for all conversations before returning.
    /// </remarks>
    private async Task<CursorPaginationResponse<List<ConversationDto>>> PaginateAsync(
        IQueryable<Conversation> baseQuery,
        int pageSize,
        string? cursorReceived,
        string? query,
        ApplicationDbContext dbContext)
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
            .OrderByDescending(p => p.LastMessageAtUtc != null)
            .ThenByDescending(p => p.LastMessageAtUtc ?? p.CreatedAtUtc) 
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
            await imageRefreshService.RefreshConversationImagesAsync(conv);
        }

        await dbContext.SaveChangesAsync();

        var conversationDtos = items.Select(conversation => conversation.ToDto()).ToList();
        
        return new CursorPaginationResponse<List<ConversationDto>>
        {
            Data = conversationDtos,
            NextCursor = nextCursor
        };
    }


    /// <summary>
    /// Retrieves a paginated list of conversations for a specific user using cursor-based pagination.
    /// </summary>
    /// <param name="creatorUserId">The unique identifier of the user to get conversations for.</param>
    /// <param name="pageSize">The number of conversations to retrieve per page.</param>
    /// <param name="cursor">Optional cursor for pagination. If provided, returns conversations before this cursor.</param>
    /// <param name="query">Optional search query to filter conversations by title or participant names (case-insensitive).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a cursor-paginated response with a list of conversation DTOs that the user participates in.
    /// </returns>
    public async Task<Result<CursorPaginationResponse<List<ConversationDto>>>> GetConversationsAsync(
        string creatorUserId,
        int pageSize,
        string? cursor,
        string? query,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        IQueryable<Conversation> baseQuery = dbContext.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Creator)
            .Include(c => c.Messages)
            .Include(m => m.Team)
            .Include(m => m.Messages)
            .Where(c => c.Participants.Any(p => p.Id == creatorUserId));

        return Result<CursorPaginationResponse<List<ConversationDto>>>.Ok(await PaginateAsync(baseQuery, pageSize,
            cursor, query, dbContext));
    }

    /// <summary>
    /// Retrieves a specific conversation by its unique identifier.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to retrieve.</param>
    /// <param name="creatorUserId">The unique identifier of the user requesting the conversation.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the conversation DTO if found and the user is a participant, or an error message if not found.
    /// </returns>
    /// <remarks>
    /// For private conversations, the title is automatically set to the other participant's display name.
    /// Images are refreshed before returning the conversation.
    /// </remarks>
    public async Task<Result<ConversationDto>> GetConversationByIdAsync(string conversationId, string creatorUserId,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        Conversation? conversation = await dbContext.Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .Include(m => m.Team)
            .Include(m => m.Messages)
            .Where(m => m.Id ==conversationId)
            .Where(m => m.Participants.Any(p => p.Id == creatorUserId))
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
        {
            return Result<ConversationDto>.Fail("No conversation found");
        }
        
        await imageRefreshService.RefreshConversationImagesAsync(conversation);

        // For private conversations, set the title to the other participant's name
        if (conversation.Type == ConversationType.Private)
        {
            conversation.Title = conversation.Participants.FirstOrDefault(p => p.Id != creatorUserId)?.DisplayName;
        }
        
        return Result<ConversationDto>.Ok(conversation.ToDto());
    }


    /// <summary>
    /// Creates a new conversation with the specified participants.
    /// </summary>
    /// <param name="createConversationDto">DTO containing conversation creation details (type, title, participants, team).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the created conversation DTO if successful, or an error message if:
    /// - Validation fails
    /// - A private conversation already exists between the same participants
    /// </returns>
    /// <remarks>
    /// For private conversations, this method prevents duplicate conversations between the same participants.
    /// The creator is automatically added as a participant.
    /// </remarks>
    public async Task<Result<ConversationDto>> CreateConversationAsync(CreateConversationDto createConversationDto,
        CancellationToken ct = default)
    {
        ValidationResult? validationResult = await createConversationValidator.ValidateAsync(createConversationDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<ConversationDto>.Fail(validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
        }
        
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Conversation conversation = createConversationDto.ToEntity();

        //if already a private conversation between these users, return null
        if (createConversationDto.ConversationType == ConversationType.Private)
        {
            bool exists = await dbContext.Conversations
                .Include(c => c.Participants)
                .Where(c => c.Type == ConversationType.Private)
                .AnyAsync(c => c.Participants.Count == createConversationDto.ParticipantIds.Count &&
                               c.Participants.All(p => createConversationDto.ParticipantIds.Contains(p.Id)), ct);
            if (exists)
            {
                return Result<ConversationDto>.Fail("Private conversation between these users already exists.");
            }
        }

        List<ApplicationUser> participants = await dbContext.Users
            .Where(u => createConversationDto.ParticipantIds.Contains(u.Id))
            .ToListAsync(ct);

        conversation.Participants = participants;

        await dbContext.Conversations.AddAsync(conversation, ct);
        await dbContext.SaveChangesAsync(ct);

        return await GetConversationByIdAsync(conversation.Id, conversation.CreatorId, ct);
    }

    /// <summary>
    /// Updates an existing conversation's details. Only the conversation creator can update it.
    /// </summary>
    /// <param name="updateConversationDto">DTO containing the conversation update details (id, title, participants, image).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the updated conversation DTO if successful, or an error message if:
    /// - Validation fails
    /// - Conversation not found or user is not the creator
    /// - Failed to update conversation image (if provided)
    /// </returns>
    /// <remarks>
    /// This method updates the conversation title and participants. If an image is provided, it is uploaded to S3 storage.
    /// </remarks>
    public async Task<Result<ConversationDto>> UpdateConversationAsync(UpdateConversationDto updateConversationDto,
        CancellationToken ct = default)
    {
        ValidationResult? validationResult = await updateConversationValidator.ValidateAsync(updateConversationDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<ConversationDto>.Fail(validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
        }
        
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        // only the creator can update participants
        Conversation? convo = await dbContext.Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .Include(m => m.Team)
            .Include(m => m.Messages)
            .Where(c => c.Id == updateConversationDto.ConversationId)
            .Where(c => c.CreatorId == updateConversationDto.CreatorUserId)
            .FirstOrDefaultAsync(ct);
        if (convo is null)
        {
            return Result<ConversationDto>.Fail("Conversation not found or user is not the creator.");
        }

        List<ApplicationUser> participants = await dbContext.Users
            .Where(u => updateConversationDto.ParticipantIds.Contains(u.Id))
            .ToListAsync(ct);
        
        convo.Title = updateConversationDto.Title;
        convo.Participants = participants;
        convo.UpdatedAtUtc = DateTime.UtcNow;

        if (updateConversationDto.File is not null)
        {
            return await UpdateConversationImageAsync(convo, updateConversationDto.CreatorUserId,
                updateConversationDto.File, dbContext, ct);
        }

        //inside updateConversationImageAsync we already save changes
        //so we only need to save changes here if no image update
        await dbContext.SaveChangesAsync(ct);
        return await GetConversationByIdAsync(convo.Id, updateConversationDto.CreatorUserId, ct);
    }

    /// <summary>
    /// Soft deletes a conversation. For private conversations, any participant can delete. For other types, only the creator can delete.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to delete.</param>
    /// <param name="userId">The unique identifier of the user attempting to delete the conversation.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the conversation was successfully soft deleted, or an error message if:
    /// - Conversation not found
    /// - User does not have permission to delete the conversation
    /// </returns>
    public async Task<Result<bool>> DeleteConversationAsync(string conversationId, string userId,
        CancellationToken ct = default)
    {await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        Conversation? convo = await dbContext.Conversations
            .Include(m => m.Participants)
            .Where(c => c.Id == conversationId)
            .FirstOrDefaultAsync(ct);

        if (convo is null)
        {
            return Result<bool>.Fail("Conversation not found.");
        }

        bool canDelete = convo.Type == ConversationType.Private
            ? await dbContext.Conversations
                .AnyAsync(c => c.Id == conversationId && c.Participants.Any(p => p.Id == userId), ct)
            : await dbContext.Conversations
                .AnyAsync(c => c.Id == conversationId && c.CreatorId == userId, ct);

        if (!canDelete)
        {
            return Result<bool>.Fail("User does not have permission to delete this conversation.");
        }

        int affected = await dbContext.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);

        return Result<bool>.Ok(affected > 0);
    }

    /// <summary>
    /// Removes a user from a conversation. If only one participant remains, the conversation is soft deleted.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to leave.</param>
    /// <param name="userId">The unique identifier of the user leaving the conversation.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing an integer indicating the operation result:
    /// - 1: Conversation was soft deleted (only one participant remained)
    /// - 2: User was removed from the conversation
    /// Or an error message if:
    /// - Conversation not found
    /// - User is not a participant of the conversation
    /// </returns>
    public async Task<Result<int>> LeaveConversationAsync(string conversationId, string userId,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        Conversation? convo = await dbContext.Conversations
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

        await dbContext.SaveChangesAsync(ct);

        return Result<int>.Ok(mustSoftDelete ? 1 : 2);
    }

    /// <summary>
    /// Updates the conversation's image by uploading it to S3 storage and generating a presigned URL.
    /// </summary>
    /// <param name="conversation">The conversation entity to update the image for.</param>
    /// <param name="userId">The unique identifier of the user performing the update.</param>
    /// <param name="file">The browser file containing the image to upload.</param>
    /// <param name="dbContext">The database context to save changes to.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the updated conversation DTO if successful, or an error message if:
    /// - Failed to upload the image to S3
    /// - Failed to generate presigned URL
    /// </returns>
    /// <remarks>
    /// This method uploads the image, generates a presigned URL (valid for 30 minutes), deletes the previous image
    /// if it exists, and updates the conversation's image metadata in the database.
    /// </remarks>
    private async Task<Result<ConversationDto>> UpdateConversationImageAsync(
        Conversation conversation,
        string userId,
        IBrowserFile file,
        ApplicationDbContext dbContext,
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

        await dbContext.SaveChangesAsync(ct);

        return await GetConversationByIdAsync(conversation.Id, userId, ct);
    }


}
