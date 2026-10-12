# Feature Specification: MCP de gestão de agentes

**Feature Branch**: `017-mcp-agent-management`  
**Created**: 2026-10-12  
**Status**: Draft  
**Input**: User description: "Crie um MCP para que a IA possa conectar, cria, editar, ver logs, e configurar os agentes. O MCP não pode excluir um agente ou ler ou gravar secrets (chave de API de model ou App ID, Directory ID e secret do BI), esses tem q ser alterados pelo usuário"

## Contexto

Hoje toda a administração de um agente (criar, ajustar o prompt, ligar o Power BI, descrever o schema, ler as conversas, testar perguntas) é feita por uma pessoa no painel. O trabalho de calibrar um agente é repetitivo: ler conversas, achar respostas ruins, ajustar o prompt ou as descrições do schema, testar de novo. Um assistente de IA (Claude, Cursor, ChatGPT ou outro que fale o protocolo MCP, Model Context Protocol) consegue fazer esse ciclo sozinho se tiver uma forma padrão de se conectar ao AvaBot e operar a conta do usuário.

Esta feature expõe o AvaBot como um servidor MCP: o usuário conecta o seu assistente à própria conta e o assistente passa a criar, editar, configurar e inspecionar os agentes dessa conta. Duas coisas ficam de fora de propósito e são o coração da regra de segurança desta feature: o assistente **nunca exclui um agente** e **nunca lê nem grava segredos** (chave de API do modelo, e no Power BI o App ID, o Directory ID e o segredo do aplicativo; por extensão, qualquer outra credencial de canal). Esses valores continuam sendo informados só pelo usuário, no painel.

Os canais públicos (chat no site, widget, Telegram, WhatsApp) não mudam.

## Clarifications

### Session 2026-10-12

- Q: O assistente conecta a um servidor MCP remoto servido pelo AvaBot, a um pacote local instalado na máquina do usuário, ou aos dois? → A: Só remoto. O AvaBot expõe o MCP no seu endereço público; o usuário configura endereço e chave no assistente. Nenhum pacote local nesta versão.
- Q: As consultas Power BI geradas por perguntas de teste (do MCP ou do painel) entram no log de consultas que o assistente lê? → A: Sim, continuam sendo gravadas sem distinção, como hoje; a listagem de logs não ganha filtro nem marcador de origem.
- Q: A auditoria das escritas feitas pelo MCP fica em registro persistido (visível ou não no painel) ou só no log técnico do servidor? → A: Só no log técnico do servidor; sem tabela nova nem tela no painel nesta versão.
- Q: A chave de acesso tem um único nível de permissão ou o usuário escolhe entre "completa" e "somente leitura" na criação? → A: Nível único: toda chave tem o mesmo acesso completo (menos exclusão de agente e segredos); sem escolha de permissão.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Conectar um assistente de IA à conta (Priority: P1)

Na página "Minha conta", o usuário cria uma chave de acesso para assistentes, dá um nome a ela ("Claude do notebook"), copia o valor que aparece uma única vez e cola na configuração do seu assistente junto com o endereço do AvaBot. A partir daí o assistente enxerga as operações disponíveis, sabe em nome de qual conta está agindo e só vê os agentes dessa conta. O usuário pode ver as chaves que criou, quando foram usadas pela última vez, e revogar qualquer uma; um assistente com chave revogada perde o acesso na hora.

**Why this priority**: sem conexão não existe o resto; e a chave dedicada é o que permite conectar sem expor a senha do painel nem o acesso do navegador.

**Independent Test**: criar uma chave, configurar um assistente com ela, pedir ao assistente "quem sou eu no AvaBot" e receber nome e e-mail da conta; revogar a chave e repetir o pedido, recebendo recusa clara.

**Acceptance Scenarios**:

1. **Given** um usuário autenticado no painel, **When** cria uma chave de acesso com um nome, **Then** o valor da chave é exibido uma única vez, com orientação de copiá-lo, e a chave aparece na lista com nome, data de criação e "nunca usada".
2. **Given** um assistente configurado com uma chave válida, **When** se conecta ao AvaBot, **Then** recebe a lista de operações e, ao perguntar pela identidade, recebe nome e e-mail do dono da chave.
3. **Given** uma chave revogada, **When** o assistente tenta qualquer operação, **Then** recebe uma recusa dizendo que a chave não é mais válida, sem nenhum dado.
4. **Given** uma chave em uso, **When** o usuário abre a lista de chaves, **Then** vê a data e hora do último uso.
5. **Given** uma chave inválida ou inexistente, **When** um assistente tenta conectar, **Then** a conexão é recusada com a mesma mensagem de uma chave revogada, sem dizer qual é o caso.

---

### User Story 2 - Criar e editar agentes pelo assistente (Priority: P1)

O usuário pede ao assistente: "crie um agente chamado Suporte Loja, que responda só sobre trocas e devoluções, coletando nome e e-mail". O assistente cria o agente com nome, descrição, prompt de sistema, modelo e opções de coleta de dados. Depois o usuário pede ajustes ("deixe o tom mais formal", "troque o modelo"), e o assistente edita. Também lista os agentes da conta, mostra os detalhes de um deles e ativa ou desativa. O assistente nunca consegue excluir um agente, e nunca vê nem altera a chave de API do modelo: ao ler um agente, enxerga apenas se há ou não uma chave configurada; ao tentar gravar uma, recebe recusa com a instrução de que isso é feito pelo usuário no painel.

**Why this priority**: é o uso principal pedido ("criar, editar, configurar"); junto com a US1, já entrega um MVP útil.

**Independent Test**: pelo assistente, criar um agente, listar e conferir que aparece, editar o prompt, desativar e reativar; tentar excluir e tentar informar uma chave de API e receber as duas recusas; conferir no painel que o agente existe com os dados informados e sem chave.

**Acceptance Scenarios**:

1. **Given** um assistente conectado, **When** cria um agente com nome, descrição, prompt, modelo e opções de coleta, **Then** o agente aparece no painel do usuário com exatamente esses dados e pertencendo a ele.
2. **Given** um agente da conta, **When** o assistente lista os agentes, **Then** vê o agente com nome, endereço (slug), modelo, situação, se há chave de API e se o Power BI está ligado, mas nenhum valor de credencial.
3. **Given** um agente da conta, **When** o assistente altera nome, descrição, prompt, modelo ou opções de coleta, **Then** a alteração vale imediatamente para as próximas conversas do agente.
4. **Given** um agente ativo, **When** o assistente pede para desativar, **Then** o agente deixa de atender nos canais públicos; o inverso ao reativar.
5. **Given** qualquer agente, **When** o assistente tenta excluí-lo, **Then** a operação não existe para ele; a recusa explica que a exclusão é feita só pelo usuário no painel.
6. **Given** uma edição que inclui a chave de API do modelo (informar ou remover), **When** o assistente envia, **Then** nada é gravado, nem os outros campos, e a resposta explica que credenciais são alteradas apenas pelo usuário no painel.
7. **Given** um agente de outra conta, **When** o assistente tenta ver ou editar pelo identificador, **Then** recebe "não encontrado", igual a um agente inexistente.

---

### User Story 3 - Ler logs e testar o agente (Priority: P2)

O usuário pergunta ao assistente "o que as pessoas perguntaram para o agente ABIPESCA ontem e onde ele respondeu mal?". O assistente lista as sessões de conversa do agente, lê as mensagens de cada uma, e, se o agente usa Power BI, lista as consultas executadas com pergunta, consulta gerada, duração e erro. Com isso em mãos, o assistente faz uma pergunta de teste ao agente (igual ao "teste de agente" do painel) e recebe a resposta com o rastro de busca e consultas, sem gerar uma sessão pública. Ajusta o prompt ou as descrições e testa de novo.

**Why this priority**: é o que fecha o ciclo de calibração; depende de a US2 existir, mas o valor é grande e foi pedido explicitamente ("ver logs").

