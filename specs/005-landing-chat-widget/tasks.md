# Tasks: Landing Page com Chat Widget Bia

**Input**: Design documents from `/specs/005-landing-chat-widget/`
**Prerequisites**: plan.md (required), spec.md (required), data-model.md, contracts/widget-api.md, research.md, quickstart.md

**Tests**: Nao solicitados — testes manuais via quickstart.md.

**Organization**: Tasks agrupadas por user story para implementacao e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependencias)
- **[Story]**: Qual user story (US1, US2, US3, US4)
- Caminhos exatos incluidos nas descricoes

## Path Conventions

- **SPA frontend**: `src/` no root do repositorio
- Componentes: `src/components/chat/`
- Hooks: `src/hooks/`
- Paginas: `src/pages/`
- Servicos: `src/Services/`

---

## Phase 1: Setup

**Purpose**: Nenhuma nova dependencia necessaria. Apenas verificar estrutura existente.

- [x] T001 Verificar que src/Services/AgentService.ts exporta getChatConfig corretamente e que os tipos em src/types/agent.ts incluem AgentChatConfigInfo

---

## Phase 2: Foundational (Hook de Coleta Conversacional)

**Purpose**: Criar o hook que gerencia a coleta conversacional de dados dentro do chat. Este hook e prerequisito para o ChatWidget (US3, US4).

**CRITICAL**: O ChatWidget depende deste hook — DEVE estar pronto antes das fases de user story.

- [x] T002 Criar hook useChatWidget em src/hooks/useChatWidget.ts que: (1) recebe fieldsToCollect do useChat, (2) gerencia estado ConversationalCollectState (phase, pendingFields, currentField, collectedData), (3) gera mensagens do assistente pedindo cada campo em sequencia ("Qual seu nome?", "Qual seu email?", "Qual seu telefone?"), (4) processa respostas do usuario validando formato basico (email com @, telefone numerico), (5) quando todos os campos coletados, chama identify() do useChat, (6) expoe mensagens combinadas (coleta + chat normal) como array unico de ChatMessage

**Checkpoint**: Hook useChatWidget funcional — pode ser testado isoladamente com useChat mockado

---

## Phase 3: User Story 1 — Visitante navega na Landing Page (Priority: P1)

**Goal**: Landing page com hero section, secoes informativas e menu com link para Admin.

**Independent Test**: Acessar `http://localhost:5173/` e verificar conteudo institucional, menu e link para `/admin/agents`.

### Implementation for User Story 1

- [x] T003 [US1] Criar a landing page em src/pages/LandingPage.tsx usando a skill `/frontend-design`. A pagina DEVE conter: (1) Navbar com logo "Avachat" e link "Admin" apontando para `/admin/agents`, (2) Hero section com titulo, descricao e CTA, (3) Secoes informativas sobre o produto (features, como funciona, etc), (4) Footer. Usar Tailwind CSS para todo o estilo. Layout claro e profissional, responsivo (desktop + mobile).
- [x] T004 [US1] Modificar src/App.tsx: trocar o `<Navigate to="/admin/agents" />` na rota `/` por `<LandingPage />`. Importar LandingPage. Manter todas as outras rotas existentes inalteradas.

**Checkpoint**: Landing page visivel em `/` com navegacao para admin funcionando

---

## Phase 4: User Story 2 — Visitante ve o balao do chat (Priority: P1)

**Goal**: Balao flutuante fixo no canto inferior direito com a mensagem de boas-vindas da Bia.

**Independent Test**: Acessar landing page e verificar balao flutuante com mensagem "Oi, eu sou a Bia. Em que posso ajudar?" no canto inferior direito.

### Implementation for User Story 2

- [x] T005 [US2] Criar componente ChatBubble em src/components/chat/ChatBubble.tsx seguindo o contrato: props (message: string, onClick: () => void, isOpen: boolean). Estilo: position fixed, bottom-right (bottom-6 right-6), com icone de chat e balao de mensagem. Quando isOpen=true, ocultar o balao. Animacao sutil de entrada. Usar Tailwind CSS.

**Checkpoint**: Balao visivel na landing page (pode ser testado com um ChatBubble isolado no LandingPage)

---

## Phase 5: User Story 3 — Visitante abre e interage com o chat widget (Priority: P1)

**Goal**: Widget completo com conexao WebSocket, streaming de respostas e estado persistente na sessao.

**Independent Test**: Clicar no balao, widget abre, conecta ao agente Bia, enviar mensagem e receber resposta com streaming.

### Implementation for User Story 3

