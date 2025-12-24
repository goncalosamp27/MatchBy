using Amazon.S3;
using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.ChatMessage;
using MatchBy.Repositories.User;
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
    IUserRepository userRepository,
    IChatMessageRepository chatMessageRepository,
    IConversationRepository conversationRepository,
    IImageRefreshService imageRefreshService) : IConversationService
{
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
        CursorPaginationResponse<List<Conversation>> result =
            await conversationRepository.GetConversationsForUserAsync(creatorUserId, pageSize, cursor, query, dbContext,
                ct);
        foreach (Conversation conv in result.Data)
        {
            await imageRefreshService.RefreshConversationImagesAsync(conv);
        }

        await dbContext.SaveChangesAsync(ct);

        var dtos = result.Data.Select(c => c.ToDto()).ToList();

        return Result<CursorPaginationResponse<List<ConversationDto>>>.Ok(
            new CursorPaginationResponse<List<ConversationDto>>()
            {
                Data = dtos,
                NextCursor = result.NextCursor
            });
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
        Conversation? conversation =
            await conversationRepository.GetByIdAsync(conversationId, creatorUserId, dbContext, ct);
        if (conversation is null)
        {
            return Result<ConversationDto>.Fail("No conversation found");
        }

        await imageRefreshService.RefreshConversationImagesAsync(conversation);
        await dbContext.SaveChangesAsync(ct);

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

        if (createConversationDto.ConversationType == ConversationType.Private)
        {
            string? existsId =
                await conversationRepository.PrivateConversationExistsAsync(createConversationDto.ParticipantIds,
                    dbContext, ct);
            if (!string.IsNullOrEmpty(existsId))
            {
                return Result<ConversationDto>.Fail("Private conversation between these users already exists.");
            }
        }

        Conversation conversation = createConversationDto.ToEntity();

        List<string> participantIds = createConversationDto.ParticipantIds.Contains(conversation.CreatorId)
            ? createConversationDto.ParticipantIds
            : createConversationDto.ParticipantIds.Append(conversation.CreatorId).ToList();

        List<ApplicationUser> participants = await userRepository.GetUsersByIdsAsync(participantIds, dbContext, ct);
        conversation.Participants = participants;

        conversationRepository.Add(conversation, dbContext);
        await dbContext.SaveChangesAsync(ct);

        Conversation? created =
            await conversationRepository.GetByIdAsync(conversation.Id, createConversationDto.CreatorUserId, dbContext,
                ct);

        if (created is null)
        {
            return Result<ConversationDto>.Fail("Failed to load created conversation.");
        }

        return await GetConversationByIdAsync(conversation.Id, conversation.CreatorId, ct);
    }

    public async Task<Result<string>> PrivateConversationExists(List<string> participantIds,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        string? existsId = await conversationRepository.PrivateConversationExistsAsync(participantIds, dbContext, ct);
        return !string.IsNullOrEmpty(existsId)
            ? Result<string>.Ok(existsId)
            : Result<string>.Fail("No private conversation exists between these users.");
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
        Conversation? convo = await conversationRepository.GetByIdAsync(updateConversationDto.ConversationId,
            updateConversationDto.CreatorUserId, dbContext, ct);
        if (convo is null)
        {
            return Result<ConversationDto>.Fail("Conversation not found or user is not the creator.");
        }

        List<ApplicationUser> participants =
            await userRepository.GetUsersByIdsAsync(updateConversationDto.ParticipantIds, dbContext, ct);

        convo.Title = updateConversationDto.Title;
        convo.Participants = participants;
        convo.UpdatedAtUtc = DateTime.UtcNow;

        if (updateConversationDto.File is not null)
        {
            return await UpdateConversationImageAsync(convo, updateConversationDto.CreatorUserId,
                updateConversationDto.File, dbContext, ct);
        }

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
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Conversation? convo = await conversationRepository.GetByIdAsync(conversationId, userId, dbContext, ct);
        if (convo is null)
        {
            return Result<bool>.Fail("Conversation not found.");
        }

        bool canDelete =
            await conversationRepository.CanDeleteConversation(conversationId, convo.Type, userId, dbContext, ct);
        if (!canDelete)
        {
            return Result<bool>.Fail("User does not have permission to delete this conversation.");
        }

        conversationRepository.Remove(convo, dbContext);
        foreach (ChatMessage msg in convo.Messages)
        {
            chatMessageRepository.Remove(msg, dbContext);
        }

        int affected = await dbContext.SaveChangesAsync(ct);

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
        Conversation? convo = await conversationRepository.GetByIdAsync(conversationId, userId, dbContext, ct);
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
            conversationRepository.Remove(convo, dbContext);
            foreach (ChatMessage msg in convo.Messages)
            {
                chatMessageRepository.Remove(msg, dbContext);
            }
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
        Result<string> uploadedKey =
            await s3Service.UploadBrowserFileAsync(file, $"conversations/{conversation.Id}/image");
        if (!uploadedKey.Success)
        {
            return Result<ConversationDto>.Fail(uploadedKey.ErrorMessages.ToArray());
        }

        // URL presign
        Result<string> url =
            await s3Service.GetPresignedUrlAsync($"conversations/{conversation.Id}/image/{uploadedKey.Data}",
                HttpVerb.GET);
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
            Url: url.Data,
            ExpireDateTimeUtc: DateTime.UtcNow.AddMinutes(30),
            Key: uploadedKey.Data,
            FileCategory: FileCategory.ConversationImage,
            FileType: FileType.Image,
            CreatedAtUtc: DateTime.UtcNow
        );
        conversation.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return await GetConversationByIdAsync(conversation.Id, userId, ct);
    }
}