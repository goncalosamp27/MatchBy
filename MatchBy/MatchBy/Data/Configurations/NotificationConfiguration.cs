using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Type)
            .IsRequired();

        builder.Property(u => u.Title)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(u => u.Message)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(u => u.SenderId)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.ReceiverId)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(u => u.RelatedEntityId)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(u => u.RelatedEntityName)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(u => u.IsRead)
            .IsRequired();
        
        builder.Property(u => u.ActionUrl)
            .HasMaxLength(2048);
        
        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();
        
        builder.HasOne(c => c.Sender)
            .WithMany()
            .HasForeignKey(c => c.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(c => c.Receiver)
            .WithMany()
            .HasForeignKey(c => c.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.ReadAtUtc);
        builder.Property(i => i.DeletedAtUtc);
        builder.HasQueryFilter(m => m.DeletedAtUtc == null);
    }
}