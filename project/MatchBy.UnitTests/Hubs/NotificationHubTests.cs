using System.Reflection;
using MatchBy.Hubs;

namespace MatchBy.UnitTests.Hubs;

public class NotificationHubTests
{

    #region GetUserConnectionsStatic Tests

    [Fact]
    public void GetUserConnectionsStatic_WithNonExistentUser_ShouldReturnEmptyCollection()
    {
        // Arrange
        string userId = "nonexistent";

        // Act
        IEnumerable<string> connections = NotificationHub.GetUserConnectionsStatic(userId);

        // Assert
        Assert.Empty(connections);
        Assert.IsAssignableFrom<IEnumerable<string>>(connections);
    }

    [Fact]
    public void GetUserConnectionsStatic_ShouldReturnIEnumerableOfString()
    {
        // Arrange
        string userId = "user1";

        // Act
        IEnumerable<string> connections = NotificationHub.GetUserConnectionsStatic(userId);

        // Assert
        Assert.IsAssignableFrom<IEnumerable<string>>(connections);
    }

    #endregion

    #region GetUserConnections Tests

    [Fact]
    public void GetUserConnections_WithNonExistentUser_ShouldReturnEmptyCollection()
    {
        // Arrange
#pragma warning disable CA2000 // NotificationHub doesn't implement IDisposable
        var hub = new NotificationHub();
#pragma warning restore CA2000
        string userId = "nonexistent";

        // Act
        IEnumerable<string> result = hub.GetUserConnections(userId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Hub Creation Tests

    [Fact]
    public void NotificationHub_CanBeInstantiated()
    {
        // Act
#pragma warning disable CA2000 // NotificationHub doesn't implement IDisposable
        var hub = new NotificationHub();
#pragma warning restore CA2000

        // Assert
        Assert.NotNull(hub);
        Assert.IsType<NotificationHub>(hub);
    }

    [Fact]
    public void NotificationHub_InheritsFromHub()
    {
        // Arrange & Act
#pragma warning disable CA2000 // NotificationHub doesn't implement IDisposable
        var hub = new NotificationHub();
#pragma warning restore CA2000

        // Assert
        Assert.IsAssignableFrom<Microsoft.AspNetCore.SignalR.Hub>(hub);
    }

    #endregion

    #region Method Visibility Tests

    [Fact]
    public void NotificationHub_HasPublicMethods()
    {
        // Arrange
        Type hubType = typeof(NotificationHub);

        // Act & Assert
        MethodInfo? registerMethod = hubType.GetMethod("Register");
        Assert.NotNull(registerMethod);
        Assert.True(registerMethod.IsPublic);

        MethodInfo? onDisconnectedMethod = hubType.GetMethod("OnDisconnectedAsync");
        Assert.NotNull(onDisconnectedMethod);
        Assert.True(onDisconnectedMethod.IsPublic);

        MethodInfo? ensureUserMethod = hubType.GetMethod("EnsureUser");
        Assert.NotNull(ensureUserMethod);
        Assert.True(ensureUserMethod.IsPublic);

        MethodInfo? getUserConnectionsMethod = hubType.GetMethod("GetUserConnections");
        Assert.NotNull(getUserConnectionsMethod);
        Assert.True(getUserConnectionsMethod.IsPublic);

        MethodInfo? getUserConnectionsStaticMethod = hubType.GetMethod("GetUserConnectionsStatic");
        Assert.NotNull(getUserConnectionsStaticMethod);
        Assert.True(getUserConnectionsStaticMethod.IsPublic);
        Assert.True(getUserConnectionsStaticMethod.IsStatic);
    }

    #endregion
}