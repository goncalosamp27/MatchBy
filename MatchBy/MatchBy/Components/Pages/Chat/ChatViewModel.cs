using System.ComponentModel;
using Blazorise;
using FluentValidation;
using FluentValidation.Results;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.DTOs.User;
using MatchBy.Hubs;
using MatchBy.Models;
using MatchBy.Services.ChatMessages;
using MatchBy.Services.Conversations;
using MatchBy.Services.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace MatchBy.Components.Pages.Chat;

public sealed class ChatViewModel(
    IConversationService conversationService,
    IChatMessageService chatMessageService,
    IUsersService usersService,
    ChatState state,
    NavigationManager navigationManager,
    IValidator<CreateChatMessageDto> createChatMessageDtoValidator,
    IValidator<UpdateChatMessageDto> updateChatMessageDtoValidator,
    IValidator<CreateConversationDto> createConversationDtoValidator,
    IValidator<UpdateConversationDto> updateConversationDtoValidator,
    IToastService toastService,
    Func<Task> onStateChanged)
    : INotifyPropertyChanged, IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? StateChanged;

    public ChatState State => state;
    private bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public string? MessageText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(MessageText));
        }
    }

    public ChatMessageDto? ReplyToMessage
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(ReplyToMessage));
        }
    }

    public PaginationResponse<List<UserDto>> AllUsers
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(AllUsers));
        }
    } = new()
    {
        Page = 0,
        TotalCount = 0,
        PageSize = 0,
        Data = []
    };

    public async Task InitializeAsync(string? conversationId, string userId)
    {
        state.Changed += OnStateChanged;
        state.InitUser(userId);

        Result<CursorPaginationResponse<List<ConversationDto>>> conversations = await conversationService.GetConversationsAsync(userId, 10, null, null, CancellationToken.None);
        if (!conversations.Success)
        {
            await toastService.Error(string.Join(", ", conversations.ErrorMessages));
        }
        else
        {
            state.AddConversations(conversations.Data!);
        }

        if (!string.IsNullOrEmpty(conversationId))
        {
            ConversationDto? selectedConversation = state.Conversations.FirstOrDefault(c => c.Id == conversationId);
            if (selectedConversation is null)
            {
                Result<ConversationDto> conversationResult = await conversationService.GetConversationByIdAsync(conversationId, userId, CancellationToken.None);
                if (conversationResult.Success)
                {
                    selectedConversation = conversationResult.Data;
                    state.UpdateConversation(selectedConversation);
                }
                else
                {
                    await toastService.Error(string.Join(", ", conversationResult.ErrorMessages));
                }
            }

            if (selectedConversation is not null)
            {
                await OnConversationSelected(selectedConversation);
            }
        }

        // Load all users except the current user
        await GetUsers(new NewConversationModal.UsersQuery("", 1, 4));
    }

    public async Task InitializeSignalRAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/hubs/chat"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<Result<ChatMessageDto>>("MessageCreated", async result =>
        {
            if (result.Success)
            {
                state.UpsertMessage(result.Data);
                await NotifyStateChangedAsync();
            }
            else
            {
                await toastService.Error(string.Join(", ", result.ErrorMessages));
            }
        });

        _hubConnection.On<Result<ChatMessageDto>>("MessageUpdated", async result =>
        {
            if (result.Success)
            {
                state.UpsertMessage(result.Data);
                await NotifyStateChangedAsync();
            }
            else
            {
                Console.WriteLine($"Failed to update message: {string.Join(", ", result.ErrorMessages)}");
                await toastService.Error(string.Join(", ", result.ErrorMessages));
            }
        });

        _hubConnection.On<Result<ChatHub.MessageDeletedDto>>("MessageDeleted", async result =>
        {
            if (result.Success)
            {
                state.RemoveMessage(result.Data.Conversation, result.Data.MessageId);
                await NotifyStateChangedAsync();
            }
            else
            {
                Console.WriteLine($"Failed to delete message: {string.Join(", ", result.ErrorMessages)}");
                await toastService.Error(string.Join(", ", result.ErrorMessages));
            }
        });

        _hubConnection.On<Result<ConversationDto>>("ConversationCreated", async result =>
        {
            if (result.Success)
            {
                state.Conversations.Add(result.Data);
                await OnConversationSelected(result.Data);
                await NotifyStateChangedAsync();
            }
            else
            {
                Console.WriteLine($"Failed to create conversation: {string.Join(", ", result.ErrorMessages)}");
                await toastService.Error(string.Join(", ", result.ErrorMessages));
            }
        });

        _hubConnection.On<Result<ConversationDto>>("ConversationUpdated", async result =>
        {
            if (result.Success)
            {
                Console.WriteLine($"Updated conversation via SignalR: {result.Data.Title}");
                state.UpdateConversation(result.Data);
                await NotifyStateChangedAsync();
            }
            else
            {
                Console.WriteLine($"Failed to update conversation: {string.Join(", ", result.ErrorMessages)}");
                await toastService.Error(string.Join(", ", result.ErrorMessages));
            }
        });

        _hubConnection.On<Result<string>>("ConversationDeleted", async result =>
        {
            if (result.Success)
            {
                string conversationId = result.Data;
                Console.WriteLine($"Deleted conversation via SignalR: {conversationId}");
                state.RemoveConversation(conversationId);
                await NotifyStateChangedAsync();
            }
            else
            {
                Console.WriteLine($"Failed to delete conversation: {string.Join(", ", result.ErrorMessages)}");
                await toastService.Error(string.Join(", ", result.ErrorMessages));
            }
        });

        _hubConnection.On<Result<ConversationDto>>("ConversationLeft", async result =>
        {
            if (result.Success)
            {
                Console.WriteLine($"Left conversation via SignalR: {result.Data.Id}");

                if (result.Data.Participants.All(p => p.Id != state.UserId))
                {
                    state.RemoveConversation(result.Data.Id);
                }
                else
                {
                    state.UpdateConversation(result.Data);
                }

                await NotifyStateChangedAsync();
            }
            else
            {
                Console.WriteLine($"Failed to leave conversation: {string.Join(", ", result.ErrorMessages)}");
                await toastService.Error(string.Join(", ", result.ErrorMessages));
            }
        });

        _hubConnection.Closed += error =>
        {
            Console.WriteLine($"Connection closed: {error?.Message}");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnecting += error =>
        {
            Console.WriteLine($"Connection reconnecting: {error?.Message}");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += async connectionId =>
        {
            Console.WriteLine($"Connection reconnected: {connectionId}");
            await JoinAllConversations();
        };

        try
        {
            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("Register", state.UserId);
            await JoinAllConversations();
            await NotifyStateChangedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to SignalR: {ex.Message}");
            await toastService.Error($"Chat real-time connection failed: {ex.Message}");
        }
    }

    public async Task SearchConversations(string? search)
    {
        Result<CursorPaginationResponse<List<ConversationDto>>> conversations = await conversationService.GetConversationsAsync(state.UserId, 10, null, search, CancellationToken.None);
        if (!conversations.Success)
        {
            await toastService.Error(conversations.ErrorMessages.ToString());
            return;
        }

        state.ClearConversations();
        state.AddConversations(conversations.Data);
    }

    public async Task LoadMoreConversations((string? nextCursor, string? search) args)
    {
        (string? nextCursor, string? search) = args;
        if (string.IsNullOrEmpty(nextCursor))
        {
            return;
        }

        Result<CursorPaginationResponse<List<ConversationDto>>> conversations = await conversationService.GetConversationsAsync(state.UserId, 10, nextCursor, search, CancellationToken.None);
        if (!conversations.Success)
        {
            await toastService.Error(conversations.ErrorMessages.ToString());
            return;
        }

        state.AddConversations(conversations.Data);
    }

    public async Task LoadMoreMessages(string? nextCursor)
    {
        if (state.Selected is null || string.IsNullOrEmpty(nextCursor))
        {
            return;
        }

        Result<CursorPaginationResponse<List<ChatMessageDto>>> messagesResult = await chatMessageService.GetChatMessagesAsync(state.Selected.Id, state.UserId, 10, nextCursor, CancellationToken.None);
        if (!messagesResult.Success)
        {
            await toastService.Error(messagesResult.ErrorMessages.ToString());
            return;
        }

        state.AddMessages(messagesResult.Data);
    }

    public async Task GetUsers(NewConversationModal.UsersQuery usersQuery)
    {
        Result<PaginationResponse<List<UserDto>>> response = await usersService.GetUsers(usersQuery.q, usersQuery.page, usersQuery.pageSize);
        if (response.Success)
        {
            AllUsers = response.Data;
        }
        else
        {
            await toastService.Error(response.ErrorMessages.ToString());
        }
    }

    private async Task JoinAllConversations()
    {
        if (!IsConnected || _hubConnection is null)
        {
            return;
        }

        var tasks = state.Conversations.Select(conv => _hubConnection.InvokeAsync("JoinConversation", conv.Id)).ToList();

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    public async Task HandleSubmit()
    {
        if (string.IsNullOrWhiteSpace(MessageText) || state.Selected is null)
        {
            return;
        }

        CreateChatMessageDto createChatMessageDto = new()
        {
            Content = MessageText,
            CreatorUserId = state.UserId,
            ConversationId = state.Selected.Id,
            ReplyToMessageId = ReplyToMessage?.Id,
            Location = null,
            InviteUrl = null
        };

        ValidationResult? result = await createChatMessageDtoValidator.ValidateAsync(createChatMessageDto);
        if (!result.IsValid)
        {
            await toastService.Error(result.Errors.FirstOrDefault()?.ErrorMessage ?? "Message validation failed.");
            return;
        }

        if (_hubConnection is not null)
        {
            await _hubConnection.SendAsync("CreateMessage", createChatMessageDto);
        }

        MessageText = null;
        ReplyToMessage = null;
    }

    public async Task ShareLocation((double Latitude, double Longitude) location)
    {
        if (state.Selected is null)
        {
            return;
        }

        CreateChatMessageDto createChatMessageDto = new()
        {
            CreatorUserId = state.UserId,
            ConversationId = state.Selected.Id,
            ReplyToMessageId = ReplyToMessage?.Id,
            Location = new Location(location.Latitude,
                location.Longitude,
                string.Empty,
                string.Empty),
            InviteUrl = null,
            Content = null
        };

        ValidationResult? result = await createChatMessageDtoValidator.ValidateAsync(createChatMessageDto);
        if (!result.IsValid)
        {
            await toastService.Error(result.Errors.FirstOrDefault()?.ErrorMessage ?? "Message validation failed.");
            return;
        }

        if (_hubConnection is not null)
        {
            try
            {
                await _hubConnection.InvokeAsync("CreateMessage", createChatMessageDto);
            }
            catch (Exception ex)
            {
                await toastService.Error($"Failed to share location: {ex.Message}");
            }
        }

        ReplyToMessage = null;
    }

    public async Task OnEditMessage(UpdateChatMessageDto msg)
    {
        ValidationResult? result = await updateChatMessageDtoValidator.ValidateAsync(msg);
        if (!result.IsValid)
        {
            await toastService.Error(result.Errors.FirstOrDefault()?.ErrorMessage ?? "Message validation failed.");
            return;
        }

        if (_hubConnection is not null)
        {
            try
            {
                await _hubConnection.InvokeAsync("UpdateMessage", msg);
            }
            catch (Exception ex)
            {
                await toastService.Error($"Failed to update message: {ex.Message}");
            }
        }
    }

    public async Task OnDeleteMessage(ChatMessageDto msg)
    {
        if (_hubConnection is not null)
        {
            try
            {
                await _hubConnection.InvokeAsync("DeleteMessage", msg.Id);
            }
            catch (Exception ex)
            {
                await toastService.Error($"Failed to delete message: {ex.Message}");
            }
        }
    }

    public Task OnReplyToMessage(ChatMessageDto msg)
    {
        ReplyToMessage = msg;
        return Task.CompletedTask;
    }

    public Task CancelReply()
    {
        ReplyToMessage = null;
        return Task.CompletedTask;
    }

    public async Task StartChat(string otherUserId)
    {
        var createConversationDto = new CreateConversationDto
        {
            CreatorUserId = state.UserId,
            ConversationType = ConversationType.Private,
            ParticipantIds = [otherUserId, state.UserId],
            TeamId = null,
            MatchId = null,
            Title = null
        };

        ValidationResult? result = await createConversationDtoValidator.ValidateAsync(createConversationDto);
        if (!result.IsValid)
        {
            await toastService.Error(result.Errors.FirstOrDefault()?.ErrorMessage ?? "Conversation validation failed.");
            return;
        }

        if (_hubConnection is not null)
        {
            try
            {
                await _hubConnection.InvokeAsync("CreateConversation", createConversationDto);
            }
            catch (Exception ex)
            {
                await toastService.Error($"Failed to start chat: {ex.Message}");
            }
        }
    }

    public async Task LeaveConversation()
    {
        string? id = state.Selected?.Id;
        if (id is null)
        {
            return;
        }

        if (_hubConnection is not null)
        {
            try
            {
                await _hubConnection.InvokeAsync("LeaveConversationAndNotify", id);
            }
            catch (Exception ex)
            {
                await toastService.Error($"Failed to leave conversation: {ex.Message}");
            }
        }
    }

    public async Task DeleteConversation()
    {
        string? id = state.Selected?.Id;
        if (id is null)
        {
            return;
        }

        if (_hubConnection is not null)
        {
            try
            {
                await _hubConnection.InvokeAsync("DeleteConversation", id);
            }
            catch (Exception ex)
            {
                await toastService.Error($"Failed to delete conversation: {ex.Message}");
            }
        }
    }

    public async Task OnConversationSelected(ConversationDto conversation)
    {
        Result<CursorPaginationResponse<List<ChatMessageDto>>> messages = await chatMessageService.GetChatMessagesAsync(conversation.Id, state.UserId, 10, state.NextChatMessagesCursor, CancellationToken.None);
        if (!messages.Success)
        {
            await toastService.Error(messages.ErrorMessages.FirstOrDefault() ?? "Failed to load messages for the selected conversation.");
            return;
        }

        state.Select(conversation, messages.Data!);
    }

    public async Task OnUpdateConversation(UpdateConversationDto updateConversationDto)
    {
        if (state.Selected is null)
        {
            await toastService.Error("No conversation selected.");
            return;
        }

        ValidationResult? result = await updateConversationDtoValidator.ValidateAsync(updateConversationDto);
        if (!result.IsValid)
        {
            await toastService.Error(result.Errors.FirstOrDefault()?.ErrorMessage ?? "Conversation validation failed.");
            return;
        }

        Result<ConversationDto> conv = await conversationService.UpdateConversationAsync(updateConversationDto);
        if (!conv.Success)
        {
            Console.WriteLine(conv.ErrorMessages.ToString());
            return;
        }

        if (_hubConnection is not null)
        {
            try
            {
                await _hubConnection.InvokeAsync("UpdateConversation",
                    updateConversationDto.ConversationId,
                    updateConversationDto.CreatorUserId);
            }
            catch (Exception ex)
            {
                await toastService.Error($"Failed to update conversation (realtime): {ex.Message}");
            }
        }

        state.UpdateConversation(conv.Data!);
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
        onStateChanged.Invoke();
    }

    private async Task NotifyStateChangedAsync()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
        if (onStateChanged is not null)
        {
            await onStateChanged();
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async ValueTask DisposeAsync()
    {
        if (state is not null)
        {
            state.Changed -= OnStateChanged;
        }

        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}

