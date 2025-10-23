using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class TeamChatMessageConfiguration: IEntityTypeConfiguration<TeamChatMessage>
{
    public void Configure(EntityTypeBuilder<TeamChatMessage> builder)
    {
        builder.ToTable("TeamChatMessages");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Content)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(c => c.Status)
            .IsRequired();
        
        builder.Property(c => c.SenderId)
            .IsRequired();
        
        builder.HasOne(c => c.Sender)
            .WithMany()
            .HasForeignKey(c => c.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(c => c.Receivers)
            .WithMany()
            .UsingEntity("TeamChatMessageReceivers");
        
        builder.Property(c => c.TeamId)
            .IsRequired();
        
        builder.HasOne(c => c.Team)
            .WithMany()
            .HasForeignKey(c => c.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();
        
        builder.Property(i => i.UpdatedAtUtc);
        builder.Property(i => i.DeletedAtUtc);
        builder.HasQueryFilter(m => m.DeletedAtUtc == null);
    }
}
