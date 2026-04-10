# Feature Specification: Retomada de Sessao e Bot Telegram

**Feature Branch**: `002-session-resume-telegram-bot`  
**Created**: 2026-04-10  
**Status**: Draft  
**Input**: User description: "Endpoint de ultima sessao com chave unica de seguranca + Bot no Telegram"

## Clarifications

### Session 2026-04-10

- Q: O webhook do Telegram deve verificar autenticidade das requisicoes com secret token? → A: Sim, verificar com secret token via header `X-Telegram-Bot-Api-Secret-Token`
- Q: Quando o usuario envia /start no Telegram apos ja ter conversado, o que acontece? → A: Criar nova sessao e enviar boas-vindas (historico anterior preservado no banco, mas conversa recomeca)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retomar ultima conversa (Priority: P1)

Um visitante do site que ja conversou anteriormente com o chatbot deseja continuar a conversa de onde parou. Ao acessar o chat, ele tem a opcao de iniciar uma nova sessao ou retomar a ultima sessao aberta. Ao retomar, ele ve as ultimas 10 mensagens para contexto visual, mas o agente de IA considera todo o historico da conversa ao responder.

**Why this priority**: Esta funcionalidade melhora diretamente a experiencia do usuario, evitando que ele precise repetir informacoes ja fornecidas. E a base para continuidade de conversas.

**Independent Test**: Pode ser testado criando uma sessao com mensagens, obtendo a chave de retomada, e usando essa chave para recuperar a sessao com as ultimas 10 mensagens.

**Acceptance Scenarios**:

1. **Given** um usuario que ja possui uma sessao com 15 mensagens para o agente "vendas", **When** ele solicita a ultima sessao usando o slug do agente e sua chave de retomada, **Then** o sistema retorna os dados da sessao com as ultimas 10 mensagens ordenadas cronologicamente
2. **Given** um usuario que nunca conversou com o agente, **When** ele tenta retomar a ultima sessao, **Then** o sistema retorna uma resposta indicando que nao ha sessao anterior
3. **Given** um usuario com uma chave de retomada, **When** ele envia uma nova mensagem na sessao retomada via WebSocket, **Then** o agente de IA responde considerando todo o historico da conversa (nao apenas as 10 ultimas exibidas)
4. **Given** um usuario retomando uma sessao, **When** a sessao e carregada, **Then** a chave de retomada utilizada e a mesma gerada quando a sessao foi criada originalmente

---

### User Story 2 - Geracao de chave de retomada na criacao de sessao (Priority: P1)

Quando um usuario inicia uma nova sessao de chat, o sistema gera automaticamente uma chave unica e segura (resume token) que e retornada ao cliente. Essa chave permite que o usuario retome a sessao futuramente sem necessidade de autenticacao.

**Why this priority**: Sem a chave de retomada, a funcionalidade de retomar sessao nao funciona. E pre-requisito da User Story 1.

**Independent Test**: Pode ser testado criando uma nova sessao e verificando que a resposta inclui um token de retomada unico e nao-previsivel.

**Acceptance Scenarios**:

1. **Given** um usuario criando uma nova sessao via endpoint REST, **When** a sessao e criada com sucesso, **Then** a resposta inclui um campo com a chave de retomada unica
2. **Given** duas sessoes criadas para o mesmo agente, **When** ambas sao criadas, **Then** cada uma possui uma chave de retomada diferente
3. **Given** uma sessao criada via WebSocket (identify), **When** a sessao e criada, **Then** o evento de confirmacao inclui a chave de retomada

---

### User Story 3 - Conversar com o agente via Telegram (Priority: P2)

Um usuario do Telegram pode conversar com um agente Avachat diretamente pelo aplicativo Telegram, sem precisar acessar o site. O bot recebe mensagens do usuario, processa via IA com o mesmo pipeline RAG existente, e responde no Telegram.

**Why this priority**: Expande o alcance do Avachat para uma nova plataforma com milhoes de usuarios, mas depende de infraestrutura adicional (webhook, processamento de mensagens).

**Independent Test**: Pode ser testado enviando uma mensagem ao bot no Telegram e verificando que ele responde com conteudo relevante baseado na base de conhecimento do agente.

**Acceptance Scenarios**:

1. **Given** o bot Telegram configurado e ativo, **When** um usuario envia uma mensagem de texto ao bot, **Then** o bot responde com a resposta gerada pelo agente de IA
2. **Given** um usuario que ja enviou mensagens anteriores, **When** ele envia uma nova mensagem, **Then** o bot considera o historico da conversa ao responder (mesma sessao)
3. **Given** o bot recebendo uma mensagem, **When** ocorre um erro no processamento, **Then** o bot responde com uma mensagem amigavel de erro

---

### User Story 4 - Configuracao do webhook do Telegram (Priority: P2)

O sistema deve expor um endpoint para receber atualizacoes (webhooks) do Telegram e fornecer uma forma de registrar/configurar o webhook junto a API do Telegram.

**Why this priority**: E pre-requisito para o bot funcionar. Sem o webhook, o Telegram nao consegue enviar mensagens para o sistema.

**Independent Test**: Pode ser testado registrando o webhook e verificando que o Telegram confirma o registro com sucesso.

**Acceptance Scenarios**:

1. **Given** o endpoint de webhook disponivel, **When** o administrador solicita o registro do webhook no Telegram, **Then** o Telegram confirma o registro com sucesso
2. **Given** o webhook registrado, **When** o Telegram envia uma atualizacao (mensagem de usuario), **Then** o sistema recebe e processa a atualizacao corretamente
3. **Given** uma requisicao malformada ao endpoint de webhook, **When** o sistema recebe a requisicao, **Then** ela e rejeitada sem causar erros internos
4. **Given** uma requisicao ao webhook sem o header `X-Telegram-Bot-Api-Secret-Token` valido, **When** o sistema recebe a requisicao, **Then** ela e rejeitada com erro de autorizacao

