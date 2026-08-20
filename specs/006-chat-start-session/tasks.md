# Tasks: Fluxo Start Session no Chat Widget

**Input**: Design documents from `/specs/006-chat-start-session/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/start-session-api.md, research.md, quickstart.md

**Tests**: Nao solicitados — testes manuais via quickstart.md.

**Organization**: Tasks agrupadas por user story.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

**Purpose**: Adicionar tipo e servico necessarios para o start-session.

- [x] T001 [P] Adicionar interface ChatSessionStartInfo em src/types/chatSession.ts com campos: userName?: string, userEmail?: string, userPhone?: string
- [x] T002 [P] Adicionar metodo startSession(slug: string, data: ChatSessionStartInfo) em src/Services/AgentService.ts que faz POST para /api/agents/{slug}/sessions e retorna Result<ChatSessionInfo>. Importar ChatSessionStartInfo e ChatSessionInfo com import type.

**Checkpoint**: Tipos e servico prontos para uso

---

## Phase 2: Foundational (Simplificar useChat)

**Purpose**: Remover logica de collect_data/identify do useChat, pois a coleta agora e feita pelo frontend via REST.

- [x] T003 Simplificar src/hooks/useChat.ts: (1) remover o estado fieldsToCollect e a funcao identify do retorno, (2) remover o case 'collect_data' do handleMessage, (3) manter apenas: chunk, done, error, ready. O hook agora so recebe wsUrl e retorna: messages, streaming, ready, error, sendMessage.

**Checkpoint**: useChat simplificado, sem logica de coleta

---

## Phase 3: User Story 1+2+3 — Novo fluxo completo (Priority: P1)

**Goal**: Refatorar useChatWidget para o novo fluxo: greeting → coleta conversacional → start-session REST → WebSocket com sessionId.

**Independent Test**: Abrir chat, responder nome/email/telefone, verificar que start-session e chamado, mensagem de confirmacao aparece, e chat funciona via WebSocket.

### Implementation

- [x] T004 [US1][US2][US3] Refatorar src/hooks/useChatWidget.ts completamente. O hook agora recebe (slug: string, greeting: string, config: AgentChatConfigInfo | null). Novo fluxo: (1) Estado inicial phase='greeting' com mensagem de greeting. (2) Ao receber primeira mensagem do usuario OU automaticamente se nao ha campos, transiciona para coleta. (3) Fase 'collecting': pergunta campos na sequencia exata — nome: "Qual seu nome?", email: "Bem vindo {nome}, qual seu email?" (ou "Qual seu email?" se nome nao foi coletado), telefone: "Qual seu numero de telefone? Com DDD". Validar email (contem @ e .) e telefone (regex numerico >=10 digitos). (4) Fase 'starting': chama AgentService.startSession(slug, collectedData). Se sucesso, adiciona mensagem "Muito obrigado pelas informacoes, agora em que posso ajudar?" e seta wsUrl com ?sessionId={chatSessionId}. Se erro, exibe mensagem de erro. (5) Fase 'ready': delega mensagens para useChat. (6) Se config tem 0 campos de coleta, pula direto de greeting para starting. (7) Retorna: messages, streaming, ready, error, isCollecting, sendMessage, wsUrl.
- [x] T005 [US1][US2][US3] Adaptar src/components/chat/ChatWidget.tsx: (1) Nao conectar WebSocket no handleOpen — apenas abrir o widget. (2) Passar config (com campos de coleta) para useChatWidget em vez de wsUrl. (3) useChatWidget retorna wsUrl quando sessao for criada — passar para useChat internamente. (4) Bloquear input (disabled) enquanto phase for 'starting'. (5) Manter agentName no header do widget.

**Checkpoint**: Fluxo completo funcionando — greeting → coleta → start-session → chat

---

## Phase 4: User Story 4 — Fluxo sem coleta (Priority: P2)

**Goal**: Se nenhum campo de coleta e necessario, start-session e chamado automaticamente.

**Independent Test**: Desativar todos os campos de coleta no agente, abrir chat, verificar que sessao inicia automaticamente apos boas-vindas.

### Implementation

- [x] T006 [US4] Verificar em src/hooks/useChatWidget.ts que o fluxo sem coleta funciona: quando config tem collectName=false, collectEmail=false, collectPhone=false, o hook deve transicionar de 'greeting' para 'starting' automaticamente (sem esperar input do usuario), chamar startSession e exibir mensagem de confirmacao. Ajustar se necessario.

**Checkpoint**: Fluxo sem coleta funciona automaticamente

---

## Phase 5: Polish & Cross-Cutting

**Purpose**: Limpeza e validacao final.

- [x] T007 [P] Remover imports e referencias nao utilizados: (1) em src/hooks/useChat.ts remover fieldsToCollect e identify se ainda presentes no tipo de retorno, (2) em src/pages/chat/ChatPage.tsx atualizar para usar o novo useChat sem fieldsToCollect/identify (adaptar o fluxo legado se necessario ou usar o novo fluxo tambem para esta pagina)
- [x] T008 Verificar build (tsc --noEmit e vite build) e testar fluxo completo conforme quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependencias — T001 e T002 em paralelo
- **Foundational (Phase 2)**: Depende de Phase 1
- **US1+2+3 (Phase 3)**: Depende de Phase 2
- **US4 (Phase 4)**: Depende de Phase 3
- **Polish (Phase 5)**: Depende de todas as fases anteriores

### Parallel Opportunities

- T001 e T002 podem rodar em paralelo (arquivos diferentes)

---

## Implementation Strategy

### MVP First

1. Phase 1: Setup (T001, T002)
2. Phase 2: Simplificar useChat (T003)
3. Phase 3: Refatorar useChatWidget + ChatWidget (T004, T005)
4. **STOP and VALIDATE**: Testar fluxo completo
5. Phase 4: Fluxo sem coleta (T006)
6. Phase 5: Polish (T007, T008)
