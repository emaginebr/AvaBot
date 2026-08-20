# Feature Specification: Autenticação JWT e Painel Administrativo

**Feature Branch**: `007-admin-auth-panel`  
**Created**: 2026-04-09  
**Status**: Draft  
**Input**: User description: "Implementar autenticação JWT, tela de login, área administrativa com gestão de agentes, sessões e base de conhecimento"

## Clarifications

### Session 2026-04-09

- Q: Como o administrador navega entre seções (Configurações, Sessões, Base de Conhecimento) após selecionar um agente? → A: Menu lateral (sidebar) com as seções como itens do menu
- Q: Como o histórico de conversa de uma sessão deve ser exibido? → A: Página dedicada (nova rota) com o histórico completo da conversa
- Q: Qual a tela padrão exibida após o login? → A: Dashboard com resumo geral (total de agentes, sessões recentes, etc.)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Login com JWT Token (Priority: P1)

O administrador acessa a aplicação e é apresentado a uma tela de login. Ele informa suas credenciais (usuário e senha) e, ao submeter, recebe um token JWT que o autentica para acessar as funcionalidades administrativas. Se as credenciais estiverem incorretas, uma mensagem de erro é exibida. O token é armazenado localmente e utilizado em todas as requisições autenticadas. Rotas administrativas são protegidas e redirecionam para o login caso o usuário não esteja autenticado. Após login bem-sucedido, o administrador é direcionado a um dashboard com resumo geral (total de agentes, sessões recentes, etc.).

**Why this priority**: Sem autenticação, nenhuma funcionalidade administrativa pode ser acessada de forma segura. É o alicerce para todas as demais funcionalidades.

**Independent Test**: Pode ser testado acessando a URL de login, inserindo credenciais e verificando se o redirecionamento para a área administrativa ocorre. Tentativas com credenciais inválidas devem mostrar erro.

**Acceptance Scenarios**:

1. **Given** o usuário não está autenticado, **When** acessa qualquer rota administrativa, **Then** é redirecionado para a tela de login
2. **Given** o usuário está na tela de login, **When** insere credenciais válidas e submete, **Then** recebe um token JWT e é redirecionado para o dashboard com resumo geral
3. **Given** o usuário está na tela de login, **When** insere credenciais inválidas e submete, **Then** vê uma mensagem de erro informando que as credenciais são inválidas
4. **Given** o usuário está autenticado, **When** o token expira, **Then** é redirecionado para a tela de login na próxima requisição
5. **Given** o usuário está autenticado, **When** clica em "Sair", **Then** o token é removido e ele é redirecionado para a tela de login

---

### User Story 2 - Gestão de Agentes na Área Administrativa (Priority: P2)

Após autenticado, o administrador acessa a área administrativa que possui um menu lateral (sidebar) para navegação entre as seções. Na barra de navegação superior (navbar), há um seletor que permite escolher o agente ativo para as operações contextuais. A sidebar exibe as seções contextuais ao agente selecionado: Configurações (Editar/Excluir Agente), Sessões e Base de Conhecimento (Arquivos/Busca). O administrador pode visualizar a lista de agentes cadastrados, criar novos agentes, editar agentes existentes e excluir agentes. A lista de agentes e os formulários de cadastro/edição existentes devem ter o layout melhorado para uma experiência mais profissional e intuitiva.

**Why this priority**: A gestão de agentes é a funcionalidade central da aplicação. Sem agentes configurados, as demais funcionalidades (sessões e base de conhecimento) não têm contexto.

**Independent Test**: Pode ser testado criando, editando e excluindo agentes, e verificando que o seletor de agente na navbar reflete as mudanças.

**Acceptance Scenarios**:

1. **Given** o administrador está autenticado, **When** acessa a área administrativa, **Then** vê a lista de agentes cadastrados com layout profissional
2. **Given** o administrador está na lista de agentes, **When** clica em "Novo Agente", **Then** é apresentado um formulário de cadastro com campos: nome, descrição, prompt do sistema, e opções de coleta de dados (nome, email, telefone)
3. **Given** o administrador preencheu o formulário de agente, **When** submete com dados válidos, **Then** o agente é criado e aparece na lista
4. **Given** o administrador está na área administrativa, **When** seleciona um agente na navbar, **Then** o agente é definido como ativo e as funcionalidades contextuais (configurações, sessões, base de conhecimento) passam a operar no contexto desse agente
5. **Given** o administrador selecionou um agente, **When** acessa "Editar Agente" nas configurações, **Then** vê o formulário preenchido com os dados do agente e pode alterá-los
6. **Given** o administrador selecionou um agente, **When** acessa "Excluir Agente" nas configurações e confirma, **Then** o agente é removido da lista
7. **Given** o administrador altera o status de um agente, **When** clica em ativar/desativar, **Then** o status do agente é alternado

---

### User Story 3 - Listagem de Sessões e Histórico de Conversa (Priority: P3)

