# Data Model: Retomada de Sessao e Bot Telegram

**Date**: 2026-04-10  
**Feature**: 002-session-resume-telegram-bot

---

## Entities

### ChatSession (Modificada)

Entidade existente que ganha um novo campo.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| ChatSessionId | bigint | PK, identity | Existente |
| AgentId | bigint? | FK -> agents, CASCADE | Existente |
| UserName | varchar(260) | nullable | Existente |
| UserEmail | varchar(260) | nullable | Existente |
| UserPhone | varchar(50) | nullable | Existente |
| **ResumeToken** | **varchar(32)** | **unique, not null** | **NOVO - GUID sem hifens, gerado na criacao** |
| StartedAt | timestamp | not null | Existente |
| EndedAt | timestamp | nullable | Existente |

**Indice novo**: `ix_avachat_chat_sessions_resume_token` UNIQUE em `resume_token`

**Regras**:
- ResumeToken e gerado automaticamente na criacao da sessao via `Guid.NewGuid().ToString("N")`
- ResumeToken e imutavel apos criacao
- Busca por ResumeToken deve usar o indice unico para performance

---

### TelegramChat (Nova)

Mapeia um chat do Telegram para uma sessao ativa do Avachat.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| TelegramChatId | bigint | PK (nao identity, valor do Telegram) | ID do chat fornecido pelo Telegram |
| AgentId | bigint | FK -> agents, CASCADE | Agente associado a este chat |
| ChatSessionId | bigint | FK -> sessions, CASCADE | Sessao ativa atual |
| TelegramUsername | varchar(260) | nullable | Username do Telegram (se disponivel) |
| TelegramFirstName | varchar(260) | nullable | Primeiro nome do usuario |
| CreatedAt | timestamp | not null | Data de criacao do registro |
| UpdatedAt | timestamp | not null | Data da ultima atualizacao |

**Tabela**: `avachat_telegram_chats`  
**PK Constraint**: `avachat_telegram_chats_pkey`  
**FK**: `fk_telegram_chat_agent` -> `avachat_agents(agent_id)`  
**FK**: `fk_telegram_chat_session` -> `avachat_chat_sessions(chat_session_id)`

**Regras**:
- Cada `telegram_chat_id` mapeia para exatamente uma sessao ativa por vez
- Quando /start e enviado, uma nova sessao e criada e o `chat_session_id` e atualizado
- O registro anterior e preservado (update, nao delete + insert)

---

### ChatMessage (Sem Alteracao)

Entidade existente, sem modificacoes.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| ChatMessageId | bigint | PK, identity | |
| ChatSessionId | bigint? | FK -> sessions, CASCADE | |
| SenderType | integer | not null (0=User, 1=Assistant) | |
| Content | text | not null | |
| CreatedAt | timestamp | not null | |

---

## Relationships

```
Agent (1) ----< (N) ChatSession
ChatSession (1) ----< (N) ChatMessage
Agent (1) ----< (N) TelegramChat
ChatSession (1) ----< (N) TelegramChat  [1 ativa por TelegramChat]
```

---

## State Transitions

### TelegramChat Session Lifecycle

```
[Novo usuario] -> /start -> Cria sessao + Cria TelegramChat -> [Ativo]
[Usuario existente] -> /start -> Cria nova sessao + Atualiza chat_session_id -> [Ativo com nova sessao]
[Usuario existente] -> mensagem -> Usa sessao ativa do TelegramChat -> [Ativo]
```

---

## Migration Notes

- Adicionar coluna `resume_token` a `avachat_chat_sessions` com valor default para registros existentes (gerar GUIDs para sessoes existentes via SQL)
- Criar tabela `avachat_telegram_chats`
- Criar indice unico em `resume_token`
