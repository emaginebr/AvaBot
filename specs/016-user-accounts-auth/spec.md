# Feature Specification: Contas de usuário e autenticação

**Feature Branch**: `016-user-accounts-auth`  
**Created**: 2026-10-10  
**Status**: Draft  
**Input**: User description: "Crie um sistema de autenticação: crie uma entidade para usuários; o usuário será o dono dos agentes; o token de autenticação deve durar 30 dias; deve ter um criar conta no site; continue usando JWT"

## Contexto

Hoje o painel tem um único acesso de administrador, com usuário e senha fixados na configuração do servidor. Não existe cadastro, não existe mais de uma pessoa com acesso, e todos os agentes são visíveis para quem entra. Para que mais de uma pessoa ou empresa use o produto, cada uma precisa de uma conta própria, criada por ela mesma no site, e precisa ver e administrar apenas os seus agentes. O acesso deve permanecer válido por 30 dias, para que o uso diário não exija login a cada sessão.

Os canais de atendimento (chat no site, widget, Telegram e WhatsApp) são usados pelo público final e continuam sem login.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criar conta e entrar (Priority: P1)

Um visitante abre o site, escolhe "Criar conta", informa nome, e-mail e senha e passa a ter acesso ao painel. Nas vezes seguintes, entra com e-mail e senha. Ao entrar, vê o painel vazio, pronto para criar o primeiro agente.

**Why this priority**: sem conta própria, nada do resto existe; é a porta de entrada do produto.

**Independent Test**: criar uma conta nova no site, sair, entrar de novo com e-mail e senha e chegar ao painel sem nenhum agente.

**Acceptance Scenarios**:

1. **Given** um visitante sem conta, **When** informa nome, e-mail válido e senha aceita na página de criar conta, **Then** a conta é criada e ele entra no painel sem precisar fazer login em seguida.
2. **Given** um e-mail já cadastrado, **When** alguém tenta criar conta com ele, **Then** o sistema recusa e informa que o e-mail já está em uso.
3. **Given** uma conta existente, **When** o usuário informa e-mail e senha corretos na página de login, **Then** entra no painel.
4. **Given** uma conta existente, **When** o usuário informa senha errada, **Then** recebe uma mensagem genérica de credenciais inválidas, sem dizer se foi o e-mail ou a senha.
5. **Given** uma senha com menos de 8 caracteres, **When** o visitante tenta criar conta, **Then** a página explica a regra e não cria a conta.

---

### User Story 2 - Cada usuário vê e administra só os próprios agentes (Priority: P1)

Cada agente pertence a exatamente um usuário, o que o criou. No painel, o usuário vê a lista dos seus agentes e tudo que depende deles: configurações, base de conhecimento, sessões de chat, teste de agente, Power BI, Telegram e WhatsApp. Agentes de outros usuários não aparecem e não podem ser alterados, mesmo que o usuário tente acessar pelo endereço direto.

**Why this priority**: é o que torna o produto utilizável por mais de uma pessoa com segurança; sem isolamento, o cadastro aberto seria um risco.

**Independent Test**: com duas contas, criar um agente em cada; confirmar que cada conta lista só o seu, e que acessar o endereço do agente da outra conta (lista, edição, sessões, arquivos, Power BI, Telegram, WhatsApp, teste) responde como se ele não existisse.

**Acceptance Scenarios**:

1. **Given** um usuário autenticado, **When** cria um agente, **Then** o agente fica registrado como dele.
2. **Given** dois usuários com agentes, **When** cada um abre a lista de agentes, **Then** vê apenas os seus.
3. **Given** um usuário autenticado, **When** tenta abrir, editar, excluir ou consultar qualquer dado (sessões, arquivos, Power BI, Telegram, WhatsApp, teste) de um agente de outro usuário pelo endereço direto, **Then** recebe a mesma resposta de "não encontrado" que receberia para um agente inexistente.
4. **Given** um agente de um usuário, **When** o público usa o chat, o widget, o Telegram ou o WhatsApp desse agente, **Then** tudo funciona sem login, como hoje.

---

### User Story 3 - Acesso que dura 30 dias (Priority: P2)

Depois de entrar, o usuário continua com acesso por 30 dias sem precisar digitar a senha de novo, mesmo fechando o navegador. Passados os 30 dias, o painel pede login de novo. Quem quiser pode sair a qualquer momento.

**Why this priority**: conveniência de uso diário; a regra de 30 dias foi definida pelo produto.

**Independent Test**: entrar, fechar e reabrir o navegador dentro dos 30 dias e continuar no painel; com um acesso emitido há mais de 30 dias, qualquer ação do painel leva à página de login.

