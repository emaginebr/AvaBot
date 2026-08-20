# Start Session API Contract

**Branch**: `006-chat-start-session` | **Date**: 2026-04-08

## POST /api/agents/{slug}/sessions

### Request

```json
{
  "userName": "Maria",
  "userEmail": "maria@email.com",
  "userPhone": "11999998888"
}
```

### Response 201 Created

```json
{
  "sucesso": true,
  "mensagem": "Sessao iniciada com sucesso",
  "erros": [],
  "dados": {
    "chatSessionId": 123,
    "agentId": 456,
    "userName": "Maria",
    "userEmail": "maria@email.com",
    "userPhone": "11999998888",
    "startedAt": "2026-04-08T10:30:00Z",
    "endedAt": null,
    "messageCount": 0
  }
}
```

### Response 400 Bad Request

```json
{
  "sucesso": false,
  "mensagem": "Campos obrigatorios nao preenchidos",
  "erros": ["userName e obrigatorio para este agente"]
}
```

### Response 404 Not Found

```json
{
  "sucesso": false,
  "mensagem": "Agente nao encontrado",
  "erros": []
}
```

## WebSocket Connection

Apos obter `chatSessionId`, conectar:

```
ws://{VITE_WS_URL}/ws/chat/{slug}?sessionId={chatSessionId}
```

Servidor envia `{ type: "ready" }` imediatamente.
