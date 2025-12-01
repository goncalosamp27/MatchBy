using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class MatchInviteConfiguration: IEntityTypeConfiguration<MatchInvite>
{
    public void Configure(EntityTypeBuilder<MatchInvite> builder)
    {
        builder.ToTable("MatchInvites");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Content)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(i => i.SenderId)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(i => i.Sender)
            .WithMany()
            .HasForeignKey(i => i.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.ReceiverId)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(i => i.Receiver)
            .WithMany()
            .HasForeignKey(i => i.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.MatchId)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(i => i.Match)
            .WithMany()
            .HasForeignKey(i => i.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(i => i.ExpiresAtUtc)
            .IsRequired();
        
        builder.Property(i => i.CreatedAtUtc)
            .IsRequired();

        builder.Property(i => i.UpdatedAtUtc);
        builder.Property(i => i.AcceptedAtUtc);
        builder.Property(i => i.DeletedAtUtc);
        builder.HasQueryFilter(m => m.DeletedAtUtc == null);
    }
}
