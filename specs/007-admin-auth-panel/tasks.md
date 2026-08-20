# Tasks: Autenticação JWT e Painel Administrativo

**Input**: Design documents from `/specs/007-admin-auth-panel/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Não solicitados na especificação — tasks de teste omitidos.

**Organization**: Tasks agrupados por user story para permitir implementação e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências)
- **[Story]**: User story associada (US1, US2, US3, US4)
- Caminhos exatos incluídos nas descrições

---

## Phase 1: Setup

**Purpose**: Criação de tipos e infraestrutura compartilhada

- [x] T001 [P] Create auth types (AuthCredentials, AuthResponse interfaces) in src/types/auth.ts
- [x] T002 [P] Add PaginatedResult<T> interface to src/types/result.ts

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Autenticação e proteção de rotas — DEVE ser completado antes de qualquer user story

**CRITICAL**: Nenhuma user story pode iniciar até esta fase estar completa

- [x] T003 Create AuthService with login(), logout(), getToken(), getAuthHeaders(), isAuthenticated() in src/Services/AuthService.ts — usar endpoint POST /auth/login, armazenar token em localStorage key "avachat:auth-token", header Authorization: Bearer {token}
- [x] T004 Create useAuthStore (Zustand) with token, isAuthenticated, login(), logout(), getAuthHeaders() in src/stores/useAuthStore.ts — inicializar token do localStorage, persistir on login, remover on logout
- [x] T005 Create ProtectedRoute component in src/components/common/ProtectedRoute.tsx — verificar useAuthStore.isAuthenticated, redirecionar para /login se não autenticado via Navigate
- [x] T006 Fix endpoint paths and add auth headers in src/Services/AgentService.ts — adicionar getAuthHeaders() em getAll(), create(), update(), delete(), toggleStatus(); corrigir path de startSession para POST /sessions/agents/{slug}
- [x] T007 [P] Fix endpoint paths, pagination params, and add auth headers in src/Services/ChatHistoryService.ts — corrigir getSessions path para GET /sessions/agents/{agentId}, corrigir params de pagina/tamanhoPagina para page/maxPage, adicionar auth headers
- [x] T008 [P] Fix endpoint paths and add auth headers in src/Services/KnowledgeFileService.ts — corrigir paths: GET /files/{agentId}, POST /files/{agentId}, DELETE /files/{agentId}/{fileId}, POST /files/{agentId}/{fileId}/reprocess, adicionar auth headers
- [x] T009 Add search method to AgentService in src/Services/AgentService.ts — método search(agentId, query, topK?) que chama GET /agents/{id}/search?query=&topK= com auth headers
- [x] T010 Update useAgentStore to add selectedAgent state in src/stores/useAgentStore.ts — adicionar campo selectedAgent: AgentInfo | null, método selectAgent(), persistir em localStorage key "avachat:selected-agent", carregar no init

**Checkpoint**: Autenticação funcional, endpoints corrigidos, proteção de rotas ativa

---

## Phase 3: User Story 1 - Login com JWT Token (Priority: P1) MVP

**Goal**: Administrador consegue fazer login, ver dashboard, e fazer logout. Rotas admin protegidas.

**Independent Test**: Acessar /admin → redirecionado para /login. Login com credenciais válidas → dashboard. Login inválido → mensagem de erro. Logout → volta para login.

### Implementation for User Story 1

- [x] T011 [US1] Create LoginPage with username/password form, error handling, and redirect to /admin on success in src/pages/auth/LoginPage.tsx — usar useAuthStore.login(), exibir erro se credenciais inválidas, design profissional com Tailwind, centralizado na tela
- [x] T012 [US1] Create DashboardPage with summary cards (total agents, active/inactive count) and recent sessions list in src/pages/admin/DashboardPage.tsx — buscar dados via AgentService.getAll() para contagem, usar layout responsivo com cards
- [x] T013 [US1] Create AdminNavbar with app title, AgentSelector placeholder, and logout button in src/components/admin/AdminNavbar.tsx — usar useAuthStore.logout(), exibir botão "Sair", layout horizontal com Tailwind
- [x] T014 [US1] Create AdminSidebar with navigation menu items (Dashboard, Agentes, and agent-contextual items disabled when no agent selected) in src/components/admin/AdminSidebar.tsx — itens: Dashboard, Agentes (sempre visíveis), Configurações, Sessões, Base de Conhecimento > Arquivos, Base de Conhecimento > Busca (habilitados somente com agente selecionado via useAgentStore.selectedAgent)
- [x] T015 [US1] Create AdminLayout wrapping AdminNavbar + AdminSidebar + Outlet in src/components/admin/AdminLayout.tsx — sidebar fixa à esquerda, navbar no topo, conteúdo principal à direita com Outlet do React Router
- [x] T016 [US1] Update routing in src/App.tsx — adicionar rota /login → LoginPage; agrupar rotas /admin/* dentro de ProtectedRoute + AdminLayout; rotas: /admin → DashboardPage, /admin/agents → AgentListPage, /admin/agents/new → AgentFormPage, /admin/agents/:id/edit → AgentFormPage, /admin/settings → AgentSettingsPage, /admin/sessions → SessionListPage, /admin/sessions/:sessionId → SessionDetailPage, /admin/knowledge/files → KnowledgeFilesPage, /admin/knowledge/search → KnowledgeSearchPage; manter rotas públicas (/, /chat/:slug, /abipesca)
- [x] T017 [US1] Add 401 response handling across all Services — em caso de resposta 401, limpar token via useAuthStore e redirecionar para /login (implementar em AuthService como utility usado pelos outros services)

**Checkpoint**: Login funcional, dashboard exibido, rotas protegidas, logout operacional

---

## Phase 4: User Story 2 - Gestão de Agentes na Área Administrativa (Priority: P2)

**Goal**: Admin gerencia agentes com CRUD completo, seletor de agente na navbar, layout melhorado.

**Independent Test**: Criar agente, editar, excluir, alternar status. Selecionar agente na navbar e verificar que sidebar reflete o contexto.

### Implementation for User Story 2

- [x] T018 [US2] Create AgentSelector dropdown component in src/components/admin/AgentSelector.tsx — listar agentes do useAgentStore, ao selecionar chamar selectAgent(), exibir nome do agente selecionado, dropdown com Tailwind
- [x] T019 [US2] Integrate AgentSelector into AdminNavbar in src/components/admin/AdminNavbar.tsx — posicionar entre título e botão logout
- [x] T020 [US2] Redesign AgentListPage with professional layout using frontend-design skill in src/pages/admin/AgentListPage.tsx — tabela/cards com status badge, ações (editar, toggle status, excluir com confirmação modal), botão "Novo Agente", layout responsivo, usar useAgentStore
- [x] T021 [US2] Redesign AgentForm component with improved layout in src/components/admin/AgentForm.tsx — campos: nome, descrição (textarea), prompt do sistema (textarea grande), toggles para collectName/collectEmail/collectPhone, botões salvar/cancelar, validação de campos obrigatórios
- [x] T022 [US2] Update AgentFormPage with improved layout in src/pages/admin/AgentFormPage.tsx — integrar AgentForm redesenhado, título dinâmico (Novo Agente / Editar Agente), carregar dados do agente se editando
- [x] T023 [US2] Create AgentSettingsPage with edit and delete options for selected agent in src/pages/admin/AgentSettingsPage.tsx — exibir dados do agente selecionado (useAgentStore.selectedAgent), botão "Editar" navega para form, botão "Excluir" com modal de confirmação, se agente excluído limpar selectedAgent
- [x] T024 [US2] Update AdminSidebar to highlight active route and enable/disable items based on selected agent in src/components/admin/AdminSidebar.tsx — usar useLocation() para highlight ativo, desabilitar itens contextuais quando selectedAgent é null, mostrar mensagem "Selecione um agente" nos itens desabilitados

**Checkpoint**: CRUD de agentes completo, seletor funcional, layout profissional

---

## Phase 5: User Story 3 - Listagem de Sessões e Histórico de Conversa (Priority: P3)

**Goal**: Admin visualiza sessões do agente selecionado e lê histórico completo de conversas em página dedicada.

**Independent Test**: Selecionar agente, acessar Sessões, ver lista paginada, clicar em sessão, ver mensagens com distinção visual, voltar à lista.

### Implementation for User Story 3

- [x] T025 [US3] Create SessionListPage with paginated sessions table for selected agent in src/pages/admin/SessionListPage.tsx — tabela com colunas: userName, userEmail, userPhone, startedAt, endedAt, messageCount; paginação com controles prev/next e indicador de página; usar ChatHistoryService.getSessions(); link para /admin/sessions/:sessionId; estado vazio quando não há sessões; requer selectedAgent
- [x] T026 [US3] Create SessionDetailPage with full conversation history in chat format in src/pages/admin/SessionDetailPage.tsx — buscar mensagens via ChatHistoryService.getMessages(sessionId); exibir em formato de chat com bolhas: mensagens do usuário (alinhadas à direita, cor ava-600) e assistente (alinhadas à esquerda, cinza); timestamp em cada mensagem; paginação para sessões longas; header com dados da sessão (nome, email, data); botão voltar para /admin/sessions; suportar markdown nas mensagens do assistente via react-markdown

**Checkpoint**: Sessões listadas, histórico de conversa legível em página dedicada

---

## Phase 6: User Story 4 - Base de Conhecimento do Agente (Priority: P4)

**Goal**: Admin gerencia arquivos da base de conhecimento e busca no Elasticsearch.

**Independent Test**: Upload de arquivo .md, verificar status, excluir arquivo, buscar por termo, ver resultados.

### Implementation for User Story 4

- [x] T027 [US4] Update KnowledgeFilesPage to work within AdminLayout context in src/pages/admin/KnowledgeFilesPage.tsx — usar selectedAgent do useAgentStore em vez de route param :agentId; manter funcionalidades existentes (upload, delete, reprocess, status polling); melhorar layout para consistência com outras páginas admin
- [x] T028 [US4] Create KnowledgeSearchPage with search input and results display in src/pages/admin/KnowledgeSearchPage.tsx — campo de busca com botão; campo numérico para topK (padrão 5); usar AgentService.search(); exibir resultados em cards com texto dos chunks; estado vazio quando sem resultados; estado de loading durante busca; mensagem de erro em caso de falha; requer selectedAgent

**Checkpoint**: Base de conhecimento gerenciável com upload, exclusão, reprocessamento e busca

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Melhorias que afetam múltiplas user stories

- [x] T029 [P] Handle edge case: deleting selected agent clears AgentSelector and resets sidebar context in src/stores/useAgentStore.ts and src/pages/admin/AgentSettingsPage.tsx
- [x] T030 [P] Handle edge case: show file size limit warning (10MB) before/during upload in src/components/admin/FileUpload.tsx
- [x] T031 [P] Handle edge case: accessing agent-contextual pages without selected agent shows "Selecione um agente" message in src/components/admin/AdminSidebar.tsx
- [x] T032 Ensure responsive design across all admin pages — verificar breakpoints sm/md/lg, sidebar colapsável em mobile, tabelas com scroll horizontal em telas pequenas
- [x] T033 Run quickstart.md validation — verificar todas as rotas, fluxo login→dashboard→agentes→sessões→base de conhecimento

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — início imediato
- **Foundational (Phase 2)**: Depende de Phase 1 — BLOQUEIA todas as user stories
- **User Stories (Phase 3-6)**: Todas dependem de Phase 2 completa
  - US1 (Phase 3): Primeiro — cria layout base usado por todas as outras
  - US2 (Phase 4): Depende de US1 (AdminLayout, routing)
  - US3 (Phase 5): Depende de US2 (AgentSelector, selectedAgent)
  - US4 (Phase 6): Depende de US2 (AgentSelector, selectedAgent)
  - US3 e US4 podem rodar em paralelo após US2
- **Polish (Phase 7)**: Depende de todas as user stories desejadas

### User Story Dependencies

- **US1 (P1)**: Depende apenas de Phase 2 — cria infraestrutura de layout
- **US2 (P2)**: Depende de US1 (AdminLayout, AdminNavbar, routing)
- **US3 (P3)**: Depende de US2 (selectedAgent no store) — pode rodar em paralelo com US4
- **US4 (P4)**: Depende de US2 (selectedAgent no store) — pode rodar em paralelo com US3

### Within Each User Story

- Componentes de layout antes de páginas
- Páginas antes de integração
- Routing após todos os componentes prontos

### Parallel Opportunities

- T001, T002 podem rodar em paralelo (Phase 1)
- T007, T008 podem rodar em paralelo (Phase 2 — arquivos diferentes)
- T025, T026 são sequenciais (US3 — SessionList antes de SessionDetail)
- T027, T028 podem rodar em paralelo (US4 — arquivos diferentes)
- T029, T030, T031 podem rodar em paralelo (Phase 7)
- **US3 e US4 inteiras podem rodar em paralelo** após US2 completa

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Batch 1 — paralelo:
Task T001: "Create auth types in src/types/auth.ts"
Task T002: "Add PaginatedResult to src/types/result.ts"

# Batch 2 — sequencial (depende de T001):
Task T003: "Create AuthService in src/Services/AuthService.ts"
Task T004: "Create useAuthStore in src/stores/useAuthStore.ts"

# Batch 3 — paralelo:
Task T005: "Create ProtectedRoute in src/components/common/ProtectedRoute.tsx"
Task T006: "Fix AgentService endpoints in src/Services/AgentService.ts"
Task T007: "Fix ChatHistoryService endpoints in src/Services/ChatHistoryService.ts"
Task T008: "Fix KnowledgeFileService endpoints in src/Services/KnowledgeFileService.ts"
Task T009: "Add search method to AgentService in src/Services/AgentService.ts"
Task T010: "Update useAgentStore in src/stores/useAgentStore.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003-T010)
3. Complete Phase 3: User Story 1 (T011-T017)
4. **STOP and VALIDATE**: Login, dashboard, rotas protegidas, logout
5. Deploy/demo se pronto

### Incremental Delivery

1. Setup + Foundational → Base pronta
2. Add US1 → Login + Dashboard + Layout → Deploy (MVP!)
3. Add US2 → CRUD Agentes + Seletor + Layout melhorado → Deploy
4. Add US3 + US4 (paralelo) → Sessões + Base Conhecimento → Deploy
5. Polish → Edge cases + responsividade → Deploy final

### Parallel Team Strategy

Com múltiplos desenvolvedores:

1. Equipe completa Setup + Foundational juntos
2. Após Foundational:
   - Dev A: US1 (Login + Layout)
3. Após US1:
   - Dev A: US2 (Agentes)
4. Após US2:
   - Dev A: US3 (Sessões)
   - Dev B: US4 (Base Conhecimento)
5. Todos: Polish

---

## Notes

- [P] tasks = arquivos diferentes, sem dependências
- [Story] label mapeia task para user story específica
- Cada user story deve ser independentemente completável e testável
- Commit após cada task ou grupo lógico
- Pare em qualquer checkpoint para validar a story independentemente
- Usar skill `frontend-design` para layout das páginas admin (conforme solicitado)
- Usar skill `react-architecture` para novas entidades (conforme constitution)
- Todos os textos da UI em português brasileiro
