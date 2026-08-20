# Data Model: Landing Page com Chat Widget Bia

**Branch**: `005-landing-chat-widget` | **Date**: 2026-04-08

## Entidades Existentes (sem alteracao)

### AgentChatConfigInfo

Retornado por `GET /api/agents/{slug}/chat-config`:

| Campo | Tipo | Descricao |
|---|---|---|
| name | string | Nome do agente |
| description | string? | Descricao do agente |
| collectName | boolean | Coletar nome do usuario |
| collectEmail | boolean | Coletar email do usuario |
| collectPhone | boolean | Coletar telefone do usuario |

### ChatMessage (frontend)

Interface usada internamente pelo hook useChat:

| Campo | Tipo | Descricao |
|---|---|---|
| role | 'user' \| 'assistant' | Remetente da mensagem |
| content | string | Conteudo da mensagem |

### WebSocket Messages

**Client → Server**:

| type | Campos extras | Quando |
|---|---|---|
| identify | name?, email?, phone? | Apos coleta de dados |
| message | content | Mensagem do usuario |

**Server → Client**:

| type | Campos extras | Quando |
|---|---|---|
| collect_data | fields: string[] | Ao conectar (se necessario) |
| ready | — | Sessao pronta |
| chunk | content: string | Token de resposta |
| done | — | Resposta completa |
| error | message: string | Erro no servidor |

## Estado do Widget (novo)

### ChatWidgetState

Estado interno gerenciado pelo componente ChatWidget:

| Campo | Tipo | Descricao |
|---|---|---|
| isOpen | boolean | Widget aberto ou fechado |
| config | AgentChatConfigInfo \| null | Config do agente |
| status | 'loading' \| 'ok' \| 'error' \| 'unavailable' | Estado do carregamento |

### ConversationalCollectState

Estado para coleta conversacional de dados:

| Campo | Tipo | Descricao |
|---|---|---|
| phase | 'idle' \| 'collecting' \| 'done' | Fase da coleta |
| pendingFields | string[] | Campos ainda nao coletados |
| currentField | string \| null | Campo sendo perguntado agora |
| collectedData | { name?, email?, phone? } | Dados ja coletados |

### Transicoes de Estado da Coleta

```
idle → collecting (quando servidor envia collect_data)
collecting → collecting (quando usuario responde um campo, proximo campo)
collecting → done (quando todos os campos coletados, envia identify)
```

Se nenhum campo for requerido, o estado vai direto para `done`
(servidor envia `ready` imediatamente).
