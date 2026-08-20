# Feature Specification: Fluxo Start Session no Chat Widget

**Feature Branch**: `006-chat-start-session`
**Created**: 2026-04-08
**Status**: Draft
**Input**: User description: "Refatorar fluxo de chat para usar endpoint start-session REST antes do WebSocket"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Abertura do chat sem iniciar sessao (Priority: P1)

O visitante clica no balao do chat. O widget abre e exibe a
mensagem de apresentacao "Oi, eu sou a Bia. Em que posso ajudar?"
sem iniciar nenhuma sessao no backend.

**Why this priority**: E o ponto de entrada do fluxo. Nenhuma
conexao deve ser feita ate que os dados sejam coletados.

**Independent Test**: Clicar no balao, verificar que o widget abre
com a mensagem de boas-vindas e que nenhuma requisicao HTTP ou
WebSocket foi feita.

**Acceptance Scenarios**:

1. **Given** a landing page esta carregada, **When** o visitante
   clica no balao, **Then** o widget abre exibindo a mensagem
   "Oi, eu sou a Bia. Em que posso ajudar?".
2. **Given** o widget abriu, **When** nenhuma interacao ocorreu,
   **Then** nenhuma sessao foi criada no backend e nenhuma conexao
   WebSocket foi estabelecida.

---

### User Story 2 - Coleta conversacional de dados (Priority: P1)

Apos a mensagem de boas-vindas, o assistente pergunta os dados do
visitante conforme a configuracao do agente. As perguntas seguem
uma sequencia fixa com mensagens personalizadas:

1. Nome: "Qual seu nome?"
2. Email: "Bem vindo **nome**, qual seu email?"
3. Telefone: "Qual seu numero de telefone? Com DDD"

Cada campo e validado antes de prosseguir. Campos nao configurados
no agente sao pulados.

**Why this priority**: A coleta e obrigatoria antes de iniciar a
sessao. Sem os dados, o backend recusara o start-session.

**Independent Test**: Configurar agente com collectName, collectEmail
e collectPhone ativos. Abrir o chat e responder as perguntas.
Verificar que a sequencia esta correta e que validacoes funcionam.

**Acceptance Scenarios**:

1. **Given** o agente tem `collectName: true`, **When** o chat
   inicia, **Then** o assistente pergunta "Qual seu nome?".
2. **Given** o visitante respondeu o nome "Maria", e o agente tem
   `collectEmail: true`, **When** a resposta e aceita, **Then** o
   assistente pergunta "Bem vindo Maria, qual seu email?".
3. **Given** o visitante informa um email invalido (sem @),
   **When** o sistema valida, **Then** exibe mensagem de erro e
   pede novamente.
4. **Given** o visitante respondeu o email, e o agente tem
   `collectPhone: true`, **When** a resposta e aceita, **Then** o
   assistente pergunta "Qual seu numero de telefone? Com DDD".
5. **Given** o visitante informa um telefone invalido (menos de 10
   digitos), **When** o sistema valida, **Then** exibe mensagem de
   erro e pede novamente.
6. **Given** o agente tem `collectName: false`, `collectEmail: true`,
   **When** o chat inicia, **Then** pula a pergunta do nome e vai
   direto para o email sem a personalizacao do nome.

---

### User Story 3 - Iniciar sessao via start-session (Priority: P1)

Apos coletar todos os dados necessarios, o frontend envia uma
requisicao ao endpoint start-session do backend com os dados
coletados. O backend retorna o ID da sessao. O frontend exibe
"Muito obrigado pelas informacoes, agora em que posso ajudar?"
e conecta ao WebSocket usando o session ID.

**Why this priority**: Este e o ponto central da feature — a
sessao DEVE ser criada via REST antes de conectar o WebSocket.

**Independent Test**: Completar a coleta de dados, verificar que o
endpoint start-session foi chamado, que a mensagem de confirmacao
aparece e que a conexao WebSocket foi estabelecida com o session ID.

**Acceptance Scenarios**:

1. **Given** todos os campos obrigatorios foram coletados, **When**
   o sistema chama o endpoint start-session, **Then** o backend
   retorna um session ID valido.
2. **Given** o start-session retornou sucesso, **When** a resposta
   chega, **Then** o assistente exibe "Muito obrigado pelas
   informacoes, agora em que posso ajudar?".
3. **Given** a sessao foi criada, **When** o WebSocket conecta,
   **Then** usa a URL com o parametro sessionId do start-session.
4. **Given** a sessao foi criada e o WebSocket conectou, **When** o
   visitante digita uma mensagem, **Then** a mensagem e enviada ao
   agente e a resposta aparece em streaming.
