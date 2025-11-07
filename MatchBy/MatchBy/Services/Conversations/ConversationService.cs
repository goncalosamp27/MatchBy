using Amazon.S3;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.FileValidator;
using MatchBy.Services.S3;
using MatchBy.Settings;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatchBy.Services.Conversations;

public class ConversationService(ApplicationDbContext applicationDbContext, IS3Service s3Service, IOptions<S3Settings> s3Settings) : IConversationService
{
    public async Task<List<ConversationDto>> GetConversationsAsync(string creatorUserId, CancellationToken ct = default)
    {
        List<Conversation> conversations = await applicationDbContext
            .Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .Where(m => m.CreatorId == creatorUserId)
            .Where(m => m.Participants.Any(p => p.Id == creatorUserId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (Conversation conversation in conversations.Where(conversation => conversation.Type == ConversationType.Private))
        {
            conversation.Title = conversation.Participants.FirstOrDefault(p => !p.Id.Equals(creatorUserId))?.DisplayName;
        }
        
        await Task.WhenAll(conversations.Select(RefreshConversationImagesAsync));
        return [.. conversations.Select(c => c.ToDto())];
    }

    public async Task<ConversationDto?> GetConversationByIdAsync(string conversationId, string creatorUserId,
        CancellationToken ct = default)
    {
        Conversation? conversation = await applicationDbContext.Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .Where(m => m.Id.Equals(conversationId))
            .Where(m => m.CreatorId == creatorUserId)
            .Where(m => m.Participants.Any(p => p.Id == creatorUserId))
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
        {
            return null;
        }
        
        conversation.Title = conversation.Participants.FirstOrDefault(p => !p.Id.Equals(creatorUserId))?.DisplayName;
        await RefreshConversationImagesAsync(conversation);
        
        return conversation.ToDto();
    }


    public async Task<ConversationDto?> CreateConversationAsync(CreateConversationDto createConversationDto, CancellationToken ct = default)
    {
        Conversation conversation = createConversationDto.ToEntity();

        List<ApplicationUser> participants = await applicationDbContext.Users
            .Where(u => createConversationDto.ParticipantIds.Contains(u.Id))
            .ToListAsync(ct);
        
        conversation.Participants = participants;

        await applicationDbContext.Conversations.AddAsync(conversation, ct);
        await applicationDbContext.SaveChangesAsync(ct);
        
        return await GetConversationByIdAsync(conversation.Id, conversation.CreatorId, ct);
    }

    public async Task<ConversationDto?> UpdateConversationAsync(UpdateConversationDto updateConversationDto,
        CancellationToken ct = default)
    {
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
            return null;
        }
        
        List<ApplicationUser> participants = await applicationDbContext.Users
            .Where(u => updateConversationDto.ParticipantIds.Contains(u.Id))
            .ToListAsync(ct);
        convo.Participants = participants;
        convo.UpdatedAtUtc = DateTime.UtcNow;
        
        if (updateConversationDto.File is not null)
        {
            return await UpdateConversationImageAsync(convo, updateConversationDto.CreatorUserId,updateConversationDto.File , ct);   
        }
        
        //inside updateConversationImageAsync we already save changes
        //so we only need to save changes here if no image update
        await applicationDbContext.SaveChangesAsync(ct);
        return await GetConversationByIdAsync(convo.Id, updateConversationDto.CreatorUserId, ct);
    }

    public async Task<bool> DeleteConversationAsync(string conversationId, string userId,
        CancellationToken ct = default)
    {
        Conversation? convo = await applicationDbContext.Conversations
            .Include(m => m.Participants)
            .Where(c => c.Id == conversationId)
            .FirstOrDefaultAsync(ct);
        
        if (convo is null)
        {
            return false;
        }

        bool canDelete = convo.Type == ConversationType.Private
            ? await applicationDbContext.Conversations
                .AnyAsync(c => c.Id == conversationId && c.Participants.Any(p => p.Id == userId), ct)
            : await applicationDbContext.Conversations
                .AnyAsync(c => c.Id == conversationId && c.CreatorId == userId, ct);

        if (!canDelete)
        {
            return false;
        }

        int affected = await applicationDbContext.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);

        return affected == 1;
    }
    
    public async Task<bool> LeaveConversationAsync(string conversationId, string userId, CancellationToken ct = default)
    {
        Conversation? convo = await applicationDbContext.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (convo is null)
        {
            return false;
        }
        
        ApplicationUser? me = convo.Participants.FirstOrDefault(p => p.Id == userId);
        if (me is null)
        {
            return false;
        }
        
        convo.Participants.Remove(me);
        convo.UpdatedAtUtc = DateTime.UtcNow;

        // Se não ficou ninguém (ou conversa privada com menos de 2), faz soft-delete
        int remaining = convo.Participants.Count;

        bool mustSoftDelete = remaining == 0;

        if (mustSoftDelete)
        {
            convo.DeletedAtUtc = DateTime.UtcNow;
        }
        
        await applicationDbContext.SaveChangesAsync(ct);
        return true;
    }
    
    private async Task<ConversationDto?> UpdateConversationImageAsync(
        Conversation conversation,
        string userId,
        IBrowserFile file,
        CancellationToken ct = default)
    {

        // upload
        string? uploadedKey = await s3Service.UploadBrowserFileAsync(file, $"conversations/{conversation.Id}/image");
        if (uploadedKey is null)
        {
            return null;
        }

        // URL presign
        string? url = await s3Service.GetPresignedUrlAsync($"conversations/{conversation.Id}/image/{uploadedKey}", HttpVerb.GET);
        if (url is null)
        {
            return null;
        }

        // delete previous, if it exists
        string? oldKey = conversation.Image?.Key;
        if (!string.IsNullOrWhiteSpace(oldKey) && !oldKey.Equals(uploadedKey, StringComparison.OrdinalIgnoreCase))
        {
            await s3Service.DeleteFileAsync($"conversations/{conversation.Id}/image/{oldKey}");
        }

        // store the image info
        conversation.Image = new FileStore(
            Url: url,
            ExpireDateTimeUtc: DateTime.UtcNow.AddMinutes(30),
            Key: uploadedKey,
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
            
            string? url = await s3Service.GetPresignedUrlAsync(
                $"conversations/{c.Id}/image/{c.Image.Key}", HttpVerb.GET);
            if (!string.IsNullOrEmpty(url))
            {
                c.Image = c.Image with
                {
                    Url = url,
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
        
        await applicationDbContext.SaveChangesAsync();
    }

    private async Task RefreshProfileImage(ApplicationUser user)
    {
        if (user.ProfileImage?.Key is null)
        {
            return;
        }

        if (user.ProfileImage.ExpireDateTimeUtc < DateTime.UtcNow || string.IsNullOrEmpty(user.ProfileImage.Url))
        {
            string? url = await s3Service.GetPresignedUrlAsync(
                $"users/{user.Id}/profile-pictures/{user.ProfileImage.Key}", HttpVerb.GET);

            if (!string.IsNullOrEmpty(url))
            {
                user.ProfileImage = user.ProfileImage with
                {
                    Url = url,
                    ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(s3Settings.Value.DefaultUrlExpiry)
                };
            }
        }
    }
}
