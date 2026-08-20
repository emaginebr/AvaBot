# Tasks: Área Administrativa WhatsApp

**Input**: Design documents from `/specs/010-whatsapp-admin/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Não solicitados — testes manuais via quickstart.md.

**Organization**: Tasks agrupadas por user story para implementação e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências)
- **[Story]**: User story associada (US1, US2, US3)
- Caminhos de arquivo exatos incluídos nas descrições

---

## Phase 1: Setup

**Purpose**: Não aplicável — projeto já inicializado, sem novas dependências.

_(Nenhuma task nesta fase)_

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Tipos TypeScript, métodos de API, rota e menu — pré-requisitos para todas as user stories.

**CRITICAL**: Nenhuma user story pode começar antes desta fase estar completa.

- [x] T001 [P] Criar interfaces `WhatsappQrCodeInfo` (campos: `agentSlug: string`, `qrCode: string`) e `WhatsappStatusInfo` (campos: `agentSlug: string`, `status: string`, `isConnected: boolean`) em `src/types/agent.ts`
- [x] T002 Adicionar 4 métodos ao AgentService em `src/Services/AgentService.ts`: `startWhatsappSession(slug: string): Promise<Result<object>>` (POST `/whatsapp/{slug}/start-session`), `getWhatsappQrCode(slug: string): Promise<Result<WhatsappQrCodeInfo>>` (GET `/whatsapp/{slug}/qrcode`), `getWhatsappStatus(slug: string): Promise<Result<WhatsappStatusInfo>>` (GET `/whatsapp/{slug}/status`), `disconnectWhatsapp(slug: string): Promise<Result<object>>` (POST `/whatsapp/{slug}/disconnect`). Todos com `AuthService.getAuthHeaders()` e `handleResponse()`. Importar os novos tipos.
- [x] T003 [P] Adicionar item de menu "WhatsApp" com `path: '/admin/whatsapp'` e `requiresAgent: true` no array `menuItems` logo abaixo de "Bot Telegram" em `src/components/admin/AdminSidebar.tsx`
- [x] T004 [P] Adicionar rota `<Route path="/admin/whatsapp" element={<WhatsappPage />} />` dentro do layout admin protegido em `src/App.tsx`, com import do componente

**Checkpoint**: Infraestrutura pronta — implementação das user stories pode começar.

---

## Phase 3: User Story 1 - Iniciar Sessão e QR Code (Priority: P1) MVP

**Goal**: O administrador inicia a sessão WhatsApp, vê o QR Code, escaneia com o celular, e o status atualiza automaticamente para "Conectado" via polling.

**Independent Test**: Acessar /admin/whatsapp, clicar "Iniciar Sessão", verificar QR Code exibido, escanear com celular, confirmar status "Conectado".

### Implementation for User Story 1

- [x] T005 [US1] Criar componente `WhatsappPage` em `src/pages/admin/WhatsappPage.tsx` com estrutura base: imports de `useAgentStore`, `AgentService`, `useState`, `useEffect`, `useRef`, `toast` (sonner). Layout com título "WhatsApp", subtítulo descritivo. Verificação de agente selecionado (exibir mensagem se nenhum selecionado). State variables: `status` (string), `isConnected` (boolean), `qrCode` (string | null), `starting` (boolean), `polling` (boolean ref).
- [x] T006 [US1] Implementar função `checkStatus` em `src/pages/admin/WhatsappPage.tsx` que chama `AgentService.getWhatsappStatus(slug)` e atualiza os states `status` e `isConnected`. Chamar `checkStatus` no `useEffect` ao montar o componente e quando o agente selecionado mudar.
- [x] T007 [US1] Implementar botão "Iniciar Sessão" em `src/pages/admin/WhatsappPage.tsx`: ao clicar, chama `AgentService.startWhatsappSession(slug)`, em caso de sucesso chama `AgentService.getWhatsappQrCode(slug)` para obter o QR Code. Exibir QR Code como `<img src="data:image/png;base64,{qrCode}" />` em tamanho adequado (256x256 ou similar). Botão desabilitado quando `isConnected` é true. Estado de loading durante a operação. Toast de erro se falhar.
- [x] T008 [US1] Implementar polling de status em `src/pages/admin/WhatsappPage.tsx`: após QR Code ser exibido, iniciar `setInterval` de 3 segundos que chama `checkStatus`. Quando `isConnected === true`: parar o polling (clearInterval), esconder o QR Code, exibir toast de sucesso "WhatsApp conectado!". Cleanup do interval no return do `useEffect` e quando o agente mudar. Usar `useRef` para armazenar o interval ID.

**Checkpoint**: US1 completa — administrador pode conectar WhatsApp via QR Code com feedback automático.

---

## Phase 4: User Story 2 - Visualizar Status da Conexão (Priority: P2)

**Goal**: O administrador visualiza o status da conexão com indicadores visuais claros (Conectado/Desconectado/Conectando).

**Independent Test**: Com sessão ativa, acessar a página e verificar indicador verde "Conectado". Sem sessão, verificar indicador cinza "Desconectado".

### Implementation for User Story 2

- [x] T009 [US2] Implementar seção de status visual em `src/pages/admin/WhatsappPage.tsx`: card com indicador de status usando cores — verde (bg-green-100 text-green-700) para "Conectado" (isConnected), amarelo (bg-yellow-100 text-yellow-700) para "Conectando..." (status === "STARTING" ou "QRCODE"), cinza (bg-gray-100 text-gray-500) para "Desconectado" (default). Incluir dot indicator animado (pulse) quando conectando. Exibir o slug do agente e o status textual traduzido.

**Checkpoint**: US2 completa — status visual sempre visível ao acessar a página.

---

## Phase 5: User Story 3 - Desconectar Sessão (Priority: P3)

**Goal**: O administrador desconecta a sessão WhatsApp com confirmação prévia.

**Independent Test**: Com sessão ativa, clicar "Desconectar", confirmar, verificar que status muda para "Desconectado".

### Implementation for User Story 3

- [x] T010 [US3] Implementar botão "Desconectar" em `src/pages/admin/WhatsappPage.tsx`: botão com estilo de alerta (border-red-200, text-red-600) que solicita confirmação via `confirm()`. Ao confirmar, chama `AgentService.disconnectWhatsapp(slug)`. Desabilitado quando `isConnected` é false. Estado de loading durante operação. Após sucesso: limpar QR Code, atualizar status para "Desconectado", parar polling se ativo, toast de sucesso. Toast de erro se falhar.

**Checkpoint**: US3 completa — administrador pode desconectar a sessão WhatsApp.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Melhorias visuais e de UX que afetam múltiplas stories.

- [x] T011 [P] Garantir que o polling para automaticamente quando o componente desmonta (cleanup no useEffect), quando o agente muda, e quando a página não está visível (document.hidden) em `src/pages/admin/WhatsappPage.tsx`
- [x] T012 [P] Adicionar responsividade mobile (QR Code centralizado, botões full-width em telas pequenas) e garantir que o QR Code tem tamanho adequado para escaneamento em `src/pages/admin/WhatsappPage.tsx`
- [x] T013 Executar validação manual completa seguindo `specs/010-whatsapp-admin/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A — projeto existente
- **Foundational (Phase 2)**: Sem dependências externas — BLOQUEIA todas as user stories
- **User Stories (Phase 3-5)**: Todas dependem da Phase 2
  - US1 (Phase 3): Sem dependência de outras stories — MVP
  - US2 (Phase 4): Depende de US1 (usa mesmos states de status)
  - US3 (Phase 5): Depende de US1 (precisa de sessão ativa para desconectar)
