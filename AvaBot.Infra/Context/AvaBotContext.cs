using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Domain.Enums;

namespace AvaBot.Infra.Context;

public class AvaBotContext : DbContext
{
    public AvaBotContext(DbContextOptions<AvaBotContext> options) : base(options) { }

    public DbSet<Agent> Agents { get; set; }
    public DbSet<KnowledgeFile> KnowledgeFiles { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<TelegramChat> TelegramChats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Agent
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.ToTable("avabot_agents");
            entity.HasKey(e => e.AgentId).HasName("avabot_agents_pkey");
            entity.Property(e => e.AgentId).HasColumnName("agent_id").UseIdentityAlwaysColumn();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(260).IsRequired();
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(260).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("avabot_agents_slug_key");
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.SystemPrompt).HasColumnName("system_prompt").HasColumnType("text").IsRequired();
            entity.Property(e => e.ChatModel).HasColumnName("chat_model").HasMaxLength(100).HasDefaultValue("gpt-4o").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(1).IsRequired();
            entity.Property(e => e.CollectName).HasColumnName("collect_name").HasDefaultValue(false).IsRequired();
            entity.Property(e => e.CollectEmail).HasColumnName("collect_email").HasDefaultValue(false).IsRequired();
            entity.Property(e => e.CollectPhone).HasColumnName("collect_phone").HasDefaultValue(false).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(e => e.TelegramBotName).HasColumnName("telegram_bot_name").HasMaxLength(260);
            entity.Property(e => e.TelegramBotToken).HasColumnName("telegram_bot_token").HasMaxLength(260);
            entity.Property(e => e.TelegramWebhookSecret).HasColumnName("telegram_webhook_secret").HasMaxLength(260);
            entity.HasIndex(e => e.TelegramBotToken)
                .IsUnique()
                .HasDatabaseName("ix_avabot_agents_telegram_bot_token")
                .HasFilter("telegram_bot_token IS NOT NULL");
        });

        // KnowledgeFile
        modelBuilder.Entity<KnowledgeFile>(entity =>
        {
            entity.ToTable("avabot_knowledge_files");
            entity.HasKey(e => e.KnowledgeFileId).HasName("avabot_knowledge_files_pkey");
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
                .HasConstraintName("avabot_fk_agents_knowledge_files")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatSession
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("avabot_chat_sessions");
            entity.HasKey(e => e.ChatSessionId).HasName("avabot_chat_sessions_pkey");
            entity.Property(e => e.ChatSessionId).HasColumnName("chat_session_id").UseIdentityAlwaysColumn();
            entity.Property(e => e.AgentId).HasColumnName("agent_id");
            entity.Property(e => e.UserName).HasColumnName("user_name").HasMaxLength(260);
            entity.Property(e => e.UserEmail).HasColumnName("user_email").HasMaxLength(260);
            entity.Property(e => e.UserPhone).HasColumnName("user_phone").HasMaxLength(50);
            entity.Property(e => e.StartedAt).HasColumnName("started_at").HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(e => e.ResumeToken).HasColumnName("resume_token").HasMaxLength(32).IsRequired();
            entity.HasIndex(e => e.ResumeToken).IsUnique().HasDatabaseName("ix_avabot_chat_sessions_resume_token");
            entity.Property(e => e.EndedAt).HasColumnName("ended_at").HasColumnType("timestamp without time zone");

            entity.HasOne(e => e.Agent)
                .WithMany(a => a.ChatSessions)
                .HasForeignKey(e => e.AgentId)
                .HasConstraintName("avabot_fk_agents_chat_sessions")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TelegramChat
        modelBuilder.Entity<TelegramChat>(entity =>
        {
            entity.ToTable("avabot_telegram_chats");
            entity.HasKey(e => e.TelegramChatId).HasName("avabot_telegram_chats_pkey");
            entity.Property(e => e.TelegramChatId).HasColumnName("telegram_chat_id").ValueGeneratedNever();
            entity.Property(e => e.AgentId).HasColumnName("agent_id").IsRequired();
            entity.Property(e => e.ChatSessionId).HasColumnName("chat_session_id").IsRequired();
            entity.Property(e => e.TelegramUsername).HasColumnName("telegram_username").HasMaxLength(260);
            entity.Property(e => e.TelegramFirstName).HasColumnName("telegram_first_name").HasMaxLength(260);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone").IsRequired();

            entity.HasOne(e => e.Agent)
                .WithMany()
                .HasForeignKey(e => e.AgentId)
                .HasConstraintName("avabot_fk_telegram_chat_agent")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChatSession)
                .WithMany()
                .HasForeignKey(e => e.ChatSessionId)
                .HasConstraintName("avabot_fk_telegram_chat_session")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("avabot_chat_messages");
            entity.HasKey(e => e.ChatMessageId).HasName("avabot_chat_messages_pkey");
            entity.Property(e => e.ChatMessageId).HasColumnName("chat_message_id").UseIdentityAlwaysColumn();
            entity.Property(e => e.ChatSessionId).HasColumnName("chat_session_id");
            entity.Property(e => e.SenderType).HasColumnName("sender_type").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").HasColumnType("text").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").IsRequired();

            entity.HasOne(e => e.ChatSession)
                .WithMany(s => s.ChatMessages)
                .HasForeignKey(e => e.ChatSessionId)
                .HasConstraintName("avabot_fk_chat_sessions_chat_messages")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
