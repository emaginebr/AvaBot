# Feature Specification: Chatbot com Agente de Conhecimento

**Feature Branch**: `001-knowledge-agent-chatbot`
**Created**: 2026-04-08
**Status**: Draft
**Input**: User description: "Sistema de chatbot para web com agentes de conhecimento baseados em arquivos Markdown, busca vetorial e respostas via RAG com streaming"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Gerenciamento de Agentes (Priority: P1)

O administrador do sistema precisa criar e configurar agentes especializados, cada um com nome, descricao, slug (identificador unico para URL) e prompt de sistema que define a personalidade e restricoes do chatbot. O agente e acessado pelos usuarios finais atraves da URL usando o slug. Alem disso, o administrador configura quais campos de identificacao (nome, e-mail, telefone) o agente deve solicitar ao usuario antes de iniciar a conversa.

**Why this priority**: Sem agentes configurados, nenhuma outra funcionalidade do sistema funciona. E o alicerce de toda a plataforma.

**Independent Test**: Criar um agente com nome, slug, descricao e prompt de sistema, verificar que ele aparece na listagem, pode ser acessado pela URL via slug, e pode ser editado/removido.

**Acceptance Scenarios**:

1. **Given** o administrador esta na tela de gerenciamento, **When** preenche nome, slug, descricao, prompt de sistema e seleciona os campos de coleta (nome, e-mail, telefone) e salva, **Then** o agente e criado com status ativo e aparece na listagem
2. **Given** um agente existente, **When** o administrador edita o prompt de sistema, **Then** as alteracoes sao salvas e refletidas imediatamente
3. **Given** um agente existente, **When** o administrador o desativa, **Then** o agente nao aparece mais na selecao do chat para usuarios finais
4. **Given** um agente existente, **When** o administrador o remove, **Then** o agente e seus dados associados sao eliminados
5. **Given** o administrador cria um agente, **When** informa um slug que ja existe em outro agente, **Then** o sistema rejeita a criacao e exibe mensagem de erro informando que o slug ja esta em uso
6. **Given** um agente com slug "suporte-tecnico", **When** um usuario acessa a URL com esse slug, **Then** a interface de chat e carregada com o agente correto

---

### User Story 2 - Upload e Indexacao da Base de Conhecimento (Priority: P1)

O administrador precisa alimentar a base de conhecimento de cada agente fazendo upload de arquivos Markdown, que serao processados, divididos em trechos e indexados para busca semantica.

**Why this priority**: A base de conhecimento e o que permite ao chatbot responder com precisao. Sem ela, o agente nao tem conteudo para consultar.

**Independent Test**: Fazer upload de um arquivo .md para um agente, acompanhar o status de processamento ate "pronto", e verificar que os trechos foram indexados.

**Acceptance Scenarios**:

1. **Given** um agente existente, **When** o administrador faz upload de um arquivo .md via drag-and-drop, **Then** o arquivo e registrado com status "processando"
2. **Given** um arquivo em processamento, **When** a indexacao e concluida, **Then** o status muda para "pronto" e os trechos ficam disponiveis para busca
3. **Given** um arquivo ja indexado, **When** o administrador faz upload de uma versao atualizada, **Then** os trechos antigos sao removidos e os novos sao indexados
4. **Given** um arquivo com erro de processamento, **When** a ingestao falha, **Then** o status muda para "erro" e uma mensagem explicativa e exibida
5. **Given** um arquivo indexado, **When** o administrador o remove, **Then** todos os trechos associados sao deletados da busca

---

### User Story 3 - Coleta de Dados do Usuario (Priority: P1)

Ao iniciar um chat, o agente deve solicitar ao usuario as informacoes configuradas pelo administrador (nome, e-mail, telefone) antes de responder qualquer pergunta. Esses campos sao configuraveis por agente — o administrador define quais campos o agente deve pedir.

**Why this priority**: A coleta de dados e pre-requisito para iniciar qualquer conversa. Sem ela, nao ha como identificar o usuario nem persistir o historico associado.

**Independent Test**: Acessar o chat de um agente configurado para pedir nome e e-mail, verificar que o agente solicita essas informacoes antes de responder, e confirmar que os dados sao salvos na sessao.

**Acceptance Scenarios**:

1. **Given** o usuario acessa o chat de um agente configurado para pedir nome e e-mail, **When** a sessao inicia, **Then** o agente solicita nome e e-mail antes de aceitar qualquer pergunta
2. **Given** o agente solicita dados, **When** o usuario fornece todas as informacoes solicitadas, **Then** o sistema registra os dados e habilita a conversa normal
3. **Given** o agente solicita dados, **When** o usuario tenta enviar uma pergunta sem fornecer os dados, **Then** o agente insiste na coleta antes de prosseguir
4. **Given** um agente configurado para pedir apenas nome, **When** a sessao inicia, **Then** o agente solicita apenas o nome (sem pedir e-mail ou telefone)
5. **Given** um agente configurado sem nenhum campo de coleta, **When** a sessao inicia, **Then** a conversa comeca diretamente sem coleta de dados

