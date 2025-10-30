using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

         var converter = new ValueConverter<ICollection<Sports>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<ICollection<Sports>>(v, (JsonSerializerOptions?)null) ?? new List<Sports>()
        );

        builder.Entity<ApplicationUser>()
            .Property(u => u.PreferredSports)
            .HasConversion(converter);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
