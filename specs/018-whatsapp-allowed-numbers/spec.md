# Feature Specification: Lista de Números Autorizados e Solicitação de Acesso no WhatsApp

**Feature Branch**: `018-whatsapp-allowed-numbers`  
**Created**: 2026-11-23  
**Status**: Draft  
**Input**: User description: "Na opção do whatsapp deve incluir a possibilidade de restringir o uso a uma lista de números. Nesse cadastro deve ter nome e telefone. Deve ser uma opção por agente, o usuário pode habilitar ele pra todo mundo ou restringir por telefone." Complemento: "Quando um usuário não estiver cadastrado o agente deverá responder com frases prontas personalizáveis (ex.: 'Me desculpe, você não possui acesso, deseja solicitar agora? responda apenas \"sim\" ou \"não\"', 'Seu acesso foi solicitado, assim que for aprovado você poderá fazer mais perguntas', 'Ainda estamos aguardando a aprovação do seu acesso'). Enquanto o usuário não for aprovado, não deve gastar token de IA com ele."

## Clarifications

### Session 2026-11-23

- Q: Como o agente reage a mensagens de números fora da lista no modo restrito? → A: Conduz um fluxo de solicitação de acesso com frases prontas personalizáveis por agente (oferta de solicitação com "sim"/"não", confirmação de solicitação, aviso de aguardando aprovação), sem acionar a IA. *(Substitui a resposta anterior de "mensagem fixa padrão".)*
- Q: O modo de acesso e a lista devem ser gerenciáveis pelo servidor MCP (feature 017)? → A: Sim — consultar/alterar o modo, listar/adicionar/editar/remover contatos e listar/aprovar/rejeitar solicitações, com as mesmas regras de permissão do painel.
- Q: Com que frequência as frases prontas são enviadas ao mesmo remetente? → A: No máximo 1 frase automática por hora por remetente e agente, exceto a resposta imediata a um "sim"/"não" à oferta de solicitação; as demais mensagens no período são ignoradas em silêncio.
- Q: Como tratar remetentes identificados pelo WhatsApp sem número de telefone (LID)? → A: O sistema tenta resolver o telefone real; a solicitação de acesso guarda o identificador do WhatsApp recebido (e o telefone, se resolvido), e a aprovação autoriza esse identificador, de modo que o remetente é reconhecido mesmo sem telefone.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Restringir o atendimento a contatos autorizados (Priority: P1)

O dono do agente acessa a área de WhatsApp do agente e muda o modo de acesso de "Todos" para "Somente contatos autorizados". A partir desse momento, o agente atende com IA apenas os contatos autorizados do agente. Qualquer outro remetente entra no fluxo de solicitação de acesso (US2) e nunca consome tokens de IA.

**Why this priority**: É o objetivo central da funcionalidade: impedir que pessoas não autorizadas usem o agente (gerando custo ou acessando informações internas) pelo WhatsApp.

**Independent Test**: Com um agente conectado ao WhatsApp, cadastrar um número, ativar a restrição e enviar uma mensagem de um número cadastrado (recebe resposta da IA) e de um não cadastrado (recebe só a frase pronta de oferta de acesso).

**Acceptance Scenarios**:

1. **Given** um agente em modo restrito com o telefone X autorizado, **When** X envia uma mensagem de texto, **Then** o agente processa com IA e responde normalmente.
2. **Given** um agente em modo restrito, **When** um remetente não autorizado envia uma mensagem, **Then** o agente responde apenas com uma frase pronta do fluxo de solicitação, sem processar com IA, e registra a tentativa no log.
3. **Given** um agente em modo "Todos", **When** qualquer telefone envia uma mensagem, **Then** o agente responde normalmente (comportamento atual preservado).
4. **Given** um agente em modo restrito participando de um grupo, **When** um participante não autorizado menciona o agente, **Then** a mensagem é ignorada em silêncio no grupo; quando um participante autorizado menciona o agente, ele é atendido normalmente.

---

### User Story 2 - Usuário não cadastrado solicita acesso pelo WhatsApp (Priority: P1)

Uma pessoa que não está na lista escreve para o agente. Ela recebe a frase de oferta ("Me desculpe, você não possui acesso, deseja solicitar agora? Responda apenas \"sim\" ou \"não\""). Se responder "sim", uma solicitação de acesso é registrada e ela recebe a confirmação ("Seu acesso foi solicitado, assim que for aprovado você poderá fazer mais perguntas"). Enquanto a solicitação estiver pendente, novas mensagens recebem o aviso de espera ("Ainda estamos aguardando a aprovação do seu acesso"). Nada disso aciona a IA.

