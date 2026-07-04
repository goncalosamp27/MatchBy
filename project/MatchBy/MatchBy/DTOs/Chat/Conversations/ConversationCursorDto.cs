using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace MatchBy.DTOs.Chat.Conversations;

public sealed record ConversationCursorDto(string Id, DateTime Date)
{
    public static string Encode(string id, DateTime date)
    {
        var cursor = new ConversationCursorDto(id, date);
        string json = JsonSerializer.Serialize(cursor);
        return Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
    }

    public static ConversationCursorDto? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            string json = Base64UrlEncoder.Decode(cursor);
            return JsonSerializer.Deserialize<ConversationCursorDto>(json);
        }
        catch
        {
            return null;
        }
    }
}
