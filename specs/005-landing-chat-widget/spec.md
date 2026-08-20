# Feature Specification: Landing Page com Chat Widget Bia

**Feature Branch**: `005-landing-chat-widget`
**Created**: 2026-04-08
**Status**: Draft
**Input**: User description: "Landing page com chat widget flutuante usando Agent Bia (slug: bia)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visitante navega na Landing Page (Priority: P1)

Um visitante acessa a URL raiz do site e visualiza uma landing page
clara e profissional que apresenta o produto/serviço Avachat. A
navegacao inclui um menu com link para a area administrativa.

**Why this priority**: A landing page e o ponto de entrada principal
para todos os visitantes. Sem ela, nao ha contexto para o widget.

**Independent Test**: Acessar `http://localhost:5173/` e verificar que
a pagina exibe conteudo institucional, menu de navegacao e link para
`/admin/agents`.

**Acceptance Scenarios**:

1. **Given** o visitante acessa a URL raiz, **When** a pagina carrega,
   **Then** exibe uma landing page com hero section, secoes informativas
   e menu de navegacao.
2. **Given** a landing page esta visivel, **When** o visitante clica no
   link "Admin" no menu, **Then** e redirecionado para `/admin/agents`.

---

### User Story 2 - Visitante ve o balao do chat (Priority: P1)

Um visitante na landing page ve um balao flutuante no canto inferior
direito com a mensagem "Oi, eu sou a Bia. Em que posso ajudar?". O
balao e visivel sem interacao do usuario.

**Why this priority**: O balao e o ponto de entrada para o chat e deve
estar sempre visivel para incentivar a interacao.

**Independent Test**: Acessar a landing page e verificar que o balao
flutuante aparece no canto inferior direito com a mensagem correta.

**Acceptance Scenarios**:

1. **Given** a landing page esta carregada, **When** o visitante olha
   para o canto inferior direito, **Then** ve um balao com a mensagem
   "Oi, eu sou a Bia. Em que posso ajudar?".
2. **Given** o visitante rola a pagina, **When** o balao permanece fixo,
   **Then** ele continua visivel na mesma posicao (position: fixed).

---

### User Story 3 - Visitante abre e interage com o chat widget (Priority: P1)

Ao clicar no balao, abre-se um widget de chat flutuante. O sistema
conecta ao agente Bia (slug: `bia`) via WebSocket. Se o agente exigir
coleta de dados (nome, email, telefone), essas perguntas sao feitas
dentro do proprio chat como mensagens do assistente, nao como formulario
separado. Apos identificacao, o visitante pode conversar normalmente.

**Why this priority**: O chat e a funcionalidade principal da landing
page — sem ele, nao ha proposito para a pagina.

**Independent Test**: Clicar no balao, verificar que o widget abre,
responder as perguntas de identificacao no chat (se configuradas) e
enviar uma mensagem ao agente.

**Acceptance Scenarios**:

1. **Given** o visitante clica no balao, **When** o widget abre,
   **Then** conecta via WebSocket ao agente Bia (`/ws/chat/bia`).
2. **Given** o agente Bia tem `collectName: true`, **When** o chat
   inicia, **Then** o assistente pergunta o nome dentro do chat
   (nao em formulario separado).
3. **Given** o visitante respondeu todas as perguntas de identificacao,
   **When** o servidor envia a mensagem `ready`, **Then** o visitante
   pode digitar e enviar mensagens normais.
4. **Given** o visitante envia uma mensagem, **When** o agente responde,
   **Then** a resposta e exibida em tempo real via streaming (chunks).
5. **Given** o widget esta aberto, **When** o visitante clica no botao
   de fechar, **Then** o widget fecha e o balao reaparece.

---

### User Story 4 - Coleta de dados via chat conversacional (Priority: P2)

O sistema busca a configuracao do agente (`chat-config`) para saber
quais campos coletar. Em vez de exibir um formulario HTML, o
assistente faz perguntas uma a uma dentro do chat. O visitante responde
como se estivesse conversando. Os dados sao enviados via mensagem
`identify` ao servidor.

**Why this priority**: A coleta conversacional e uma melhoria de UX
sobre formularios tradicionais, mas depende do chat basico funcionar.

