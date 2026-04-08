using Microsoft.EntityFrameworkCore;
using Avachat.Domain.Models;
using Avachat.Domain.Enums;

namespace Avachat.Infra.Context;

public class AvachatContext : DbContext
{
    public AvachatContext(DbContextOptions<AvachatContext> options) : base(options) { }

    public DbSet<Agent> Agents { get; set; }
    public DbSet<KnowledgeFile> KnowledgeFiles { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Agent
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.ToTable("agents");
            entity.HasKey(e => e.AgentId).HasName("agents_pkey");
            entity.Property(e => e.AgentId).HasColumnName("agent_id").UseIdentityAlwaysColumn();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(260).IsRequired();
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(260).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("agents_slug_key");
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.SystemPrompt).HasColumnName("system_prompt").HasColumnType("text").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(1).IsRequired();
            entity.Property(e => e.CollectName).HasColumnName("collect_name").HasDefaultValue(false).IsRequired();
            entity.Property(e => e.CollectEmail).HasColumnName("collect_email").HasDefaultValue(false).IsRequired();
            entity.Property(e => e.CollectPhone).HasColumnName("collect_phone").HasDefaultValue(false).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone").IsRequired();
        });

        // KnowledgeFile
        modelBuilder.Entity<KnowledgeFile>(entity =>
        {
            entity.ToTable("knowledge_files");
            entity.HasKey(e => e.KnowledgeFileId).HasName("knowledge_files_pkey");
            entity.Property(e => e.KnowledgeFileId).HasColumnName("knowledge_file_id").UseIdentityAlwaysColumn();
            entity.Property(e => e.AgentId).HasColumnName("agent_id");
            entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileContent).HasColumnName("file_content").HasColumnType("text").IsRequired();
            entity.Property(e => e.FileSize).HasColumnName("file_size").IsRequired();
            entity.Property(e => e.ProcessingStatus).HasColumnName("processing_status").HasDefaultValue(ProcessingStatus.Processing).IsRequired();
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone").IsRequired();

            entity.HasOne(e => e.Agent)
                .WithMany(a => a.KnowledgeFiles)
                .HasForeignKey(e => e.AgentId)
                .HasConstraintName("fk_agents_knowledge_files")
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ChatSession
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("chat_sessions");
            entity.HasKey(e => e.ChatSessionId).HasName("chat_sessions_pkey");
            entity.Property(e => e.ChatSessionId).HasColumnName("chat_session_id").UseIdentityAlwaysColumn();
            entity.Property(e => e.AgentId).HasColumnName("agent_id");
            entity.Property(e => e.UserName).HasColumnName("user_name").HasMaxLength(260);
            entity.Property(e => e.UserEmail).HasColumnName("user_email").HasMaxLength(260);
            entity.Property(e => e.UserPhone).HasColumnName("user_phone").HasMaxLength(50);
            entity.Property(e => e.StartedAt).HasColumnName("started_at").HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(e => e.EndedAt).HasColumnName("ended_at").HasColumnType("timestamp without time zone");

            entity.HasOne(e => e.Agent)
                .WithMany(a => a.ChatSessions)
                .HasForeignKey(e => e.AgentId)
                .HasConstraintName("fk_agents_chat_sessions")
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(e => e.ChatMessageId).HasName("chat_messages_pkey");
            entity.Property(e => e.ChatMessageId).HasColumnName("chat_message_id").UseIdentityAlwaysColumn();
            entity.Property(e => e.ChatSessionId).HasColumnName("chat_session_id");
            entity.Property(e => e.SenderType).HasColumnName("sender_type").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").HasColumnType("text").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").IsRequired();

            entity.HasOne(e => e.ChatSession)
                .WithMany(s => s.ChatMessages)
                .HasForeignKey(e => e.ChatSessionId)
                .HasConstraintName("fk_chat_sessions_chat_messages")
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}
