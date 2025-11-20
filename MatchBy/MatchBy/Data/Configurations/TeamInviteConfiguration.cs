using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class TeamInviteConfiguration: IEntityTypeConfiguration<TeamInvite>
{
    public void Configure(EntityTypeBuilder<TeamInvite> builder)
    {
        builder.ToTable("TeamInvites");
        builder.HasKey(ti => ti.Id);
        builder.Property(ti => ti.Id)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(ti => ti.Content)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ti => ti.SenderId)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(ti => ti.Sender)
            .WithMany()
            .HasForeignKey(ti => ti.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ti => ti.ReceiverId)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(ti => ti.Receiver)
            .WithMany()
            .HasForeignKey(ti => ti.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(ti => ti.TeamId)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.HasOne(ti => ti.Team)
            .WithMany()
            .HasForeignKey(ti => ti.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ti => ti.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ti => ti.ExpiresAtUtc)
            .IsRequired();
        
        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.Property(t => t.UpdatedAtUtc);
        builder.Property(t => t.AcceptedAtUtc);
        builder.Property(t => t.DeclinedAtUtc);
        builder.Property(t => t.DeletedAtUtc);
        builder.HasQueryFilter(m => m.DeletedAtUtc == null);
    }
}
