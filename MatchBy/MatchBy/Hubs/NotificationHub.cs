using System.Collections.Concurrent;
using MatchBy.DTOs.Notification;
using Microsoft.AspNetCore.SignalR;

namespace MatchBy.Hubs;

public class NotificationHub : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionUsers = new();

    /// <summary>
    /// Handles client disconnection by removing the connection from user tracking dictionaries.
    /// </summary>
    /// <param name="exception">Optional exception that caused the disconnection.</param>
    /// <returns>
    /// A task representing the asynchronous disconnection operation.
    /// </returns>
    /// <remarks>
    /// This method removes the connection from both the connection-to-user mapping and the user-to-connections mapping.
    /// If a user has no remaining connections, their entry is removed from the UserConnections dictionary.
    /// </remarks>
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

    /// <summary>
    /// Registers a client connection with a specific user ID for notification routing.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to register this connection for.</param>
    /// <returns>
    /// A task representing the asynchronous registration operation. Sends a "Registered" message to the caller.
    /// </returns>
    /// <remarks>
    /// This method associates the current SignalR connection with a user ID, allowing notifications
    /// to be routed to all active connections for that user. Multiple connections per user are supported.
    /// </remarks>
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

    /// <summary>
    /// Ensures that the current connection is registered with a user ID.
    /// </summary>
    /// <returns>
    /// The user ID associated with the current connection.
    /// </returns>
    /// <exception cref="HubException">Thrown when the connection is not registered with a user ID.</exception>
    public string EnsureUser()
        => ConnectionUsers.TryGetValue(Context.ConnectionId, out string? uid)
            ? uid
            : throw new HubException("Connection not registered.");

    /// <summary>
    /// Gets all active SignalR connection IDs for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>
    /// A collection of connection IDs for the user, or an empty collection if the user has no active connections.
    /// </returns>
    /// <remarks>
    /// This method returns a thread-safe snapshot of the user's connections.
    /// </remarks>
    public IEnumerable<string> GetUserConnections(string userId)
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
    /// Gets all active SignalR connection IDs for a specific user. This static method is provided for external services to send notifications.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>
    /// A collection of connection IDs for the user, or an empty collection if the user has no active connections.
    /// </returns>
    /// <remarks>
    /// This method returns a thread-safe snapshot of the user's connections. It is used by the NotificationService
    /// to determine which SignalR connections to send notifications to.
    /// </remarks>
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

