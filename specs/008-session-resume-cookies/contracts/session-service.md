# Contract: Session Service (Frontend)

**Date**: 2026-04-10  
**Branch**: `008-session-resume-cookies`

## Novo método: resumeSession

**Propósito**: Chamar o endpoint de retomada de sessão do backend.

**Assinatura**:
```
resumeSession(slug: string, resumeToken: string) → Result<ChatSessionResumeInfo>
```

**Parâmetros**:
- `slug` (string): Identificador do agente
- `resumeToken` (string): Token de retomada armazenado no cookie

**Request**:
- Method: `GET`
- URL: `{API_URL}/sessions/resume/{slug}`
- Headers: `X-Resume-Token: {resumeToken}`, `Content-Type: application/json`

**Response (sucesso)**:
```json
{
  "sucesso": true,
  "mensagem": "...",
  "erros": [],
  "dados": {
    "chatSessionId": 123,
    "agentId": 1,
    "userName": "João",
    "userEmail": "joao@email.com",
    "userPhone": "11999999999",
    "resumeToken": "abc123...",
    "startedAt": "2026-04-10T10:00:00Z",
    "endedAt": null,
    "messageCount": 25,
    "messages": [
      {
        "chatMessageId": 100,
        "chatSessionId": 123,
        "senderType": 0,
        "content": "Olá",
        "createdAt": "2026-04-10T10:05:00Z"
      }
    ]
  }
}
```

**Response (falha)**:
```json
{
  "sucesso": false,
  "mensagem": "Sessão não encontrada ou expirada",
  "erros": ["Token inválido"],
  "dados": null
}
```

**Tratamento de erros**:
- Sessão não encontrada → limpar cookies, iniciar fluxo de primeiro acesso
- Erro de rede → exibir mensagem de erro, oferecer iniciar nova conversa

## Novo módulo: CookieService

**Propósito**: Gerenciar cookies de sessão por agente.

### setCookies

```
setCookies(slug: string, data: { resumeToken, userName, userEmail?, userPhone? }) → void
```

Armazena cookies com prefixo `avabot_{slug}_` e expiração de 30 dias.

### getCookies

```
getCookies(slug: string) → { resumeToken, userName, userEmail, userPhone } | null
```

Lê cookies do agente. Retorna `null` se `resumeToken` estiver ausente.

### clearCookies

```
clearCookies(slug: string) → void
```

Remove todos os cookies do agente especificado.

## Widget API pública (renomeada)

### Inicialização

```
window.Avabot.init(options: AvabotInitOptions)
```

**AvabotInitOptions**:
- `slug` (string, obrigatório): Slug do agente
- `greeting` (string, opcional): Mensagem de saudação
- `color` (string, opcional): Cor do tema
- `agentAvatar` (string, opcional): URL do avatar do agente
- `apiUrl` (string, opcional): URL da API
- `wsUrl` (string, opcional): URL do WebSocket
