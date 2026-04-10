# Tasks: Retomada de Sessao e Bot Telegram

**Input**: Design documents from `/specs/002-session-resume-telegram-bot/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Nao solicitados explicitamente na spec. Testes omitidos.

**Organization**: Tasks agrupadas por user story para implementacao e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependencias)
- **[Story]**: User story associada (US1/US2 = Resume Token, US3/US4 = Telegram Bot)
- Caminhos absolutos baseados na estrutura Clean Architecture existente

---

## Phase 1: Setup

**Purpose**: Instalar dependencias e configurar variaveis de ambiente

- [x] T001 Instalar pacote NuGet `Telegram.Bot` no projeto Avachat.Infra.csproj
- [x] T002 [P] Adicionar variaveis de ambiente ao .env.example: `TELEGRAM_BOT_TOKEN`, `TELEGRAM_WEBHOOK_SECRET`, `TELEGRAM_WEBHOOK_URL`, `TELEGRAM_AGENT_SLUG`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modelo de dados e migracao que bloqueiam todas as user stories

**CRITICAL**: Nenhuma user story pode iniciar ate esta fase estar completa

- [x] T003 Adicionar propriedade `ResumeToken` (string, max 32) ao modelo `ChatSession` em Avachat.Domain/Models/ChatSession.cs
- [x] T004 Criar modelo `TelegramChat` em Avachat.Domain/Models/TelegramChat.cs conforme data-model.md (TelegramChatId bigint PK, AgentId FK, ChatSessionId FK, TelegramUsername, TelegramFirstName, CreatedAt, UpdatedAt)
- [x] T005 Adicionar `DbSet<TelegramChat>` e configurar Fluent API no Avachat.Infra/Context/AvachatContext.cs (tabela `avachat_telegram_chats`, indice unico em `resume_token`, FKs com CASCADE, coluna `resume_token` varchar(32) NOT NULL com indice unico na tabela `avachat_chat_sessions`)
- [x] T006 Criar migracao EF Core: `dotnet ef migrations add AddResumeTokenAndTelegramChat` e gerar SQL para popular `resume_token` em sessoes existentes com GUIDs
- [x] T007 Adicionar propriedade `ResumeToken` ao DTO `ChatSessionInfo` em Avachat.DTO/ChatDTOs.cs
- [x] T008 [P] Criar DTO `ChatSessionResumeInfo` em Avachat.DTO/ChatDTOs.cs (herda campos de ChatSessionInfo + lista de ChatMessageInfo para as ultimas 10 mensagens)
- [x] T009 Atualizar `ChatSessionProfile` no AutoMapper em Avachat.Application/Profiles/ para mapear `ResumeToken`

**Checkpoint**: Modelo de dados pronto, migracoes aplicadas, DTOs atualizados

---

## Phase 3: User Story 1 + 2 - Retomada de Sessao com Resume Token (Priority: P1) MVP

**Goal**: Usuarios podem criar sessoes que retornam um resume token unico, e usar esse token para retomar a ultima sessao com as ultimas 10 mensagens.

**Independent Test**: Criar sessao via REST, verificar que retorna resumeToken. Usar resumeToken para recuperar sessao com ultimas 10 mensagens via GET /sessions/resume/{slug}.

### Implementation

- [x] T010 [US1] Atualizar `ChatService.CreateSessionAsync` em Avachat.Application/Services/ChatService.cs para gerar `ResumeToken = Guid.NewGuid().ToString("N")` na criacao da sessao
- [x] T011 [US1] Adicionar metodo `GetByResumeTokenAsync(string resumeToken)` a interface `IChatSessionRepository` em Avachat.Infra.Interfaces/Repository/IChatSessionRepository.cs
- [x] T012 [US1] Implementar `GetByResumeTokenAsync` no `ChatSessionRepository` em Avachat.Infra/Repository/ChatSessionRepository.cs (busca por resume_token com include do Agent)
- [x] T013 [US1] Adicionar metodo `GetLastBySessionIdAsync(long sessionId, int count = 10)` a interface `IChatMessageRepository` em Avachat.Infra.Interfaces/Repository/IChatMessageRepository.cs
- [x] T014 [US1] Implementar `GetLastBySessionIdAsync` no `ChatMessageRepository` em Avachat.Infra/Repository/ChatMessageRepository.cs (ultimas N mensagens ordenadas cronologicamente)
- [x] T015 [US1] Criar endpoint `GET /sessions/resume/{slug}` no `SessionController` em Avachat.API/Controllers/SessionController.cs conforme contrato em contracts/session-resume-api.md (AllowAnonymous, header X-Resume-Token, retorna sessao + ultimas 10 mensagens)
- [x] T016 [US2] Atualizar o endpoint `POST /sessions/agents/{slug}` no `SessionController` para retornar `resumeToken` na resposta (ja incluido pelo AutoMapper apos T009 e T010)
- [x] T017 [US2] Atualizar o evento `ready` no `ChatWebSocketHandler` em Avachat.API/WebSocket/ChatWebSocketHandler.cs para incluir campo `resumeToken` na mensagem JSON enviada ao cliente

**Checkpoint**: Resume token funcional. Sessoes criadas via REST e WebSocket retornam token. Endpoint de retomada retorna sessao com ultimas 10 mensagens.

---

## Phase 4: User Story 3 + 4 - Bot Telegram (Priority: P2)

**Goal**: Bot Telegram recebe mensagens via webhook, processa com pipeline RAG existente, e responde. Endpoint de setup para registrar webhook.

**Independent Test**: Registrar webhook via POST /telegram/setup-webhook. Enviar /start ao bot no Telegram, receber boas-vindas. Enviar mensagem de texto, receber resposta da IA.

### Implementation

- [x] T018 [P] [US3] Criar interface `ITelegramChatRepository` em Avachat.Infra.Interfaces/Repository/ITelegramChatRepository.cs (GetByChatIdAsync, CreateAsync, UpdateAsync)
- [x] T019 [P] [US3] Implementar `TelegramChatRepository` em Avachat.Infra/Repository/TelegramChatRepository.cs
- [x] T020 [US3] Criar `TelegramService` em Avachat.Application/Services/TelegramService.cs com metodos: ProcessUpdateAsync(Update), HandleTextMessageAsync(Message), HandleStartCommandAsync(Message), SendMessageAsync(chatId, text). Usar ChatService para processar mensagens e Telegram.Bot TelegramBotClient para enviar respostas
- [x] T021 [US4] Criar `TelegramController` em Avachat.API/Controllers/TelegramController.cs conforme contrato em contracts/telegram-webhook-api.md com endpoints: POST /telegram/webhook (AllowAnonymous, valida X-Telegram-Bot-Api-Secret-Token) e POST /telegram/setup-webhook (Authorize)
- [x] T022 [US3] Registrar servicos no DI em Avachat.API/Program.cs: TelegramBotClient como singleton (token via env), TelegramService como scoped, TelegramChatRepository como scoped. Ler configuracoes: TELEGRAM_BOT_TOKEN, TELEGRAM_WEBHOOK_SECRET, TELEGRAM_WEBHOOK_URL, TELEGRAM_AGENT_SLUG
- [x] T023 [US3] Implementar tratamento de mensagens nao-texto no TelegramService: responder com mensagem informativa "Desculpe, eu so consigo processar mensagens de texto."
- [x] T024 [US4] Implementar logica de setup-webhook no TelegramService: chamar setWebhook na API Telegram com url, secret_token e allowed_updates=["message"]

**Checkpoint**: Bot Telegram funcional. Webhook registrado, /start cria sessao e envia boas-vindas, mensagens de texto recebem respostas da IA.

---

## Phase 5: Polish e Cross-Cutting Concerns

**Purpose**: Melhorias que afetam multiplas user stories

- [x] T025 [P] Adicionar logging (ILogger) nos novos servicos: TelegramService e nos novos metodos do ChatService em Avachat.Application/Services/
- [x] T026 [P] Atualizar documentacao Swagger com descricoes para os novos endpoints no SessionController e TelegramController
- [x] T027 Validar fluxo completo via quickstart.md: criar sessao, verificar resumeToken, retomar sessao, registrar webhook, testar bot Telegram

---

## Dependencies e Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependencias — pode iniciar imediatamente
- **Foundational (Phase 2)**: Depende da Phase 1 — BLOQUEIA todas as user stories
- **US1+US2 Resume Token (Phase 3)**: Depende da Phase 2 — pode rodar em paralelo com Phase 4
- **US3+US4 Telegram Bot (Phase 4)**: Depende da Phase 2 — pode rodar em paralelo com Phase 3
- **Polish (Phase 5)**: Depende das Phases 3 e 4

### User Story Dependencies

- **US1+US2 (P1)**: Pode iniciar apos Phase 2 — sem dependencia de outras stories
- **US3+US4 (P2)**: Pode iniciar apos Phase 2 — sem dependencia de US1/US2

### Within Each Story

- Repository interfaces antes de implementacoes
- Implementacoes de repository antes de services
- Services antes de controllers/endpoints
- Core implementation antes de integracao

### Parallel Opportunities

- T001 e T002 podem rodar em paralelo (Phase 1)
- T007 e T008 podem rodar em paralelo (Phase 2)
- Phase 3 (Resume Token) e Phase 4 (Telegram) podem rodar em paralelo inteiramente
- T018 e T019 podem rodar em paralelo com T020 (dentro de Phase 4)
- T025 e T026 podem rodar em paralelo (Phase 5)

---

## Parallel Example: Phase 3 (Resume Token)

```bash
# Apos T010 (gerar token no service), paralelo:
Task T011: "Interface GetByResumeTokenAsync em IChatSessionRepository"
Task T013: "Interface GetLastBySessionIdAsync em IChatMessageRepository"

