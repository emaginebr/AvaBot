# API Contract: Telegram Webhook

**Date**: 2026-04-10

---

## POST /telegram/webhook

Endpoint que recebe updates do Telegram Bot API.

**Authentication**: Secret token via header

**Headers**:
- `X-Telegram-Bot-Api-Secret-Token: {webhook_secret}` (obrigatorio)

**Request Body**: Telegram Update object (JSON) conforme Telegram Bot API spec.

Exemplo (mensagem de texto):
```json
{
  "update_id": 123456789,
  "message": {
    "message_id": 100,
    "from": {
      "id": 987654321,
      "is_bot": false,
      "first_name": "Joao",
      "username": "joao123"
    },
    "chat": {
      "id": 987654321,
      "first_name": "Joao",
      "username": "joao123",
      "type": "private"
    },
    "date": 1712745600,
    "text": "Qual o horario de funcionamento?"
  }
}
```

**Response 200 OK**: Corpo vazio (Telegram espera apenas status 200)

**Response 401 Unauthorized** (secret token invalido ou ausente): Corpo vazio

**Behavior**:
1. Valida secret token no header
2. Deserializa Update do Telegram
3. Se mensagem contém `/start`: cria nova sessao, envia boas-vindas
4. Se mensagem e texto: processa via ChatService, envia resposta
5. Se mensagem nao e texto: envia aviso "apenas texto e aceito"
6. Retorna 200 OK em todos os casos de sucesso

---

## POST /telegram/setup-webhook

Registra o webhook no Telegram Bot API. Endpoint administrativo.

**Authentication**: Bearer token (Authorize)

**Request Body**: Nenhum

**Response 200 OK**:
```json
{
  "sucesso": true,
  "mensagem": "Webhook registrado com sucesso",
  "erros": [],
  "dados": {
    "url": "https://api.example.com/telegram/webhook",
    "hasCustomCertificate": false,
    "pendingUpdateCount": 0
  }
}
```

**Response 500 Error**:
```json
{
  "sucesso": false,
  "mensagem": "Erro ao registrar webhook",
  "erros": ["Detalhes do erro do Telegram"],
  "dados": null
}
```

**Behavior**:
1. Chama `setWebhook` na API do Telegram com:
   - `url`: URL publica do endpoint /telegram/webhook
   - `secret_token`: Valor de `TELEGRAM_WEBHOOK_SECRET`
   - `allowed_updates`: ["message"]
2. Retorna status do webhook