Com um agente selecionado na navbar, o administrador pode acessar a seção "Sessões" para visualizar todas as sessões de chat daquele agente. A lista mostra informações do usuário (nome, email, telefone), data de início, data de término e quantidade de mensagens. Ao clicar em uma sessão, o administrador acessa uma visualização completa do histórico de conversa, exibindo todas as mensagens trocadas em formato de chat (bolhas de conversa), com distinção visual clara entre mensagens do usuário e do assistente, incluindo timestamps de cada mensagem. O histórico é paginado para sessões com muitas mensagens e permite ao administrador ler toda a conversa de forma cronológica.

**Why this priority**: Permite ao administrador acompanhar o uso do agente, avaliar a qualidade das respostas do assistente e identificar oportunidades de melhoria no prompt ou na base de conhecimento.

**Independent Test**: Pode ser testado selecionando um agente, acessando a lista de sessões, clicando em uma sessão e verificando que todas as mensagens aparecem em ordem cronológica com identificação do remetente.

**Acceptance Scenarios**:

1. **Given** o administrador selecionou um agente na navbar, **When** acessa a seção "Sessões", **Then** vê a lista paginada de sessões daquele agente com nome do usuário, email, telefone, data de início, data de término e quantidade de mensagens
2. **Given** a lista de sessões está exibida, **When** o administrador navega entre páginas, **Then** os dados são carregados corretamente com paginação
3. **Given** a lista de sessões está exibida, **When** o administrador clica em uma sessão, **Then** é redirecionado para uma página dedicada (nova rota) com o histórico completo da conversa
4. **Given** o histórico de conversa está aberto na página dedicada, **When** o administrador visualiza as mensagens, **Then** vê todas as mensagens em ordem cronológica com distinção visual entre mensagens do usuário (alinhadas à direita) e do assistente (alinhadas à esquerda), cada uma com seu timestamp
5. **Given** a sessão possui muitas mensagens, **When** o administrador navega no histórico, **Then** as mensagens são carregadas com paginação
6. **Given** o administrador está no histórico de conversa, **When** clica em voltar, **Then** retorna à lista de sessões mantendo a página anterior
7. **Given** não há sessões para o agente, **When** o administrador acessa a seção, **Then** vê uma mensagem informando que não há sessões

---

### User Story 4 - Base de Conhecimento do Agente (Priority: P4)

Com um agente selecionado, o administrador pode acessar a seção "Base de Conhecimento" que contém duas sub-funcionalidades: Arquivos e Busca. Na sub-seção "Arquivos", pode visualizar a lista de arquivos carregados (com nome, tamanho, status de processamento e datas), fazer upload de novos arquivos (.md), excluir arquivos e reprocessar arquivos com erro. Na sub-seção "Busca", pode realizar buscas na base de conhecimento indexada no Elasticsearch, informando um termo e visualizando os resultados.

**Why this priority**: A base de conhecimento é o que dá inteligência contextual ao agente. Gerenciá-la é essencial, mas depende dos agentes já estarem configurados.

**Independent Test**: Pode ser testado fazendo upload de um arquivo .md, verificando o status de processamento, buscando por um termo presente no arquivo e vendo os resultados.

**Acceptance Scenarios**:

1. **Given** o administrador selecionou um agente, **When** acessa "Base de Conhecimento > Arquivos", **Then** vê a lista de arquivos com nome, tamanho, status de processamento e datas
2. **Given** o administrador está na lista de arquivos, **When** faz upload de um arquivo .md, **Then** o arquivo aparece na lista com status "Processando"
3. **Given** um arquivo tem status "Erro", **When** o administrador clica em reprocessar, **Then** o processamento é reiniciado
4. **Given** o administrador está na lista de arquivos, **When** clica em excluir um arquivo e confirma, **Then** o arquivo é removido da lista e os dados indexados são removidos
5. **Given** o administrador selecionou um agente, **When** acessa "Base de Conhecimento > Busca" e insere um termo, **Then** vê os resultados da busca no Elasticsearch com os trechos relevantes
6. **Given** a busca não encontra resultados, **When** o administrador busca por um termo inexistente, **Then** vê uma mensagem informando que nenhum resultado foi encontrado

---

### Edge Cases

