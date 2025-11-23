using System.Collections.Concurrent;
using MatchBy.DTOs.Notification;
using Microsoft.AspNetCore.SignalR;

namespace MatchBy.Hubs;

public class NotificationHub : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionUsers = new();

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (!ConnectionUsers.TryRemove(Context.ConnectionId, out string? userId) ||
            !UserConnections.TryGetValue(userId, out HashSet<string>? connections))
        {
            return base.OnDisconnectedAsync(exception);
        }

        lock (connections)
        {
            connections.Remove(Context.ConnectionId);
            if (connections.Count == 0)
            {
                UserConnections.TryRemove(userId, out _);
            }
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task Register(string userId)
    {
        ConnectionUsers[Context.ConnectionId] = userId;

        UserConnections.AddOrUpdate(
            userId,
            _ => [Context.ConnectionId],
            (_, connections) =>
            {
                lock (connections)
                {
                    connections.Add(Context.ConnectionId);
                }

                return connections;
            });

        await Clients.Caller.SendAsync("Registered", new { userId });
    }

    private string EnsureUser()
        => ConnectionUsers.TryGetValue(Context.ConnectionId, out string? uid)
            ? uid
            : throw new HubException("Connection not registered.");

    private IEnumerable<string> GetUserConnections(string userId)
    {
        if (!UserConnections.TryGetValue(userId, out HashSet<string>? connections))
        {
            return [];
        }

        lock (connections)
        {
            return [.. connections];
        }
    }

    /// <summary>
    /// Gets user connections for external services to send notifications
    /// </summary>
    #pragma warning disable S4144
    public static IEnumerable<string> GetUserConnectionsStatic(string userId)
    #pragma warning restore S4144
    {
        if (!UserConnections.TryGetValue(userId, out HashSet<string>? connections))
        {
            return [];
        }

        lock (connections)
        {
            return [.. connections];
        }
    }
}

