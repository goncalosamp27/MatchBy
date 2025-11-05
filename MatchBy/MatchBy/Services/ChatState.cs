using MatchBy.Models;

namespace MatchBy.Services;

public sealed class ChatState
{
    public string? UserId { get; private set; }
    public List<Conversation> Conversations { get; } = [];
    public Conversation? Selected { get; set; }
    public List<ChatMessage> MessagesOfSelectedConversation { get; set; } = [];

    public event EventHandler? Changed;

    public void InitUser(string userId) => UserId = userId;

    private void NotifyStateChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetConversations(IEnumerable<Conversation> items)
    {
        Conversations.Clear();
        Conversations.AddRange(items);
        NotifyStateChanged();
    }

    public void Select(Conversation c, List<ChatMessage> messages)
    {
        Selected = c;
        MessagesOfSelectedConversation = messages;
        Conversation? conv = Conversations.FirstOrDefault(cn => cn.Id == c.Id);
        if (conv is not null)
        {
            conv.Messages = messages;
        }
        NotifyStateChanged();
    }

    public void UpsertMessage(ChatMessage msg)
    {
        //if the conversation doesn't exist, ignore
        Conversation? conv = Conversations.FirstOrDefault(c => c.Id == msg.ConversationId);
        if (conv is null)
        {
            return;
        }
        
        //update the selected conversation if it's the same
        if (Selected?.Id == conv.Id)
        {
            int idx2 = MessagesOfSelectedConversation.FindIndex(c => c.Id == msg.Id);
            if (idx2 >= 0)
            {
                MessagesOfSelectedConversation[idx2] = msg;
            }
            else
            {
                MessagesOfSelectedConversation.Add(msg);
            }
        }
        conv.Messages = MessagesOfSelectedConversation;
        conv.LastMessageAtUtc = DateTime.UtcNow;
        NotifyStateChanged();
    }

    public void RemoveMessage(string conversationId, string messageId)
    {
        if (Selected?.Id == conversationId)
        {
            MessagesOfSelectedConversation.RemoveAll(m => m.Id == messageId);
        }
        Conversation? conv = Conversations.FirstOrDefault(cn => cn.Id == conversationId);
        if (conv is not null)
        {
            conv.Messages = MessagesOfSelectedConversation;
            conv.LastMessageAtUtc = conv.Messages.LastOrDefault()?.CreatedAtUtc;
        }

        NotifyStateChanged();
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
}