**Independent Test**: Configurar o agente Bia com coleta de nome, email
e telefone. Abrir o chat e verificar que o assistente pergunta cada
dado em sequencia e so envia `identify` apos coletar todos os campos.

**Acceptance Scenarios**:

1. **Given** o agente tem `collectName: true`, `collectEmail: true`,
   `collectPhone: true`, **When** o chat inicia, **Then** o assistente
   pergunta primeiro o nome.
2. **Given** o visitante respondeu o nome, **When** a resposta e
   validada, **Then** o assistente pergunta o email.
3. **Given** o visitante respondeu o email, **When** a resposta e
   validada, **Then** o assistente pergunta o telefone.
4. **Given** todos os campos foram coletados, **When** o sistema envia
   a mensagem `identify`, **Then** o servidor responde com `ready`.

---

### Edge Cases

- O agente Bia pode estar inativo (status 0) — o widget deve exibir
  mensagem de indisponibilidade.
- O endpoint `chat-config` pode retornar 404 se o slug estiver errado.
- A conexao WebSocket pode falhar — o widget deve mostrar mensagem de
  erro e permitir reconexao.
- O visitante pode fechar o widget e reabrir — o estado do chat deve
  persistir durante a sessao da pagina.
- O visitante pode enviar mensagem vazia — deve ser ignorada.
- Campos de coleta podem ser parciais (ex: so nome, sem email/telefone).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE exibir uma landing page ao acessar a URL
  raiz (`/`).
- **FR-002**: A landing page DEVE conter um menu de navegacao com link
  para a area administrativa (`/admin/agents`).
- **FR-003**: O sistema DEVE exibir um balao flutuante fixo no canto
  inferior direito com a mensagem "Oi, eu sou a Bia. Em que posso
  ajudar?".
- **FR-004**: Ao clicar no balao, o sistema DEVE abrir um widget de
  chat flutuante como componente separado.
- **FR-005**: O widget DEVE conectar via WebSocket ao agente Bia
  (slug: `bia`).
- **FR-006**: O widget DEVE consultar o endpoint `chat-config` para
  determinar quais campos de dados coletar.
- **FR-007**: As perguntas de coleta de dados (nome, email, telefone)
  DEVEM ser feitas dentro do chat como mensagens do assistente, nao
  como formulario HTML separado.
- **FR-008**: O widget DEVE exibir as respostas do agente em tempo
  real via streaming (chunks do WebSocket).
- **FR-009**: O widget DEVE permitir ser fechado e reaberto sem perder
  o historico da conversa durante a sessao.
- **FR-010**: O widget DEVE ser um componente React separado e
  reutilizavel.

### Key Entities

- **Landing Page**: Pagina principal com hero section, secoes de
  conteudo e menu de navegacao.
- **Chat Widget**: Componente flutuante que encapsula a conexao
  WebSocket, a coleta de dados conversacional e a interface de chat.
- **Chat Bubble**: Balao de notificacao que exibe a mensagem de
  boas-vindas e serve como gatilho para abrir o widget.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Visitantes conseguem visualizar a landing page e navegar
  para a area administrativa em menos de 2 cliques.
- **SC-002**: O balao de chat e visivel em todas as paginas sem
  obstruir conteudo essencial.
- **SC-003**: Visitantes conseguem iniciar uma conversa com o agente
  em ate 5 segundos apos clicar no balao.
- **SC-004**: A coleta de dados via chat tem taxa de conclusao
  equivalente ou superior a formularios tradicionais.
- **SC-005**: As respostas do agente aparecem em tempo real sem
  atraso perceptivel entre chunks.
- **SC-006**: O widget funciona corretamente em telas desktop e
  mobile (responsivo).

## Assumptions

- O agente Bia (slug: `bia`) ja existe e esta ativo no backend.
- O backend esta rodando e acessivel via `VITE_API_URL` e `VITE_WS_URL`.
- A landing page e uma single-page application dentro do mesmo projeto
  React existente.
- O layout da landing page sera criado usando a skill `frontend-design`.
- O chat widget e um componente independente que pode ser reutilizado
  em outras paginas no futuro.
- Nao ha autenticacao necessaria para o visitante acessar a landing
  page ou usar o chat.
