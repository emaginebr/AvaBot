# Tasks: Configuracao Telegram por Agente

**Input**: Design documents from `/specs/003-telegram-per-agent-config/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Nao solicitados na spec. Tarefas de teste nao incluidas.

**Organization**: Tasks agrupadas por user story para implementacao e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependencias)
- **[Story]**: User story associada (US1, US2, US3, US4)
- Caminhos de arquivo incluidos nas descricoes

---

## Phase 1: Setup

**Purpose**: Nenhuma inicializacao de projeto necessaria - projeto ja existente.

Nenhuma tarefa nesta fase.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Alteracoes no modelo, banco de dados, DTOs e DI que DEVEM estar completas antes de qualquer user story.

**CRITICAL**: Nenhuma user story pode comecar ate esta fase estar completa.

- [x] T001 [P] Adicionar propriedades TelegramBotName, TelegramBotToken e TelegramWebhookSecret ao modelo Agent em AvaBot.Domain/Models/Agent.cs
- [x] T002 [P] Adicionar campos telegramBotName, telegramBotToken e telegramWebhookSecret ao AgentInfo DTO em AvaBot.DTO/AgentDTOs.cs
- [x] T003 [P] Adicionar campos telegramBotName e telegramBotToken ao AgentInsertInfo DTO em AvaBot.DTO/AgentDTOs.cs
- [x] T004 [P] Criar TelegramWebhookInfo DTO com campos agentId, agentSlug, webhookUrl, isConfigured em AvaBot.DTO/AgentDTOs.cs
- [x] T005 Adicionar configuracao Fluent API para as 3 novas colunas (telegram_bot_name, telegram_bot_token, telegram_webhook_secret) e unique filtered index em telegram_bot_token no AvaBotContext em AvaBot.Infra/Context/AvaBotContext.cs
- [x] T006 Gerar migration EF Core AddTelegramFieldsToAgent via dotnet ef migrations add no projeto AvaBot.Infra com startup-project AvaBot.API
- [x] T007 [P] Adicionar metodo GetByTelegramBotTokenAsync a IAgentRepository em AvaBot.Infra.Interfaces/Repository/IAgentRepository.cs
- [x] T008 Implementar GetByTelegramBotTokenAsync no AgentRepository em AvaBot.Infra/Repository/AgentRepository.cs
- [x] T009 Atualizar mapeamento AutoMapper no AgentProfile para incluir novos campos Telegram em AvaBot.Application/Profiles/AgentProfile.cs
- [x] T010 Remover registro singleton de ITelegramBotClient e remover secao Telegram do DependencyInjection em AvaBot.Application/DependencyInjection.cs
- [x] T011 [P] Remover secao Telegram do appsettings.Docker.json em AvaBot.API/appsettings.Docker.json
- [x] T012 [P] Remover variaveis TELEGRAM_* do .env.example (se existir) e do docker-compose.yml (se existir)

**Checkpoint**: Fundacao pronta - modelo, banco, DTOs e DI atualizados. User stories podem comecar.

---

## Phase 3: User Story 1 - Configurar Bot Telegram no Agente (Priority: P1) MVP

**Goal**: Permitir que o administrador configure TelegramBotName e TelegramBotToken em um agente, com geracao automatica do TelegramWebhookSecret. Registrar o webhook no Telegram automaticamente.

**Independent Test**: Criar/atualizar um agente com TelegramBotToken via API e verificar que o secret foi gerado e o webhook registrado na URL `https://avabot.net/api/telegram/{slug}/webhook`.

### Implementation for User Story 1

- [x] T013 [US1] Atualizar AgentService.CreateAsync para gerar TelegramWebhookSecret automaticamente quando TelegramBotToken for fornecido, e validar unicidade do token via GetByTelegramBotTokenAsync em AvaBot.Application/Services/AgentService.cs
- [x] T014 [US1] Atualizar AgentService.UpdateAsync para gerar TelegramWebhookSecret quando TelegramBotToken for configurado pela primeira vez, e validar unicidade do token em AvaBot.Application/Services/AgentService.cs
- [x] T015 [US1] Adicionar metodo SetupWebhookAsync(long agentId) ao TelegramService que cria TelegramBotClient dinamicamente com token do agente, registra webhook na URL https://avabot.net/api/telegram/{slug}/webhook em AvaBot.Application/Services/TelegramService.cs
- [x] T016 [US1] Criar endpoint POST /api/telegram/{id}/setup-webhook com [Authorize] no TelegramController que chama TelegramService.SetupWebhookAsync(agentId) em AvaBot.API/Controllers/TelegramController.cs

