# API Contracts: Telegram Endpoints

**Date**: 2026-04-11

## Endpoints Modificados

### POST /api/telegram/{slug}/webhook (refatorado)

Recebe updates do Telegram para o bot do agente identificado pelo slug.

**Auth**: AllowAnonymous (validacao via header secret)
**Route change**: De `POST /telegram/webhook` para `POST /api/telegram/{slug}/webhook`

**Headers**:
| Header | Required | Description |
|--------|----------|-------------|
| `X-Telegram-Bot-Api-Secret-Token` | Yes | Deve corresponder ao TelegramWebhookSecret do agente |

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | string | Slug do agente |

**Body**: `Telegram.Bot.Types.Update` (definido pelo pacote Telegram.Bot)

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | empty | Mensagem processada com sucesso |
| 200 | empty | Erro no processamento (retorna 200 para evitar retries do Telegram) |
| 401 | empty | Secret invalido ou agente nao encontrado/inativo |

---

## Endpoints Novos

### POST /api/telegram/{id}/setup-webhook

Registra o webhook do agente na API do Telegram.

**Auth**: `[Authorize]`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | long | AgentId |

**Body**: Nenhum

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | `Result<TelegramWebhookInfo>` | Webhook registrado com sucesso |
| 400 | `Result<object>` | Agente sem TelegramBotToken configurado |
| 404 | `Result<object>` | Agente nao encontrado |
| 500 | `Result<object>` | Erro ao registrar no Telegram (token invalido, etc.) |

**Response example (200)**:
```json
{
  "sucesso": true,
  "mensagem": "Webhook registrado com sucesso",
  "erros": [],
  "dados": {
    "agentId": 1,
    "agentSlug": "assistente-vendas",
    "webhookUrl": "https://avabot.net/api/telegram/assistente-vendas/webhook",
    "isConfigured": true
  }
}
```

---

### GET /api/telegram/{id}/webhook-info

Consulta a configuracao atual do webhook no Telegram para o bot do agente.

**Auth**: `[Authorize]`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | long | AgentId |

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | `Result<TelegramWebhookInfo>` | Informacao retornada |
| 400 | `Result<object>` | Agente sem TelegramBotToken configurado |
| 404 | `Result<object>` | Agente nao encontrado |
| 500 | `Result<object>` | Erro ao consultar API do Telegram |

**Response example (200)**:
```json
{
  "sucesso": true,
  "mensagem": "Informacao do webhook obtida com sucesso",
  "erros": [],
  "dados": {
    "agentId": 1,
    "agentSlug": "assistente-vendas",
    "webhookUrl": "https://avabot.net/api/telegram/assistente-vendas/webhook",
    "isConfigured": true
  }
}
```

---

### POST /api/telegram/{id}/regenerate-secret

Gera um novo webhook secret para o agente e atualiza o webhook no Telegram.

**Auth**: `[Authorize]`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | long | AgentId |

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | `Result<TelegramWebhookInfo>` | Secret regenerado e webhook atualizado |
| 400 | `Result<object>` | Agente sem TelegramBotToken configurado |
| 404 | `Result<object>` | Agente nao encontrado |
| 500 | `Result<object>` | Erro ao atualizar no Telegram |

---

## Endpoints Removidos

### POST /telegram/setup-webhook (removido)

Substituido por `POST /api/telegram/{id}/setup-webhook`.

### POST /telegram/webhook (removido)

Substituido por `POST /api/telegram/{slug}/webhook`.
