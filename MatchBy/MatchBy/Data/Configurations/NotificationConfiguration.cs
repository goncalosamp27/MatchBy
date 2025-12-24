using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
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
        
        builder.HasOne(n => n.Sender)
            .WithMany()
            .HasForeignKey(n => n.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Receiver)
            .WithMany()
            .HasForeignKey(n => n.ReceiverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.ReadAtUtc);
    }
}