# Data Model: Área Administrativa Bot Telegram

**Date**: 2026-04-11  
**Feature**: 009-telegram-bot-admin

## Entidades

### AgentInfo (extensão — campos Telegram a adicionar no frontend)

Campos Telegram já existentes no backend (AgentInfo DTO), mas ausentes na interface TypeScript do frontend:

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| telegramBotName | string \| null | Não | Nome do bot no Telegram (deve terminar com "bot") |
| telegramBotToken | string \| null | Não | Token de autenticação do bot (do BotFather) |
| telegramWebhookSecret | string \| null | Não | Secret para validação do webhook (gerado pelo backend) |

**Validação**:
- `telegramBotName`: deve terminar com "bot" (case-insensitive), regex `/bot$/i`
- `telegramBotToken`: formato livre, campo sensível (mostrar/ocultar)
- `telegramWebhookSecret`: somente leitura no frontend

### AgentInsertInfo (extensão — campos para update)

Campos já aceitos pelo backend no PUT /agents/{id}:

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| telegramBotName | string \| null | Não | Nome do bot |
| telegramBotToken | string \| null | Não | Token do bot |

**Nota**: `telegramWebhookSecret` NÃO é enviado no update — é gerenciado pelo backend.

### TelegramWebhookInfo (novo tipo no frontend)

Resposta dos endpoints de webhook:

| Campo | Tipo | Descrição |
|-------|------|-----------|
| agentId | number | ID do agente |
| agentSlug | string | Slug do agente |
| webhookUrl | string \| null | URL do webhook registrada |
| isConfigured | boolean | Se o webhook está configurado |

## Relacionamentos

```
Agent 1:1 Configuração Telegram (campos no próprio Agent)
Agent 1:N TelegramWebhookInfo (resultado de operações, não persistido no frontend)
```

## Estados

### Estado da Configuração Telegram

```
[Não Configurado] → (preenche nome + token + salva) → [Configurado sem Webhook]
[Configurado sem Webhook] → (clica Configurar Webhook) → [Webhook Ativo]
[Webhook Ativo] → (clica Regenerar Secret) → [Webhook Ativo com novo secret]
[Webhook Ativo] → (altera token + salva) → [Configurado sem Webhook] (webhook anterior pode ficar inválido)
```
