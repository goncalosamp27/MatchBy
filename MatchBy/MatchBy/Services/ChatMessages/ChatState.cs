using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;

namespace MatchBy.Services.ChatMessages;

public sealed class ChatState
{
    public string UserId { get; private set; } //this is required
    public List<ConversationDto> Conversations { get; } = [];
    public ConversationDto? Selected { get; set; }
    public List<ChatMessageDto> MessagesOfSelectedConversation { get; set; } = [];
    
    public string? NextChatMessagesCursor { get; private set; }
    public string? NextConversationCursor { get; private set; }

    public event EventHandler? Changed;

    public void InitUser(string userId) => UserId = userId;

    private void NotifyStateChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Select(ConversationDto c, CursorPaginationResponse<List<ChatMessageDto>> messages)
    {
        Selected = c;
        NextChatMessagesCursor = messages.NextCursor;
        MessagesOfSelectedConversation = messages.Data;
        NotifyStateChanged();
    }

    public void UpsertMessage(ChatMessageDto msg)
    {
        int idx = Conversations.FindIndex(c => c.Id == msg.ConversationId);
        if (idx < 0)
        {
            return;
        }
        
        if (Selected?.Id == msg.ConversationId)
        {
            int i = MessagesOfSelectedConversation.FindIndex(m => m.Id == msg.Id);
            if (i >= 0)
            {
                MessagesOfSelectedConversation[i] = msg;
            }
            else
            {
                MessagesOfSelectedConversation.Add(msg);
            }
        }
        
        ConversationDto updated = Conversations[idx] with
        {
            LastMessageAtUtc = DateTime.UtcNow,
            LastMessageContent = msg.Content
        };
        Conversations[idx] = updated;
        
        if (Selected?.Id == updated.Id)
        {
            Selected = updated;
        }

        NotifyStateChanged();
    }

    public void RemoveMessage(ConversationDto updatedConversationDto, string messageId)
    {
        if (Selected?.Id == updatedConversationDto.Id)
        {
            MessagesOfSelectedConversation.RemoveAll(m => m.Id == messageId);
            Selected = updatedConversationDto;
        }
        
        int idxSel = Conversations.FindIndex(c => c.Id == updatedConversationDto.Id);
        if (idxSel >= 0)
        {
            Conversations[idxSel] = updatedConversationDto;
        }
        
        NotifyStateChanged();
    }
    
    public void AddMessages(CursorPaginationResponse<List<ChatMessageDto>> messages)
    {
        MessagesOfSelectedConversation.InsertRange(0, messages.Data);
        NextChatMessagesCursor = messages.NextCursor;
        NotifyStateChanged();
    }
    
    public void AddConversations(CursorPaginationResponse<List<ConversationDto>> conversations)
    {
        var newConversations = conversations.Data.Where(conv => !Conversations.Exists(c => c.Id == conv.Id)).ToList();
        Conversations.AddRange(newConversations);
        NextConversationCursor = conversations.NextCursor;
        NotifyStateChanged();
    }

    public void ClearConversations()
    {
        Conversations.Clear();
    }

    public void RemoveConversation(string conversationId)
    {
        Conversations.RemoveAll(c => c.Id == conversationId);
        if (Selected?.Id == conversationId)
        {
            Selected = null;
        }

        NotifyStateChanged();
    }
    
    public void UpdateConversation(ConversationDto updated)
    {
        int idx = Conversations.FindIndex(c => c.Id == updated.Id);
        if (idx >= 0)
        {
            Conversations[idx] = updated;
        }
        else
        {
            Conversations.Add(updated);
        }

        if (Selected?.Id == updated.Id)
        {
            Selected = updated;
        }

        NotifyStateChanged();
    }
}

