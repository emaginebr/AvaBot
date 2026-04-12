# Feature Specification: Configuracao Telegram por Agente

**Feature Branch**: `003-telegram-per-agent-config`  
**Created**: 2026-04-11  
**Status**: Draft  
**Input**: User description: "As configuracoes do telegram devem ficar no banco de dados, por agente. Campos na entidade Agente: TelegramBotName, TelegramBotToken, TelegramWebhookSecret (gerado automaticamente). Metodo para gerar novo Webhook Secret. Metodo para verificar configuracao do Webhook. Dominio https://avabot.net. Webhook com slug: https://avabot.net/api/telegram/<SLUG>/webhook"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configurar Bot Telegram no Agente (Priority: P1)

O administrador acessa o agente e configura a integracao com o Telegram preenchendo o nome do bot e o token. Ao salvar, o sistema gera automaticamente um webhook secret e registra o webhook no Telegram usando a URL padrao com o slug do agente.

**Why this priority**: Esta e a funcionalidade central da feature. Sem configurar o bot no agente, nenhuma outra funcionalidade Telegram funciona. Permite que multiplos agentes tenham bots diferentes.

**Independent Test**: Pode ser testado criando um agente, preenchendo os campos do Telegram e verificando que o webhook foi registrado corretamente na API do Telegram.

**Acceptance Scenarios**:

1. **Given** um agente existente sem configuracao Telegram, **When** o administrador preenche TelegramBotName e TelegramBotToken e salva, **Then** o sistema gera automaticamente um TelegramWebhookSecret e persiste os tres campos no banco de dados.
2. **Given** um agente com TelegramBotToken configurado, **When** o webhook e registrado, **Then** a URL registrada no Telegram segue o padrao `https://avabot.net/api/telegram/{slug}/webhook` usando o slug do agente.
3. **Given** um agente sem TelegramBotToken, **When** uma mensagem chega no webhook do slug desse agente, **Then** o sistema retorna erro indicando que o bot nao esta configurado.

---

### User Story 2 - Receber Mensagens pelo Webhook por Slug (Priority: P1)

O bot do Telegram recebe mensagens de usuarios e as encaminha para o webhook do agente correto, identificado pelo slug na URL. O sistema processa a mensagem usando o token do bot configurado no agente correspondente.

**Why this priority**: Essencial para o funcionamento multi-bot. O webhook precisa rotear para o agente correto baseado no slug.

**Independent Test**: Pode ser testado enviando um request simulado para `/api/telegram/{slug}/webhook` e verificando que a mensagem e processada pelo agente correto.

**Acceptance Scenarios**:

1. **Given** dois agentes com bots Telegram diferentes configurados, **When** uma mensagem chega no webhook de cada slug, **Then** cada mensagem e processada pelo agente correspondente ao slug.
2. **Given** um slug que nao corresponde a nenhum agente, **When** uma mensagem chega nesse webhook, **Then** o sistema retorna erro apropriado.
3. **Given** um agente com bot configurado, **When** uma mensagem chega com o secret incorreto no header, **Then** o sistema rejeita o request.

---

### User Story 3 - Gerar Novo Webhook Secret (Priority: P2)

O administrador pode gerar um novo webhook secret para um agente, por exemplo em caso de comprometimento do secret anterior. Ao gerar um novo secret, o sistema atualiza o registro do webhook no Telegram automaticamente.

**Why this priority**: Importante para seguranca, mas nao e necessario para o fluxo inicial de configuracao. O secret ja e gerado automaticamente na primeira configuracao.

**Independent Test**: Pode ser testado gerando um novo secret para um agente e verificando que o webhook no Telegram foi atualizado com o novo secret.

**Acceptance Scenarios**:

1. **Given** um agente com Telegram configurado, **When** o administrador solicita a geracao de um novo webhook secret, **Then** o sistema gera um novo secret aleatorio, persiste no banco e atualiza o webhook no Telegram.
2. **Given** um agente com Telegram configurado e um novo secret gerado, **When** uma mensagem chega com o secret antigo, **Then** o sistema rejeita o request.

---

### User Story 4 - Verificar Configuracao do Webhook (Priority: P2)

O administrador pode consultar o status do webhook de um agente para verificar se esta corretamente configurado no Telegram, vendo a URL atualmente registrada.

**Why this priority**: Funcionalidade de diagnostico que ajuda a identificar problemas de configuracao, mas nao bloqueia o uso do bot.

**Independent Test**: Pode ser testado chamando o endpoint de verificacao e comparando a URL retornada com a URL esperada para o slug do agente.

**Acceptance Scenarios**:

1. **Given** um agente com Telegram configurado e webhook registrado, **When** o administrador consulta a verificacao, **Then** o sistema retorna a URL atualmente configurada no Telegram para aquele bot.
2. **Given** um agente sem Telegram configurado, **When** o administrador consulta a verificacao, **Then** o sistema informa que o bot nao esta configurado.

---

### Edge Cases

- O que acontece quando o token do bot e invalido ou expirado? O sistema deve informar o erro ao tentar registrar o webhook.
- O que acontece quando dois agentes tentam usar o mesmo bot token? O sistema deve impedir duplicidade de tokens.
- O que acontece quando o agente e desativado (status=0)? Mensagens no webhook devem ser rejeitadas com mensagem amigavel.
- O que acontece quando o slug do agente e alterado apos o webhook estar configurado? O webhook antigo deve ser atualizado para a nova URL.
- O que acontece ao remover a configuracao Telegram de um agente com chats ativos? Apenas os campos sao limpos; o webhook no Telegram e os chats existentes nao sao alterados automaticamente. Mensagens que chegarem serao rejeitadas por FR-010 (sem bot configurado).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE armazenar os campos TelegramBotName, TelegramBotToken e TelegramWebhookSecret na entidade Agente, todos opcionais.
- **FR-002**: O sistema DEVE gerar automaticamente um TelegramWebhookSecret seguro quando o TelegramBotToken for configurado pela primeira vez.
- **FR-003**: O sistema DEVE expor webhooks individuais por agente na URL `https://avabot.net/api/telegram/{slug}/webhook`, onde `{slug}` e o slug do agente.
- **FR-004**: O sistema DEVE validar o header `X-Telegram-Bot-Api-Secret-Token` usando o TelegramWebhookSecret do agente correspondente ao slug.
- **FR-005**: O sistema DEVE resolver o agente correto a partir do slug na URL do webhook e usar o TelegramBotToken desse agente para enviar respostas.
- **FR-006**: O sistema DEVE fornecer um metodo para gerar um novo TelegramWebhookSecret, substituindo o anterior e atualizando o webhook no Telegram.
- **FR-007**: O sistema DEVE fornecer um metodo para consultar a configuracao atual do webhook no Telegram, retornando a URL registrada.
- **FR-008**: O sistema DEVE impedir que dois agentes usem o mesmo TelegramBotToken.
- **FR-009**: O sistema DEVE registrar o webhook no Telegram automaticamente quando o TelegramBotToken for configurado ou o secret for regenerado.
- **FR-010**: O sistema DEVE rejeitar mensagens de webhook para agentes inativos (status=0) ou sem bot configurado.

### Key Entities

- **Agent (atualizado)**: Passa a conter os campos TelegramBotName (nome do bot para exibicao), TelegramBotToken (token de autenticacao do bot) e TelegramWebhookSecret (secret para validar requests do webhook). Os tres campos sao opcionais, permitindo agentes sem integracao Telegram.
- **TelegramChat (existente)**: Mantem o vinculo entre chat do Telegram, agente e sessao de chat. Sem alteracoes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Administradores conseguem configurar um bot Telegram em um agente em menos de 2 minutos (preencher nome e token).
- **SC-002**: O sistema suporta multiplos bots Telegram simultaneos, cada um vinculado a um agente diferente, sem interferencia entre eles.
- **SC-003**: Mensagens enviadas por usuarios no Telegram sao roteadas para o agente correto em 100% dos casos, baseado no slug da URL do webhook.
- **SC-004**: A regeneracao do webhook secret invalida imediatamente o secret anterior, garantindo que requests com o secret antigo sejam rejeitados.
- **SC-005**: A verificacao do webhook retorna a URL correta registrada no Telegram em tempo real.

## Clarifications

### Session 2026-04-11

- Q: Como o TelegramBotToken deve ser armazenado no banco de dados? → A: Armazenado em texto puro, sem criptografia.
- Q: O que fazer com a configuracao global do Telegram no appsettings? → A: Remover completamente. A configuracao passa a ser exclusivamente por agente no banco de dados.
- Q: O que acontece ao remover a configuracao Telegram de um agente? → A: Apenas limpar os campos no banco. Webhook e chats existentes nao sao alterados automaticamente.

## Assumptions

- O dominio `https://avabot.net` esta configurado e acessivel publicamente com HTTPS valido.
- Cada agente possui um slug unico que nao muda com frequencia. Caso mude, a URL do webhook precisa ser atualizada.
- O webhook secret gerado automaticamente sera uma string criptograficamente segura com pelo menos 32 caracteres.
- A configuracao global do Telegram no appsettings (BotToken, WebhookSecret, WebhookUrl, AgentSlug) sera removida completamente. Nao havera fallback para config global; toda configuracao Telegram e exclusivamente por agente no banco de dados.
- O TelegramBotClient sera instanciado dinamicamente por agente em vez de registrado como singleton global.
