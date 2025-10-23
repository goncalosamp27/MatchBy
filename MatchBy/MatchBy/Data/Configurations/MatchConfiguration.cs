using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class MatchConfiguration: IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .IsRequired()
            .HasMaxLength(500);

        builder.OwnsOne(u => u.Location);
        
        builder.Property(m => m.Description)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(m => m.minPlayers)
            .IsRequired();

        builder.Property(m => m.maxPlayers)
            .IsRequired();
        
        builder.Property(m => m.Sport)
            .IsRequired();
        
        builder.Property(m => m.Status)
            .IsRequired();
        
        builder.Property(m => m.Privacy)
            .IsRequired();
        
        builder.Property(m => m.CreatorId)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.HasOne(m => m.Creator)
            .WithMany()
            .HasForeignKey(m => m.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(m => m.Participants)
            .WithMany()
            .UsingEntity("MatchParticipants");
        
        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();
        
        builder.Property(i => i.UpdatedAtUtc);
        builder.Property(i => i.DeletedAtUtc);
        builder.HasQueryFilter(m => m.DeletedAtUtc == null);
    }
}
