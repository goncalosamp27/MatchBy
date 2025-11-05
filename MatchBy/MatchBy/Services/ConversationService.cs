using Amazon.S3;
using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services;

public class ConversationService(ApplicationDbContext applicationDbContext, IFileValidator fileValidator, IS3Service s3Service) : IConversationService
{
    public async Task<List<Conversation>> GetConversationsAsync(string creatorUserId, CancellationToken ct = default)
    {
        List<Conversation> conversations = await applicationDbContext
            .Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .ThenInclude(c => c.Sender)
            .Where(m => m.CreatorId == creatorUserId)
            .Where(m => m.Participants.Any(p => p.Id == creatorUserId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (Conversation conversation in conversations.Where(conversation => conversation.Type == ConversationType.Private))
        {
            conversation.Title = conversation.Participants.FirstOrDefault(p => !p.Id.Equals(creatorUserId))?.DisplayName;
        }
        
        return conversations;
    }

    public async Task<Conversation?> GetConversationByIdAsync(string conversationId, string creatorUserId,
        CancellationToken ct = default)
    {
        Conversation? conversation = await applicationDbContext.Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .ThenInclude(c => c.Sender)
            .Where(m => m.Id.Equals(conversationId))
            .Where(m => m.CreatorId == creatorUserId)
            .Where(m => m.Participants.Any(p => p.Id == creatorUserId))
            .FirstOrDefaultAsync(ct);

        if (conversation != null)
        {
            conversation.Title = conversation.Participants.FirstOrDefault(p => !p.Id.Equals(creatorUserId))?.DisplayName;
        }
        
        return conversation;
    }


    public async Task<Conversation?> CreateConversationAsync(string creatorUserId, ConversationType conversationType, List<string> participantIds,
        string? title, string? teamId, string? matchId, CancellationToken ct = default)
    {
        var conversation = new Conversation
        {
            Id = $"conversation_{Guid.CreateVersion7()}",
            Type = conversationType,
            Title = title,
            Image = null, // could be set later
            CreatorId = creatorUserId,
            CreatedAtUtc = DateTime.UtcNow,
            TeamId = teamId,
            MatchId = matchId,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        List<ApplicationUser> participants = await applicationDbContext.Users
            .Where(u => participantIds.Contains(u.Id))
            .ToListAsync(ct);
        
        conversation.Participants = participants;

        await applicationDbContext.Conversations.AddAsync(conversation, ct);
        await applicationDbContext.SaveChangesAsync(ct);
        
        return await GetConversationByIdAsync(conversation.Id, creatorUserId, ct);
    }

    public async Task<Conversation?> UpdateConversationAsync(string conversationId, string creatorUserId, List<string> participantIds,
        CancellationToken ct = default)
    {
        // only the creator can update participants
        Conversation? convo = await applicationDbContext.Conversations
            .Where(c => c.Id == conversationId)
            .FirstOrDefaultAsync(ct);

        if (convo is null || convo.CreatorId != creatorUserId)
        {
            return null;
        }
        
        convo.UpdatedAtUtc = DateTime.UtcNow;
        
        List<ApplicationUser> participants = await applicationDbContext.Users
            .Where(u => participantIds.Contains(u.Id))
            .ToListAsync(ct);
        
        convo.Participants = participants;

        await applicationDbContext.SaveChangesAsync(ct);
        
        return await GetConversationByIdAsync(convo.Id, creatorUserId, ct);
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
    
    public async Task<Conversation?> UpdateConversationImageAsync(
        string conversationId,
        string userId,
        IBrowserFile file,
        CancellationToken ct = default)
    {
        if (!fileValidator.IsValidBrowserImage(file))
        {
            return null;
        }

        Conversation? convo = await applicationDbContext.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);
        if (convo is null)
        {
            return null;
        }
        
        bool canUpdate = convo.CreatorId == userId;
        if (!canUpdate)
        {
            return null;
        }
        
        // upload
        string? uploadedKey = await s3Service.UploadBrowserFileAsync(file, $"conversations/{convo.Id}/image");
        if (uploadedKey is null)
        {
            return null;
        }

        // URL presign
        string? url = await s3Service.GetPresignedUrlAsync($"conversations/{convo.Id}/image/{uploadedKey}", HttpVerb.GET);
        if (url is null)
        {
            return null;
        }

        // delete previous, if it exists
        string? oldKey = convo.Image?.Key;
        if (!string.IsNullOrWhiteSpace(oldKey) && !oldKey.Equals(uploadedKey, StringComparison.OrdinalIgnoreCase))
        {
            await s3Service.DeleteFileAsync($"conversations/{convo.Id}/image/{oldKey}");
        }

        // store the image info
        convo.Image = new FileStore(
            Url: url,
            ExpireDateTimeUtc: DateTime.UtcNow.AddMinutes(30),
            Key: uploadedKey,
            FileCategory: FileCategory.ConversationImage,
            FileType: FileType.Image,
            CreatedAtUtc: DateTime.UtcNow
        );
        convo.UpdatedAtUtc = DateTime.UtcNow;

        await applicationDbContext.SaveChangesAsync(ct);
        
        return await GetConversationByIdAsync(convo.Id, userId, ct);
    }
}