**Independent Test**: com um agente que já tem conversas e consultas BI, pedir ao assistente as últimas sessões, as mensagens de uma delas e as últimas consultas com erro; pedir uma pergunta de teste e receber a resposta com o rastro.

**Acceptance Scenarios**:

1. **Given** um agente com conversas, **When** o assistente pede as sessões, **Then** recebe uma lista paginada (mais recentes primeiro) com data, canal, dados coletados do visitante e quantidade de mensagens.
2. **Given** uma sessão do agente, **When** o assistente pede as mensagens, **Then** recebe todas as mensagens em ordem, com papel (visitante ou agente) e horário.
3. **Given** um agente com Power BI, **When** o assistente pede os logs de consulta, **Then** recebe uma lista paginada com pergunta do visitante, dataset, consulta gerada, duração, linhas retornadas e erro, podendo filtrar só os com erro.
4. **Given** um agente, **When** o assistente faz uma pergunta de teste (com histórico opcional), **Then** recebe a resposta do agente mais o texto buscado na base, as consultas BI executadas e o rastro por rodada, e nenhuma sessão nova aparece na lista de conversas.
5. **Given** uma sessão de outra conta, **When** o assistente tenta ler, **Then** recebe "não encontrado".

---

### User Story 4 - Configurar Power BI e canais sem tocar em segredos (Priority: P2)

O usuário já informou no painel o Directory ID, o App ID e o segredo do aplicativo Power BI. A partir daí, pede ao assistente: "ligue o Power BI no agente Vendas, adicione o dataset Faturamento do workspace Comercial, gere o schema e descreva as colunas de data em português". O assistente vê que a credencial está configurada (sem ver os valores), liga o recurso, roda o teste de conexão, lista workspaces e datasets disponíveis, vincula datasets, gera e lê o schema e grava as descrições de tabelas, colunas e medidas. No Telegram, o assistente vê se o bot está configurado e registra o webhook, mas não lê nem grava o token do bot. No WhatsApp, vê a situação da conexão.

**Why this priority**: é a parte mais trabalhosa da configuração de um agente BI e o maior ganho de automação; mas depende do usuário ter informado as credenciais antes.

**Independent Test**: num agente com credenciais BI já salvas pelo usuário, o assistente liga o Power BI, lista datasets, vincula um, gera o schema e grava uma descrição; conferir no painel. Tentar ler a configuração e confirmar que Directory ID, App ID e segredo não aparecem; tentar gravá-los e receber recusa.

**Acceptance Scenarios**:

1. **Given** um agente com credenciais BI salvas, **When** o assistente pede a configuração do Power BI, **Then** recebe apenas: ligado ou não, credencial configurada ou não, e resultado do último teste; nunca Directory ID, App ID ou segredo, nem mascarados.
2. **Given** um agente sem credenciais BI, **When** o assistente tenta ligar o Power BI ou listar datasets, **Then** recebe uma recusa dizendo que o usuário precisa informar as credenciais no painel primeiro.
3. **Given** qualquer agente, **When** o assistente tenta gravar Directory ID, App ID ou segredo, **Then** nada é gravado e a resposta explica que credenciais são alteradas apenas pelo usuário no painel.
4. **Given** um agente com credenciais BI, **When** o assistente roda o teste de conexão, **Then** recebe o resultado por etapa, igual ao do painel.
5. **Given** um agente com credenciais BI, **When** o assistente lista workspaces e datasets, vincula um dataset, gera o schema, lê o schema e grava descrições, **Then** tudo aparece no painel exatamente como se o usuário tivesse feito.
6. **Given** um agente com bot do Telegram configurado pelo usuário, **When** o assistente pede a situação do Telegram, **Then** vê nome do bot e se o webhook está registrado, sem o token nem o segredo do webhook; **When** pede para registrar o webhook, **Then** o registro é feito.
7. **Given** um agente, **When** o assistente tenta gravar o token do bot do Telegram ou o token do WhatsApp, **Then** recebe a mesma recusa de credencial.
8. **Given** um agente, **When** o assistente pede a situação do WhatsApp, **Then** vê se está conectado; o pareamento por QR code continua sendo feito pelo usuário no painel.