**Why this priority**: Permite que o agente restrito capte novos usuários sem expor a IA e resolve a identificação de remetentes que chegam sem telefone (LID), já que a solicitação guarda o identificador real usado pelo WhatsApp.

**Independent Test**: Em modo restrito, enviar mensagem de um número não cadastrado, responder "sim" e conferir a confirmação, a solicitação pendente no painel e o aviso de espera na mensagem seguinte (após o intervalo de 1 hora), sem nenhum consumo de IA.

**Acceptance Scenarios**:

1. **Given** um remetente não autorizado e sem solicitação, **When** envia qualquer mensagem, **Then** recebe a frase de oferta de solicitação do agente.
2. **Given** a oferta enviada, **When** o remetente responde "sim" (sem diferenciar maiúsculas, acentos ou pontuação; "s" também é aceito), **Then** uma solicitação pendente é criada com o identificador do WhatsApp, o telefone (se conhecido) e o nome de perfil do WhatsApp (se disponível), e o remetente recebe a frase de confirmação.
3. **Given** a oferta enviada, **When** o remetente responde "não" ("nao" e "n" também são aceitos), **Then** nenhuma solicitação é criada e o remetente recebe a frase de recusa.
4. **Given** a oferta enviada, **When** o remetente responde algo diferente de "sim"/"não", **Then** a resposta é tratada como nova mensagem não autorizada (sujeita ao limite de 1 frase por hora).
5. **Given** uma solicitação pendente, **When** o remetente envia nova mensagem, **Then** recebe a frase de aguardando aprovação, respeitando o limite de 1 frase por hora.
6. **Given** qualquer etapa deste fluxo, **When** a mensagem é processada, **Then** nenhum token de IA é consumido e nenhuma sessão de conversa é criada.

---

### User Story 3 - Aprovar ou rejeitar solicitações (Priority: P1)

Na área de WhatsApp do agente, o dono vê as solicitações pendentes (nome de perfil, telefone ou identificador, data) e pode aprovar ou rejeitar. Ao aprovar, informa ou confirma o nome, e o remetente vira contato autorizado; a partir da próxima mensagem ele é atendido pela IA.

**Why this priority**: Fecha o ciclo da solicitação; sem aprovação, o fluxo da US2 não leva a lugar nenhum.

**Independent Test**: Com uma solicitação pendente, aprovar no painel e enviar nova mensagem do mesmo remetente: ela é respondida pela IA. Repetir rejeitando e conferir que o remetente passa a ser ignorado.

**Acceptance Scenarios**:

1. **Given** uma solicitação pendente, **When** o dono aprova (podendo editar o nome), **Then** o remetente vira contato autorizado do agente e sua próxima mensagem é atendida pela IA.
2. **Given** uma solicitação pendente de um remetente sem telefone conhecido (apenas identificador do WhatsApp), **When** o dono aprova, **Then** o contato é criado vinculado a esse identificador e o remetente é reconhecido nas próximas mensagens.
3. **Given** uma solicitação pendente, **When** o dono rejeita, **Then** a solicitação fica como rejeitada e as próximas mensagens do remetente são ignoradas em silêncio, sem nova oferta.
4. **Given** uma solicitação rejeitada, **When** o dono a aprova posteriormente ou cadastra o remetente manualmente, **Then** o remetente passa a ser atendido.
5. **Given** a área de WhatsApp com solicitações pendentes, **When** o dono abre a página, **Then** a quantidade de pendentes fica visível.

---

### User Story 4 - Gerenciar a lista de contatos autorizados (Priority: P1)

Na área de WhatsApp do agente, o dono vê a lista de contatos autorizados (nome e telefone) e pode adicionar, editar e remover contatos manualmente. Cada agente tem a sua própria lista.

**Why this priority**: Permite pré-cadastrar pessoas conhecidas sem que precisem passar pelo fluxo de solicitação.

**Independent Test**: Adicionar um contato com nome e telefone, editar o nome, removê-lo, e confirmar que outro agente não vê esse contato.

**Acceptance Scenarios**:

