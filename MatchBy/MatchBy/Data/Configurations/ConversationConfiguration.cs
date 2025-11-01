using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Type)
            .IsRequired();
        
        builder.OwnsOne(u => u.Image);

        builder.Property(c => c.Title)
            .HasMaxLength(200);

        builder.Property(c => c.CreatorId)
            .IsRequired();

        // Relacionamento com Creator
        builder.HasOne(c => c.Creator)
            .WithMany()
            .HasForeignKey(c => c.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com Participants (many-to-many)
        builder.HasMany(c => c.Participants)
            .WithMany()
            .UsingEntity("ConversationParticipants");

        // Relacionamento com Messages
        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com Team (opcional)
        builder.HasOne(c => c.Team)
            .WithOne(m => m.Conversation)
            .HasForeignKey<Conversation>(c => c.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com Match (opcional)
        builder.HasOne(c => c.Match)
            .WithOne(m => m.Conversation)
            .HasForeignKey<Conversation>(c => c.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc);

        builder.Property(c => c.LastMessageAtUtc);

        builder.Property(c => c.DeletedAtUtc);

        // Filtro global para soft delete
        builder.HasQueryFilter(c => c.DeletedAtUtc == null);
    }
}