---

### User Story 5 - Manter a base de conhecimento (Priority: P3)

O usuário pede ao assistente: "adicione na base do agente Suporte Loja a política de trocas que está neste texto". O assistente lista os documentos da base do agente, com situação de processamento, adiciona um documento novo a partir de um texto (com nome), pede o reprocessamento de um documento com falha e remove um documento que ficou obsoleto.

**Why this priority**: completa "configurar os agentes", mas o usuário consegue fazer isso no painel sem grande esforço; vem depois do ciclo de calibração.

**Independent Test**: pelo assistente, listar a base de um agente, adicionar um documento de texto, ver que ele é processado e passa a ser usado em uma pergunta de teste, e removê-lo.

**Acceptance Scenarios**:

1. **Given** um agente com documentos, **When** o assistente lista a base, **Then** vê nome, tamanho, situação de processamento e data de cada documento.
2. **Given** um agente, **When** o assistente adiciona um documento informando nome e conteúdo em texto, **Then** o documento entra na base e é processado como um documento enviado pelo painel.
3. **Given** um documento com falha de processamento, **When** o assistente pede reprocessamento, **Then** o documento volta a ser processado.
4. **Given** um documento da base, **When** o assistente o remove, **Then** ele deixa de ser usado nas respostas do agente.

---

### Edge Cases