---

### User Story 4 - Conversa com o Agente via Chat (Priority: P1)

O usuario final acessa um agente via URL e inicia uma conversa em tempo real. Apos fornecer seus dados (se configurado), o chatbot busca informacoes relevantes na base de conhecimento do agente e responde com streaming, exibindo a resposta token por token. Todo o historico da conversa e persistido em banco de dados.

**Why this priority**: E a funcionalidade principal do produto — a experiencia de conversa inteligente com streaming e persistencia.

**Independent Test**: Selecionar um agente com base de conhecimento indexada, fornecer os dados solicitados, enviar uma pergunta sobre o conteudo dos documentos, verificar que a resposta e relevante e aparece progressivamente na tela, e confirmar que o historico foi salvo no banco.

**Acceptance Scenarios**:

1. **Given** o usuario acessa a URL com o slug de um agente ativo com base de conhecimento pronta e ja forneceu seus dados, **Then** a janela de chat e habilitada para envio de mensagens
2. **Given** uma sessao de chat ativa, **When** o usuario envia uma mensagem, **Then** o sistema busca trechos relevantes e retorna uma resposta baseada no conhecimento do agente
3. **Given** uma resposta sendo gerada, **When** o streaming esta ativo, **Then** os tokens aparecem progressivamente na tela com indicador de digitacao
4. **Given** uma pergunta fora do dominio do agente, **When** nao ha trechos relevantes na base, **Then** o agente informa que nao possui informacao sobre o assunto em vez de inventar uma resposta
5. **Given** uma sessao de chat ativa, **When** o usuario envia multiplas mensagens, **Then** o historico recente e mantido e utilizado como contexto para respostas subsequentes
6. **Given** uma sessao de chat ativa, **When** mensagens sao trocadas, **Then** todas as mensagens (usuario e assistente) sao persistidas no banco de dados em tempo real
7. **Given** o administrador acessa o painel, **When** consulta o historico de conversas de um agente, **Then** todas as sessoes e mensagens anteriores estao disponiveis para visualizacao

---

### User Story 5 - Visualizacao de Respostas em Markdown (Priority: P2)

As respostas do chatbot sao renderizadas em formato Markdown, permitindo formatacao rica com titulos, listas, blocos de codigo e links.

**Why this priority**: Melhora significativamente a experiencia do usuario, mas o chat funciona sem renderizacao rica.

**Independent Test**: Enviar uma pergunta que gere resposta com formatacao Markdown (lista, codigo, titulo) e verificar que a renderizacao esta correta.

**Acceptance Scenarios**:

1. **Given** o chatbot retorna uma resposta com Markdown, **When** a resposta contem titulos, listas e blocos de codigo, **Then** o conteudo e renderizado com formatacao visual adequada
2. **Given** uma resposta com links em Markdown, **When** o usuario clica no link, **Then** ele abre em uma nova aba

---

### Edge Cases

- O que acontece quando um arquivo .md vazio e enviado para upload?
- Como o sistema se comporta quando o servico de embeddings esta indisponivel durante a ingestao? O arquivo fica com status "erro" e pode ser reprocessado manualmente.
- Quando a API de IA esta indisponivel durante o chat, o sistema exibe mensagem de erro amigavel e permite reenvio manual.
- O que acontece se o usuario envia uma mensagem enquanto a base de conhecimento do agente ainda esta sendo indexada?
- Como o sistema se comporta quando a conexao WebSocket cai durante o streaming de uma resposta? A resposta parcial e descartada e o usuario reenvia a pergunta ao reconectar.
- O que acontece se dois administradores editam o mesmo agente simultaneamente?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE permitir criar agentes com nome, slug, descricao, prompt de sistema e configuracao de campos de coleta de dados (nome, e-mail, telefone)
- **FR-016**: O sistema DEVE garantir que o slug de cada agente e unico na base de dados
- **FR-017**: O sistema DEVE permitir acesso ao chat de um agente atraves de URL contendo o slug do agente
- **FR-018**: O sistema DEVE exibir pagina 404 quando o slug na URL nao corresponde a nenhum agente
- **FR-019**: O sistema DEVE exibir mensagem "temporariamente indisponivel" quando o slug corresponde a um agente inativo
- **FR-002**: O sistema DEVE permitir editar e remover agentes existentes
- **FR-003**: O sistema DEVE permitir ativar e desativar agentes
- **FR-004**: O sistema DEVE aceitar upload de arquivos Markdown (.md) associados a um agente, com limite maximo de 10MB por arquivo
- **FR-005**: O sistema DEVE processar arquivos Markdown, dividindo-os em trechos de aproximadamente 2000 caracteres com sobreposicao de 200 caracteres
- **FR-006**: O sistema DEVE converter cada trecho em vetor de embeddings e indexa-lo para busca
- **FR-007**: O sistema DEVE exibir o status de processamento de cada arquivo (processando, pronto, erro)
- **FR-008**: O sistema DEVE realizar busca hibrida (semantica + textual) na base de conhecimento do agente ao receber uma mensagem
- **FR-009**: O sistema DEVE recuperar os 5 trechos mais relevantes para compor o contexto da resposta
- **FR-010**: O sistema DEVE combinar os trechos recuperados com o historico recente e o prompt de sistema para gerar a resposta
- **FR-011**: O sistema DEVE transmitir a resposta via comunicacao em tempo real, token por token (streaming)
- **FR-012**: O sistema DEVE persistir todo o historico da conversa (mensagens do usuario e do assistente) no banco de dados em tempo real, com o numero de mensagens recentes usadas como contexto da IA configuravel via appsettings
- **FR-013**: O sistema DEVE restringir as respostas ao conhecimento indexado do agente, informando o usuario quando nao encontrar informacao relevante
- **FR-020**: O sistema DEVE solicitar ao usuario os dados de identificacao configurados pelo agente (nome, e-mail, telefone) antes de aceitar qualquer pergunta
- **FR-021**: O sistema DEVE permitir que o administrador configure, por agente, quais campos de identificacao sao solicitados ao usuario (nome, e-mail, telefone — todos opcionais e independentes)
- **FR-022**: O sistema DEVE armazenar os dados de identificacao do usuario associados a sessao de chat no banco de dados
- **FR-014**: O sistema DEVE reindexar automaticamente os trechos quando um arquivo e atualizado ou removido
- **FR-015**: O sistema DEVE renderizar as respostas do chatbot em formato Markdown