**Checkpoint**: Agentes podem ser configurados com bot Telegram. Webhook registrado no Telegram via endpoint dedicado.

---

## Phase 4: User Story 2 - Receber Mensagens pelo Webhook por Slug (Priority: P1)

**Goal**: Receber e processar mensagens do Telegram via webhook identificado pelo slug do agente, roteando para o agente correto.

**Independent Test**: Enviar request simulado para POST /api/telegram/{slug}/webhook com header secret correto e verificar que a mensagem e processada pelo agente correspondente.

### Implementation for User Story 2

- [x] T017 [US2] Refatorar TelegramService: remover campos _agentSlug, _webhookUrl, _webhookSecret do construtor. Remover dependencia de IConfiguration para config Telegram. Remover dependencia de ITelegramBotClient singleton em AvaBot.Application/Services/TelegramService.cs
- [x] T018 [US2] Refatorar TelegramService.ProcessUpdateAsync para receber Agent como parametro (ou slug) e criar TelegramBotClient dinamicamente com token do agente em AvaBot.Application/Services/TelegramService.cs
- [x] T019 [US2] Refatorar HandleStartCommandAsync e HandleTextMessageAsync para usar o Agent recebido por parametro em vez de buscar por _agentSlug em AvaBot.Application/Services/TelegramService.cs
- [x] T020 [US2] Refatorar SendMessageAsync para receber ITelegramBotClient como parametro (ou criar internamente a partir do token do agente) em AvaBot.Application/Services/TelegramService.cs
- [x] T021 [US2] Criar endpoint POST /api/telegram/{slug}/webhook com [AllowAnonymous] no TelegramController. Resolver agente pelo slug, validar TelegramWebhookSecret via header, chamar TelegramService.ProcessUpdateAsync em AvaBot.API/Controllers/TelegramController.cs
- [x] T022 [US2] Remover endpoints antigos POST /telegram/webhook e POST /telegram/setup-webhook do TelegramController em AvaBot.API/Controllers/TelegramController.cs

**Checkpoint**: Multiplos bots Telegram funcionando simultaneamente. Mensagens roteadas pelo slug na URL do webhook.

---

## Phase 5: User Story 3 - Gerar Novo Webhook Secret (Priority: P2)

**Goal**: Permitir que o administrador regenere o webhook secret de um agente e atualize automaticamente o webhook no Telegram.

**Independent Test**: Chamar endpoint de regeneracao, verificar que novo secret foi persistido e que o webhook no Telegram foi atualizado.

### Implementation for User Story 3

- [x] T023 [US3] Implementar metodo RegenerateWebhookSecretAsync(long agentId) no TelegramService que gera novo secret, persiste no banco e atualiza webhook no Telegram em AvaBot.Application/Services/TelegramService.cs
- [x] T024 [US3] Criar endpoint POST /api/telegram/{id}/regenerate-secret com [Authorize] no TelegramController em AvaBot.API/Controllers/TelegramController.cs

**Checkpoint**: Administradores podem regenerar secrets com atualizacao automatica no Telegram.

---

## Phase 6: User Story 4 - Verificar Configuracao do Webhook (Priority: P2)

**Goal**: Permitir que o administrador consulte a URL do webhook atualmente registrada no Telegram para um agente.

**Independent Test**: Chamar endpoint de verificacao e comparar URL retornada com a esperada para o slug do agente.

### Implementation for User Story 4

- [x] T025 [US4] Implementar metodo GetWebhookInfoAsync(long agentId) no TelegramService que cria TelegramBotClient dinamico e chama getWebhookInfo na API do Telegram em AvaBot.Application/Services/TelegramService.cs
- [x] T026 [US4] Criar endpoint GET /api/telegram/{id}/webhook-info com [Authorize] no TelegramController em AvaBot.API/Controllers/TelegramController.cs

