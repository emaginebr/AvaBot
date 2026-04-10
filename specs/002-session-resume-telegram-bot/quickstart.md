# Quickstart: Retomada de Sessao e Bot Telegram

**Date**: 2026-04-10  
**Feature**: 002-session-resume-telegram-bot

---

## Pre-requisitos

- .NET 9.0 SDK
- PostgreSQL 17
- Elasticsearch 8.17
- Token do Bot Telegram (obtido via @BotFather)
- URL publica HTTPS (para webhook do Telegram)

---

## Variaveis de Ambiente Novas

Adicionar ao `.env`:

```bash
# Telegram Bot
TELEGRAM_BOT_TOKEN=seu_token_aqui
TELEGRAM_WEBHOOK_SECRET=gere_um_guid_aqui
TELEGRAM_WEBHOOK_URL=https://seu-dominio.com/telegram/webhook
TELEGRAM_AGENT_SLUG=slug_do_agente_para_telegram
```

---

## Passos de Implementacao (Ordem)

### 1. Resume Token na Sessao

1. Adicionar campo `ResumeToken` ao modelo `ChatSession`
2. Criar migracao EF Core: `dotnet ef migrations add AddResumeToken`
3. Aplicar migracao: `dotnet ef database update`
4. Gerar tokens para sessoes existentes (SQL direto)
5. Atualizar `ChatSessionInfo` DTO com campo `ResumeToken`
6. Atualizar `ChatSessionProfile` (AutoMapper)
7. Atualizar `ChatService.CreateSessionAsync` para gerar token
8. Atualizar endpoint POST de criacao para retornar token
9. Atualizar WebSocket `ready` event para incluir token

### 2. Endpoint de Retomada

1. Adicionar metodo `GetByResumeTokenAsync` ao repository
2. Adicionar metodo `GetLastMessagesBySessionIdAsync(sessionId, count=10)` ao message repository
3. Criar endpoint GET `/sessions/resume/{slug}` no `SessionController`
4. Criar DTO `ChatSessionResumeInfo` com mensagens incluidas

### 3. Bot Telegram

1. Instalar pacote NuGet: `Telegram.Bot`
2. Criar modelo `TelegramChat` e configurar no DbContext
3. Criar migracao: `dotnet ef migrations add AddTelegramChat`
4. Criar `ITelegramChatRepository` e `TelegramChatRepository`
5. Criar `TelegramService` na Application layer
6. Criar `TelegramController` com endpoints webhook e setup
7. Registrar servicos no DI (Program.cs)
8. Registrar webhook via endpoint ou manualmente

### 4. Testes

1. Testes unitarios para geracao de resume token
2. Testes unitarios para TelegramService
3. Testes de integracao para endpoint de retomada
4. Testes de integracao para webhook

---

## Verificacao Rapida

```bash
# 1. Criar sessao e verificar resume token
curl -X POST http://localhost:5000/sessions/agents/meu-agente \
  -H "Content-Type: application/json" \
  -d '{"userName": "Teste"}'
# Resposta deve incluir "resumeToken"

# 2. Retomar sessao
curl http://localhost:5000/sessions/resume/meu-agente \
  -H "X-Resume-Token: token_retornado_acima"
# Resposta deve incluir sessao com ultimas 10 mensagens

# 3. Registrar webhook (requer auth)
curl -X POST http://localhost:5000/telegram/setup-webhook \
  -H "Authorization: Bearer seu_token_jwt"
# Deve retornar sucesso

# 4. Enviar mensagem ao bot no Telegram
# Abrir Telegram, buscar o bot, enviar /start
```
