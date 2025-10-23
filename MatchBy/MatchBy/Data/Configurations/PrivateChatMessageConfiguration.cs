using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class PrivateChatMessageConfiguration: IEntityTypeConfiguration<PrivateChatMessage>
{
    public void Configure(EntityTypeBuilder<PrivateChatMessage> builder)
    {
        builder.ToTable("PrivateChatMessages");
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
        
        builder.Property(c => c.ReceiverId)
            .IsRequired();
        
        builder.HasOne(c => c.Receiver)
            .WithMany()
            .HasForeignKey(c => c.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();
        
        builder.Property(i => i.UpdatedAtUtc);
        builder.Property(i => i.DeletedAtUtc);
        builder.HasQueryFilter(m => m.DeletedAtUtc == null);
    }
}