---

### Edge Cases

- O que acontece quando o usuario tenta retomar uma sessao com uma chave invalida ou expirada? O sistema retorna erro 404 ou 401 sem vazar informacoes sobre a existencia da sessao.
- O que acontece quando o usuario tenta retomar uma sessao de um agente que foi desativado? O sistema retorna erro informando que o agente nao esta disponivel.
- O que acontece quando a sessao retomada tem menos de 10 mensagens? O sistema retorna todas as mensagens disponiveis.
- O que acontece quando a sessao retomada tem 0 mensagens? O sistema retorna a sessao com lista vazia de mensagens.
- O que acontece quando multiplos usuarios do Telegram enviam mensagens simultaneamente? Cada conversa e processada de forma independente com sessoes separadas.
- O que acontece quando o Telegram envia um tipo de mensagem nao suportado (foto, video, sticker)? O bot responde informando que aceita apenas mensagens de texto.
- O que acontece quando o bot Telegram recebe o comando /start? O bot cria uma nova sessao (preservando a anterior no banco) e responde com uma mensagem de boas-vindas explicando como usar.
- O que acontece quando uma requisicao chega ao webhook sem o secret token correto? O sistema rejeita a requisicao com erro de autorizacao.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE gerar uma chave de retomada unica, segura e nao-previsivel para cada sessao criada
- **FR-002**: O sistema DEVE armazenar a chave de retomada associada a sessao no banco de dados
- **FR-003**: O sistema DEVE expor um endpoint para recuperar a ultima sessao de um agente usando a chave de retomada
- **FR-004**: O endpoint de retomada DEVE retornar os dados da sessao com as ultimas 10 mensagens ordenadas cronologicamente
- **FR-005**: O historico completo da conversa DEVE continuar sendo utilizado pelo pipeline de IA ao gerar respostas (comportamento existente mantido)
- **FR-006**: O sistema DEVE permitir que o usuario continue enviando mensagens na sessao retomada via WebSocket
- **FR-007**: A criacao de sessao via REST DEVE retornar a chave de retomada na resposta
- **FR-008**: A criacao de sessao via WebSocket DEVE incluir a chave de retomada no evento de confirmacao
- **FR-009**: O sistema DEVE expor um endpoint de webhook para receber atualizacoes do Telegram
- **FR-010**: O bot DEVE processar mensagens de texto recebidas do Telegram e responder com a resposta do agente de IA
- **FR-011**: O bot DEVE manter sessoes por usuario do Telegram, preservando historico de conversa entre mensagens
- **FR-012**: O sistema DEVE expor um endpoint para registrar o webhook junto a API do Telegram
- **FR-013**: O bot DEVE responder ao comando /start com uma mensagem de boas-vindas
- **FR-014**: O bot DEVE informar o usuario quando receber tipos de mensagem nao suportados (apenas texto e aceito)
- **FR-015**: O token do bot Telegram DEVE ser configuravel via variavel de ambiente (nao hardcoded)
- **FR-016**: O sistema DEVE associar cada usuario do Telegram a um agente especifico para direcionar as conversas
- **FR-017**: O endpoint de webhook DEVE verificar a autenticidade das requisicoes do Telegram usando um secret token enviado no header `X-Telegram-Bot-Api-Secret-Token`, rejeitando requisicoes sem token valido
- **FR-018**: O comando /start no Telegram DEVE criar uma nova sessao para o usuario (preservando sessoes anteriores no banco) e enviar uma mensagem de boas-vindas

### Key Entities

- **ChatSession (existente, modificada)**: Sessao de conversa entre usuario e agente. Ganha um novo atributo: chave de retomada (token unico, seguro, gerado na criacao)
- **ChatMessage (existente, sem alteracao)**: Mensagem individual dentro de uma sessao, com tipo (usuario/assistente), conteudo e timestamp
- **TelegramChat (novo)**: Representa a associacao entre um chat/usuario do Telegram e uma sessao do Avachat. Contem: identificador do chat Telegram, referencia a sessao, referencia ao agente

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Usuarios conseguem retomar a ultima sessao e ver as ultimas 10 mensagens em menos de 2 segundos
- **SC-002**: A chave de retomada e unica para cada sessao — nenhuma colisao em 100.000 sessoes criadas
- **SC-003**: Mensagens enviadas na sessao retomada recebem respostas que consideram o historico completo da conversa
- **SC-004**: O bot Telegram responde mensagens de usuarios em menos de 10 segundos (incluindo processamento de IA)
- **SC-005**: O webhook do Telegram processa 100% das mensagens recebidas sem perda
- **SC-006**: Tentativas de retomar sessao com chave invalida sao rejeitadas em 100% dos casos

## Assumptions

- O bot Telegram sera associado a um unico agente (configuravel via variavel de ambiente), ja que o Telegram nao tem conceito nativo de "selecionar agente"
- O token do bot Telegram (fornecido pelo usuario) sera armazenado como variavel de ambiente, nao no codigo-fonte
- A chave de retomada nao expira (a sessao pode ser retomada a qualquer momento enquanto existir no banco)
- O webhook do Telegram requer que o servidor seja acessivel via HTTPS com certificado valido
- O pipeline de IA existente (RAG com Elasticsearch + OpenAI) sera reutilizado integralmente para o bot Telegram
- O sistema de WebSocket existente sera reutilizado para sessoes retomadas — o usuario conecta ao WebSocket passando o sessionId da sessao retomada
- Apenas mensagens de texto sao processadas pelo bot Telegram; fotos, videos, audios e stickers sao respondidos com mensagem informativa
