# Feature Specification: Redesenho das páginas da área administrativa

**Feature Branch**: `019-admin-pages-redesign`  
**Created**: 2026-11-25  
**Status**: Draft  
**Input**: User description: "Altere todas as outras páginas da área administrativa, usando o mesmo padrão. Já crie a proposta usando o agente ux-designer e já implemente usando o frontend-react-developer sem aprovação, seguindo o mesmo padrão"

## Contexto

A área administrativa ganhou uma nova identidade visual: o painel (dashboard), o login e o cadastro já seguem o novo padrão. O padrão tem menu lateral azul-escuro em sanfona, cabeçalho, rodapé, cartões brancos com bordas suaves, a tipografia nova da marca e a decoração do site avabot.net. As referências aprovadas estão em `docs/DASHBOARD_PROPOSTA.html` e `docs/LOGIN_PROPOSTA.html`.

As demais páginas administrativas já aparecem dentro da nova moldura, mas o conteúdo delas ainda segue o visual antigo. O resultado é uma experiência inconsistente.

Páginas no escopo:

- Conta
- Meus agentes (lista)
- Novo/editar agente
- Configurações do agente
- Telegram
- WhatsApp, incluindo a lista de contatos autorizados e os pedidos de acesso
- Power BI
- Sessões (lista)
- Detalhe da sessão
- Arquivos de conhecimento
- Busca na base
- Teste do agente

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Navegar por um painel visualmente coerente (Priority: P1)

O administrador navega entre o dashboard e qualquer outra página administrativa e percebe a mesma linguagem visual em todas. Isso vale para títulos de página, cartões, tabelas, botões, campos de formulário, selos de status, mensagens de vazio e de carregamento.

**Why this priority**: é o objetivo central do pedido. Sem consistência, o redesenho do dashboard fica isolado e o produto parece inacabado.

**Independent Test**: abrir cada página do escopo após o dashboard e verificar que todas usam o mesmo cabeçalho de página, os mesmos estilos de cartão, botão, campo, tabela e selo, e a mesma tipografia.

**Acceptance Scenarios**:

1. **Given** um administrador logado com ao menos um agente, **When** ele abre qualquer página do escopo, **Then** a página exibe um cabeçalho com título e descrição curta no padrão do dashboard, e o conteúdo fica organizado em cartões no novo estilo.
2. **Given** qualquer página com botões de ação, **When** ela é exibida, **Then** a ação principal usa o botão primário da marca e as ações secundárias e destrutivas seguem os estilos padronizados.
3. **Given** qualquer página com lista ou tabela, **When** não há dados, **Then** é exibido um estado vazio padronizado com orientação do que fazer.
4. **Given** qualquer página carregando dados, **When** a resposta ainda não chegou, **Then** é exibido um indicador de carregamento padronizado.

---

### User Story 2 - Manter todas as funções existentes intactas (Priority: P1)

O administrador continua conseguindo fazer tudo o que já fazia em cada página:

- criar, editar e excluir agentes;
- configurar o agente;
- enviar e excluir arquivos;
- buscar na base e testar o agente;
- configurar o Telegram;
- conectar o WhatsApp e gerir os contatos e pedidos de acesso;
- configurar o Power BI;
- ver as sessões e mensagens;
- gerir a conta e as chaves MCP.

**Why this priority**: um redesenho que quebre funcionalidade causa regressão em produção; a mudança deve ser apenas visual e de usabilidade.

**Independent Test**: executar o fluxo principal de cada página antes e depois da mudança e obter o mesmo resultado.

**Acceptance Scenarios**:

1. **Given** a página de WhatsApp, **When** o administrador inicia a sessão, **Then** o fluxo de QR Code continua funcionando como hoje, inclusive a renovação automática e o cancelamento.
2. **Given** qualquer formulário existente, **When** o administrador salva dados válidos ou inválidos, **Then** as mesmas validações e mensagens de sucesso ou erro ocorrem, agora no novo estilo.
3. **Given** ações destrutivas existentes (excluir agente, arquivo, contato, chave), **When** acionadas, **Then** continuam exigindo confirmação.

---

### User Story 3 - Usar o painel no celular (Priority: P2)