**Checkpoint**: Administradores podem diagnosticar a configuracao do webhook de qualquer agente.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Limpeza, documentacao e validacao final.

- [x] T027 [P] Atualizar collection Bruno com novas rotas (setup-webhook, webhook-info, regenerate-secret, webhook por slug) em bruno/Telegram/
- [x] T028 [P] Atualizar documentacao docs/TELEGRAM_SETUP.md com novo fluxo de configuracao por agente
- [x] T029 [P] Criar script SQL de migracao manual em scripts/ para ambientes sem EF migrations
- [x] T030 Validar build completo com dotnet build e testar fluxo end-to-end conforme quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Nenhuma tarefa
- **Foundational (Phase 2)**: BLOQUEIA todas as user stories
- **US1 (Phase 3)**: Depende de Phase 2. Primeira story a implementar.
- **US2 (Phase 4)**: Depende de Phase 2 e Phase 3 (precisa do setup-webhook funcional e da config de agente)
- **US3 (Phase 5)**: Depende de Phase 2 e Phase 3 (precisa de agente com Telegram configurado)
- **US4 (Phase 6)**: Depende de Phase 2 e Phase 3 (precisa de agente com Telegram configurado)
- **Polish (Phase 7)**: Depende de todas as user stories

### User Story Dependencies

- **US1 (P1)**: Apos Phase 2. Base para todas as outras stories.
- **US2 (P1)**: Apos US1. Depende da refatoracao do TelegramService que US1 inicia.
- **US3 (P2)**: Apos US1. Pode rodar em paralelo com US2 se coordenado.
- **US4 (P2)**: Apos US1. Pode rodar em paralelo com US2 e US3.

### Within Each User Story

- Models/DTOs antes de services
- Services antes de endpoints
- Core antes de integracao

### Parallel Opportunities

- Phase 2: T001, T002, T003, T004 em paralelo (arquivos diferentes)
- Phase 2: T007, T011, T012 em paralelo
- Phase 5 e Phase 6: US3 e US4 podem rodar em paralelo apos US1
- Phase 7: T027, T028, T029 em paralelo

---

## Parallel Example: Phase 2 (Foundational)

```text
# Batch 1 - Modelo e DTOs (arquivos diferentes):
T001: Agent.cs (Domain)
T002: AgentInfo DTO (DTO)
T003: AgentInsertInfo DTO (DTO) -- mesmo arquivo que T002, executar junto
T004: TelegramWebhookInfo DTO (DTO) -- mesmo arquivo, executar junto com T002/T003

# Batch 2 - Infra (depende de T001):
T005: AvaBotContext.cs
T007: IAgentRepository.cs

# Batch 3 - Pos-infra:
T006: Migration (depende de T005)
T008: AgentRepository.cs (depende de T007)
T009: AgentProfile.cs

# Batch 4 - Cleanup (independente):
T010: DependencyInjection.cs
T011: appsettings.Docker.json
T012: .env.example / docker-compose.yml
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2)

1. Complete Phase 2: Foundational (modelo, banco, DTOs, DI)
2. Complete Phase 3: US1 - Configurar Bot no Agente
3. Complete Phase 4: US2 - Receber Mensagens por Slug
4. **STOP and VALIDATE**: Testar configuracao de bot e envio de mensagens
5. Deploy/demo se pronto

### Incremental Delivery

1. Phase 2 → Fundacao pronta
2. + US1 → Agentes configuraveis com Telegram
3. + US2 → Webhooks multi-bot funcionais (MVP completo!)
4. + US3 → Regeneracao de secrets
5. + US4 → Diagnostico de webhooks
6. + Polish → Documentacao e validacao final

---

## Notes

- [P] tasks = arquivos diferentes, sem dependencias
- [Story] label mapeia task para user story especifica
- Cada user story deve ser independentemente completavel e testavel
- Commit apos cada task ou grupo logico
- Pare em qualquer checkpoint para validar a story
- Usar skill `dotnet-architecture` conforme Constitution para alteracoes em entidades/services/repositories
