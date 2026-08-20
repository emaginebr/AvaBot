# Feature Specification: Renomeação para AvaBot + Gestão de Sessão com Cookies e Retomada de Conversa

**Feature Branch**: `008-session-resume-cookies`  
**Created**: 2026-04-10  
**Status**: Draft  
**Input**: User description: "Renomear projeto para avabot-app, armazenar dados do usuário em cookies, implementar fluxo de retorno com resumeSession e opções de retomar/iniciar conversa"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Usuário retornante é reconhecido e escolhe retomar conversa (Priority: P1)

Um usuário que já conversou anteriormente retorna ao site. O sistema detecta que existe uma sessão prévia armazenada nos cookies do navegador. O chat exibe "Bem-vindo de volta, [nome do usuário]" seguido de dois botões: "Desejo retomar nossa última conversa" e "Iniciar uma nova conversa". O chat fica inativo (campo de digitação desabilitado) enquanto os botões estão visíveis. O usuário clica em "Desejo retomar nossa última conversa". O texto da opção aparece no chat como se o usuário tivesse digitado, os botões desaparecem, o chat é reativado e as últimas 10 mensagens da sessão anterior são carregadas.

**Why this priority**: Este é o cenário principal que diferencia a experiência - reconhecer o usuário e permitir continuidade na conversa. Entrega o maior valor ao reduzir fricção para usuários recorrentes.

**Independent Test**: Pode ser testado criando uma sessão, fechando o navegador, retornando e verificando que o nome aparece, as opções são exibidas e as mensagens anteriores são carregadas ao clicar em retomar.

**Acceptance Scenarios**:

1. **Given** um usuário que possui cookie com resumeToken e userName de sessão anterior, **When** ele abre o chat, **Then** o sistema exibe "Bem-vindo de volta, [nome]" e mostra dois botões de opção.
2. **Given** os botões de opção estão visíveis, **When** o usuário observa o chat, **Then** o campo de digitação está desabilitado e não aceita entrada de texto.
3. **Given** os botões estão visíveis, **When** o usuário clica em "Desejo retomar nossa última conversa", **Then** o texto da opção aparece como mensagem do usuário no chat, os botões desaparecem e o campo de digitação é reativado.
4. **Given** o usuário selecionou retomar conversa, **When** a sessão é carregada, **Then** as últimas 10 mensagens da sessão anterior são exibidas no chat em ordem cronológica e a sessão é reconectada.

---

### User Story 2 - Usuário retornante escolhe iniciar nova conversa (Priority: P1)

Um usuário retornante é reconhecido pelo sistema via cookies. Após a saudação personalizada e exibição dos botões, o usuário clica em "Iniciar uma nova conversa". O texto da opção aparece no chat como mensagem do usuário, os botões desaparecem, o chat é reativado e uma nova sessão é criada no backend mantendo os dados do usuário (nome, email, telefone) já armazenados nos cookies.

**Why this priority**: Igualmente crítico ao P1 anterior pois ambos os caminhos formam o fluxo de decisão do usuário retornante. Sem este caminho, o usuário retornante ficaria preso.

**Independent Test**: Pode ser testado retornando com cookies válidos, clicando em "Iniciar uma nova conversa" e verificando que uma nova sessão é criada sem pedir dados novamente.

**Acceptance Scenarios**:

1. **Given** os botões de opção estão visíveis para um usuário retornante, **When** o usuário clica em "Iniciar uma nova conversa", **Then** o texto aparece como mensagem do usuário, os botões desaparecem e o campo de digitação é reativado.
2. **Given** o usuário escolheu nova conversa, **When** a sessão é criada, **Then** os dados do usuário (nome, email, telefone) são reutilizados dos cookies sem nova coleta e um novo resumeToken é armazenado nos cookies.
3. **Given** a nova sessão foi criada, **When** o chat está pronto, **Then** o usuário pode conversar normalmente como se fosse uma sessão nova.

---

### User Story 3 - Primeiro acesso do usuário com persistência em cookies (Priority: P1)

