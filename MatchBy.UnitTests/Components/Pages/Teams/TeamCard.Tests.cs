using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using MatchBy.Components.Pages.Teams;
using MatchBy.DTOs.Team;
using MatchBy.DTOs.User;
using MatchBy.Models;

namespace MatchBy.UnitTests.Components.Pages.Teams;

public class TeamCardTests
{
    [Fact]
    public void Render_WithBasicTeamData_ShouldDisplayTeamNameAndDescription()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeam();
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team));
        
        // Assert
        cut.Find("h3").MarkupMatches($"<h3 class=\"font-bold text-lg text-[var(--text)]\">{team.Name}</h3>");
        Assert.Equal(team.Description, cut.Find("p").TextContent);
    }

    [Fact]
    public void Render_WhenUserIdIsOwner_ShouldDisplayOwnerBadge()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeam();
        string userId = team.OwnerId;
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team)
            .Add(p => p.UserId, userId));
        
        // Assert
        IElement badge = cut.Find("span");
        Assert.Equal("Owner", badge.TextContent);
        Assert.Contains("bg-[var(--main)]", badge.ClassName);
    }

    [Fact]
    public void Render_WhenUserIdIsMember_ShouldDisplayMemberBadge()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeam();
        string userId = team.Members.First().Id;
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team)
            .Add(p => p.UserId, userId));
        
        // Assert
        IElement badge = cut.Find("span");
        Assert.Equal("Member", badge.TextContent);
        Assert.Contains("bg-[var(--section)]", badge.ClassName);
    }

    [Fact]
    public void Render_WhenUserIdIsNotOwnerOrMember_ShouldDisplayInvitedBadge()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeam();
        string userId = "different-user-id";
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team)
            .Add(p => p.UserId, userId));
        
        // Assert
        IElement badge = cut.Find("span");
        Assert.Equal("Invited", badge.TextContent);
        Assert.Contains("bg-[var(--section)]", badge.ClassName);
    }

    [Fact]
    public void Render_WhenTeamHasImageUrl_ShouldDisplayImage()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeam(imageUrl: "https://example.com/team-image.jpg");
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team));
        
        // Assert
        IElement image = cut.Find("img");
        Assert.Equal("https://example.com/team-image.jpg", image.GetAttribute("src"));
        Assert.Equal($"{team.Name} image", image.GetAttribute("alt"));
    }

    [Fact]
    public void Render_WhenTeamHasNoImageUrl_ShouldDisplayPlaceholderDiv()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeam(imageUrl: null);
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team));
        
        // Assert
        // Find the placeholder div by checking for divs with the specific classes
        IReadOnlyList<IElement> divs = cut.FindAll("div");
        IElement? placeholder = divs.FirstOrDefault(d => 
            d.ClassName?.Contains("w-14") == true && 
            d.ClassName.Contains("h-14") && 
            d.ClassName.Contains("rounded-full") &&
            d.ClassName.Contains("bg-[var(--section)]"));
        Assert.NotNull(placeholder);
    }

    [Fact]
    public void Render_WithTeamLink_ShouldHaveCorrectHref()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeam();
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team));
        
        // Assert
        IElement link = cut.Find("a");
        Assert.Equal($"/teams/{team.Id}", link.GetAttribute("href"));
    }

    [Fact]
    public void Render_WithMembers_ShouldDisplayMemberAvatars()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeamWithMembers(2);
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team));
        
        // Assert
        IReadOnlyList<IElement> memberImages = cut.FindAll("img");
        // Should have member avatars (team has no image, so only member avatars)
        Assert.True(memberImages.Count >= 2);
    }

    [Fact]
    public void Render_WithMoreThanThreeMembers_ShouldDisplayOverflowIndicator()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeamWithMembers(5);
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team));
        
        // Assert
        // Find the overflow div by checking for divs containing the overflow text
        IReadOnlyList<IElement> divs = cut.FindAll("div");
        IElement? overflowDiv = divs.FirstOrDefault(d => d.TextContent.Contains("+2"));
        Assert.NotNull(overflowDiv);
        Assert.Contains("+2", overflowDiv.TextContent);
    }

    [Fact]
    public void Render_WithMembers_ShouldDisplayCorrectMemberCount()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeamWithMembers(3);
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team));
        
        // Assert
        IReadOnlyList<IElement> spans = cut.FindAll("span");
        IElement? memberCountText = spans.FirstOrDefault(s => s.TextContent.Contains("member"));
        Assert.NotNull(memberCountText);
        Assert.Contains("3", memberCountText.TextContent);
        Assert.Contains(team.MaxMembers.ToString(CultureInfo.InvariantCulture), memberCountText.TextContent);
    }

    [Fact]
    public void Render_WithSingleMember_ShouldDisplaySingularMemberText()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeamWithMembers(1);
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team));
        
        // Assert
        IReadOnlyList<IElement> spans = cut.FindAll("span");
        IElement? memberCountText = spans.FirstOrDefault(s => s.TextContent.Contains("member"));
        Assert.NotNull(memberCountText);
        Assert.Contains("1", memberCountText.TextContent);
        Assert.DoesNotContain("members", memberCountText.TextContent);
    }

    [Fact]
    public void Render_WithOwner_ShouldDisplayOwnerName()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeam();
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team));
        
        // Assert
        Assert.Contains(team.Owner!.DisplayName, cut.Markup);
    }

    [Fact]
    public void Render_WithNullUserId_ShouldDisplayInvitedBadge()
    {
        // Arrange
        using var ctx = new BunitContext();
        TeamDto team = CreateTestTeam();
        
        // Act
        IRenderedComponent<TeamCard> cut = ctx.Render<TeamCard>(parameters => parameters
            .Add(p => p.Team, team)
            .Add(p => p.UserId, null));
        
        // Assert
        IElement badge = cut.Find("span");
        Assert.Equal("Invited", badge.TextContent);
    }

    private static TeamDto CreateTestTeam(string? imageUrl = null)
    {
        var owner = new UserDto("owner-id", "Team Owner", "https://example.com/owner-avatar.jpg");
        var member = new UserDto("member-id", "Team Member", "https://example.com/member-avatar.jpg");
        
        return new TeamDto
        {
            Id = "team-id-1",
            Name = "Test Team",
            Description = "This is a test team description",
            OwnerId = owner.Id,
            Owner = owner,
            Members = new List<UserDto> { member },
            MaxMembers = 10,
            ImageUrl = imageUrl,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static TeamDto CreateTestTeamWithMembers(int memberCount)
    {
        var owner = new UserDto("owner-id", "Team Owner", "https://example.com/owner-avatar.jpg");
        var members = Enumerable.Range(1, memberCount)
            .Select(i => new UserDto($"member-id-{i}", $"Member {i}", $"https://example.com/member-{i}-avatar.jpg"))
            .ToList();
        
        return new TeamDto
        {
            Id = "team-id-1",
            Name = "Test Team",
            Description = "This is a test team description",
            OwnerId = owner.Id,
            Owner = owner,
            Members = members,
            MaxMembers = 10,
            ImageUrl = null,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