**Acceptance Scenarios**:

1. **Given** um usuário que entrou há menos de 30 dias, **When** volta ao painel, **Then** continua autenticado.
2. **Given** um acesso emitido há mais de 30 dias, **When** o usuário faz qualquer ação no painel, **Then** é levado ao login com uma mensagem de sessão expirada.
3. **Given** um usuário autenticado, **When** escolhe "Sair", **Then** o painel esquece o acesso neste navegador e o leva ao login.

---

### User Story 4 - Agentes existentes continuam com dono (Priority: P2)

Os agentes que já existem antes desta mudança não podem ficar órfãos nem visíveis para qualquer conta nova. Na primeira execução da nova versão, o acesso de administrador atual vira uma conta de usuário, dona de todos os agentes existentes, e entra com as mesmas credenciais de hoje.

**Why this priority**: garante continuidade da operação atual (ABIPESCA e demais agentes) no dia do deploy.

**Independent Test**: num ambiente com agentes cadastrados, publicar a nova versão, entrar com as credenciais atuais do administrador e ver todos os agentes antigos; criar uma conta nova e confirmar que ela não vê nenhum deles.

**Acceptance Scenarios**:

1. **Given** uma base com agentes e o acesso de administrador configurado, **When** a nova versão sobe pela primeira vez, **Then** existe uma conta com o e-mail/usuário do administrador, dona de todos os agentes existentes.
2. **Given** essa conta, **When** o administrador entra com as credenciais de hoje, **Then** vê todos os agentes antigos.
3. **Given** uma base sem agentes, **When** a nova versão sobe, **Then** nada é migrado e o cadastro funciona normalmente.

---

### User Story 5 - Gerenciar a própria conta (Priority: P3)

O usuário vê o nome e o e-mail da própria conta no painel e pode alterar o nome e a senha, informando a senha atual para trocar.

**Why this priority**: necessário para um cadastro aberto, mas não bloqueia as histórias anteriores.

**Independent Test**: alterar a senha, sair e entrar com a senha nova; a antiga deixa de funcionar.

**Acceptance Scenarios**:

1. **Given** um usuário autenticado, **When** altera a senha informando a senha atual correta e uma nova válida, **Then** a próxima entrada só funciona com a nova.
2. **Given** um usuário autenticado, **When** informa a senha atual errada ao tentar trocar, **Then** nada muda e a página avisa.

---

### Edge Cases

- **E-mail em maiúsculas ou com espaços**: `Ana@Exemplo.com ` e `ana@exemplo.com` são a mesma conta; a comparação ignora caixa e espaços nas pontas.
- **Acesso expira no meio de uma ação**: a ação falha com "sessão expirada" e o painel leva ao login sem perder dados já salvos.
- **Criar conta já autenticado**: a página de criar conta leva direto ao painel.
- **Muitas tentativas de login erradas**: após 5 falhas seguidas para o mesmo e-mail, novas tentativas ficam bloqueadas por 15 minutos.
- **Agente sem dono depois da migração**: não pode existir; a migração falha de forma visível se houver agente que não possa ser atribuído.
- **Dois usuários com o mesmo nome**: permitido; a identidade é o e-mail.
- **Acesso aos canais públicos de um agente cujo dono trocou a senha**: nada muda para o público.
- **Sair não invalida outros navegadores**: um acesso ativo em outro dispositivo continua até expirar; isso é aceito nesta versão.
- **E-mail digitado errado no cadastro**: a conta é criada mesmo assim (não há confirmação); o usuário corrige o e-mail depois ou cria outra conta.
- **Senha esquecida**: não há recuperação automática; a página de login orienta a procurar o suporte.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST manter um cadastro de usuários com nome, e-mail, senha protegida, situação (ativo/inativo) e data de criação.
- **FR-002**: O e-mail MUST ser único entre os usuários, comparado sem diferenciar maiúsculas de minúsculas e sem espaços nas pontas.
- **FR-003**: O site MUST oferecer uma página pública de criar conta, acessível a partir da página inicial e da página de login, pedindo nome, e-mail e senha.
- **FR-004**: A senha MUST ter ao menos 8 caracteres e MUST ser guardada apenas de forma irreversível; o sistema nunca a exibe nem a envia por nenhum canal.
- **FR-005**: Ao criar a conta, o usuário MUST entrar automaticamente no painel.
- **FR-006**: O login MUST ser por e-mail e senha; em falha, a mensagem MUST ser genérica, sem revelar se o e-mail existe.
- **FR-007**: Após 5 falhas seguidas de login para o mesmo e-mail, o sistema MUST recusar novas tentativas por 15 minutos.
- **FR-008**: O acesso concedido no login ou no cadastro MUST valer por 30 dias; depois disso, qualquer ação do painel MUST exigir novo login.
- **FR-009**: Todo agente MUST ter exatamente um usuário dono, definido na criação como o usuário autenticado que o criou.
- **FR-010**: Todas as listagens e operações do painel sobre agentes e seus dados (configuração, base de conhecimento e busca, sessões e mensagens, teste de agente, Power BI, Telegram, WhatsApp) MUST ser restritas aos agentes do usuário autenticado.
- **FR-011**: O acesso a um agente de outro usuário, por qualquer operação do painel, MUST responder exatamente como para um agente inexistente, sem revelar que ele existe.
- **FR-012**: Os canais públicos (página inicial, chat por endereço do agente, widget, Telegram e WhatsApp) MUST continuar funcionando sem autenticação.
- **FR-013**: Na primeira execução da nova versão, o sistema MUST criar uma conta a partir das credenciais de administrador atualmente configuradas e atribuir a ela todos os agentes existentes; depois disso, o login por credenciais de configuração deixa de existir.
- **FR-014**: O usuário MUST poder ver nome e e-mail da própria conta e alterar nome e senha, confirmando a senha atual para trocar a senha.
- **FR-015**: O usuário MUST poder sair, e o painel MUST esquecer o acesso naquele navegador.
- **FR-016**: A conta MUST ficar ativa imediatamente após o cadastro, sem confirmação de e-mail; o sistema não envia nenhum e-mail nesta versão.
- **FR-017**: O sistema MUST NOT oferecer recuperação de senha ("esqueci minha senha") nesta versão; a página de login MUST orientar quem esqueceu a senha a procurar o suporte.
- **FR-018**: Todas as contas MUST ter os mesmos direitos; não existe perfil de administrador global, e nenhuma conta, incluindo a migrada do administrador atual, vê ou administra agentes de outra conta.

