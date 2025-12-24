using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class FriendConfiguration: IEntityTypeConfiguration<Friend>
{
    public void Configure(EntityTypeBuilder<Friend> builder)
    {
        builder.ToTable("Friends");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(f => f.SenderId)
            .HasMaxLength(500)
            .IsRequired();
        
        //Friend is associated with one Sender
        builder.HasOne(f => f.Sender)
            .WithMany()
            .HasForeignKey(f => f.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(f => f.ReceiverId)
            .HasMaxLength(500)
            .IsRequired();

        //Friend is associated with one Receiver
        builder.HasOne(f => f.Receiver)
            .WithMany()
            .HasForeignKey(f => f.ReceiverId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(f => f.CreatedAtUtc)
            .IsRequired();
        
        //Unique constraint to prevent duplicate friend requests between the same users
        builder
            .HasIndex(f => new { f.SenderId, f.ReceiverId })
            .IsUnique();
        
        builder.Property(i => i.UpdatedAtUtc);
    }
}
