# Feature Specification: Área Administrativa Bot Telegram

**Feature Branch**: `009-telegram-bot-admin`  
**Created**: 2026-04-11  
**Status**: Draft  
**Input**: User description: "Criar área administrativa 'Bot Telegram' com configuração de nome do bot, token, webhook secret, URL do bot, e botões para configurar e verificar webhook"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configurar Bot Telegram (Priority: P1)

O administrador acessa a seção "Bot Telegram" no painel administrativo, logo abaixo de "Configurações" no menu lateral. Nesta página ele pode preencher o nome do bot no Telegram (que deve terminar com "bot"), o token do bot (obtido via BotFather no Telegram), e visualizar o webhook secret gerado automaticamente. A URL do bot no Telegram é exibida automaticamente com base no nome informado (formato: https://t.me/{nome_do_bot}). Ao salvar, os dados são persistidos no agente selecionado.

**Why this priority**: Esta é a funcionalidade central — sem a configuração dos dados do bot, nenhuma outra ação (webhook, verificação) é possível.

**Independent Test**: Pode ser testado acessando a página, preenchendo os campos e verificando que os dados foram salvos corretamente no agente.

**Acceptance Scenarios**:

1. **Given** um agente selecionado sem configuração de Telegram, **When** o administrador acessa "Bot Telegram", **Then** os campos nome, token e webhook secret são exibidos vazios (secret será gerado ao salvar pela primeira vez).
2. **Given** o administrador preenche o nome do bot sem terminar com "bot", **When** tenta salvar, **Then** uma mensagem de validação é exibida informando que o nome deve terminar com "bot".
3. **Given** o administrador preenche nome (terminado em "bot") e token válidos, **When** salva, **Then** os dados são persistidos e um webhook secret é gerado automaticamente se ainda não existir.
4. **Given** um agente com bot configurado, **When** o administrador acessa "Bot Telegram", **Then** os campos são preenchidos com os dados atuais e a URL do bot (https://t.me/{nome_do_bot}) é exibida.

---

### User Story 2 - Configurar Webhook no Telegram (Priority: P2)

O administrador, após ter configurado o bot com nome e token, clica no botão "Configurar Webhook". O sistema chama o backend que registra o webhook junto à API do Telegram, associando a URL de callback do sistema ao bot. Um feedback visual (sucesso ou erro) é exibido ao administrador.

**Why this priority**: O webhook é necessário para que o bot receba mensagens, mas depende dos dados da Story 1 estarem preenchidos.

**Independent Test**: Com um bot configurado, clicar em "Configurar Webhook" e verificar a resposta de sucesso/erro.

**Acceptance Scenarios**:

1. **Given** um agente com nome do bot e token preenchidos, **When** o administrador clica em "Configurar Webhook", **Then** o sistema registra o webhook na API do Telegram e exibe mensagem de sucesso.
2. **Given** um agente sem token preenchido, **When** o administrador clica em "Configurar Webhook", **Then** uma mensagem informa que é necessário preencher o token primeiro.
3. **Given** um token inválido, **When** o administrador clica em "Configurar Webhook", **Then** uma mensagem de erro é exibida com o detalhe retornado pela API.

---

### User Story 3 - Verificar Status do Webhook (Priority: P3)

O administrador clica no botão "Verificar Webhook" para consultar o status atual do webhook registrado no Telegram. O sistema exibe informações como URL registrada, se está ativo, e eventuais erros pendentes.

**Why this priority**: É uma funcionalidade de diagnóstico que complementa a configuração, mas não é bloqueante para o funcionamento do bot.

**Independent Test**: Com webhook configurado, clicar em "Verificar Webhook" e confirmar que as informações retornadas correspondem à configuração atual.

**Acceptance Scenarios**:

1. **Given** um agente com webhook configurado, **When** o administrador clica em "Verificar Webhook", **Then** as informações do webhook (URL, status) são exibidas.
2. **Given** um agente sem webhook configurado, **When** o administrador clica em "Verificar Webhook", **Then** uma mensagem informa que o webhook não está configurado.

---

### User Story 4 - Regenerar Webhook Secret (Priority: P3)

O administrador clica no botão de regenerar ao lado do campo Webhook Secret. O sistema gera um novo secret, atualiza o agente e re-registra o webhook no Telegram com o novo secret.

**Why this priority**: Funcionalidade de segurança complementar, usada ocasionalmente.

**Independent Test**: Clicar no botão de regenerar e verificar que o secret mudou e o webhook foi atualizado.

**Acceptance Scenarios**:

1. **Given** um agente com webhook secret existente, **When** o administrador clica em "Regenerar", **Then** um novo secret é gerado, exibido e o webhook é re-registrado automaticamente.
2. **Given** o administrador clica em "Regenerar", **When** a operação falha, **Then** uma mensagem de erro é exibida e o secret anterior é mantido.

---

### Edge Cases

- O que acontece quando o nome do bot contém caracteres especiais não permitidos pelo Telegram?
- Como o sistema se comporta quando a API do Telegram está indisponível durante configuração do webhook?
- O que acontece se o administrador troca de agente enquanto uma operação de webhook está em andamento?
- Como o sistema se comporta quando o token do bot é alterado após o webhook já estar configurado?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE exibir uma seção "Bot Telegram" no menu lateral do painel administrativo, posicionada logo abaixo de "Configurações".
- **FR-002**: A seção "Bot Telegram" DEVE estar disponível apenas quando um agente está selecionado.
- **FR-003**: O sistema DEVE exibir um campo para o nome do bot no Telegram com validação de que o valor termina com "bot" (case-insensitive).
- **FR-004**: O sistema DEVE exibir um campo para o token do bot (texto sensível, com opção de mostrar/ocultar).
- **FR-005**: O sistema DEVE exibir o campo Webhook Secret como somente leitura, com um botão para regenerar.
- **FR-006**: O sistema DEVE gerar automaticamente um webhook secret ao salvar a configuração pela primeira vez, caso não exista.
- **FR-007**: O sistema DEVE exibir a URL do bot no Telegram no formato https://t.me/{nome_do_bot} quando o nome estiver preenchido.
- **FR-008**: O sistema DEVE fornecer um botão "Configurar Webhook" que registra o webhook na API do Telegram via backend.
- **FR-009**: O sistema DEVE fornecer um botão "Verificar Webhook" que consulta o status atual do webhook via backend.
- **FR-010**: O sistema DEVE exibir feedback visual (sucesso/erro) para todas as operações (salvar, configurar webhook, verificar webhook, regenerar secret).
- **FR-011**: O botão "Configurar Webhook" DEVE estar desabilitado quando nome do bot ou token não estiverem preenchidos.
- **FR-012**: O sistema DEVE persistir as configurações do bot Telegram (nome, token) no agente selecionado.

### Key Entities

- **Configuração Telegram do Agente**: Representa os dados de integração Telegram de um agente — nome do bot, token, webhook secret. Associada 1:1 com um Agente.
- **Informação de Webhook**: Dados retornados pela API do Telegram sobre o estado do webhook — URL registrada, status de configuração, erros pendentes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O administrador consegue configurar um bot Telegram (nome + token) em menos de 2 minutos.
- **SC-002**: A configuração do webhook é concluída com feedback visual em até 5 segundos após o clique.
- **SC-003**: A verificação do webhook retorna informações completas de status em até 5 segundos.
- **SC-004**: 100% das tentativas de salvar com nome de bot inválido (não terminando em "bot") são bloqueadas com mensagem clara.
- **SC-005**: A regeneração do webhook secret atualiza automaticamente o webhook no Telegram sem ação adicional do administrador.
- **SC-006**: O administrador consegue visualizar a URL pública do bot (https://t.me/{nome}) imediatamente após preencher o nome.

## Assumptions

- O backend já possui os endpoints necessários para configuração e verificação do webhook Telegram (setup-webhook, webhook-info, regenerate-secret).
- O modelo Agent já possui os campos TelegramBotName, TelegramBotToken e TelegramWebhookSecret.
- O padrão de criação de páginas admin existente (service + page + rota + sidebar) será seguido.
- O administrador obtém o token do bot externamente via BotFather do Telegram antes de configurar no sistema.
- A URL base do webhook no backend (avabot.net) já está configurada e acessível publicamente.
- O sistema de autenticação admin existente será reutilizado para proteger as operações de Telegram.