5. **Given** o start-session falhou (ex: agente inativo), **When** o
   erro e recebido, **Then** exibe mensagem de erro no chat.

---

### User Story 4 - Fluxo sem coleta de dados (Priority: P2)

Se o agente nao exige nenhum dado (collectName, collectEmail e
collectPhone sao todos false), apos a mensagem de boas-vindas
o sistema chama o start-session imediatamente (sem body) e
exibe a mensagem de confirmacao.

**Why this priority**: Cenario alternativo que deve funcionar
corretamente para agentes sem coleta.

**Independent Test**: Configurar agente sem coleta de dados. Abrir
o chat e verificar que a sessao e iniciada automaticamente apos
a mensagem de boas-vindas.

**Acceptance Scenarios**:

1. **Given** o agente tem todos os collect como false, **When** o
   widget abre, **Then** exibe a mensagem de boas-vindas e
   automaticamente chama start-session.
2. **Given** o start-session retornou sucesso, **When** a conexao
   WebSocket e estabelecida, **Then** exibe "Muito obrigado pelas
   informacoes, agora em que posso ajudar?" e aceita mensagens.

---

### Edge Cases

- O endpoint start-session pode retornar 400 se campos obrigatorios
  estiverem faltando — o frontend deve ter validado antes.
- O endpoint pode retornar 404 se o agente nao existe.
- O endpoint pode retornar 400 se o agente esta inativo.
- A conexao WebSocket pode falhar apos o start-session — mostrar
  erro e permitir retry.
- O visitante pode fechar o widget durante a coleta — ao reabrir,
  o estado da coleta deve persistir.
- O visitante pode enviar mensagens antes da sessao ser criada —
  o input deve estar bloqueado ate a sessao existir.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Ao clicar no balao, o widget DEVE abrir e exibir a
  mensagem "Oi, eu sou a Bia. Em que posso ajudar?" sem criar
  sessao ou conexao WebSocket.
- **FR-002**: O sistema DEVE consultar o endpoint chat-config
  para determinar quais campos coletar (nome, email, telefone).
- **FR-003**: As perguntas de coleta DEVEM seguir a sequencia
  exata: nome → email → telefone, pulando campos nao configurados.
- **FR-004**: A pergunta do email DEVE incluir o nome do visitante:
  "Bem vindo **nome**, qual seu email?".
- **FR-005**: O sistema DEVE validar email (formato com @) e
  telefone (formato numerico valido com DDD) antes de prosseguir.
- **FR-006**: Apos coletar todos os dados, o sistema DEVE chamar o
  endpoint start-session com os dados coletados.
- **FR-007**: Apos o start-session retornar sucesso, o sistema DEVE
  exibir "Muito obrigado pelas informacoes, agora em que posso
  ajudar?".
- **FR-008**: A conexao WebSocket DEVE ser estabelecida apenas apos
  o start-session, usando o session ID retornado.
- **FR-009**: O campo de input de mensagem DEVE estar bloqueado ate
  que a sessao esteja ativa e o WebSocket conectado.
- **FR-010**: Se nenhum campo de coleta for necessario, o sistema
  DEVE chamar start-session automaticamente apos a boas-vindas.

### Key Entities

- **ChatSessionStartInfo**: Dados enviados ao start-session
  (userName, userEmail, userPhone).
- **ChatSessionInfo**: Resposta do start-session com chatSessionId.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O chat abre em menos de 1 segundo sem fazer nenhuma
  requisicao ao backend (exceto chat-config ja carregado).
- **SC-002**: A coleta conversacional tem taxa de conclusao de 100%
  para dados validos (sem campos pulados ou perdidos).
- **SC-003**: O tempo entre o envio dos dados e o inicio da sessao
  e inferior a 3 segundos.
- **SC-004**: Mensagens do visitante so sao aceitas apos a sessao
  estar ativa — tentativas anteriores sao bloqueadas.
- **SC-005**: O fluxo funciona corretamente para qualquer combinacao
  de campos de coleta (0, 1, 2 ou 3 campos).

## Assumptions

- O endpoint POST /api/agents/{slug}/sessions ja existe no backend.
- O WebSocket aceita o parametro sessionId na query string.
- O WebSocket envia ready imediatamente quando sessionId e fornecido.
- A mensagem de boas-vindas e fixa, vinda da prop greeting do
  ChatWidget.
- O fluxo legado (WebSocket com collect_data) nao sera mais usado
  pelo widget — substituido pelo novo fluxo REST + WebSocket.