1. **Given** a área de WhatsApp de um agente, **When** o dono informa nome e telefone válidos e salva, **Then** o contato aparece na lista do agente.
2. **Given** um contato cadastrado, **When** o dono altera nome ou telefone e salva, **Then** a alteração vale a partir da próxima mensagem recebida.
3. **Given** um contato cadastrado, **When** o dono o remove e confirma, **Then** o contato sai da lista e, em modo restrito, o remetente volta a ser tratado como não autorizado (recebe a oferta de solicitação).
4. **Given** um telefone já cadastrado no agente, **When** o dono tenta cadastrá-lo de novo (mesmo em outro formato), **Then** o sistema impede a duplicidade e informa o motivo.
5. **Given** o formulário de contato, **When** o nome está vazio ou o telefone é inválido, **Then** o sistema não salva e indica o campo com problema.
6. **Given** dois agentes, **When** um telefone é cadastrado no agente A, **Then** ele não aparece nem é autorizado no agente B.

---

### User Story 5 - Configurar modo e frases prontas (Priority: P2)

O dono alterna o modo entre "Todos" e "Somente contatos autorizados" e personaliza as frases prontas do fluxo de solicitação. Cada frase vem preenchida com um texto padrão. A lista e as solicitações são preservadas quando o modo volta para "Todos".

**Why this priority**: Ajusta o tom e a flexibilidade operacional; os textos padrão já tornam o fluxo utilizável sem configuração.

**Independent Test**: Alterar a frase de oferta, enviar mensagem de um número não cadastrado e conferir o novo texto; voltar para "Todos" e conferir que a lista continua lá e qualquer número é atendido.

**Acceptance Scenarios**:

1. **Given** um agente novo ou já existente antes desta funcionalidade, **When** o dono abre a área de WhatsApp, **Then** o modo exibido é "Todos" e as frases exibem os textos padrão.
2. **Given** o dono altera uma frase e salva, **When** a próxima mensagem que a dispara chega, **Then** o texto novo é enviado.
3. **Given** o dono apaga o texto de uma frase, **When** tenta salvar, **Then** o sistema não salva e pede um texto (ou permite restaurar o padrão).
4. **Given** um agente em modo restrito com contatos, **When** o dono muda para "Todos", **Then** lista e solicitações são mantidas e todos os números voltam a ser atendidos.
5. **Given** o modo ou frases alterados, **When** a próxima mensagem chega, **Then** ela segue a nova configuração sem reconectar o WhatsApp.

---

### Edge Cases

- Telefone digitado com formatações diferentes ("+55 (11) 98765-4321", "5511987654321", "11 98765-4321"): comparação sempre pela forma normalizada, só com dígitos e com código do país.
- Celulares brasileiros com e sem o nono dígito: o contato cadastrado com o 9 é reconhecido quando a mensagem chega sem ele, e vice-versa.
- Remetente identificado pelo WhatsApp só por identificador interno (LID): o sistema tenta resolver o telefone; se não conseguir, o remetente segue o fluxo de solicitação normalmente, e a aprovação autoriza o identificador. Um contato cadastrado manualmente só por telefone não é reconhecido se o WhatsApp não fornecer o telefone; nesse caso ele precisa solicitar acesso.
- Remetente envia várias mensagens seguidas sem responder "sim"/"não": recebe no máximo 1 frase automática por hora; as demais são ignoradas em silêncio.
- Remetente responde "sim" mais de uma vez: não cria solicitação duplicada; recebe o aviso de aguardando aprovação (respeitando o limite).
- Remetente que respondeu "não": volta a receber a oferta na próxima mensagem após o intervalo de 1 hora.
- Mensagem de tipo não suportado (imagem, áudio) vinda de não autorizado em modo restrito: segue o fluxo de solicitação (não recebe a mensagem de rejeição de tipo).
- Mensagens de grupo: o fluxo de solicitação não acontece em grupos; menções de não autorizados são ignoradas em silêncio, e mensagens sem menção continuam ignoradas como hoje.
- Solicitação pendente cujo remetente é cadastrado manualmente pelo dono: a solicitação é encerrada como aprovada automaticamente.
- Exclusão do agente: contatos, solicitações e frases dele são removidos junto.
- Sessões de conversa já abertas por um número que depois sai da lista: as próximas mensagens deixam de ser atendidas pela IA; o histórico existente é mantido.
- Os canais Telegram e chat web não são afetados.

## Requirements *(mandatory)*

### Functional Requirements

**Modo de acesso**

