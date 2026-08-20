# Data Model: Fluxo Start Session

**Branch**: `006-chat-start-session` | **Date**: 2026-04-08

## Entidades Novas

### ChatSessionStartInfo (request)

Enviado ao `POST /api/agents/{slug}/sessions`:

| Campo | Tipo | Obrigatorio | Descricao |
|---|---|---|---|
| userName | string | Condicional | Nome do usuario |
| userEmail | string | Condicional | Email do usuario |
| userPhone | string | Condicional | Telefone do usuario |

Campos sao obrigatorios conforme configuracao do agente
(collectName, collectEmail, collectPhone).

### ChatSessionInfo (response)

Retornado pelo start-session:

| Campo | Tipo | Descricao |
|---|---|---|
| chatSessionId | number | ID da sessao criada |
| agentId | number | ID do agente |
| userName | string? | Nome do usuario |
| userEmail | string? | Email do usuario |
| userPhone | string? | Telefone do usuario |
| startedAt | string | Data de inicio (ISO) |
| endedAt | string? | Sempre null ao criar |
| messageCount | number | Sempre 0 ao criar |

## Estado do Widget (refatorado)

### CollectState (atualizado)

| Campo | Tipo | Descricao |
|---|---|---|
| phase | 'greeting' \| 'collecting' \| 'starting' \| 'ready' | Fase atual |
| pendingFields | string[] | Campos ainda nao coletados |
| currentField | string \| null | Campo sendo perguntado |
| collectedData | { name?, email?, phone? } | Dados coletados |
| sessionId | number \| null | ID da sessao (apos start) |

### Transicoes de Estado

```
greeting → collecting (se ha campos para coletar)
greeting → starting (se nao ha campos para coletar)
collecting → collecting (proximo campo)
collecting → starting (todos os campos coletados, chama start-session)
starting → ready (start-session retornou sucesso, conecta WebSocket)
starting → error (start-session falhou)
```

## WebSocket URL (atualizado)

Antes: `ws://{VITE_WS_URL}/ws/chat/{slug}`
Agora: `ws://{VITE_WS_URL}/ws/chat/{slug}?sessionId={chatSessionId}`
