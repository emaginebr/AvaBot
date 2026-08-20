# API Contract: Telegram Bot Admin

**Date**: 2026-04-11  
**Feature**: 009-telegram-bot-admin

## Endpoints Consumidos pelo Frontend

Todos os endpoints requerem header `Authorization: Basic {token}` via `AuthService.getAuthHeaders()`.

### 1. Salvar Configuração Telegram

Usa o endpoint existente de update do agente.

```
PUT /agents/{id}
Content-Type: application/json
Authorization: Basic {token}

Request Body (campos relevantes):
{
  "name": "string",
  "description": "string | null",
  "systemPrompt": "string",
  "collectName": boolean,
  "collectEmail": boolean,
  "collectPhone": boolean,
  "chatModel": "string",
  "telegramBotName": "string | null",
  "telegramBotToken": "string | null"
}

Response: Result<AgentInfo>
{
  "sucesso": true,
  "mensagem": "string",
  "erros": [],
  "dados": { ...AgentInfo com campos Telegram }
}
```

### 2. Configurar Webhook

```
POST /telegram/{agentId}/setup-webhook
Authorization: Basic {token}

Request Body: (nenhum)

Response 200: Result<TelegramWebhookInfo>
{
  "sucesso": true,
  "mensagem": "string",
  "erros": [],
  "dados": {
    "agentId": number,
    "agentSlug": "string",
    "webhookUrl": "string",
    "isConfigured": true
  }
}

Response 400: Token não configurado
Response 404: Agente não encontrado
Response 500: Erro na API do Telegram
```

### 3. Verificar Webhook

```
GET /telegram/{agentId}/webhook-info
Authorization: Basic {token}

Response 200: Result<TelegramWebhookInfo>
{
  "sucesso": true,
  "mensagem": "string",
  "erros": [],
  "dados": {
    "agentId": number,
    "agentSlug": "string",
    "webhookUrl": "string | null",
    "isConfigured": boolean
  }
}

Response 400: Token não configurado
Response 404: Agente não encontrado
```

### 4. Regenerar Webhook Secret

```
POST /telegram/{agentId}/regenerate-secret
Authorization: Basic {token}

Request Body: (nenhum)

Response 200: Result<TelegramWebhookInfo>
{
  "sucesso": true,
  "mensagem": "string",
  "erros": [],
  "dados": {
    "agentId": number,
    "agentSlug": "string",
    "webhookUrl": "string",
    "isConfigured": true
  }
}

Response 400: Token não configurado
Response 404: Agente não encontrado
Response 500: Erro ao regenerar
```

## Tipos TypeScript (a adicionar no frontend)

```typescript
interface TelegramWebhookInfo {
  agentId: number
  agentSlug: string
  webhookUrl: string | null
  isConfigured: boolean
}
```

Campos a adicionar em `AgentInfo`:
```typescript
telegramBotName: string | null
telegramBotToken: string | null
telegramWebhookSecret: string | null
```

Campos a adicionar em `AgentInsertInfo`:
```typescript
telegramBotName: string | null
telegramBotToken: string | null
```