Um novo usuário acessa o chat pela primeira vez. Não existem cookies de sessão anterior. O fluxo segue normalmente como hoje: o usuário se identifica (nome, email, telefone conforme configuração do agente), uma nova sessão é criada e os dados do usuário junto com o resumeToken são armazenados em cookies para futuras visitas.

**Why this priority**: Sem o fluxo de primeiro acesso funcionando corretamente com persistência em cookies, os cenários de retorno não funcionam. É a base de todo o sistema.

**Independent Test**: Pode ser testado acessando o chat sem cookies, completando a identificação e verificando que os cookies foram criados com os dados corretos.

**Acceptance Scenarios**:

1. **Given** um usuário sem cookies de sessão, **When** ele abre o chat, **Then** o fluxo de identificação normal é iniciado (coleta de nome, email, telefone conforme agente).
2. **Given** o usuário completou a identificação, **When** a sessão é criada com sucesso, **Then** o resumeToken, userName, userEmail e userPhone são armazenados em cookies no navegador.
3. **Given** os cookies foram armazenados, **When** o usuário retorna ao site posteriormente, **Then** ele é reconhecido como usuário retornante (User Story 1 ou 2).

---

### User Story 4 - Renomeação do projeto de avachat-app para avabot-app (Priority: P2)

O projeto frontend é renomeado de "avachat-app" para "avabot-app" para alinhar com o backend que já foi renomeado de "AbaChat" para "AvaBot". Todas as referências internas ao nome antigo são atualizadas (nome do pacote, identificadores, chaves de armazenamento).

**Why this priority**: Importante para consistência do projeto mas não bloqueia a funcionalidade principal. Pode ser feito independentemente dos fluxos de sessão.

**Independent Test**: Pode ser testado verificando que não existem referências a "avachat" no código, que o build completa com sucesso e que a aplicação funciona normalmente após a renomeação.

**Acceptance Scenarios**:

1. **Given** o projeto atual se chama "avachat-app", **When** a renomeação é aplicada, **Then** o nome do pacote, títulos e identificadores refletem "avabot-app"/"avabot".
2. **Given** o projeto foi renomeado, **When** a aplicação é construída e executada, **Then** todas as funcionalidades continuam operando normalmente sem erros.
3. **Given** chaves de armazenamento usavam prefixo "avachat:", **When** a migração é aplicada, **Then** as chaves passam a usar o novo prefixo consistente com o novo nome.

---

### Edge Cases

- O que acontece quando o cookie de resumeToken existe mas a sessão no backend foi excluída ou expirou? O sistema deve tratar como primeiro acesso, limpando os cookies antigos e iniciando o fluxo de identificação.
- O que acontece quando os cookies estão parcialmente corrompidos (ex: resumeToken presente mas userName ausente)? O sistema deve limpar todos os cookies de sessão e iniciar fluxo de primeiro acesso.
- O que acontece quando o usuário usa navegação privada? Os cookies não persistem entre sessões, então o fluxo será sempre de primeiro acesso — comportamento aceitável.
- O que acontece quando o backend retorna erro ao tentar resumir a sessão? O sistema deve informar que não foi possível retomar e oferecer iniciar nova conversa.
- O que acontece se o usuário não clicar em nenhum botão e tentar digitar? O campo de digitação deve permanecer desabilitado enquanto os botões estiverem visíveis.
- O que acontece se o usuário retornante fecha o chat sem escolher uma opção e reabre na mesma visita? O estado é reiniciado: a saudação e os botões são exibidos novamente.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE armazenar resumeToken, userName, userEmail e userPhone em cookies do navegador, separados por agente (slug), após criação bem-sucedida de uma sessão.
- **FR-002**: O sistema DEVE verificar a existência de cookies de sessão ao abrir o chat e determinar se o usuário é novo ou retornante.
- **FR-003**: Para usuários retornantes, o sistema DEVE exibir a mensagem "Bem-vindo de volta, [nome do usuário]" usando o nome armazenado nos cookies.
- **FR-004**: Para usuários retornantes, o sistema DEVE exibir dois botões: "Desejo retomar nossa última conversa" e "Iniciar uma nova conversa".
- **FR-005**: O sistema DEVE desabilitar o campo de entrada de texto enquanto os botões de opção estiverem visíveis.
- **FR-006**: Ao clicar em uma opção, o sistema DEVE exibir o texto da opção como mensagem do usuário no chat, remover os botões e reabilitar o campo de entrada.
- **FR-007**: Ao selecionar "Desejo retomar nossa última conversa", o sistema DEVE chamar o endpoint de retomada de sessão do backend com o resumeToken e carregar as últimas 10 mensagens retornadas.
- **FR-008**: Ao selecionar "Iniciar uma nova conversa", o sistema DEVE criar uma nova sessão reutilizando os dados do usuário dos cookies e atualizar o resumeToken no cookie.
- **FR-009**: O sistema DEVE manter o fluxo de identificação atual (coleta de nome/email/telefone) para usuários sem cookies de sessão.
- **FR-010**: Quando a sessão não puder ser retomada (token inválido, sessão expirada), o sistema DEVE limpar os cookies e iniciar o fluxo de primeiro acesso.
- **FR-011**: O projeto DEVE ser renomeado de "avachat-app" para "avabot-app" em todas as referências, incluindo identificadores públicos do widget (objeto global `window.Avabot`, ID do host `avabot-widget-host`, interface `AvabotInitOptions`), nome do pacote e chaves de armazenamento.