- [x] T006 [US3] Criar componente ChatWidget em src/components/chat/ChatWidget.tsx seguindo o contrato: props (slug: string, greeting: string). O componente DEVE: (1) buscar chat-config via AgentService.getChatConfig(slug) no mount, (2) gerenciar estado isOpen (toggle), (3) renderizar ChatBubble quando fechado, (4) quando aberto, renderizar painel flutuante (position fixed, bottom-6 right-6, largura ~400px, altura ~500px) com header (nome do agente + botao fechar X), area de mensagens reusando ChatWindow, (5) conectar WebSocket apenas quando aberto pela primeira vez (lazy connect), (6) usar useChatWidget para integrar coleta conversacional + chat normal, (7) preservar estado de mensagens ao fechar/reabrir. Se agente indisponivel (404 ou status 0), exibir mensagem de erro no widget.
- [x] T007 [US3] Integrar ChatWidget na LandingPage: adicionar `<ChatWidget slug="bia" greeting="Oi, eu sou a Bia. Em que posso ajudar?" />` no final do JSX de src/pages/LandingPage.tsx

**Checkpoint**: Widget abre, conecta ao agente, recebe e envia mensagens com streaming

---

## Phase 6: User Story 4 — Coleta de dados via chat conversacional (Priority: P2)

**Goal**: Perguntas de coleta de dados feitas dentro do chat como conversa, nao como formulario separado.

**Independent Test**: Configurar agente Bia com collectName=true, collectEmail=true, collectPhone=true. Abrir chat, verificar que assistente pergunta cada dado em sequencia dentro do chat.

### Implementation for User Story 4

- [x] T008 [US4] Verificar e ajustar a integracao entre useChatWidget e ChatWidget para garantir que: (1) quando useChat recebe collect_data, useChatWidget intercepta e inicia coleta conversacional, (2) mensagens do assistente ("Qual seu nome?", etc) aparecem na mesma lista de mensagens do ChatWindow, (3) respostas do usuario sao capturadas pelo useChatWidget (nao enviadas como mensagens normais), (4) apos coletar todos os campos, identify() e chamado automaticamente, (5) apos ready, transiciona para chat normal seamlessly. Testar cenarios: so nome, so email, nome+email+telefone, nenhum campo.

**Checkpoint**: Coleta conversacional funciona para qualquer combinacao de campos

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Melhorias que afetam multiplas user stories

- [x] T009 [P] Garantir responsividade do widget em telas mobile (largura 100% em telas < 640px, altura adaptativa)
- [x] T010 [P] Adicionar tratamento de erro no ChatWidget para: WebSocket desconectado (mostrar mensagem + opcao de reconectar), agente inativo (mensagem "indisponivel"), timeout de conexao
- [x] T011 Testar fluxo completo conforme quickstart.md: landing page → balao → widget → coleta → chat → fechar → reabrir → admin

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependencias — verificacao imediata
- **Foundational (Phase 2)**: Depende do Setup — BLOQUEIA user stories 3 e 4
- **US1 (Phase 3)**: Depende apenas do Setup — pode comecar em paralelo com Phase 2
- **US2 (Phase 4)**: Depende apenas do Setup — pode comecar em paralelo com Phase 2
- **US3 (Phase 5)**: Depende de Phase 2 (useChatWidget) + Phase 3 (LandingPage) + Phase 4 (ChatBubble)
- **US4 (Phase 6)**: Depende de Phase 5 (ChatWidget funcionando)
- **Polish (Phase 7)**: Depende de todas as user stories

### Parallel Opportunities

- T003 (LandingPage) e T005 (ChatBubble) podem rodar em paralelo
- T002 (useChatWidget) pode rodar em paralelo com T003 e T005
- T009 e T010 (polish) podem rodar em paralelo

### Within Each User Story

- Modelos/hooks antes de componentes
- Componentes antes de integracao
- Core antes de polish

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 + 3)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Hook foundational (T002)
3. Complete Phase 3: Landing Page (T003, T004) — em paralelo com Phase 2
4. Complete Phase 4: ChatBubble (T005) — em paralelo com Phase 2
5. Complete Phase 5: ChatWidget + integracao (T006, T007)
6. **STOP and VALIDATE**: Testar landing page + balao + chat completo
7. Deploy/demo if ready (MVP!)

### Incremental Delivery

1. Setup + Foundational + US1 + US2 → Landing page com balao (visual)
2. + US3 → Chat widget funcional (MVP!)
3. + US4 → Coleta conversacional refinada
4. + Polish → Responsividade e tratamento de erros