### Key Entities

- **Usuário**: pessoa com acesso ao painel. Tem nome, e-mail (único), senha protegida, situação, data de criação. Possui zero ou mais agentes.
- **Agente**: passa a ter um dono (usuário). Tudo que já depende do agente (sessões, mensagens, arquivos da base, configuração do Power BI, datasets, histórico de consultas, Telegram, WhatsApp) continua ligado ao agente e, por ele, ao dono.
- **Acesso**: o comprovante de login do usuário, válido por 30 dias a partir da emissão; identifica o usuário em cada ação do painel.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um visitante cria a conta e chega ao painel em menos de 2 minutos, sem ajuda.
- **SC-002**: Em teste com duas contas, 100% das operações do painel sobre agentes da outra conta respondem como "não encontrado"; nenhuma lista mostra agente alheio.
- **SC-003**: Um acesso emitido há 29 dias continua funcionando; um emitido há 31 dias é recusado em 100% das ações do painel.
- **SC-004**: Após a publicação em um ambiente com agentes, 100% deles pertencem à conta migrada do administrador e o login com as credenciais atuais funciona na primeira tentativa.
- **SC-005**: Nenhuma senha aparece em texto em banco, logs ou respostas, verificado por inspeção após criar contas e trocar senhas.
- **SC-006**: Os canais públicos de todos os agentes existentes continuam respondendo após a publicação, sem nenhuma reconfiguração.

## Assumptions

- O painel é usado por pessoas ou empresas independentes; não há compartilhamento de um agente entre contas nesta versão (um agente, um dono).
- O comprovante de acesso continua sendo um token assinado (JWT), como hoje, agora com validade de 30 dias e identificando o usuário; é uma restrição dada pelo pedido, não uma escolha desta spec.
- "Sair" só esquece o acesso no navegador atual; não há revogação central de acessos nesta versão.
- O nome de usuário de administrador atual pode não ser um e-mail; na migração, se não for, a conta criada usa esse valor como e-mail de login mesmo assim e o administrador pode trocá-lo depois pela própria conta.
- Mensagens e páginas em português.
- Cadastro aberto ao público; sem convite, aprovação manual ou limite de contas nesta versão.
- Sem confirmação de e-mail e sem recuperação de senha nesta versão (decisões do produto); um e-mail digitado errado cria uma conta que o próprio usuário não reconhece, e quem esquecer a senha depende do suporte. Ambos podem entrar numa versão seguinte quando houver serviço de envio de e-mail.
- Suporte a um cliente que precise de intervenção (senha esquecida, e-mail errado) é feito fora do painel, por acesso direto ao banco, já que não há administrador global.
- Exclusão de conta, papéis dentro de uma conta (equipe), administrador global e compartilhamento de agentes ficam fora de escopo.
