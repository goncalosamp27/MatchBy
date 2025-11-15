using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchBy.Data.Configurations;

public class ChatMessageConfiguration: IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasMaxLength(500)
            .IsRequired();

        builder.OwnsOne(u => u.Location);
        builder.Property(c => c.Content)
            .HasMaxLength(500);
        
        builder.Property(c => c.InviteUrl)
            .HasMaxLength(500);
        
        builder.Property(c => c.SenderId)
            .IsRequired();
        
        //MatchChatMessage is associated with one Sender
        builder.HasOne(c => c.Sender)
            .WithMany()
            .HasForeignKey(c => c.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        //MatchChatMessage can be associated with one ReplyToMessage
        builder.HasOne(c => c.ReplyToMessage)
            .WithMany()
            .HasForeignKey(c => c.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);
        
        //MatchChatMessage is associated with one Conversation
        builder.HasOne(c => c.Conversation)
            .WithMany()
            .HasForeignKey(c => c.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();
        
        builder.Property(i => i.UpdatedAtUtc);
        builder.Property(i => i.DeletedAtUtc);
        builder.HasQueryFilter(m => m.DeletedAtUtc == null);
    }
}