- **Chave revogada no meio de uma sequência de operações**: a operação em curso termina (ou falha) normalmente; a próxima é recusada.
- **Assistente envia um campo de segredo junto com campos permitidos**: a operação inteira é recusada e nada é gravado, para que o assistente não "pense" que a credencial foi salva.
- **Assistente pede um dado que inclui um segredo embutido** (por exemplo, a configuração completa do agente): a resposta vem sem os campos de segredo, apenas com indicadores "configurado/não configurado".
- **Assistente tenta operar um agente de outra conta ou inexistente**: "não encontrado", sem distinguir os dois casos.
- **Conta do dono da chave desativada**: todas as chaves da conta param de funcionar.
- **Mais de uma chave por conta**: permitido; cada assistente ou máquina pode ter a sua, revogável separadamente.
- **Chave reutilizada em dois assistentes ao mesmo tempo**: funciona; o "último uso" reflete o mais recente.
- **Logs muito grandes**: sessões, mensagens e consultas vêm paginadas com um tamanho máximo por página; o assistente pede as próximas páginas.
- **Documento de texto grande demais**: recusado com o limite informado, o mesmo que o painel aplica a arquivos.
- **Pergunta de teste demorada ou com falha do modelo**: a resposta informa a falha no rastro, sem derrubar a conexão.
- **Nome de chave repetido**: permitido; o que identifica a chave é o seu valor.
- **Usuário muda a senha do painel**: as chaves de acesso continuam válidas; só a revogação as invalida.
- **Mensagem de erro para o assistente**: sempre em linguagem clara, dizendo o que fazer (por exemplo, "informe a credencial no painel, em Agentes > Vendas > Power BI"), porque quem lê é o assistente e ele repassa ao usuário.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST expor as operações de gestão de agentes por meio de um servidor MCP, de forma que qualquer assistente compatível com o protocolo se conecte informando apenas o endereço do AvaBot e uma chave de acesso.
- **FR-002**: O usuário MUST poder criar, listar e revogar chaves de acesso para assistentes na própria conta, no painel, dando um nome a cada chave; o valor da chave MUST ser exibido uma única vez na criação e nunca mais.
- **FR-003**: A lista de chaves MUST mostrar nome, data de criação e data/hora do último uso de cada chave; chaves revogadas MUST deixar de funcionar imediatamente.
- **FR-004**: Toda operação do MCP MUST agir em nome do dono da chave e enxergar somente os agentes e dados dessa conta; agentes de outras contas MUST responder como inexistentes.
- **FR-005**: O assistente MUST poder descobrir quem é a conta conectada (nome e e-mail).
- **FR-006**: O assistente MUST poder listar os agentes da conta e ver os detalhes de um agente: nome, endereço (slug), descrição, prompt de sistema, modelo, situação, opções de coleta, nome do bot do Telegram, se há chave de API do modelo, se o Power BI está ligado, datas de criação e alteração.
- **FR-007**: O assistente MUST poder criar agentes e editar nome, descrição, prompt de sistema, modelo, opções de coleta (nome, e-mail, telefone) e nome do bot do Telegram, e ativar ou desativar um agente.
- **FR-008**: O MCP MUST NOT oferecer nenhuma operação que exclua um agente; a exclusão continua existindo apenas no painel.
- **FR-009**: O MCP MUST NOT retornar, em nenhuma resposta, o valor de nenhum segredo, nem mesmo mascarado: chave de API do modelo, Directory ID (tenant), App ID (client) e segredo do aplicativo Power BI, token do bot do Telegram, segredo do webhook do Telegram e token do WhatsApp. No lugar deles, as respostas MUST trazer apenas indicadores de "configurado / não configurado".
- **FR-010**: O MCP MUST NOT aceitar, em nenhuma operação, a gravação ou remoção de qualquer segredo listado em FR-009; uma solicitação que contenha um desses campos MUST ser recusada por inteiro, sem gravar os demais campos, com uma mensagem dizendo que a alteração é feita pelo usuário no painel e onde.
- **FR-011**: O assistente MUST poder listar as sessões de conversa de um agente (paginadas, mais recentes primeiro, com data, canal, dados coletados e quantidade de mensagens) e ler as mensagens de uma sessão em ordem, com papel e horário.
- **FR-012**: O assistente MUST poder listar os logs de consulta Power BI de um agente (paginados, com pergunta, dataset, consulta, duração, linhas, situação e erro), com filtro por situação.
- **FR-013**: O assistente MUST poder fazer uma pergunta de teste a um agente, com histórico opcional, e receber a resposta com o rastro completo (texto buscado, consultas BI, rodadas), sem criar sessão pública nem mensagem no histórico de conversas. As consultas Power BI executadas durante o teste continuam entrando no log de consultas do agente, sem sessão vinculada e sem marcação de origem, igual ao teste do painel hoje.
- **FR-014**: O assistente MUST poder, no Power BI de um agente: ver se está ligado, se a credencial está configurada e o resultado do último teste; ligar e desligar; rodar o teste de conexão; listar workspaces e datasets disponíveis; vincular, editar (nome e descrição) e desvincular datasets; gerar e ler o schema; gravar descrições de tabelas, colunas e medidas. Operações que dependem de credencial MUST responder, quando ela não existe, que o usuário precisa informá-la no painel.
- **FR-015**: O assistente MUST poder ver a situação do Telegram de um agente (nome do bot, bot configurado ou não, webhook registrado ou não) e registrar o webhook; MUST NOT poder ler ou gravar o token nem regenerar o segredo do webhook.
- **FR-016**: O assistente MUST poder ver a situação da conexão do WhatsApp de um agente; o pareamento por QR code e a desconexão continuam no painel.
- **FR-017**: O assistente MUST poder listar os documentos da base de conhecimento de um agente, adicionar um documento a partir de nome e conteúdo em texto, pedir reprocessamento e remover um documento, com os mesmos limites de tamanho do painel.
- **FR-018**: Toda operação do MCP MUST aplicar as mesmas validações e regras que o painel aplica (nome obrigatório, modelo válido, limites de tamanho), devolvendo as mesmas mensagens de erro.
- **FR-019**: Toda operação de escrita feita pelo MCP MUST ficar registrada no log técnico do servidor, com a identificação da chave usada, da conta, da operação e do agente afetado, de forma que o operador do sistema consiga saber depois o que cada assistente alterou. Não há registro em banco nem tela de auditoria no painel nesta versão.
- **FR-020**: Toda recusa do MCP (chave inválida, operação inexistente, segredo, agente de outra conta, credencial ausente) MUST vir com uma mensagem em português que diga ao assistente o que fazer ou pedir ao usuário.
- **FR-021**: O MCP MUST vir com uma descrição de cada operação e de seus parâmetros, clara o bastante para que um assistente a use sem documentação externa; e a documentação do projeto MUST explicar como criar a chave e conectar os assistentes mais comuns.

