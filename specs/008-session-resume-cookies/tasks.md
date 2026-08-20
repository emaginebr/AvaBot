# Tasks: Renomeação para AvaBot + Gestão de Sessão com Cookies e Retomada de Conversa

**Input**: Design documents from `/specs/008-session-resume-cookies/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Não solicitado — tarefas de teste não incluídas.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Nenhuma configuração adicional necessária — projeto já existe e está funcional.

*(Sem tarefas nesta fase)*

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Criar infraestrutura compartilhada que TODAS as user stories de sessão necessitam (CookieService, tipos, método resumeSession no AgentService).

**⚠️ CRITICAL**: Nenhuma user story pode iniciar até esta fase estar completa.

- [x] T001 [P] Adicionar interface ChatSessionResumeInfo e ChatMessageInfo no tipo de retomada em src/types/chatSession.ts — incluir campos: chatSessionId, agentId, userName, userEmail, userPhone, resumeToken, startedAt, endedAt, messageCount, messages (array de ChatMessageInfo com chatMessageId, chatSessionId, senderType, content, createdAt)
- [x] T002 [P] Criar CookieService em src/Services/CookieService.ts — implementar métodos setCookies(slug, data), getCookies(slug), clearCookies(slug) com cookies nomeados como avabot_{slug}_resumeToken, avabot_{slug}_userName, avabot_{slug}_userEmail, avabot_{slug}_userPhone e expiração de 30 dias
- [x] T003 Adicionar método resumeSession(slug, resumeToken) no src/Services/AgentService.ts — fazer GET para {API_URL}/sessions/resume/{slug} com header X-Resume-Token, retornando Result<ChatSessionResumeInfo> conforme contrato em contracts/session-service.md

**Checkpoint**: Infraestrutura de cookies, tipos e API de retomada prontos — user stories podem iniciar.

---

## Phase 3: User Story 3 - Primeiro acesso com persistência em cookies (Priority: P1) 🎯 MVP

**Goal**: Ao completar o fluxo de identificação e criar sessão, salvar resumeToken e dados do usuário em cookies para reconhecimento futuro.

**Independent Test**: Acessar chat sem cookies → completar identificação → verificar que cookies avabot_{slug}_* foram criados com dados corretos.

### Implementation for User Story 3

- [x] T004 [US3] Modificar src/hooks/useChatWidget.ts — após startSession bem-sucedido (fase 'starting' → 'ready'), chamar CookieService.setCookies(slug, { resumeToken, userName, userEmail, userPhone }) com os dados retornados pela API. O startSession já retorna ChatSessionInfo que inclui o resumeToken — usar esse valor.
- [x] T005 [US3] Modificar src/hooks/useChatWidget.ts — ao inicializar o hook, chamar CookieService.getCookies(slug) para verificar se existem cookies. Se não existir (retorna null), manter fluxo atual (greeting → collecting → starting → ready). Se existir, transicionar para nova fase 'returning' (será implementada na US1).

**Checkpoint**: Primeiro acesso salva cookies. Retorno detecta cookies existentes. Base para US1 e US2.

---

## Phase 4: User Story 1 - Usuário retornante retoma conversa (Priority: P1)

**Goal**: Usuário retornante é reconhecido, vê saudação personalizada e botões, pode retomar última conversa com as 10 últimas mensagens carregadas.

**Independent Test**: Criar sessão (US3) → fechar/reabrir chat → verificar "Bem-vindo de volta, [nome]" → clicar "Desejo retomar nossa última conversa" → verificar 10 mensagens carregadas e chat funcional.

### Implementation for User Story 1

- [x] T006 [P] [US1] Criar componente ActionButtons em src/components/chat/ActionButtons.tsx — exibir dois botões estilizados com Tailwind: "Desejo retomar nossa última conversa" e "Iniciar uma nova conversa". Props: onSelect(optionText: string), disabled: boolean. Ao clicar, chamar onSelect com o texto exato do botão clicado.
- [x] T007 [US1] Implementar fase 'returning' em src/hooks/useChatWidget.ts — quando getCookies retorna dados válidos: (1) adicionar mensagem de bot "Bem-vindo de volta, {userName}" ao chat, (2) setar showActionButtons=true e inputDisabled=true, (3) expor handler handleActionSelect que: adiciona texto selecionado como mensagem do usuário, seta showActionButtons=false e inputDisabled=false. Para opção "Desejo retomar nossa última conversa": chamar AgentService.resumeSession(slug, resumeToken), em caso de sucesso carregar messages no chat e conectar WebSocket com sessionId retornado; em caso de falha chamar CookieService.clearCookies(slug) e iniciar fluxo de primeiro acesso.
- [x] T008 [US1] Modificar src/hooks/useChat.ts — adicionar método loadHistoricalMessages(messages: ChatMessageInfo[]) que mapeia array de ChatMessageInfo do backend para o formato ChatMessage[] interno do hook (respeitando senderType 0=user, 1=assistant) e seta no estado messages do chat, mantendo ordem cronológica.
- [x] T009 [US1] Modificar src/components/chat/ChatWindow.tsx — (1) aceitar props inputDisabled e showActionButtons, (2) quando inputDisabled=true desabilitar o campo de input (disabled attribute + visual de desabilitado com Tailwind), (3) quando showActionButtons=true renderizar componente ActionButtons acima do campo de input.
- [x] T010 [US1] Modificar src/components/chat/ChatWidget.tsx — passar as novas props (inputDisabled, showActionButtons, onActionSelect) do useChatWidget para ChatWindow. Garantir que ao fechar e reabrir o widget (isOpen toggle), o estado da fase 'returning' é reiniciado (mostra botões novamente se cookies existem e nenhuma escolha foi feita).

**Checkpoint**: Fluxo completo de retomada funcional — usuário retornante vê saudação, clica "retomar", mensagens carregam e chat reconecta.

---

## Phase 5: User Story 2 - Usuário retornante inicia nova conversa (Priority: P1)

**Goal**: Usuário retornante pode escolher iniciar nova conversa, reutilizando dados dos cookies sem nova coleta.

**Independent Test**: Retornar com cookies válidos → clicar "Iniciar uma nova conversa" → verificar que nova sessão é criada sem pedir dados → verificar que cookie resumeToken é atualizado.

### Implementation for User Story 2

- [x] T011 [US2] Adicionar path "nova conversa" em src/hooks/useChatWidget.ts — no handler handleActionSelect, quando texto é "Iniciar uma nova conversa": (1) obter dados do cookie (userName, userEmail, userPhone) via CookieService.getCookies(slug), (2) chamar AgentService.startSession(slug, { userName, userEmail, userPhone }), (3) atualizar cookie com novo resumeToken via CookieService.setCookies, (4) conectar WebSocket com novo sessionId, (5) transicionar para fase 'ready'.
- [x] T012 [US2] Tratar edge case de falha na criação de sessão em src/hooks/useChatWidget.ts — se startSession falhar ao tentar nova conversa com dados do cookie, exibir mensagem de erro no chat e oferecer iniciar fluxo de primeiro acesso (limpar cookies).

**Checkpoint**: Ambos os caminhos do retornante funcionais — retomar conversa (US1) e nova conversa (US2).

---

## Phase 6: User Story 4 - Renomeação avachat → avabot (Priority: P2)

**Goal**: Renomear todas as referências de "avachat" para "avabot" no projeto, incluindo API pública do widget.

**Independent Test**: Buscar "avachat" no código (exceto specs/ e git) → zero resultados. Build completa com sucesso. Widget funciona com window.Avabot.init().

### Implementation for User Story 4

- [x] T013 [P] [US4] Renomear nome do pacote em package.json — alterar "name" de "chatbot-app" para "avabot-app"
- [x] T014 [P] [US4] Renomear base path em vite.config.ts — alterar base de '/avachat/' para '/avabot/'
- [x] T015 [P] [US4] Renomear library name em vite.config.widget.ts — alterar name de 'Avachat' para 'Avabot'
- [x] T016 [P] [US4] Renomear identificadores do widget em src/widget/entry.tsx — alterar: interface AvachatInitOptions → AvabotInitOptions, host.id 'avachat-widget-host' → 'avabot-widget-host', IDs 'avachat-fonts' → 'avabot-fonts', window.Avachat → window.Avabot, warn '[Avachat]' → '[Avabot]'
- [x] T017 [P] [US4] Renomear basename do router em src/main.tsx — alterar basename de "/avachat" para "/avabot"
- [x] T018 [P] [US4] Renomear chave de storage em src/Services/AuthService.ts — alterar AUTH_STORAGE_KEY de 'avachat:auth-token' para 'avabot:auth-token'
- [x] T019 [P] [US4] Renomear chave de storage em src/stores/useAgentStore.ts — alterar SELECTED_AGENT_KEY de 'avachat:selected-agent' para 'avabot:selected-agent'
- [x] T020 [P] [US4] Renomear paths no nginx.conf — alterar todas as ocorrências de '/avachat/' para '/avabot/' e '/avachat' para '/avabot'
- [x] T021 [P] [US4] Atualizar referências em README.md — alterar todas as ocorrências de 'avachat-app' para 'avabot-app' e URLs do repositório
- [x] T022 [US4] Validar renomeação — executar busca por "avachat" em todo o código fonte (excluindo specs/, node_modules/, dist/, .git/) e confirmar zero resultados. Executar npm run build para garantir que o build completa sem erros.

**Checkpoint**: Projeto completamente renomeado. Zero referências a "avachat" no código. Build funcional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Tratamento de edge cases restantes e validação final.

- [x] T023 Tratar edge case de cookies parcialmente corrompidos em src/hooks/useChatWidget.ts — na verificação inicial, se getCookies retorna dados mas resumeToken está vazio ou userName está ausente, chamar clearCookies e iniciar fluxo de primeiro acesso
- [x] T024 Validação completa via quickstart.md — executar todos os cenários de validação documentados em specs/008-session-resume-cookies/quickstart.md para confirmar funcionamento end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem tarefas — projeto já existe
- **Foundational (Phase 2)**: T001, T002, T003 — BLOCKS todas as user stories
- **US3 (Phase 3)**: Depende de Phase 2 (T002 CookieService) — BLOCKS US1 e US2 (cookies precisam ser salvos primeiro)
- **US1 (Phase 4)**: Depende de US3 (cookies existem) + Phase 2 (T001 tipos, T003 resumeSession)
- **US2 (Phase 5)**: Depende de US1 (compartilha ActionButtons e fase 'returning')
- **US4 (Phase 6)**: Independente — pode rodar em paralelo com qualquer fase após Phase 2
- **Polish (Phase 7)**: Depende de US1 + US2 + US3 + US4

### User Story Dependencies

- **US3 (P1)**: Depende apenas de Phase 2 → É o MVP base
- **US1 (P1)**: Depende de US3 (precisa que cookies existam para testar retorno)
- **US2 (P1)**: Depende de US1 (compartilha infraestrutura de 'returning' e ActionButtons)
- **US4 (P2)**: Independente de todas as outras stories — pode ser feita a qualquer momento após Phase 2

### Within Each User Story

- Tipos e serviços antes de hooks
- Hooks antes de componentes
- Componentes antes de integração no widget

### Parallel Opportunities

- T001, T002, T003 podem rodar em paralelo (Phase 2 — arquivos diferentes)
- T006 (ActionButtons) pode rodar em paralelo com T007 (hook returning) e T008 (useChat)
- Todas as tarefas de US4 marcadas [P] podem rodar em paralelo (T013-T021 — arquivos diferentes)
- US4 pode rodar em paralelo com US3/US1/US2 (independente)

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Todas em paralelo — arquivos diferentes, sem dependências entre si:
T001: "Adicionar interface ChatSessionResumeInfo em src/types/chatSession.ts"
T002: "Criar CookieService em src/Services/CookieService.ts"
T003: "Adicionar resumeSession no src/Services/AgentService.ts"
```

