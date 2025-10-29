using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace MatchBy.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public DbSet<Friend> Friends { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchChatMessage> MatchChatMessages { get; set; }
    public DbSet<MatchInvite> MatchInvites { get; set; }
    public DbSet<PlayerRating> PlayerRatings { get; set; }
    public DbSet<PrivateChatMessage> PrivateChatMessages { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamChatMessage> TeamChatMessages { get; set; }
    public DbSet<TeamInvite> TeamInvites { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(u => u.PreferredSports)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Sports>>(v, (JsonSerializerOptions?)null) ?? new List<Sports>()
            );

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