- **Polish (Phase 6)**: Depende de todas as user stories completas

### User Story Dependencies

- **US1 (P1)**: Pode iniciar após Phase 2 — MVP completo
- **US2 (P2)**: Depende de US1 (reutiliza states de status do componente)
- **US3 (P3)**: Depende de US1 (precisa dos botões e states base)

### Within Each User Story

- Todas as stories modificam o mesmo arquivo (`WhatsappPage.tsx`)
- Implementação sequencial obrigatória

### Parallel Opportunities

- **Phase 2**: T001 (types), T003 (sidebar), T004 (rota) podem rodar em paralelo
- **Phase 6**: T011 e T012 podem rodar em paralelo

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Paralelo — arquivos diferentes:
Task T001: "Tipos em agent.ts"
Task T003: "Menu item em AdminSidebar.tsx"
Task T004: "Rota em App.tsx"

# Sequencial — depende dos tipos:
Task T002: "Métodos em AgentService.ts" (depende de T001 para imports)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 2: Foundational (T001-T004)
2. Completar Phase 3: User Story 1 (T005-T008)
3. **STOP and VALIDATE**: Testar conexão via QR Code independentemente
4. Deploy/demo se pronto — administrador já pode conectar WhatsApp

### Incremental Delivery

1. Phase 2 → Foundation ready
2. US1 (T005-T008) → Iniciar sessão + QR Code + polling → MVP!
3. US2 (T009) → Status visual com indicadores → Incremento 2
4. US3 (T010) → Desconectar sessão → Incremento 3
5. Polish (T011-T013) → Refinamento final

---

## Notes

- Feature 100% frontend — backend já possui todos os endpoints via WPP Connect
- Todas as user stories modificam o mesmo arquivo (`WhatsappPage.tsx`) — implementação sequencial recomendada
- **Diferença do Telegram**: endpoints usam `slug` (string) ao invés de `agentId` (number)
- QR Code renderizado como `<img>` com data URI base64 — sem biblioteca adicional
- Polling com `setInterval` + `useRef` para interval ID + cleanup no `useEffect`
- Seguir padrões existentes: `TelegramBotPage.tsx` e `AgentSettingsPage.tsx` como referência
- Usar `toast.success()` / `toast.error()` da sonner para feedback
- Usar classes Tailwind do projeto: `bg-white rounded-xl border border-gray-200` para cards, `bg-ava-600` para botões primários