### Key Entities

- **Cookie de Sessão**: Conjunto de dados persistidos no navegador, separados por agente (slug), contendo resumeToken (identificador único da sessão), userName, userEmail e userPhone. Cada agente mantém cookies independentes, permitindo retomada de sessão por agente sem conflito.
- **Sessão de Chat**: Registro no backend que vincula o usuário ao agente, contendo histórico de mensagens. Pode ser retomada via resumeToken ou uma nova pode ser criada com os mesmos dados do usuário.
- **Opções de Ação**: Botões interativos exibidos para o usuário retornante que determinam o fluxo seguinte (retomar sessão anterior ou iniciar nova).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Usuários retornantes são reconhecidos e saudados pelo nome em menos de 2 segundos após abrir o chat.
- **SC-002**: 100% das sessões criadas resultam em dados do usuário armazenados em cookies para futuro reconhecimento.
- **SC-003**: Ao retomar conversa, as últimas 10 mensagens são carregadas e exibidas em menos de 3 segundos.
- **SC-004**: O campo de entrada permanece desabilitado enquanto os botões de opção estão visíveis, impedindo entrada de texto em 100% dos casos.
- **SC-005**: A seleção de opção aparece como mensagem do usuário e os botões desaparecem instantaneamente após o clique.
- **SC-006**: Após renomeação, zero referências ao nome antigo "avachat" permanecem no código fonte (exceto histórico git).

## Clarifications

### Session 2026-04-10

- Q: Cookies devem ser por agente (slug) ou globais? → A: Cookies separados por agente (slug) — cada agente tem seu próprio resumeToken e dados de sessão.
- Q: A renomeação deve incluir identificadores públicos do widget (window.Avachat, IDs do DOM, interfaces)? → A: Sim, renomear tudo incluindo API pública (window.Avabot, avabot-widget-host, AvabotInitOptions).
- Q: Ao fechar e reabrir o chat na mesma visita sem ter escolhido uma opção, qual o comportamento? → A: Reiniciar — mostrar saudação e botões novamente.

## Assumptions

- O backend AvaBot já possui o endpoint de retomada de sessão funcional, que aceita um token de identificação e retorna os dados da sessão com as últimas 10 mensagens.
- O backend já possui o endpoint de criação de sessão funcional para novas sessões.
- O fluxo de identificação atual (coleta de nome, email, telefone) continuará funcionando da mesma forma para novos usuários.
- Cookies serão armazenados com escopo do domínio do site, sem necessidade de configuração cross-domain.
- Os cookies terão validade de 30 dias para permitir reconhecimento em visitas futuras.
- O WebSocket de chat continuará sendo usado para troca de mensagens em tempo real após sessão estabelecida.
- A renomeação do projeto não afeta URLs de deployment ou configurações de infraestrutura (apenas código fonte).
