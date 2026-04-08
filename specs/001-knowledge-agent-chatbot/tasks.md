# Tasks: Chatbot com Agente de Conhecimento

**Input**: Design documents from `specs/001-knowledge-agent-chatbot/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Nao solicitados na especificacao. Tarefas de teste nao incluidas.

**Organization**: Tasks agrupadas por user story para implementacao e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependencias)
- **[Story]**: User story associada (US1, US2, US3, US4, US5)
- Caminhos exatos em cada descricao

## Path Conventions

- **Backend**: `Backend/Avachat.API/`, `Backend/Avachat.Domain/`, `Backend/Avachat.Infra/`, `Backend/Avachat.Infra.Interfaces/`, `Backend/Avachat.Application/`
- **Frontend**: `Frontend/src/`

---

## Phase 1: Setup

**Purpose**: Inicializacao dos projetos e estrutura base

- [x] T001 Criar solution .NET 9 com 5 projetos (Avachat.API, Avachat.Domain, Avachat.Infra, Avachat.Infra.Interfaces, Avachat.Application) e configurar dependencias entre projetos em Backend/Avachat.sln
- [x] T002 Instalar pacotes NuGet: EntityFrameworkCore.Design, Npgsql.EntityFrameworkCore.PostgreSQL, Elastic.Clients.Elasticsearch, OpenAI, MediatR, FluentValidation.AspNetCore, Swashbuckle em Backend/
- [x] T003 [P] Criar projeto React 19 + TypeScript com Vite em Frontend/chatbot-app/ e instalar dependencias: zustand, tailwindcss, react-markdown, react-dropzone, react-router-dom
- [x] T004 [P] Configurar TailwindCSS em Frontend/chatbot-app/vite.config.ts e Frontend/chatbot-app/src/index.css
- [x] T005 Configurar appsettings.json e appsettings.Development.json com ConnectionStrings, Elasticsearch, OpenAI e Chat sections em Backend/Avachat.API/
- [x] T006 Configurar Program.cs com DI, CORS, Swagger, WebSocket middleware, EF Core e MediatR em Backend/Avachat.API/Program.cs
- [x] T007 [P] Configurar Docker Compose com servicos api, frontend, postgres, elasticsearch e kibana em docker-compose.yml

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestrutura core que DEVE estar pronta antes de qualquer user story

- [x] T008 [P] Criar enums ProcessingStatus (Processing=0, Ready=1, Error=2) em Backend/Avachat.Domain/Enums/ProcessingStatus.cs
- [x] T009 [P] Criar enum SenderType (User=0, Assistant=1) em Backend/Avachat.Domain/Enums/SenderType.cs
- [x] T010 [P] Criar model Agent com todas as propriedades (Name, Slug, Description, SystemPrompt, Status, CollectName, CollectEmail, CollectPhone, timestamps) em Backend/Avachat.Domain/Models/Agent.cs
- [x] T011 [P] Criar model KnowledgeFile com propriedades (AgentId, FileName, FileContent, FileSize, ProcessingStatus, ErrorMessage, timestamps) em Backend/Avachat.Domain/Models/KnowledgeFile.cs
- [x] T012 [P] Criar model ChatSession com propriedades (AgentId, UserName, UserEmail, UserPhone, StartedAt, EndedAt) em Backend/Avachat.Domain/Models/ChatSession.cs
- [x] T013 [P] Criar model ChatMessage com propriedades (ChatSessionId, SenderType, Content, CreatedAt) em Backend/Avachat.Domain/Models/ChatMessage.cs
- [x] T014 Criar AvachatContext com DbSets e Fluent API (snake_case, constraints, relacionamentos, ClientSetNull) em Backend/Avachat.Infra/Context/AvachatContext.cs
- [ ] T015 Gerar e aplicar migration inicial com `dotnet ef migrations add InitialCreate` em Backend/Avachat.Infra/ (REQUER PostgreSQL rodando)
- [x] T016 [P] Criar interfaces de repositorio IAgentRepository, IKnowledgeFileRepository, IChatSessionRepository, IChatMessageRepository em Backend/Avachat.Infra.Interfaces/Repository/
- [x] T017 [P] Criar interface IElasticsearchService (CreateIndex, IndexChunks, DeleteChunksByFileId, HybridSearch) em Backend/Avachat.Infra.Interfaces/AppServices/IElasticsearchService.cs
- [x] T018 [P] Criar interface IOpenAIService (GenerateEmbedding, StreamChatCompletion) em Backend/Avachat.Infra.Interfaces/AppServices/IOpenAIService.cs
- [x] T019 [P] Implementar AgentRepository com CRUD e busca por slug em Backend/Avachat.Infra/Repository/AgentRepository.cs
- [x] T020 [P] Implementar KnowledgeFileRepository em Backend/Avachat.Infra/Repository/KnowledgeFileRepository.cs
- [x] T021 [P] Implementar ChatSessionRepository em Backend/Avachat.Infra/Repository/ChatSessionRepository.cs
- [x] T022 [P] Implementar ChatMessageRepository em Backend/Avachat.Infra/Repository/ChatMessageRepository.cs
- [x] T023 Implementar ElasticsearchService (criar indice knowledge_chunks, indexar/deletar chunks, busca hibrida kNN+BM25 com RRF) em Backend/Avachat.Infra/AppServices/ElasticsearchService.cs
- [x] T024 Implementar OpenAIService (gerar embeddings com text-embedding-3-small, streaming chat completion com gpt-4o) em Backend/Avachat.Infra/AppServices/OpenAIService.cs
- [x] T025 Registrar todos os services, repositories e app services no DI em Backend/Avachat.Application/DependencyInjection.cs
- [x] T026 [P] Criar tipos base do frontend: agent.ts, knowledgeFile.ts, chatSession.ts, chatMessage.ts em Frontend/chatbot-app/src/types/
- [x] T027 [P] Configurar roteamento React Router com rotas admin e chat em Frontend/chatbot-app/src/App.tsx e Frontend/chatbot-app/src/main.tsx

**Checkpoint**: Fundacao pronta — implementacao de user stories pode comecar

---

## Phase 3: User Story 1 — Gerenciamento de Agentes (Priority: P1)

**Goal**: Administrador cria, edita, ativa/desativa e remove agentes com slug unico e campos de coleta configuraveis

**Independent Test**: Criar agente com nome, slug, descricao e prompt de sistema; verificar listagem, acesso por slug e edicao/remocao

### Implementation for User Story 1

- [x] T028 [US1] Criar AgentService com metodos CRUD, validacao de slug unico e toggle de status em Backend/Avachat.Application/Services/AgentService.cs
- [x] T029 [US1] Criar DTOs AgentInfo, AgentInsertInfo, AgentChatConfigInfo e Result<T> com JsonPropertyName em Backend/Avachat.Domain/DTOs/AgentDTOs.cs
- [x] T030 [US1] Criar validador FluentValidation para AgentInsertInfo em Backend/Avachat.API/Validators/AgentInsertInfoValidator.cs
- [x] T031 [US1] Criar AgentController com endpoints CRUD + chat-config + toggle status em Backend/Avachat.API/Controllers/AgentController.cs
- [x] T032 [P] [US1] Criar AgentService.ts com metodos CRUD usando Fetch API em Frontend/chatbot-app/src/Services/AgentService.ts
- [x] T033 [P] [US1] Criar useAgentStore com Zustand em Frontend/chatbot-app/src/stores/useAgentStore.ts
- [x] T034 [US1] Criar componente AgentForm em Frontend/chatbot-app/src/components/admin/AgentForm.tsx
- [x] T035 [US1] Criar pagina AgentListPage em Frontend/chatbot-app/src/pages/admin/AgentListPage.tsx
- [x] T036 [US1] Criar pagina AgentFormPage em Frontend/chatbot-app/src/pages/admin/AgentFormPage.tsx

**Checkpoint**: CRUD completo de agentes funcionando no admin. Agentes podem ser criados com slug unico e campos de coleta configuraveis.

---

## Phase 4: User Story 2 — Upload e Indexacao da Base de Conhecimento (Priority: P1)

**Goal**: Administrador faz upload de arquivos .md que sao processados, chunked, vetorizados e indexados no Elasticsearch

**Independent Test**: Upload de arquivo .md, acompanhar status ate "pronto", verificar chunks indexados no ES

### Implementation for User Story 2

- [x] T037 [US2] Criar IngestionService com logica de chunking em Backend/Avachat.Application/Services/IngestionService.cs
- [x] T038 [US2] EmbeddingService integrado diretamente via IOpenAIService (wrapper nao necessario)
- [x] T039 [US2] Criar KnowledgeFileController com 4 endpoints em Backend/Avachat.API/Controllers/KnowledgeFileController.cs
- [x] T040 [P] [US2] Criar KnowledgeFileService.ts em Frontend/chatbot-app/src/Services/KnowledgeFileService.ts
- [x] T041 [P] [US2] Criar useKnowledgeFileStore em Frontend/chatbot-app/src/stores/useKnowledgeFileStore.ts
- [x] T042 [US2] Criar componente FileUpload com React Dropzone em Frontend/chatbot-app/src/components/admin/FileUpload.tsx
- [x] T043 [P] [US2] Criar componente FileStatusBadge em Frontend/chatbot-app/src/components/admin/FileStatusBadge.tsx
- [x] T044 [US2] Criar pagina KnowledgeFilesPage em Frontend/chatbot-app/src/pages/admin/KnowledgeFilesPage.tsx

**Checkpoint**: Upload de .md funcional, processamento assincrono com status visivel, chunks indexados no Elasticsearch.

---

## Phase 5: User Story 3 — Coleta de Dados do Usuario (Priority: P1)

**Goal**: Agente solicita dados de identificacao configuraveis (nome, e-mail, telefone) antes de iniciar a conversa

**Independent Test**: Acessar chat de agente configurado para pedir nome e e-mail; verificar que solicita dados antes de responder; confirmar dados salvos na sessao

### Implementation for User Story 3

- [x] T045 [US3] Criar endpoint GET /api/agents/{slug}/chat-config (ja integrado no AgentController)
- [x] T046 [US3] Criar componente UserDataForm em Frontend/chatbot-app/src/components/chat/UserDataForm.tsx
- [x] T047 [US3] Criar ChatWebSocketHandler com identify + session creation em Backend/Avachat.API/WebSocket/ChatWebSocketHandler.cs
- [x] T048 [US3] Criar ChatPage com fluxo completo em Frontend/chatbot-app/src/pages/chat/ChatPage.tsx

**Checkpoint**: Coleta de dados funcional. Agente pede dados antes de conversar. Sessao criada no banco com dados do usuario.

---

## Phase 6: User Story 4 — Conversa com o Agente via Chat (Priority: P1)

**Goal**: Chat em tempo real com busca RAG, streaming de respostas e persistencia completa do historico

**Independent Test**: Enviar pergunta sobre conteudo indexado, verificar resposta relevante com streaming, confirmar historico salvo no banco

### Implementation for User Story 4

- [x] T049 [US4] Criar SearchService em Backend/Avachat.Application/Services/SearchService.cs
- [x] T050 [US4] Criar ChatService com orquestracao RAG completa em Backend/Avachat.Application/Services/ChatService.cs
- [x] T051 [US4] ChatWebSocketHandler com loop de mensagens, streaming e error handling em Backend/Avachat.API/WebSocket/ChatWebSocketHandler.cs
- [x] T052 [P] [US4] Criar hook useWebSocket em Frontend/chatbot-app/src/hooks/useWebSocket.ts
- [x] T053 [P] [US4] Criar hook useChat em Frontend/chatbot-app/src/hooks/useChat.ts
- [x] T054 [US4] Estado do chat gerenciado pelo hook useChat (store separado nao necessario)
- [x] T055 [US4] Criar componente ChatWindow em Frontend/chatbot-app/src/components/chat/ChatWindow.tsx
- [x] T056 [P] [US4] Criar componente MessageBubble com Markdown rendering em Frontend/chatbot-app/src/components/chat/MessageBubble.tsx
- [x] T057 [P] [US4] Criar componente TypingIndicator em Frontend/chatbot-app/src/components/chat/TypingIndicator.tsx
- [x] T058 [US4] ChatPage integrado com useChat, ChatWindow e UserDataForm em Frontend/chatbot-app/src/pages/chat/ChatPage.tsx
- [x] T059 [US4] Criar NotFoundPage e UnavailablePage em Frontend/chatbot-app/src/components/common/
- [x] T060 [US4] Rota /chat/:slug configurada no React Router em Frontend/chatbot-app/src/App.tsx

**Checkpoint**: Chat RAG completo funcionando. Streaming de respostas via WebSocket. Historico persistido no banco.

---

## Phase 7: User Story 5 — Visualizacao de Respostas em Markdown (Priority: P2)

**Goal**: Respostas do chatbot renderizadas em Markdown formatado

**Independent Test**: Enviar pergunta que gere resposta com titulos, listas e blocos de codigo; verificar renderizacao

### Implementation for User Story 5

- [x] T061 [US5] React Markdown integrado no MessageBubble (implementado junto com T056)
- [x] T062 [US5] TailwindCSS typography (classe prose) configurado no MessageBubble
- [x] T063 [US5] Links em Markdown abrem em nova aba (target="_blank") no MessageBubble

**Checkpoint**: Respostas com Markdown renderizado corretamente com formatacao rica.

---

## Phase 8: Admin — Historico de Conversas

**Purpose**: Painel administrativo para consultar historico de sessoes e mensagens

- [x] T064 Criar ChatSessionController com endpoints de historico em Backend/Avachat.API/Controllers/ChatSessionController.cs
- [x] T065 [P] Criar ChatHistoryService.ts em Frontend/chatbot-app/src/Services/ChatHistoryService.ts
- [x] T066 Criar pagina ChatHistoryPage em Frontend/chatbot-app/src/pages/admin/ChatHistoryPage.tsx

**Checkpoint**: Admin pode consultar historico completo de conversas por agente.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Melhorias que afetam multiplas user stories

- [x] T067 [P] Tratamento de erro ja implementado nos controllers (try/catch + StatusCode 500)
- [x] T068 [P] Tratamento de erro no frontend via error state nos stores e componentes
- [x] T069 CORS configurado no Program.cs (AllowAnyOrigin em Development)
- [x] T070 [P] Criar Dockerfile para backend (.NET 9) em Backend/Dockerfile
- [x] T071 [P] Criar Dockerfile para frontend (build Vite + Nginx) em Frontend/chatbot-app/Dockerfile
- [x] T072 Validacao estrutural completa: backend compila 0 erros, todos os componentes frontend criados
- [x] T073 [P] Variaveis de ambiente VITE_API_URL e VITE_WS_URL configuradas em Frontend/chatbot-app/.env e .env.production

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependencias — pode comecar imediatamente
- **Foundational (Phase 2)**: Depende de Setup — BLOQUEIA todas as user stories
- **US1 - Agentes (Phase 3)**: Depende de Foundational
- **US2 - Knowledge Base (Phase 4)**: Depende de Foundational + ElasticsearchService (T023)
- **US3 - Coleta Dados (Phase 5)**: Depende de US1 (precisa de agente com config de coleta)
- **US4 - Chat (Phase 6)**: Depende de US2 (precisa de chunks indexados) + US3 (precisa de coleta de dados)
- **US5 - Markdown (Phase 7)**: Depende de US4 (precisa de MessageBubble existente)
- **Admin Historico (Phase 8)**: Depende de US4 (precisa de sessoes/mensagens no banco)
- **Polish (Phase 9)**: Depende de todas as phases anteriores

### User Story Dependencies

- **US1 (Agentes)**: Pode comecar apos Foundational — sem dependencia de outras stories
- **US2 (Knowledge Base)**: Pode comecar apos Foundational — independente de US1 (usa agentId diretamente)
- **US3 (Coleta Dados)**: Depende de US1 (agente com campos de coleta criados)
- **US4 (Chat)**: Depende de US2 (chunks indexados) e US3 (coleta de dados e sessao criada)
- **US5 (Markdown)**: Depende de US4 (componente MessageBubble existente)

### Within Each User Story

- Models/DTOs antes de services
- Services antes de controllers/endpoints
- Backend antes de frontend (API precisa existir)
- Core implementation antes de integracao
- Story completa antes de avancar para proxima prioridade

### Parallel Opportunities

- T008-T013: Todos os models e enums em paralelo
- T016-T018: Todas as interfaces em paralelo
- T019-T022: Todos os repositories em paralelo
- T032-T033: Frontend service + store em paralelo (US1)
- T040-T041, T043: Frontend service + store + badge em paralelo (US2)
- T052-T053, T056-T057: Hooks e componentes visuais em paralelo (US4)
- T070-T071: Dockerfiles em paralelo

---

## Implementation Strategy

### MVP First (US1 + US2 + US3 + US4)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL)
3. Complete Phase 3: US1 — Gerenciamento de Agentes
4. Complete Phase 4: US2 — Upload e Indexacao
5. Complete Phase 5: US3 — Coleta de Dados
6. Complete Phase 6: US4 — Chat com Streaming
7. **STOP and VALIDATE**: Testar fluxo completo (criar agente → upload → chat)
8. Deploy/demo se pronto

### Incremental Delivery

1. Setup + Foundational → Fundacao pronta
2. US1 → Admin de agentes funcional
3. US2 → Upload e indexacao funcional
4. US3 + US4 → Chat completo com coleta e streaming (MVP!)
5. US5 → Markdown rendering
6. Admin Historico → Visualizacao de conversas
7. Polish → Docker, error handling, validacao final

---

## Notes

- [P] tasks = arquivos diferentes, sem dependencias
- [Story] label mapeia task para user story especifica
- Cada user story deve ser independentemente completavel e testavel
- Commit apos cada task ou grupo logico
- Pare em qualquer checkpoint para validar story independentemente
- Evitar: tasks vagas, conflitos no mesmo arquivo, dependencias cross-story que quebrem independencia
