using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class MatchChatMessageConfiguration: IEntityTypeConfiguration<MatchChatMessage>
{
    public void Configure(EntityTypeBuilder<MatchChatMessage> builder)
    {
        builder.ToTable("MatchChatMessages");
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
        
        //MatchChatMessage is associated with one Sender
        builder.HasOne(c => c.Sender)
            .WithMany()
            .HasForeignKey(c => c.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        //MatchChatMessage is associated with one Match
        builder.HasOne(c => c.Match)
            .WithMany()
            .HasForeignKey(c => c.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
        
        //MatchChatMessage can have multiple receivers
        builder.HasMany(c => c.Receivers)
            .WithMany()
            .UsingEntity("MatchChatMessageReceiver");
        
        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();
        
        builder.Property(i => i.UpdatedAtUtc);
        builder.Property(i => i.DeletedAtUtc);
        builder.HasQueryFilter(m => m.DeletedAtUtc == null);
    }
}
