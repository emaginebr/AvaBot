# Data Model: 008-session-resume-cookies

**Date**: 2026-04-10  
**Branch**: `008-session-resume-cookies`

## Entities

### Cookie de Sessão (Frontend — Browser Cookies)

Conjunto de cookies separados por agente (slug), armazenados no navegador.

| Cookie Name Pattern           | Type   | Description                                      | Expiry  |
|-------------------------------|--------|--------------------------------------------------|---------|
| `avabot_{slug}_resumeToken`   | string | Token único de 32 caracteres para retomar sessão  | 30 dias |
| `avabot_{slug}_userName`      | string | Nome do usuário coletado na identificação          | 30 dias |
| `avabot_{slug}_userEmail`     | string | Email do usuário (se coletado)                     | 30 dias |
| `avabot_{slug}_userPhone`     | string | Telefone do usuário (se coletado)                  | 30 dias |

**Regras de validação**:
- Todos os cookies devem ser verificados em conjunto: se `resumeToken` está ausente ou qualquer cookie essencial está corrompido, todos devem ser limpos.
- O `slug` no nome do cookie deve corresponder ao slug do agente ativo.

**Ciclo de vida**:
1. **Criação**: Após `startSession` bem-sucedido (primeiro acesso) ou após `startSession` ao iniciar nova conversa (retornante).
2. **Leitura**: Ao abrir o chat, para determinar se usuário é novo ou retornante.
3. **Atualização**: Quando nova sessão é criada para um retornante que escolheu "Iniciar nova conversa" — o `resumeToken` é atualizado, demais dados mantidos.
4. **Remoção**: Quando resumeSession falha (sessão expirada/inválida) — todos os cookies do agente são removidos.

### ChatSessionResumeInfo (Backend Response — já existente)

Dados retornados pelo endpoint `GET /sessions/resume/{slug}`.

| Field          | Type                | Description                                |
|----------------|---------------------|--------------------------------------------|
| chatSessionId  | number              | ID único da sessão                         |
| agentId        | number              | ID do agente vinculado                     |
| userName       | string (nullable)   | Nome do usuário                            |
| userEmail      | string (nullable)   | Email do usuário                           |
| userPhone      | string (nullable)   | Telefone do usuário                        |
| resumeToken    | string              | Token de retomada (32 chars)               |
| startedAt      | datetime            | Data/hora de início da sessão              |
| endedAt        | datetime (nullable) | Data/hora de encerramento (null se ativa)  |
| messageCount   | number              | Total de mensagens na sessão               |
| messages       | ChatMessageInfo[]   | Últimas 10 mensagens em ordem cronológica  |

### ChatMessageInfo (Backend Response — já existente)

| Field          | Type     | Description                           |
|----------------|----------|---------------------------------------|
| chatMessageId  | number   | ID único da mensagem                  |
| chatSessionId  | number   | ID da sessão vinculada                |
| senderType     | number   | 0 = Usuário, 1 = Assistente          |
| content        | string   | Conteúdo textual da mensagem          |
| createdAt      | datetime | Data/hora de criação                  |

### Estado do Widget (Frontend — Component State)

Novos estados necessários no hook `useChatWidget`.

| State              | Type                                                                    | Description                                    |
|--------------------|-------------------------------------------------------------------------|------------------------------------------------|
| phase              | 'greeting' \| 'collecting' \| 'starting' \| 'ready' \| **'returning'** | Nova fase para usuário retornante              |
| showActionButtons  | boolean                                                                 | Controla visibilidade dos botões de opção      |
| inputDisabled      | boolean                                                                 | Controla se o campo de entrada está desabilitado |

**Transições de estado**:

```
[Abrir chat]
    │
    ├── (sem cookies) → phase: 'greeting' → 'collecting' → 'starting' → 'ready'
    │
    └── (com cookies) → phase: 'returning'
                            │ showActionButtons: true, inputDisabled: true
                            │
                            ├── [Clique "Retomar"] → showActionButtons: false
                            │                       → inputDisabled: false
                            │                       → carregar mensagens + reconectar WS
                            │                       → phase: 'ready'
                            │
                            └── [Clique "Nova conversa"] → showActionButtons: false
                                                         → inputDisabled: false
                                                         → criar nova sessão (dados do cookie)
                                                         → phase: 'ready'
```

## Relationships

```
Browser Cookies ──(resumeToken)──► Backend Session
       │                                  │
       │ (userName, userEmail, userPhone)  │ (messages[])
       │                                  │
       └── Widget State (phase)           └── Chat Messages Display
```