## Parallel Example: User Story 4 (Rename)

```bash
# Todas em paralelo — cada tarefa modifica um arquivo diferente:
T013: "package.json"
T014: "vite.config.ts"
T015: "vite.config.widget.ts"
T016: "src/widget/entry.tsx"
T017: "src/main.tsx"
T018: "src/Services/AuthService.ts"
T019: "src/stores/useAgentStore.ts"
T020: "nginx.conf"
T021: "README.md"
```

---

## Implementation Strategy

### MVP First (User Story 3 Only)

1. Complete Phase 2: Foundational (T001-T003)
2. Complete Phase 3: US3 — primeiro acesso salva cookies (T004-T005)
3. **STOP and VALIDATE**: Verificar que cookies são criados após identificação
4. Deploy/demo se pronto

### Incremental Delivery

1. Phase 2 → Infraestrutura pronta
2. US3 → Cookies salvos no primeiro acesso → Validar (MVP!)
3. US1 → Retornante retoma conversa → Validar
4. US2 → Retornante inicia nova → Validar
5. US4 → Renomeação completa → Validar build
6. Polish → Edge cases + validação final

### Parallel Team Strategy

Com múltiplos desenvolvedores após Phase 2:

1. Equipe completa Phase 2 juntos
2. Uma vez pronto:
   - Dev A: US3 → US1 → US2 (sequencial — dependências)
   - Dev B: US4 (independente, em paralelo)
3. Merge e validação final (Phase 7)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US3 é o MVP base — sem cookies salvos, nada mais funciona
- US4 (renomeação) é totalmente independente — pode ser feita primeiro se preferir
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
