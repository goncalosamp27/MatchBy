using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(50);

        builder.OwnsOne(u => u.ProfileImage);
        builder.Property(u => u.Id)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Rating)
            .IsRequired();

        builder.Property(u => u.Bio)
            .HasMaxLength(500);

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.Property(i => i.UpdatedAtUtc);

        builder.HasIndex(u => u.UserName).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
