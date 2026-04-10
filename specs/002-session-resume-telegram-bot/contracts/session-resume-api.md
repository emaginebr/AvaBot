# API Contract: Session Resume

**Date**: 2026-04-10

---

## GET /sessions/resume/{slug}

Recupera a ultima sessao de um agente usando o resume token.

**Authentication**: Nenhuma (AllowAnonymous)

**Headers**:
- `X-Resume-Token: {resume_token}` (obrigatorio)

**Path Parameters**:
- `slug` (string): Slug do agente

**Response 200 OK**:
```json
{
  "sucesso": true,
  "mensagem": "",
  "erros": [],
  "dados": {
    "chatSessionId": 123,
    "agentId": 1,
    "userName": "Joao",
    "userEmail": "joao@email.com",
    "userPhone": "11999999999",
    "resumeToken": "a1b2c3d4e5f6...",
    "startedAt": "2026-04-10T10:00:00",
    "endedAt": null,
    "messageCount": 15,
    "messages": [
      {
        "chatMessageId": 140,
        "chatSessionId": 123,
        "senderType": 0,
        "content": "Ola, preciso de ajuda",
        "createdAt": "2026-04-10T10:05:00"
      }
    ]
  }
}
```

**Response 404 Not Found** (token invalido ou sessao nao encontrada):
```json
{
  "sucesso": false,
  "mensagem": "Sessao nao encontrada",
  "erros": [],
  "dados": null
}
```

**Notes**:
- Retorna as ultimas 10 mensagens no campo `messages`
- `messageCount` contem o total real de mensagens na sessao
- O campo `resumeToken` e incluido na resposta para que o cliente possa armazena-lo

---

## POST /sessions/agents/{slug} (Modificado)

Criacao de sessao existente, agora retorna `resumeToken`.

**Response 201 Created** (adicionado campo):
```json
{
  "sucesso": true,
  "mensagem": "Sessao criada com sucesso",
  "erros": [],
  "dados": {
    "chatSessionId": 124,
    "agentId": 1,
    "userName": "Maria",
    "userEmail": "maria@email.com",
    "userPhone": null,
    "resumeToken": "f7e8d9c0b1a2...",
    "startedAt": "2026-04-10T11:00:00",
    "endedAt": null,
    "messageCount": 0
  }
}
```

---

## WebSocket /ws/chat/{slug} (Modificado)

Evento `ready` agora inclui `resumeToken`.

**Server -> Client (ready)**:
```json
{
  "type": "ready",
  "sessionId": 124,
  "resumeToken": "f7e8d9c0b1a2..."
}
```
