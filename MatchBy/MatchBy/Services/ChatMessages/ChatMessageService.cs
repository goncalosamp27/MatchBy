using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using ChatMessage = MatchBy.Models.ChatMessage;

namespace MatchBy.Services.ChatMessages;

public class ChatMessageService(
    ApplicationDbContext applicationDbContext,
    IValidator<CreateChatMessageDto> createChatMessageValidator,
    IValidator<UpdateChatMessageDto> updateChatMessageValidator) : IChatMessageService
{
    public async Task<Result<CursorPaginationResponse<List<ChatMessageDto>>>> GetChatMessagesAsync(
        string conversationId,
        string userId,
        int pageSize,
        string? cursor,
        CancellationToken ct = default)
    {
        bool isUserInvolved = await applicationDbContext.Conversations
            .Where(c => c.Id == conversationId)
            .Where(c => c.Participants.Any(p => p.Id == userId))
            .AnyAsync(ct);

        if (!isUserInvolved)
        {
            return Result<CursorPaginationResponse<List<ChatMessageDto>>>.Fail(
                "User is not a participant in the conversation.");
        }

        IQueryable<ChatMessage> query = applicationDbContext
            .ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
            .ThenInclude(r => r!.Sender)
            .Include(m => m.Conversation)
            .Where(m => m.ConversationId == conversationId)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(cursor))
        {
            query = query.Where(m => m.Id.CompareTo(cursor) < 0);
        }

        List<ChatMessage> messages = await query
            .OrderByDescending(m => m.Id)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        bool hasNextPage = messages.Count > pageSize;

        if (hasNextPage)
        {
            messages.RemoveAt(messages.Count - 1);
        }
        
        messages.Reverse();

        var chatMessages = messages.Select(m => m.ToDto()).ToList();

        string? nextCursor = hasNextPage && chatMessages.Any()
            ? chatMessages[0].Id
            : null;
    
        return Result<CursorPaginationResponse<List<ChatMessageDto>>>.Ok(
            new CursorPaginationResponse<List<ChatMessageDto>>
            {
                Data = chatMessages,
                NextCursor = nextCursor
            });
    }

    public async Task<Result<ChatMessageDto>> GetChatMessageByIdAsync(string chatMessageId, string userId,
        CancellationToken ct = default)
    {
        ChatMessage? chatMessage = await applicationDbContext.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
            .ThenInclude(r => r!.Sender)
            .Include(m => m.Conversation)
            .Where(m => m.Id.Equals(chatMessageId))
            .FirstOrDefaultAsync(ct);

        if (chatMessage is null)
        {
            return Result<ChatMessageDto>.Fail("Chat message not found.");
        }

        bool isUserInvolved = await applicationDbContext.Conversations
            .Where(c => c.Id == chatMessage.ConversationId)
            .Where(c => c.Participants.Any(p => p.Id == userId))
            .AnyAsync(ct);

        return !isUserInvolved
            ? Result<ChatMessageDto>.Fail("User is not a participant in the conversation.")
            : Result<ChatMessageDto>.Ok(chatMessage.ToDto());
    }

    public async Task<Result<ChatMessageDto>> CreateChatMessageAsync(CreateChatMessageDto createChatMessageDto,
        CancellationToken ct = default)
    {
        ValidationResult? validationResult = await createChatMessageValidator.ValidateAsync(createChatMessageDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<ChatMessageDto>.Fail(validationResult.ToString());
        }

        ApplicationUser? sender = await applicationDbContext.Users
            .Where(u => u.Id == createChatMessageDto.CreatorUserId)
            .FirstOrDefaultAsync(ct);
        if (sender is null)
        {
            return Result<ChatMessageDto>.Fail("Sender user not found.");
        }

        Conversation? conversation = await applicationDbContext.Conversations
            .Where(c => c.Id == createChatMessageDto.ConversationId)
            .Where(c => c.CreatorId == createChatMessageDto.CreatorUserId ||
                        c.Participants.Any(p => p.Id == createChatMessageDto.CreatorUserId))
            .FirstOrDefaultAsync(ct);
        if (conversation is null)
        {
            return Result<ChatMessageDto>.Fail("Conversation not found or user is not a participant.");
        }

        conversation.LastMessageContent = createChatMessageDto.Content;
        conversation.LastMessageAtUtc = DateTime.UtcNow;
        ChatMessage chatMessage = createChatMessageDto.ToEntity();

        await applicationDbContext.ChatMessages.AddAsync(chatMessage, ct);
        await applicationDbContext.SaveChangesAsync(ct);

        return await GetChatMessageByIdAsync(chatMessage.Id, createChatMessageDto.CreatorUserId, ct);
    }

    public async Task<Result<ChatMessageDto>> UpdateChatMessageAsync(UpdateChatMessageDto updateChatMessageDto,
        CancellationToken ct = default)
    {
        ValidationResult? validationResult = await updateChatMessageValidator.ValidateAsync(updateChatMessageDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<ChatMessageDto>.Fail(validationResult.ToString());
        }

        ApplicationUser? sender = await applicationDbContext.Users
            .Where(u => u.Id == updateChatMessageDto.CreatorUserId)
            .FirstOrDefaultAsync(ct);
        if (sender is null)
        {
            return Result<ChatMessageDto>.Fail("Sender user not found.");
        }

        ChatMessage? chatMessage = await applicationDbContext.ChatMessages
            .Where(m => m.Id == updateChatMessageDto.ChatMessageId && m.DeletedAtUtc == null)
            .Where(m => m.SenderId == updateChatMessageDto.CreatorUserId)
            .Include(m => m.ReplyToMessage)
            .Include(m => m.Conversation)
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(ct);

        if (chatMessage is null)
        {
            return Result<ChatMessageDto>.Fail("Chat message not found or user is not the sender.");
        }

        Conversation? conversation = await applicationDbContext.Conversations
            .Where(c => c.Id == chatMessage.ConversationId)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
        {
            return Result<ChatMessageDto>.Fail("Conversation not found.");
        }

        conversation.LastMessageAtUtc = DateTime.UtcNow;
        chatMessage.Content = updateChatMessageDto.Content;
        chatMessage.UpdatedAtUtc = DateTime.UtcNow;

        await applicationDbContext.SaveChangesAsync(ct);

        return Result<ChatMessageDto>.Ok(chatMessage.ToDto());
    }

    public async Task<Result<bool>> DeleteChatMessageAsync(string chatMessageId, string userId,
        CancellationToken ct = default)
    {
        ChatMessage? chatMessage = await applicationDbContext.ChatMessages
            .Where(c => c.Id == chatMessageId && c.DeletedAtUtc == null)
            .Where(c => c.SenderId == userId)
            .FirstOrDefaultAsync(ct);

        if (chatMessage is null)
        {
            return Result<bool>.Fail("Chat message not found or user is not the sender.");
        }

        Conversation? conversation = await applicationDbContext.Conversations
            .Where(c => c.Id == chatMessage.ConversationId)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
        {
            return Result<bool>.Fail("Conversation not found.");
        }

        //we have a query filter for DeletedAtUtc == null, so Messages will not include deleted messages
        conversation.LastMessageAtUtc = conversation.Messages.Count == 1
            ? null
            : conversation.Messages.Last(m => m.Id != chatMessageId).CreatedAtUtc;
        conversation.LastMessageContent = conversation.Messages.Count == 1
            ? null
            : conversation.Messages.Last(m => m.Id != chatMessageId).Content;

        // only the sender can delete their message
        bool canDelete = await applicationDbContext.ChatMessages
            .AnyAsync(c => c.Id == chatMessageId && c.SenderId == userId && c.DeletedAtUtc == null, ct);

        if (!canDelete)
        {
            return Result<bool>.Fail("User is not authorized to delete this message.");
        }

        int affected = await applicationDbContext.ChatMessages
            .Where(c => c.Id == chatMessageId && c.DeletedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);
        
        List<ChatMessage> messages = [.. conversation.Messages.Where(m => m.ReplyToMessageId == chatMessageId)];
        foreach (ChatMessage msg in messages)
        {
            msg.ReplyToMessageId = null;
            msg.ReplyToMessage = null;
        }
        
        await applicationDbContext.SaveChangesAsync(ct);

        return affected > 0 ? Result<bool>.Ok(true) : Result<bool>.Fail("Failed to delete chat message.");
    }
}