- **FR-001**: Cada agente MUST ter uma configuração de modo de acesso ao WhatsApp com dois valores: "Todos" (padrão) e "Somente contatos autorizados".
- **FR-002**: Agentes já existentes MUST ficar no modo "Todos" após a entrada da funcionalidade, sem mudança de comportamento.
- **FR-003**: Alterações no modo, na lista, nas solicitações ou nas frases MUST valer para a próxima mensagem recebida, sem reconectar a sessão do WhatsApp.
- **FR-004**: Mudar o modo para "Todos" MUST preservar contatos e solicitações.

**Contatos autorizados**

- **FR-005**: Cada agente MUST ter sua própria lista de contatos autorizados; cada contato tem nome (obrigatório) e telefone e/ou identificador do WhatsApp (pelo menos um obrigatório; cadastro manual exige telefone).
- **FR-006**: O dono do agente MUST conseguir listar, adicionar, editar e remover contatos autorizados na área de WhatsApp do painel.
- **FR-007**: O sistema MUST normalizar o telefone (somente dígitos, com código do país; Brasil (55) assumido quando informado sem código) e rejeitar telefones que, normalizados, não tenham entre 10 e 15 dígitos.
- **FR-008**: O sistema MUST impedir telefones (e identificadores) duplicados na lista de um mesmo agente, considerando a forma normalizada e a equivalência com/sem nono dígito de celulares brasileiros.

**Atendimento em modo restrito**

- **FR-009**: Em modo restrito, o sistema MUST acionar a IA apenas para remetentes reconhecidos como contatos autorizados do agente, por telefone ou por identificador do WhatsApp.
- **FR-010**: Para remetentes não autorizados em conversa privada, o sistema MUST conduzir o fluxo de solicitação de acesso usando exclusivamente frases prontas, sem acionar a IA e sem criar sessão de conversa.
- **FR-011**: O sistema MUST tentar resolver o telefone real de remetentes identificados apenas por identificador interno do WhatsApp antes de verificar a autorização.
- **FR-012**: Em mensagens de grupo, a verificação MUST considerar o participante que enviou a mensagem; menções de participantes não autorizados MUST ser ignoradas em silêncio (sem fluxo de solicitação no grupo).
- **FR-013**: Cada mensagem de remetente não autorizado MUST ser registrada no log técnico com o agente, o identificador/telefone e a etapa do fluxo.

**Fluxo de solicitação de acesso**

- **FR-014**: O fluxo MUST usar quatro frases prontas por agente, cada uma com texto padrão:
  - Oferta: "Me desculpe, você não possui acesso. Deseja solicitar agora? Responda apenas \"sim\" ou \"não\"."
  - Confirmação: "Seu acesso foi solicitado. Assim que for aprovado, você poderá fazer mais perguntas."
  - Aguardando: "Ainda estamos aguardando a aprovação do seu acesso."
  - Recusa: "Tudo bem. Se mudar de ideia, é só enviar uma nova mensagem."
- **FR-015**: Sem solicitação existente, uma mensagem de não autorizado MUST receber a frase de oferta.
- **FR-016**: A resposta à oferta MUST ser reconhecida sem IA, ignorando maiúsculas, acentos, espaços e pontuação: "sim"/"s" cria a solicitação pendente e envia a confirmação; "não"/"nao"/"n" envia a recusa e não cria solicitação; qualquer outro texto é tratado como nova mensagem não autorizada.
- **FR-017**: A resposta "sim"/"não" só MUST ser considerada se chegar dentro de 24 horas após a oferta; depois disso, é tratada como nova mensagem.
- **FR-018**: Com solicitação pendente, novas mensagens do remetente MUST receber a frase de aguardando aprovação.
- **FR-019**: Com solicitação rejeitada, mensagens do remetente MUST ser ignoradas em silêncio (apenas log).
- **FR-020**: O sistema MUST enviar no máximo 1 frase automática por hora para cada combinação de remetente e agente; a confirmação e a recusa, por serem resposta direta a "sim"/"não", não estão sujeitas a esse limite. Mensagens dentro do intervalo MUST ser ignoradas em silêncio.
- **FR-021**: Cada solicitação MUST guardar: identificador do WhatsApp do remetente, telefone (se conhecido), nome de perfil do WhatsApp (se disponível), data da solicitação, situação (pendente, aprovada, rejeitada) e data/autor da decisão.
- **FR-022**: Deve existir no máximo uma solicitação pendente por remetente e agente.