- O que acontece quando o token JWT expira durante uma operação? O sistema deve tratar graciosamente, informar o usuário e redirecionar para o login.
- O que acontece quando o administrador tenta excluir o agente que está selecionado na navbar? O seletor deve ser limpo e o contexto resetado.
- O que acontece quando um upload de arquivo falha por exceder o limite de 10MB? O sistema deve informar o limite máximo antes ou durante o upload.
- O que acontece quando o administrador tenta acessar sessões sem ter selecionado um agente? O sistema deve orientar a selecionar um agente primeiro.
- O que acontece quando há erro de rede durante a busca na base de conhecimento? O sistema deve exibir mensagem de erro amigável.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE apresentar uma tela de login com campos de usuário e senha
- **FR-002**: O sistema DEVE autenticar o usuário via endpoint POST /auth/login e armazenar o token JWT retornado
- **FR-003**: O sistema DEVE incluir o token JWT no header Authorization (Bearer) em todas as requisições a endpoints protegidos
- **FR-004**: O sistema DEVE proteger todas as rotas administrativas, redirecionando para login quando não autenticado
- **FR-005**: O sistema DEVE permitir ao usuário fazer logout, removendo o token armazenado
- **FR-005a**: O sistema DEVE exibir um dashboard com resumo geral (total de agentes, sessões recentes) como tela padrão após o login
- **FR-005b**: O sistema DEVE possuir um menu lateral (sidebar) para navegação entre as seções da área administrativa, exibindo as seções contextuais ao agente selecionado
- **FR-006**: O sistema DEVE exibir uma lista de agentes cadastrados com layout profissional e intuitivo
- **FR-007**: O sistema DEVE permitir criar novos agentes com os campos: nome, descrição, prompt do sistema, e opções de coleta (nome, email, telefone)
- **FR-008**: O sistema DEVE permitir editar agentes existentes
- **FR-009**: O sistema DEVE permitir excluir agentes com confirmação do usuário
- **FR-010**: O sistema DEVE permitir ativar/desativar agentes
- **FR-011**: O sistema DEVE exibir um seletor de agente na navbar da área administrativa
- **FR-012**: O sistema DEVE listar sessões de chat do agente selecionado com paginação
- **FR-013**: O sistema DEVE permitir visualizar o histórico completo de conversa de uma sessão em uma página dedicada (rota própria), exibindo todas as mensagens em formato de chat com distinção visual entre remetente (usuário vs assistente), ordem cronológica, timestamps e paginação
- **FR-014**: O sistema DEVE listar arquivos da base de conhecimento do agente selecionado
- **FR-015**: O sistema DEVE permitir upload de arquivos .md para a base de conhecimento (limite de 10MB)
- **FR-016**: O sistema DEVE permitir excluir arquivos da base de conhecimento
- **FR-017**: O sistema DEVE permitir reprocessar arquivos com erro
- **FR-018**: O sistema DEVE permitir buscar na base de conhecimento via Elasticsearch e exibir os resultados
- **FR-019**: O sistema DEVE exibir o status de processamento dos arquivos (Processando, Pronto, Erro)
- **FR-020**: O sistema DEVE tratar token expirado redirecionando para login
- **FR-021**: O sistema DEVE ajustar os endpoints do frontend para corresponder aos endpoints reais do backend (corrigir caminhos e parâmetros de paginação)

### Key Entities

- **Usuário Administrativo**: Entidade que se autentica via credenciais (usuário/senha) e recebe um token JWT. Possui acesso total às funcionalidades administrativas.
- **Agente**: Entidade principal da aplicação. Possui nome, slug, descrição, prompt do sistema, configurações de coleta de dados (nome, email, telefone), status (ativo/inativo) e timestamps de criação/atualização.
- **Sessão de Chat**: Registro de uma conversa entre um visitante e um agente. Contém dados do visitante (nome, email, telefone), timestamps de início/fim e contagem de mensagens.
- **Mensagem de Chat**: Mensagem individual dentro de uma sessão. Possui tipo de remetente (usuário ou assistente), conteúdo textual e timestamp.
- **Arquivo de Conhecimento**: Arquivo .md carregado para a base de conhecimento de um agente. Possui nome, tamanho, status de processamento (Processando, Pronto, Erro) e timestamps.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O administrador consegue completar o fluxo de login em menos de 30 segundos
- **SC-002**: 100% das rotas administrativas são inacessíveis sem autenticação
- **SC-003**: O administrador consegue criar um novo agente em menos de 2 minutos
- **SC-004**: A lista de sessões carrega e exibe dados em menos de 3 segundos
- **SC-004a**: O histórico completo de conversa de uma sessão carrega e exibe as mensagens em menos de 3 segundos
- **SC-005**: O upload de arquivo e início do processamento ocorrem em menos de 5 segundos para arquivos até 10MB
- **SC-006**: Os resultados de busca na base de conhecimento são exibidos em menos de 2 segundos
- **SC-007**: A troca de agente na navbar reflete imediatamente (menos de 1 segundo) no contexto das funcionalidades
- **SC-008**: O layout da área administrativa é consistente, responsivo e profissional em todas as seções
- **SC-009**: Todas as operações CRUD de agentes funcionam corretamente sem erros de comunicação com o backend

## Assumptions

- O backend já está implementado e funcional com todos os endpoints descritos (auth, agents, sessions, files, search)
- As credenciais de login são gerenciadas pelo backend (não há cadastro de usuários no frontend)
- O token JWT tem validade de 8 horas (480 minutos, conforme configuração do backend)
- O proxy/nginx está configurado para rotear as requisições do frontend para o backend
- Apenas arquivos .md são aceitos para upload na base de conhecimento
- O Elasticsearch já está configurado e operacional no backend
- O frontend utiliza Tailwind CSS 4.x para estilização e o layout deve seguir padrões modernos de design
- A paginação do backend utiliza os parâmetros `page` e `maxPage` (não `pagina` e `tamanhoPagina`)
- A resposta padrão do backend segue o formato `{ sucesso, mensagem, erros, dados }`
- Existe apenas um usuário administrativo (sem gestão de múltiplos usuários)
