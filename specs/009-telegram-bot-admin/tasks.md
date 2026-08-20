# Tasks: Área Administrativa Bot Telegram

**Input**: Design documents from `/specs/009-telegram-bot-admin/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Não solicitados — testes manuais via quickstart.md.

**Organization**: Tasks agrupadas por user story para implementação e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências)
- **[Story]**: User story associada (US1, US2, US3, US4)
- Caminhos de arquivo exatos incluídos nas descrições

---

## Phase 1: Setup

**Purpose**: Não aplicável — projeto já inicializado, sem novas dependências.

_(Nenhuma task nesta fase)_

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Tipos TypeScript, métodos de API, rota e menu — pré-requisitos para todas as user stories.

**CRITICAL**: Nenhuma user story pode começar antes desta fase estar completa.

- [x] T001 [P] Adicionar campos Telegram em AgentInfo interface (`telegramBotName`, `telegramBotToken`, `telegramWebhookSecret` como `string | null`) e em AgentInsertInfo interface (`telegramBotName`, `telegramBotToken` como `string | null`) em `frontend/src/types/agent.ts`
- [x] T002 [P] Criar interface `TelegramWebhookInfo` com campos `agentId: number`, `agentSlug: string`, `webhookUrl: string | null`, `isConfigured: boolean` em `frontend/src/types/agent.ts`
- [x] T003 Adicionar 3 métodos ao AgentService em `frontend/src/Services/AgentService.ts`: `setupTelegramWebhook(agentId: number): Promise<Result<TelegramWebhookInfo>>` (POST `/telegram/{id}/setup-webhook`), `getTelegramWebhookInfo(agentId: number): Promise<Result<TelegramWebhookInfo>>` (GET `/telegram/{id}/webhook-info`), `regenerateTelegramSecret(agentId: number): Promise<Result<TelegramWebhookInfo>>` (POST `/telegram/{id}/regenerate-secret`). Todos com `AuthService.getAuthHeaders()` e `handleResponse()`.
- [x] T004 [P] Adicionar item de menu "Bot Telegram" com `path: '/admin/telegram'` e `requiresAgent: true` no array `menuItems` logo abaixo de "Configurações" em `frontend/src/components/admin/AdminSidebar.tsx`
- [x] T005 [P] Adicionar rota `<Route path="telegram" element={<TelegramBotPage />} />` dentro do layout admin protegido em `frontend/src/App.tsx`, com import do componente

**Checkpoint**: Infraestrutura pronta — implementação das user stories pode começar.

---

## Phase 3: User Story 1 - Configurar Bot Telegram (Priority: P1) MVP

**Goal**: O administrador consegue preencher nome do bot, token, visualizar webhook secret e URL do bot, e salvar a configuração.

**Independent Test**: Acessar /admin/telegram, preencher nome (terminado em "bot") e token, salvar, recarregar a página e verificar que os dados persistem. Testar validação do nome sem "bot".

### Implementation for User Story 1

- [x] T006 [US1] Criar componente `TelegramBotPage` em `frontend/src/pages/admin/TelegramBotPage.tsx` com estrutura base: import de `useAgentStore`, `AgentService`, `useState`, `useEffect`, `toast` (sonner). Layout com título "Bot Telegram", subtítulo descritivo, e verificação de agente selecionado (exibir mensagem se nenhum selecionado).
- [x] T007 [US1] Implementar seção de formulário com 3 campos em `frontend/src/pages/admin/TelegramBotPage.tsx`: (1) "Nome do Bot" — input text, placeholder ex: "MeuBot_bot", com validação client-side regex `/bot$/i`; (2) "Bot Token" — input com toggle mostrar/ocultar (type password/text), placeholder ex: "123456:ABC-DEF..."; (3) "Webhook Secret" — input readonly exibindo valor de `selectedAgent.telegramWebhookSecret`, com visual de campo desabilitado.
- [x] T008 [US1] Implementar exibição da URL do bot em `frontend/src/pages/admin/TelegramBotPage.tsx`: quando `telegramBotName` estiver preenchido, exibir link clicável `https://t.me/{telegramBotName}` com ícone ou estilo de link externo. Ocultar quando campo vazio.
- [x] T009 [US1] Implementar lógica de salvar em `frontend/src/pages/admin/TelegramBotPage.tsx`: botão "Salvar" que chama `AgentService.update(agentId, { ...dadosAtuaisDoAgente, telegramBotName, telegramBotToken })`. Validar nome termina com "bot" antes de enviar. Exibir toast sucesso/erro. Chamar `loadAgents()` do store após sucesso. Carregar campos do `selectedAgent` no `useEffect` ao montar e quando agente mudar.

**Checkpoint**: US1 completa — administrador pode configurar e salvar dados do bot Telegram.

---

## Phase 4: User Story 2 - Configurar Webhook (Priority: P2)

**Goal**: O administrador clica "Configurar Webhook" e o sistema registra o webhook na API do Telegram.

**Independent Test**: Com bot configurado (nome + token), clicar "Configurar Webhook" e verificar toast de sucesso. Testar com token inválido para ver mensagem de erro.

### Implementation for User Story 2

- [x] T010 [US2] Adicionar botão "Configurar Webhook" em `frontend/src/pages/admin/TelegramBotPage.tsx`: botão primário (bg-ava-600) que chama `AgentService.setupTelegramWebhook(agentId)`. Desabilitado quando `telegramBotName` ou `telegramBotToken` estão vazios. Estado de loading durante a chamada. Toast de sucesso com URL do webhook retornada, ou toast de erro com mensagem da API.