### Key Entities

- **Agent**: Assistente especializado com nome, slug (identificador unico para URL), descricao, prompt de sistema, status (ativo/inativo), configuracao de campos de coleta (nome, e-mail, telefone — booleanos por campo) e timestamps de criacao/atualizacao. O slug e unico na tabela e usado para acessar o agente via URL. Possui uma colecao de arquivos de conhecimento.
- **KnowledgeFile**: Arquivo Markdown vinculado a um agente, com nome do arquivo, conteudo original, status de processamento (processando/pronto/erro) e timestamps. Cada arquivo gera multiplos trechos indexados.
- **ChatSession**: Sessao de conversa persistida no banco de dados, vinculada a um agente. Contem os dados de identificacao do usuario (nome, e-mail, telefone — conforme configurado no agente), timestamps de inicio/fim e referencia ao agente.
- **ChatMessage**: Mensagem individual dentro de uma sessao, com remetente (usuario ou assistente), conteudo da mensagem e timestamp. Persistida no banco de dados em tempo real.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Administradores conseguem criar um agente completo (com nome, descricao e prompt) em menos de 2 minutos
- **SC-002**: Arquivos Markdown de ate 50KB sao processados e ficam disponiveis para consulta em menos de 30 segundos
- **SC-003**: O primeiro token da resposta do chatbot aparece na tela em menos de 3 segundos apos o envio da mensagem
- **SC-004**: 90% das respostas do chatbot sao relevantes ao conteudo indexado quando a pergunta esta dentro do dominio do agente
- **SC-005**: O chatbot recusa inventar respostas em 100% dos casos quando a pergunta esta fora do dominio do agente
- **SC-006**: O sistema suporta pelo menos 50 sessoes de chat simultaneas sem degradacao perceptivel

## Clarifications

### Session 2026-04-08

- Q: Quantas mensagens recentes devem ser enviadas como contexto ao modelo de IA? → A: Configuravel via appsettings (nao hardcoded). Valor padrao definido em configuracao.
- Q: Comportamento quando o servico de IA esta indisponivel durante o chat? → A: Exibir mensagem de erro amigavel ao usuario e permitir reenvio manual da mensagem. Sem retry automatico.
- Q: Comportamento ao acessar URL com slug inexistente ou agente inativo? → A: Slug inexistente exibe pagina 404. Agente inativo exibe mensagem "temporariamente indisponivel".
- Q: Limite maximo de tamanho por arquivo Markdown no upload? → A: 10MB por arquivo.
- Q: Comportamento quando a conexao WebSocket cai durante o streaming? → A: Resposta parcial descartada. Usuario reenvia a pergunta ao reconectar.

## Assumptions

- O sistema nao requer autenticacao de usuarios finais (fora do escopo)
- Todo o historico de conversas e persistido no banco de dados (incluindo dados de identificacao do usuario)
- Apenas arquivos no formato Markdown (.md) sao suportados para a base de conhecimento
- Nao ha necessidade de multi-tenancy; o sistema opera como instancia unica
- O administrador tem acesso a uma interface web para gerenciamento de agentes e arquivos
- O servico de embeddings e de geracao de respostas depende de uma API externa de IA (OpenAI)
- Os usuarios finais acessam o chat via navegador web moderno com suporte a WebSocket
- A base de conhecimento de cada agente e isolada — um agente nao consulta documentos de outro
- O sistema e executado em ambiente containerizado com todos os servicos necessarios
