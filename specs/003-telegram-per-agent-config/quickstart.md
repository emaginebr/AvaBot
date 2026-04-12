# Quickstart: Configuracao Telegram por Agente

**Date**: 2026-04-11

## Pre-requisitos

- .NET 9.0 SDK
- PostgreSQL rodando localmente ou via connection string
- Projeto compilando (`dotnet build`)

## Ordem de Implementacao

### 1. Domain Layer (AvaBot.Domain)

Adicionar propriedades Telegram ao `Agent.cs`:
- `TelegramBotName` (string?, nullable)
- `TelegramBotToken` (string?, nullable)
- `TelegramWebhookSecret` (string?, nullable)

### 2. DTO Layer (AvaBot.DTO)

- Adicionar campos Telegram ao `AgentInfo` e `AgentInsertInfo`
- Criar `TelegramWebhookInfo` DTO

### 3. Infrastructure Layer (AvaBot.Infra)

- Atualizar `AvaBotContext.OnModelCreating()` com Fluent API para as 3 novas colunas
- Adicionar unique filtered index em `telegram_bot_token`
- Gerar migration EF Core: `dotnet ef migrations add AddTelegramFieldsToAgent`
- Adicionar metodo `GetByTelegramBotTokenAsync` ao `AgentRepository`

### 4. Interface Layer (AvaBot.Infra.Interfaces)

- Adicionar `GetByTelegramBotTokenAsync` ao `IAgentRepository`

### 5. Application Layer (AvaBot.Application)

- Atualizar `AgentProfile` (AutoMapper) para mapear novos campos
- Atualizar `AgentService`:
  - Validar unicidade do TelegramBotToken no Create/Update
  - Gerar TelegramWebhookSecret automaticamente quando token for configurado
- Refatorar `TelegramService`:
  - Remover dependencia de `IConfiguration` para config Telegram
  - Remover dependencia de `ITelegramBotClient` (singleton)
  - Resolver agent por slug via repositorio
  - Criar `TelegramBotClient` dinamicamente com token do agent
  - Novos metodos: `SetupWebhookAsync(long agentId)`, `GetWebhookInfoAsync(long agentId)`, `RegenerateSecretAsync(long agentId)`
- Atualizar `DependencyInjection.cs`:
  - Remover registro singleton de `ITelegramBotClient`

### 6. API Layer (AvaBot.API)

- Refatorar `TelegramController`:
  - Nova rota: `POST /api/telegram/{slug}/webhook`
  - Nova rota: `POST /api/telegram/{id}/setup-webhook`
  - Nova rota: `GET /api/telegram/{id}/webhook-info`
  - Nova rota: `POST /api/telegram/{id}/regenerate-secret`
  - Remover rotas antigas
- Remover secao `Telegram` dos appsettings

### 7. Cleanup

- Remover variaveis TELEGRAM_* do `.env.example`
- Atualizar `docker-compose.yml` (remover env vars Telegram)
- Atualizar collection Bruno com novas rotas

## Comandos Uteis

```bash
# Gerar migration
cd AvaBot.Infra
dotnet ef migrations add AddTelegramFieldsToAgent --startup-project ../AvaBot.API

# Aplicar migration
dotnet ef database update --startup-project ../AvaBot.API

# Build
dotnet build

# Run
cd AvaBot.API
dotnet run
```

## Verificacao Rapida

1. Criar/atualizar agente com TelegramBotToken via API
2. Verificar que TelegramWebhookSecret foi gerado automaticamente
3. Chamar `POST /api/telegram/{id}/setup-webhook`
4. Chamar `GET /api/telegram/{id}/webhook-info` e verificar URL
5. Enviar `/start` no bot e verificar resposta