**Aprovação**

- **FR-023**: O dono MUST conseguir listar as solicitações do agente filtrando por situação e ver a quantidade de pendentes na área de WhatsApp.
- **FR-024**: Ao aprovar, o dono MUST poder informar ou editar o nome; o sistema MUST criar o contato autorizado com o nome, o identificador e o telefone (se conhecido) da solicitação.
- **FR-025**: O dono MUST conseguir rejeitar uma solicitação pendente e, depois, aprovar uma solicitação rejeitada.
- **FR-026**: Cadastrar manualmente um contato cujo telefone corresponde a uma solicitação pendente MUST marcar essa solicitação como aprovada.

**Frases prontas**

- **FR-027**: O dono MUST conseguir editar as quatro frases prontas por agente e restaurar o texto padrão de cada uma; frases não podem ficar vazias e têm limite de 500 caracteres.

**Permissões, ciclo de vida e integrações**

- **FR-028**: Apenas usuários com permissão de gerenciar o agente (dono ou administrador, conforme as regras atuais) MUST conseguir ver ou alterar modo, contatos, solicitações e frases daquele agente.
- **FR-029**: A exclusão de um agente MUST remover seus contatos, solicitações e frases.
- **FR-030**: Modo, contatos, solicitações e frases MUST afetar exclusivamente o canal WhatsApp.
- **FR-031**: O servidor MCP de gerenciamento de agentes MUST permitir consultar e alterar o modo, listar/adicionar/editar/remover contatos, listar/aprovar/rejeitar solicitações e consultar/alterar as frases, com as mesmas validações e permissões do painel.

### Key Entities

- **Agente** (existente): ganha o modo de acesso ao WhatsApp (Todos / Somente contatos autorizados) e as quatro frases prontas do fluxo de solicitação.
- **Contato autorizado do WhatsApp**: pertence a um agente; tem nome, telefone (como digitado e normalizado, opcional quando vindo de aprovação sem telefone), identificador do WhatsApp (opcional), origem (manual ou aprovação) e datas de criação e atualização. Telefone e identificador únicos por agente.
- **Solicitação de acesso do WhatsApp**: pertence a um agente; tem identificador do WhatsApp, telefone (se conhecido), nome de perfil, situação (pendente, aprovada, rejeitada), data da solicitação, data e autor da decisão. Ao ser aprovada, gera um contato autorizado.
- **Estado do remetente no fluxo** (transitório): por agente e remetente, guarda a última frase enviada e quando, para aplicar o limite de 1 frase por hora e reconhecer a resposta à oferta em até 24 horas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em modo restrito, 0 tokens de IA são consumidos com remetentes não autorizados, pendentes ou rejeitados.
- **SC-002**: Em modo restrito, 100% das mensagens de contatos autorizados são respondidas pela IA, independentemente do formato do telefone cadastrado ou de o remetente chegar por identificador interno já aprovado.
- **SC-003**: Um remetente não cadastrado consegue solicitar acesso em no máximo 2 mensagens (a mensagem inicial e o "sim").
- **SC-004**: Após a aprovação, a próxima mensagem do remetente é atendida pela IA, sem passos extras.
- **SC-005**: Nenhum remetente recebe mais de 1 frase automática por hora do mesmo agente, além das respostas diretas a "sim"/"não".
- **SC-006**: O dono consegue aprovar uma solicitação em menos de 30 segundos a partir da área de WhatsApp.
- **SC-007**: Nenhum agente existente muda de comportamento após a implantação (todos começam em "Todos").

## Assumptions

- Todo o gerenciamento fica na área de WhatsApp já existente do painel administrativo, no contexto do agente selecionado.
- O remetente não é notificado pelo WhatsApp quando o acesso é aprovado ou rejeitado; ele descobre ao enviar a próxima mensagem (a frase de confirmação já diz isso).
- A rejeição é silenciosa para o remetente e definitiva até o dono reverter, para evitar pedidos repetidos.
- Não há importação em massa (CSV) nem limite rígido de contatos ou solicitações; algumas centenas por agente é o cenário esperado.
- O nome do contato e o nome de perfil do WhatsApp servem só para identificação no painel; não são enviados à IA.
- As frases prontas não usam variáveis (como o nome do remetente) nesta versão.
- As regras de propriedade e permissão de agentes da feature 016 se aplicam sem alteração.