# Apos T011+T013, paralelo:
Task T012: "Implementar GetByResumeTokenAsync no ChatSessionRepository"
Task T014: "Implementar GetLastBySessionIdAsync no ChatMessageRepository"
```

## Parallel Example: Phase 3 + Phase 4

```bash
# Apos Phase 2 completa, dois fluxos em paralelo:
# Fluxo A (Resume Token):
Task T010 -> T011+T013 -> T012+T014 -> T015 -> T016+T017

# Fluxo B (Telegram):
Task T018+T019 -> T020 -> T021+T022 -> T023+T024
```

---

## Implementation Strategy

### MVP First (User Stories 1+2 Only)

1. Completar Phase 1: Setup
2. Completar Phase 2: Foundational (CRITICAL)
3. Completar Phase 3: Resume Token (US1+US2)
4. **PARAR E VALIDAR**: Testar retomada de sessao independentemente
5. Deploy/demo se pronto

### Incremental Delivery

1. Setup + Foundational → Base pronta
2. Resume Token (US1+US2) → Testar → Deploy (MVP!)
3. Telegram Bot (US3+US4) → Testar → Deploy
4. Polish → Validacao final → Deploy

### Parallel Team Strategy

Com dois desenvolvedores apos Phase 2:
- Dev A: Phase 3 (Resume Token) — T010 a T017
- Dev B: Phase 4 (Telegram Bot) — T018 a T024
- Ambos: Phase 5 (Polish)

---

## Notes

- [P] tasks = arquivos diferentes, sem dependencias entre si
- [Story] label mapeia task a user story especifica para rastreabilidade
- US1+US2 combinadas pois US2 e pre-requisito direto de US1
- US3+US4 combinadas pois US4 (webhook) e pre-requisito de US3 (bot)
- Commit apos cada task ou grupo logico
- Pare em qualquer checkpoint para validar a story independentemente
