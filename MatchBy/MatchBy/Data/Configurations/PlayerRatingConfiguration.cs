using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class PlayerRatingConfiguration: IEntityTypeConfiguration<PlayerRating>
{
    public void Configure(EntityTypeBuilder<PlayerRating> builder)
    {
        builder.ToTable("PlayerRatings");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(r => r.Rating)
            .IsRequired();
        
        builder.Property(r => r.SentById)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(r => r.SentBy)
            .WithMany()
            .HasForeignKey(r => r.SentById)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(r => r.ReceivedById)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(r => r.ReceivedBy)
            .WithMany()
            .HasForeignKey(r => r.ReceivedById)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(r => r.MatchId)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(r => r.Match)
            .WithMany()
            .HasForeignKey(r => r.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc);
        builder.Property(r => r.DeletedAtUtc);
        builder.HasQueryFilter(m => m.DeletedAtUtc == null);
    }
}
