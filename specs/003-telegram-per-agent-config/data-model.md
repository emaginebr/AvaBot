# Data Model: Configuracao Telegram por Agente

**Date**: 2026-04-11

## Entity Changes

### Agent (atualizado)

**Table**: `avabot_agents`

#### New Columns

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `telegram_bot_name` | `varchar(260)` | Yes | NULL | Nome do bot Telegram para exibicao |
| `telegram_bot_token` | `varchar(260)` | Yes | NULL | Token do bot obtido no BotFather |
| `telegram_webhook_secret` | `varchar(260)` | Yes | NULL | Secret para validar requests do webhook |

#### New Indexes

| Index Name | Column(s) | Type | Filter |
|-----------|-----------|------|--------|
| `ix_avabot_agents_telegram_bot_token` | `telegram_bot_token` | Unique | WHERE `telegram_bot_token` IS NOT NULL |

#### C# Properties (adicionadas ao Agent.cs)

```csharp
public string? TelegramBotName { get; set; }
public string? TelegramBotToken { get; set; }
public string? TelegramWebhookSecret { get; set; }
```

#### Fluent API Configuration (adicionada ao OnModelCreating)

```csharp
entity.Property(e => e.TelegramBotName).HasColumnName("telegram_bot_name").HasMaxLength(260);
entity.Property(e => e.TelegramBotToken).HasColumnName("telegram_bot_token").HasMaxLength(260);
entity.Property(e => e.TelegramWebhookSecret).HasColumnName("telegram_webhook_secret").HasMaxLength(260);
entity.HasIndex(e => e.TelegramBotToken)
    .IsUnique()
    .HasDatabaseName("ix_avabot_agents_telegram_bot_token")
    .HasFilter("telegram_bot_token IS NOT NULL");
```

### TelegramChat (sem alteracoes)

**Table**: `avabot_telegram_chats` - Nenhuma alteracao necessaria.

## Migration SQL (referencia)

```sql
ALTER TABLE avabot_agents ADD COLUMN telegram_bot_name varchar(260);
ALTER TABLE avabot_agents ADD COLUMN telegram_bot_token varchar(260);
ALTER TABLE avabot_agents ADD COLUMN telegram_webhook_secret varchar(260);

CREATE UNIQUE INDEX ix_avabot_agents_telegram_bot_token
    ON avabot_agents (telegram_bot_token)
    WHERE telegram_bot_token IS NOT NULL;
```

## DTO Changes

### AgentInfo (response - adicionar campos)

```csharp
[JsonPropertyName("telegramBotName")]
public string? TelegramBotName { get; set; }

[JsonPropertyName("telegramBotToken")]
public string? TelegramBotToken { get; set; }

[JsonPropertyName("telegramWebhookSecret")]
public string? TelegramWebhookSecret { get; set; }
```

### AgentInsertInfo (input - adicionar campos opcionais)

```csharp
[JsonPropertyName("telegramBotName")]
public string? TelegramBotName { get; set; }

[JsonPropertyName("telegramBotToken")]
public string? TelegramBotToken { get; set; }
```

> Nota: `TelegramWebhookSecret` NAO entra no InsertInfo - e gerado automaticamente pelo sistema.

### TelegramWebhookInfo (novo DTO)

```csharp
public class TelegramWebhookInfo
{
    [JsonPropertyName("agentId")]
    public long AgentId { get; set; }

    [JsonPropertyName("agentSlug")]
    public string AgentSlug { get; set; } = string.Empty;

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }

    [JsonPropertyName("isConfigured")]
    public bool IsConfigured { get; set; }
}
```

## Relationships

```
Agent (1) ----< (N) TelegramChat     [existente, sem alteracao]
Agent (1) ----< (N) ChatSession      [existente, sem alteracao]
Agent (1) ----< (N) KnowledgeFile    [existente, sem alteracao]
```

Nenhum novo relacionamento introduzido. Os campos Telegram sao atributos do Agent.