**Checkpoint**: US2 completa — webhook pode ser configurado via botão.

---

## Phase 5: User Story 3 - Verificar Status do Webhook (Priority: P3)

**Goal**: O administrador clica "Verificar Webhook" e visualiza o status atual do webhook.

**Independent Test**: Com webhook configurado, clicar "Verificar Webhook" e confirmar que URL e status são exibidos corretamente.

### Implementation for User Story 3

- [x] T011 [US3] Adicionar botão "Verificar Webhook" em `frontend/src/pages/admin/TelegramBotPage.tsx`: botão secundário (border, text-gray) que chama `AgentService.getTelegramWebhookInfo(agentId)`. Exibir resultado em card/seção abaixo dos botões com: URL do webhook, status (configurado/não configurado com indicador visual verde/cinza). Estado de loading durante a chamada. Toast de erro se falhar.

**Checkpoint**: US3 completa — status do webhook pode ser verificado.

---

## Phase 6: User Story 4 - Regenerar Webhook Secret (Priority: P3)

**Goal**: O administrador regenera o webhook secret e o webhook é automaticamente re-registrado.

**Independent Test**: Clicar botão "Regenerar" ao lado do campo secret, verificar que o valor mudou e que toast de sucesso é exibido.

### Implementation for User Story 4

- [x] T012 [US4] Adicionar botão "Regenerar" ao lado do campo Webhook Secret em `frontend/src/pages/admin/TelegramBotPage.tsx`: botão pequeno (ícone ou texto "Regenerar") que chama `AgentService.regenerateTelegramSecret(agentId)`. Confirmação antes de executar (confirm dialog ou toast de aviso). Estado de loading. Após sucesso: chamar `loadAgents()` para atualizar o secret exibido, toast de sucesso. Toast de erro se falhar.

**Checkpoint**: US4 completa — secret pode ser regenerado com re-registro automático.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Melhorias visuais e de UX que afetam múltiplas stories.

- [x] T013 [P] Adicionar estados visuais de loading (spinner) nos botões durante operações assíncronas e estado empty quando nenhum agente selecionado em `frontend/src/pages/admin/TelegramBotPage.tsx`
- [x] T014 [P] Garantir responsividade mobile da página (grid layout, botões full-width em telas pequenas) em `frontend/src/pages/admin/TelegramBotPage.tsx`
- [x] T015 Executar validação manual completa seguindo `specs/009-telegram-bot-admin/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A — projeto existente
- **Foundational (Phase 2)**: Sem dependências externas — BLOQUEIA todas as user stories
- **User Stories (Phase 3-6)**: Todas dependem da Phase 2
  - US1 (Phase 3): Sem dependência de outras stories
  - US2 (Phase 4): Depende de US1 (precisa dos campos de formulário para desabilitar botão)
  - US3 (Phase 5): Independente de US2, mas faz mais sentido após US2
  - US4 (Phase 6): Independente, mas o campo secret é criado em US1
- **Polish (Phase 7)**: Depende de todas as user stories completas

### User Story Dependencies

- **US1 (P1)**: Pode iniciar após Phase 2 — MVP completo
- **US2 (P2)**: Depende de US1 (formulário com campos nome/token para validação do botão)
- **US3 (P3)**: Pode iniciar após Phase 2, mas recomendado após US2
- **US4 (P3)**: Pode iniciar após US1 (precisa do campo secret renderizado)

### Within Each User Story

- Todos os componentes de uma story são no mesmo arquivo (`TelegramBotPage.tsx`)
- Implementação incremental: cada story adiciona funcionalidade ao componente

### Parallel Opportunities

- **Phase 2**: T001+T002 (mesmo arquivo, mas seções diferentes), T004, T005 podem rodar em paralelo
- **Phase 7**: T013 e T014 podem rodar em paralelo

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Paralelo — arquivos diferentes:
Task T004: "Adicionar menu item em AdminSidebar.tsx"
Task T005: "Adicionar rota em App.tsx"

# Sequencial — mesmo arquivo:
Task T001 → T002: "Tipos em agent.ts"
Task T003: "Métodos em AgentService.ts" (depende de T001+T002 para imports de tipo)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 2: Foundational (T001-T005)
2. Completar Phase 3: User Story 1 (T006-T009)
3. **STOP and VALIDATE**: Testar configuração do bot independentemente
4. Deploy/demo se pronto — administrador já pode salvar dados do bot

### Incremental Delivery

1. Phase 2 → Foundation ready
2. US1 (T006-T009) → Configurar bot → MVP!
3. US2 (T010) → Configurar webhook → Incremento 2
4. US3 (T011) → Verificar webhook → Incremento 3
5. US4 (T012) → Regenerar secret → Incremento 4
6. Polish (T013-T015) → Refinamento final

---

## Notes

- Feature 100% frontend — backend já possui todos os endpoints
- Todas as user stories modificam o mesmo arquivo (`TelegramBotPage.tsx`) — implementação sequencial recomendada
- Seguir padrões existentes: `AgentSettingsPage.tsx` como referência de layout, `AgentService.ts` para padrão de API calls
- Usar `toast.success()` / `toast.error()` da sonner para feedback
- Usar classes Tailwind do projeto: `bg-white rounded-xl border border-gray-200` para cards, `bg-ava-600` para botões primários