### Key Entities

- **Chave de acesso**: credencial criada pelo usuário para conectar assistentes. Tem nome, dono (usuário), data de criação, data/hora do último uso e situação (ativa ou revogada). Não tem nível de permissão: toda chave dá o mesmo acesso completo às operações do MCP. O valor só existe para o usuário no momento da criação; o sistema guarda apenas uma forma irreversível dele.
- **Operação MCP**: cada ação que um assistente pode executar (listar agentes, criar agente, ler mensagens, gerar schema...). Tem nome, descrição, parâmetros e resultado; o conjunto de operações é fixo nesta versão e exclui exclusão de agente e qualquer leitura ou gravação de segredo.
- **Agente, Sessão, Mensagem, Documento da base, Configuração Power BI, Dataset, Schema, Log de consulta**: entidades que já existem; o MCP as lê e altera dentro das regras acima, sempre restritas ao dono da chave.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um usuário cria a chave, configura o assistente e obtém a primeira resposta ("quem sou eu") em menos de 5 minutos seguindo a documentação, sem ajuda.
- **SC-002**: Em uma inspeção de todas as operações do MCP, 100% das respostas estão livres de valores de segredo (chave de API, Directory ID, App ID, segredo BI, tokens de Telegram e WhatsApp, segredo de webhook), inclusive mascarados.
- **SC-003**: 100% das tentativas de gravar um segredo ou de excluir um agente pelo MCP são recusadas, nada é alterado e a mensagem de recusa orienta o assistente a pedir ao usuário.
- **SC-004**: Com duas contas e uma chave de cada, 100% das operações sobre agentes, sessões e documentos da outra conta respondem "não encontrado".
- **SC-005**: Um assistente consegue, sozinho, executar o ciclo "ler últimas 20 sessões → fazer 5 perguntas de teste → ajustar o prompt → testar de novo" em um agente, sem nenhuma ação no painel.
- **SC-006**: Após revogar uma chave, a primeira operação seguinte com ela é recusada em 100% dos casos.
- **SC-007**: Tudo que o assistente cria ou altera aparece no painel com os mesmos dados, sem diferença entre "feito pelo MCP" e "feito no painel".

## Assumptions

- O servidor MCP é servido pelo próprio AvaBot, no mesmo endereço público do produto, e esta é a única forma de conexão nesta versão (confirmado em Clarifications); o usuário não instala nada localmente, só configura endereço e chave no assistente. Assistentes que só aceitam processo local ficam fora do escopo até uma versão seguinte.
- As chaves de acesso não expiram; são revogadas manualmente. Troca de senha não afeta chaves. Isso é aceito nesta versão; expiração opcional fica para o futuro.
- "Segredo" inclui, além dos citados no pedido (chave de API do modelo, Directory ID, App ID e segredo do Power BI), o token do bot do Telegram, o segredo do webhook e o token do WhatsApp, por serem credenciais da mesma natureza. Nome do bot do Telegram não é segredo.
- "Logs" são as conversas (sessões e mensagens) e os logs de consulta Power BI de cada agente. Logs técnicos do servidor não são expostos.
- "Configurar" inclui dados do agente, Power BI (sem credenciais), Telegram (sem token), situação do WhatsApp e base de conhecimento. Remover documento da base e desvincular dataset são permitidos; a única exclusão proibida é a do agente em si.
- A pergunta de teste pelo MCP usa a chave de API do modelo já configurada no agente (ou a do servidor, como o painel); o assistente nunca a informa.
- O MCP respeita o mesmo isolamento por conta da feature 016; não existe conta administradora com visão global.
- Mensagens e descrições das operações em português, como o painel.
- Fora de escopo nesta versão: exclusão de agente, leitura ou escrita de qualquer credencial, pareamento de WhatsApp, envio de arquivos binários para a base (só texto), gestão de usuários e chaves de outras contas, limite de uso por chave.