O administrador acessa qualquer página do escopo em um celular e consegue ler e operar o conteúdo sem rolagem horizontal da página. Tabelas largas podem rolar dentro do próprio cartão ou se transformar em lista.

**Why this priority**: o novo padrão já é responsivo; as páginas internas precisam acompanhar.

**Independent Test**: abrir cada página em largura de 375 px e verificar que não há rolagem horizontal da página e que todas as ações estão acessíveis.

**Acceptance Scenarios**:

1. **Given** uma tela de 375 px de largura, **When** o administrador abre qualquer página do escopo, **Then** o conteúdo se ajusta em uma coluna e as ações continuam visíveis e clicáveis.

---

### Edge Cases

- **Sem agente selecionado:** páginas que dependem de um agente exibem um estado vazio padronizado orientando a selecionar um agente no menu lateral.
- **Erro de carregamento da API:** a página mostra uma mensagem de erro padronizada no próprio conteúdo, sem quebrar o layout.
- **Textos longos** (nomes de arquivos, prompts, mensagens de sessão, URLs de webhook): quebram ou são truncados sem estourar os cartões.
- **Muitos itens em listas:** a paginação existente é mantida com o novo estilo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Todas as páginas do escopo MUST adotar a identidade visual aprovada no dashboard: tipografia da marca, paleta azul `ava`, cartões brancos com bordas suaves, cantos arredondados e botões com o estilo da marca.
- **FR-002**: Cada página MUST ter um cabeçalho de página padronizado: título, descrição curta e, quando houver, a ação principal da página.
- **FR-003**: Elementos repetidos MUST ser padronizados e reutilizados entre páginas:
  - cartões e seções;
  - botões primário, secundário e destrutivo;
  - campos de formulário com rótulo, ajuda e erro;
  - interruptores e seletores;
  - tabelas e listas;
  - selos de status;
  - estados de vazio, carregamento e erro.
- **FR-004**: Todas as funcionalidades, validações, confirmações e fluxos existentes de cada página MUST continuar funcionando sem mudança de comportamento.
- **FR-005**: Erros de formulário MUST aparecer junto ao campo quando a validação for local. Erros da API MUST aparecer de forma visível e consistente entre as páginas.
- **FR-006**: Todas as páginas MUST ser utilizáveis em telas de 375 px de largura ou mais, sem rolagem horizontal da página.
- **FR-007**: As páginas MUST atender a contraste mínimo AA, ter foco visível no teclado, rótulos reais em campos e respeitar a preferência de movimento reduzido.
- **FR-008**: Uma proposta visual das páginas MUST ser gerada antes da implementação, como HTML autossuficiente em `docs/`, e servir de referência para a implementação.
- **FR-009**: A mudança MUST se limitar ao frontend administrativo; nenhuma página pública é afetada (landing, chat, widget e página da Abipesca) e nenhuma mudança é feita no backend.
- **FR-010**: Textos visíveis MUST estar em português do Brasil com acentuação correta.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das 12 páginas do escopo usam o cabeçalho de página e os componentes padronizados.
- **SC-002**: 100% dos fluxos principais listados na User Story 2 continuam funcionando após a mudança.
- **SC-003**: Nenhuma página do escopo apresenta rolagem horizontal em 375 px de largura.
- **SC-004**: Um administrador identifica a ação principal de qualquer página em até 5 segundos.
- **SC-005**: A verificação de tipos e o build do frontend passam sem erros, e nenhum erro novo de lint é introduzido.

## Assumptions

- O usuário autorizou explicitamente gerar a proposta e implementar **sem etapa de aprovação intermediária**.
- O padrão de referência é o já implementado: o dashboard, a moldura administrativa, o login e o cadastro, além das propostas em `docs/`.
- Não há novas funcionalidades nem novos endpoints; melhorias de usabilidade são aceitas se não mudarem o comportamento. Exemplos: reorganizar uma página em seções ou mover a ação principal para o cabeçalho.
- Componentes de modal existentes podem ser restilizados mantendo o comportamento.
- O trabalho parte do estado atual não commitado do frontend (login, cadastro, dashboard e moldura novos) e o mantém.
