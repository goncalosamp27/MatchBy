using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.ChatMessage;
using MatchBy.Repositories.User;
using Microsoft.EntityFrameworkCore;
using ChatMessage = MatchBy.Models.ChatMessage;

namespace MatchBy.Services.ChatMessages;

public class ChatMessageService(
    IConversationRepository conversationRepository,
    IUserRepository userRepository,
    IChatMessageRepository chatMessageRepository,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
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
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        Conversation? conversation = await conversationRepository.GetByIdAsync(conversationId, userId, dbContext, ct);
        if(conversation is null)
        {
            return Result<CursorPaginationResponse<List<ChatMessageDto>>>.Fail(
                "Conversation not found or user is not a participant.");
        }
        
        bool isUserInvolved = conversation.Participants.Any(p => p.Id == userId);
        if (!isUserInvolved)
        {
            return Result<CursorPaginationResponse<List<ChatMessageDto>>>.Fail(
                "User is not a participant in the conversation.");
        }
        
        CursorPaginationResponse<List<ChatMessage>> messages = await chatMessageRepository.GetChatMessagesAsync(conversationId, userId, dbContext, pageSize, cursor, ct);

        var chatMessages = messages.Data.Select(m => m.ToDto()).ToList();
    
        return Result<CursorPaginationResponse<List<ChatMessageDto>>>.Ok(
            new CursorPaginationResponse<List<ChatMessageDto>>
            {
                Data = chatMessages,
                NextCursor = messages.NextCursor
            });
    }

    public async Task<Result<ChatMessageDto>> GetChatMessageByIdAsync(string chatMessageId, string userId,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        ChatMessage? chatMessage = await chatMessageRepository.GetByIdAsync(chatMessageId, dbContext, ct);
        if (chatMessage is null)
        {
            return Result<ChatMessageDto>.Fail("Chat message not found.");
        }
        
        Conversation? conversation = await conversationRepository.GetByIdAsync(chatMessage.ConversationId, userId, dbContext, ct);
        if (conversation is null)
        {
            return Result<ChatMessageDto>.Fail("Conversation not found.");
        }

        bool isUserInvolved = conversation.Participants.Any(p => p.Id == userId);
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

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        ApplicationUser? sender = await userRepository.GetByIdAsync(createChatMessageDto.CreatorUserId, dbContext, ct);
        if (sender is null)
        {
            return Result<ChatMessageDto>.Fail("Sender user not found.");
        }

        Conversation? conversation = await conversationRepository.GetByIdAsync(createChatMessageDto.ConversationId, createChatMessageDto.CreatorUserId, dbContext, ct);
        if (conversation is null)
        {
            return Result<ChatMessageDto>.Fail("Conversation not found or user is not a participant.");
        }

        conversation.LastMessageContent = createChatMessageDto.Content;
        conversation.LastMessageAtUtc = DateTime.UtcNow;
        ChatMessage chatMessage = createChatMessageDto.ToEntity();

        chatMessageRepository.Add(chatMessage, dbContext);
        await dbContext.SaveChangesAsync(ct);

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
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        ApplicationUser? sender = await userRepository.GetByIdAsync(updateChatMessageDto.CreatorUserId, dbContext, ct);
        if (sender is null)
        {
            return Result<ChatMessageDto>.Fail("Sender user not found.");
        }

        ChatMessage? chatMessage = await chatMessageRepository.GetByIdAsync(updateChatMessageDto.ChatMessageId, dbContext, ct);
        if (chatMessage is null)
        {
            return Result<ChatMessageDto>.Fail("Chat message not found or user is not the sender.");
        }

        Conversation? conversation = await conversationRepository.GetByIdAsync(chatMessage.ConversationId, updateChatMessageDto.CreatorUserId, dbContext, ct);
        if (conversation is null)
        {
            return Result<ChatMessageDto>.Fail("Conversation not found.");
        }

        conversation.LastMessageAtUtc = DateTime.UtcNow;
        chatMessage.Content = updateChatMessageDto.Content;
        chatMessage.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Result<ChatMessageDto>.Ok(chatMessage.ToDto());
    }

    public async Task<Result<bool>> DeleteChatMessageAsync(string chatMessageId, string userId,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        ChatMessage? chatMessage = await chatMessageRepository.GetByIdAsync(chatMessageId, dbContext, ct);
        if (chatMessage is null)
        {
            return Result<bool>.Fail("Chat message not found or user is not the sender.");
        }

        Conversation? conversation = await conversationRepository.GetByIdAsync(chatMessage.ConversationId, userId, dbContext, ct);
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
        ChatMessage? chatMessageToDelete = await chatMessageRepository.GetByIdAsync(chatMessageId, dbContext, ct);
        if (chatMessageToDelete is null || chatMessageToDelete.SenderId != userId)
        {
            return Result<bool>.Fail("User is not authorized to delete this message.");
        }

        chatMessageRepository.Remove(chatMessage, dbContext);
        
        List<ChatMessage> messages = [.. conversation.Messages.Where(m => m.ReplyToMessageId == chatMessageId)];
        foreach (ChatMessage msg in messages)
        {
            msg.ReplyToMessageId = null;
            msg.ReplyToMessage = null;
        }
        
        int affected = await dbContext.SaveChangesAsync(ct);
        return affected > 0 ? Result<bool>.Ok(true) : Result<bool>.Fail("Failed to delete chat message.");
    }
}
