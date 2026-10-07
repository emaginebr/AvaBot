# Feature Specification: Integração do Agente com Power BI

**Feature Branch**: `011-powerbi-agent-tools`  
**Created**: 2026-10-07  
**Status**: Draft  
**Input**: User description: "Integração do agente com Power BI via function calling (tools da OpenAI), sem alterar o fluxo RAG existente. Credenciais do Power BI armazenadas por agente. O agente possui uma flag 'Power BI ativo'; somente quando ativa o agente tenta consultar o Power BI. O usuário pode vincular vários datasets ao agente. O app gera automaticamente o schema de cada dataset e o usuário pode visualizá-lo. Frontend admin: nova área chamada 'Power BI' onde o usuário informa as credenciais, testa conexão, cadastra datasets, gera e visualiza o schema e ativa/desativa a flag. Secret nunca retornado em claro."

## Clarifications

### Session 2026-10-07

- Q: Quem pode receber dados do Power BI pelo agente? → A: Qualquer usuário de qualquer canal do agente; o controle de exposição é feito pela flag "Power BI ativo" e pela escolha dos datasets vinculados.
- Q: Consultas salvas e flag de consulta livre entram nesta versão? → A: Não; ficam para versão futura. Nesta versão o agente sempre formula as consultas a partir do schema.
- Q: Onde ficam os registros das consultas ao Power BI? → A: Gravados no banco e exibidos na área "Power BI" como lista das últimas consultas do agente, com retenção de 30 dias.
- Q: Qual o formato das respostas com dados? → A: Sempre texto, em todos os canais; pode usar listas destacando os principais valores; nunca tabelas.
- Q: Como o agente trata perguntas incompletas sobre dados (ex.: sem período)? → A: Sempre pede ao usuário a informação que falta antes de consultar o Power BI.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configurar Credenciais do Power BI no Agente (Priority: P1)

O administrador acessa a nova área "Power BI" no painel administrativo, com um agente selecionado. Ele informa as credenciais de acesso ao Power BI daquele agente (identificador do tenant, identificador do aplicativo e segredo do aplicativo), salva e clica em "Testar conexão". O sistema valida as credenciais junto ao Power BI e exibe se a conexão foi bem-sucedida ou o motivo da falha. As credenciais ficam armazenadas por agente; agentes diferentes podem usar credenciais diferentes.

**Why this priority**: Sem credenciais válidas nenhuma outra parte da integração funciona. É a base de toda a funcionalidade.

**Independent Test**: Acessar /admin/powerbi com um agente selecionado, preencher as credenciais, salvar, clicar em "Testar conexão" e verificar a mensagem de sucesso. Recarregar a página e confirmar que o segredo aparece mascarado.

**Acceptance Scenarios**:

1. **Given** um agente selecionado sem credenciais do Power BI, **When** o administrador preenche tenant, aplicativo e segredo e salva, **Then** as credenciais são armazenadas para aquele agente e uma confirmação é exibida.
2. **Given** credenciais salvas, **When** o administrador clica em "Testar conexão", **Then** o sistema informa "Conexão bem-sucedida" ou uma mensagem de erro compreensível (ex.: credenciais inválidas, aplicativo sem permissão).
3. **Given** credenciais já salvas, **When** o administrador reabre a área "Power BI", **Then** o segredo é exibido apenas de forma mascarada e nunca é retornado em claro.
4. **Given** credenciais já salvas, **When** o administrador edita outros campos e deixa o segredo em branco, **Then** o segredo anterior é mantido.
5. **Given** nenhum agente selecionado, **When** o administrador acessa "Power BI", **Then** uma mensagem informa que é necessário selecionar um agente.

---

### User Story 2 - Cadastrar Datasets e Gerar o Schema (Priority: P1)

Na área "Power BI", o administrador cadastra um ou mais datasets que o agente poderá consultar, informando o workspace, o dataset, um nome amigável e uma descrição de negócio (ex.: "Vendas: faturamento, pedidos e clientes por região"). Para cada dataset, ele clica em "Gerar schema". O sistema lê a estrutura do dataset diretamente do Power BI (tabelas, colunas com seus tipos e medidas) e a armazena. O administrador pode visualizar o schema gerado, organizado por tabela.

**Why this priority**: O agente só consegue formular consultas corretas se conhecer a estrutura dos dados. A geração automática elimina a necessidade de o usuário descrever o modelo manualmente.

**Independent Test**: Com credenciais válidas, cadastrar um dataset, clicar em "Gerar schema" e verificar que as tabelas, colunas e medidas do dataset aparecem na visualização, junto com a data/hora da última geração.

**Acceptance Scenarios**:

1. **Given** credenciais válidas, **When** o administrador cadastra um dataset com workspace, dataset, nome e descrição, **Then** o dataset aparece na lista de datasets do agente.
2. **Given** um dataset cadastrado, **When** o administrador clica em "Gerar schema", **Then** o sistema obtém do Power BI as tabelas, colunas (com tipo) e medidas e exibe o resultado com a data/hora da geração.
3. **Given** um schema já gerado, **When** o administrador clica novamente em "Gerar schema", **Then** o schema é atualizado com a estrutura atual do dataset.
4. **Given** um dataset inexistente ou sem permissão de acesso, **When** o administrador clica em "Gerar schema", **Then** uma mensagem de erro clara é exibida e o schema anterior (se houver) é preservado.
5. **Given** um dataset cadastrado, **When** o administrador o edita ou remove, **Then** a alteração é refletida imediatamente na lista e nas consultas futuras do agente.
6. **Given** um agente com vários datasets, **When** o administrador visualiza a área "Power BI", **Then** cada dataset mostra seu nome, descrição e status do schema (não gerado / gerado em data X / erro na última geração).

---

### User Story 3 - Ativar o Power BI no Agente e Responder com Dados (Priority: P1)

O administrador ativa a flag "Power BI ativo" do agente. A partir daí, quando um usuário final faz uma pergunta que exige dados (ex.: "Qual foi o faturamento de setembro na região Sul?"), o agente decide consultar o dataset adequado, obtém os números do Power BI e responde com base neles. Perguntas que não exigem dados continuam sendo respondidas pela base de conhecimento, como hoje. Com a flag desativada, o agente se comporta exatamente como antes desta funcionalidade e nunca tenta acessar o Power BI.

**Why this priority**: É o valor final da funcionalidade para o usuário final: obter dados reais de negócio pela conversa com o agente.

**Independent Test**: Ativar a flag em um agente com um dataset com schema gerado, perguntar pelo chat algo cuja resposta está no dataset e comparar o valor respondido com o valor exibido no relatório do Power BI. Desativar a flag, repetir a pergunta e confirmar que o agente responde apenas com a base de conhecimento.

**Acceptance Scenarios**:

1. **Given** um agente com a flag "Power BI ativo" ligada e um dataset com schema gerado, **When** o usuário pergunta algo respondível pelo dataset, **Then** o agente consulta o Power BI e responde com os valores retornados.
2. **Given** um agente com a flag desligada, **When** o usuário faz qualquer pergunta, **Then** o agente responde apenas com a base de conhecimento, sem nenhuma tentativa de acesso ao Power BI.
3. **Given** um agente com a flag ligada, **When** o usuário faz uma pergunta que não envolve dados (ex.: sobre um procedimento documentado), **Then** o agente responde com a base de conhecimento normalmente, sem consultar o Power BI.
4. **Given** um agente com vários datasets, **When** o usuário pergunta sobre um assunto coberto por um deles, **Then** o agente escolhe o dataset adequado com base no nome e na descrição cadastrados.
5. **Given** um agente com a flag ligada, **When** a consulta ao Power BI falha (indisponibilidade, credencial expirada, consulta inválida), **Then** o agente informa ao usuário que não conseguiu obter os dados no momento, sem inventar valores, e a conversa continua funcionando.
6. **Given** um agente com a flag ligada, **When** o usuário conversa pelo chat web, Telegram ou WhatsApp, **Then** o comportamento é o mesmo em todos os canais.
7. **Given** um agente sem credenciais salvas ou sem nenhum dataset com schema gerado, **When** o administrador tenta ativar a flag, **Then** o sistema impede a ativação e explica o que falta configurar.

---

### User Story 4 - Complementar o Schema com Descrições de Negócio (Priority: P2)

Ao visualizar o schema, o administrador pode adicionar descrições a tabelas, colunas e medidas (ex.: "Receita Líquida já desconta devoluções"; "use a coluna DataEmissao para filtros por período"). Essas descrições ajudam o agente a escolher os campos corretos. Ao gerar o schema novamente, as descrições já cadastradas são preservadas para os itens que continuam existindo.

**Why this priority**: Melhora significativamente a precisão das respostas, mas o agente já funciona sem essas descrições.

**Independent Test**: Adicionar uma descrição a uma medida, gerar o schema novamente e confirmar que a descrição foi mantida.

**Acceptance Scenarios**:

1. **Given** um schema gerado, **When** o administrador adiciona uma descrição a uma tabela, coluna ou medida e salva, **Then** a descrição é exibida na visualização do schema.
2. **Given** itens com descrições, **When** o schema é gerado novamente, **Then** as descrições dos itens que continuam existindo são mantidas e os itens removidos do dataset deixam de aparecer.

---

### Edge Cases

- O aplicativo autentica e lista os datasets, mas não tem permissão para consultá-los (ex.: papel Viewer sem permissão Build): o "Testar conexão" deve distinguir "credenciais válidas" de "sem permissão de consulta no dataset" e indicar a correção.
- A consulta retorna um volume muito grande de linhas: o resultado entregue ao agente é limitado a um número máximo de linhas e o agente é informado de que o resultado foi truncado.
- O agente tenta várias consultas seguidas sem chegar a uma resposta: há um limite de consultas por mensagem do usuário. Ao atingi-lo, o agente responde com o que tem ou informa que não conseguiu obter os dados.
- A consulta formulada pelo agente é inválida: o erro é devolvido ao agente, que pode corrigir a consulta dentro do limite de tentativas.
- As credenciais expiram ou são revogadas depois da configuração: as consultas falham com mensagem amigável ao usuário final, e o erro fica registrado nos logs. O "Testar conexão" passa a indicar falha.
- O dataset muda de estrutura (tabela/coluna renomeada) sem que o schema seja gerado de novo: consultas podem falhar. O administrador vê a data da última geração e pode gerar novamente.
- A flag está ativa, mas todos os datasets foram removidos: o agente deixa de ter acesso ao Power BI e responde só com a base de conhecimento.
- O Power BI limita a taxa de requisições: o usuário recebe mensagem de indisponibilidade temporária, sem travar a conversa.
- O agente é excluído: suas credenciais, datasets e schemas são removidos junto.
- O usuário faz uma pergunta sobre dados sem informar o período ou outro filtro necessário: o agente pergunta o que falta antes de consultar; a consulta só é feita após a resposta do usuário, considerando o histórico da conversa.
- Duas abas do admin geram o schema do mesmo dataset ao mesmo tempo: prevalece o último resultado concluído, sem corromper os dados.

## Requirements *(mandatory)*

### Functional Requirements

**Configuração e credenciais**

- **FR-001**: O sistema MUST permitir armazenar, por agente, as credenciais de acesso ao Power BI: identificador do tenant, identificador do aplicativo e segredo do aplicativo.
- **FR-002**: O sistema MUST armazenar o segredo de forma protegida e NUNCA retorná-lo em claro em nenhuma resposta, exibindo apenas uma indicação mascarada de que ele está configurado.
- **FR-003**: O sistema MUST manter o segredo existente quando o administrador atualizar as credenciais sem informar um novo segredo.
- **FR-004**: O sistema MUST oferecer uma ação "Testar conexão" que valida as credenciais junto ao Power BI e retorna sucesso ou uma mensagem de erro compreensível.
- **FR-005**: O agente MUST possuir uma flag "Power BI ativo", desativada por padrão para agentes novos e existentes.
- **FR-006**: O sistema MUST impedir a ativação da flag quando o agente não tiver credenciais salvas ou nenhum dataset com schema gerado, informando o que falta.

**Datasets e schema**

- **FR-007**: O sistema MUST permitir cadastrar, editar e remover vários datasets por agente, cada um com workspace, dataset, nome amigável e descrição de negócio.
- **FR-008**: O sistema MUST gerar automaticamente o schema de um dataset sob demanda ("Gerar schema"), obtendo do próprio Power BI as tabelas, colunas (com tipo de dado) e medidas.
- **FR-009**: O sistema MUST armazenar o schema gerado com a data/hora da geração e reutilizá-lo nas conversas, sem consultar a estrutura no Power BI a cada mensagem.
- **FR-010**: O sistema MUST preservar o schema anterior quando uma nova geração falhar e registrar o erro da última tentativa.
- **FR-011**: O administrador MUST poder visualizar o schema gerado de cada dataset, organizado por tabela, com colunas, tipos e medidas.
- **FR-012**: O administrador MUST poder adicionar descrições a tabelas, colunas e medidas do schema. Essas descrições MUST ser preservadas em novas gerações para os itens que continuam existindo.
- **FR-013**: O sistema MUST omitir do schema exibido ao agente as tabelas e colunas marcadas como ocultas no próprio dataset.

**Uso pelo agente na conversa**

- **FR-014**: Quando a flag "Power BI ativo" estiver desligada, o processamento das mensagens MUST ser idêntico ao atual (somente base de conhecimento), sem nenhuma chamada ao Power BI.
- **FR-015**: Quando a flag estiver ligada, o agente MUST continuar usando a base de conhecimento como hoje e, adicionalmente, poder decidir por conta própria consultar o Power BI quando a pergunta exigir dados.
- **FR-016**: As capacidades de consulta oferecidas ao agente MUST ser montadas dinamicamente a partir da configuração do agente (datasets e schemas), sem necessidade de desenvolvimento específico para cada dataset.
- **FR-017**: O agente MUST conseguir: (a) obter o schema de um dos datasets vinculados; (b) executar uma consulta, formulada por ele a partir do schema, em um dos datasets vinculados.
- **FR-018**: O agente MUST conseguir consultar somente os datasets vinculados ao próprio agente.
- **FR-019**: As consultas executadas no Power BI MUST ser somente leitura.
- **FR-020**: O resultado de cada consulta entregue ao agente MUST ser limitado a um número máximo de linhas configurável (padrão: 100), indicando quando houve truncamento.
- **FR-021**: O sistema MUST limitar o número de consultas ao Power BI por mensagem do usuário (padrão: 5) e cada consulta a um tempo máximo de execução (padrão: 30 segundos).
- **FR-022**: O agente MUST ser instruído a usar apenas os valores retornados pelas consultas e a nunca inventar números. Em caso de falha, MUST informar ao usuário que não conseguiu obter os dados.
- **FR-022a**: Respostas com dados do Power BI MUST ser sempre em texto, em todos os canais. O agente pode usar listas e MUST destacar os principais valores. Tabelas MUST NOT ser usadas. Quando o resultado tiver muitas linhas, o agente MUST resumir (ex.: totais, maiores e menores valores) em vez de listar tudo.
- **FR-022b**: Quando a pergunta do usuário não tiver informação necessária para a consulta (ex.: período, produto, indicador), o agente MUST perguntar ao usuário o que falta antes de consultar o Power BI e MUST NOT assumir valores padrão.
- **FR-023**: Falhas na consulta ao Power BI MUST NOT interromper a conversa nem impedir a resposta baseada na base de conhecimento.
- **FR-024**: O comportamento MUST ser o mesmo no chat web, no Telegram e no WhatsApp. Qualquer usuário final do agente, identificado ou anônimo, pode receber dados dos datasets vinculados. Não há restrição por usuário nem por canal nesta versão.
- **FR-024a**: A área "Power BI" MUST exibir, ao ativar a flag, um aviso de que os dados dos datasets vinculados ficarão acessíveis a qualquer usuário que converse com o agente, em todos os canais.

**Observabilidade**

- **FR-027**: O sistema MUST registrar em log cada consulta ao Power BI feita durante a conversa: agente, sessão, dataset, consulta executada, duração, quantidade de linhas e erro (se houver). O segredo MUST NOT aparecer nos logs.
- **FR-027a**: Os registros de consulta MUST ser persistidos e a área "Power BI" MUST exibir as consultas mais recentes do agente (data/hora, pergunta do usuário, dataset, consulta executada, duração, quantidade de linhas, status e erro), da mais recente para a mais antiga, com paginação.
- **FR-027b**: Os registros de consulta MUST ser retidos por 30 dias e removidos automaticamente após esse prazo.
- **FR-028**: A funcionalidade de teste de agente já existente no admin SHOULD exibir as consultas ao Power BI executadas e seus resultados, para apoiar o ajuste da configuração.

**Interface administrativa**

- **FR-029**: O painel administrativo MUST ter uma nova área chamada "Power BI" no menu lateral, seguindo o padrão das áreas "Bot Telegram" e "WhatsApp", que opera sobre o agente selecionado.
- **FR-030**: A área "Power BI" MUST permitir: informar credenciais, testar conexão, ativar/desativar a flag do agente, gerenciar datasets, gerar e visualizar schema, editar descrições do schema e visualizar o histórico de consultas.
- **FR-031**: Somente administradores autenticados MUST poder acessar e alterar as configurações do Power BI.

### Key Entities

- **Configuração Power BI do Agente**: credenciais de acesso de um agente (tenant, aplicativo, segredo protegido) e a flag "Power BI ativo". Relação 1:1 com Agente.
- **Dataset Power BI**: dataset vinculado a um agente. Contém workspace, dataset, nome amigável, descrição de negócio, data/hora e status da última geração de schema. Relação N:1 com Agente.
- **Schema do Dataset**: estrutura gerada do dataset, com tabelas, colunas (nome, tipo) e medidas, mais as descrições complementares do administrador. Pertence a um Dataset.
- **Registro de Consulta**: registro persistido de cada consulta executada na conversa (agente, sessão, pergunta do usuário, dataset, consulta, duração, linhas, status, erro), retido por 30 dias.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um administrador consegue configurar credenciais, cadastrar um dataset, gerar o schema e ativar o Power BI em um agente em menos de 10 minutos, sem apoio técnico.
- **SC-002**: A geração de schema de um dataset com até 50 tabelas é concluída em menos de 30 segundos.
- **SC-003**: Em um conjunto de perguntas de teste cobertas pelos datasets, pelo menos 85% das respostas trazem valores iguais aos exibidos nos relatórios do Power BI.
- **SC-004**: Em 100% dos casos de falha na consulta, o agente informa a indisponibilidade dos dados e não apresenta valores inventados.
- **SC-005**: Agentes com a flag desligada não apresentam nenhuma mudança de comportamento nem de tempo de resposta em relação à versão anterior.
- **SC-006**: Perguntas que exigem consulta ao Power BI são respondidas em até 20 segundos em 90% dos casos.
- **SC-007**: O segredo das credenciais não aparece em claro em nenhuma resposta do sistema nem nos logs.

## Assumptions

- O acesso ao Power BI é feito com um aplicativo registrado no Microsoft Entra ID (service principal) usando o fluxo de credenciais do aplicativo. A organização do cliente é responsável por habilitar nas configurações do tenant o uso de APIs por service principals e a execução de consultas em datasets, além de dar ao aplicativo acesso ao workspace.
- Para executar consultas, o aplicativo precisa de permissão **Build** em cada dataset, ou de papel **Contributor** ou superior no workspace. O papel **Viewer** permite listar workspaces e datasets, mas não consultá-los. Isso foi verificado em teste em 2026-10-07: um aplicativo com papel Viewer listou os datasets, mas recebeu `PowerBIEntityNotFound` ao consultar. As mensagens de "Testar conexão" e "Gerar schema" MUST orientar o administrador sobre essa permissão quando o erro ocorrer.
- A geração de schema usa as funções de metadados do próprio dataset (DAX `INFO.VIEW.*`). Caso o ambiente do cliente não permita essas funções, a geração falha com mensagem explicativa. Edição manual completa do schema fica fora do escopo desta versão.
- As consultas ao Power BI usam a linguagem de consulta nativa dos datasets (DAX).
- Segurança em nível de linha (RLS) por usuário final está fora do escopo desta versão: todas as consultas usam a identidade do aplicativo configurado no agente.
- A decisão de quando consultar o Power BI é tomada pelo modelo de IA já usado pelo agente, com o recurso de chamada de ferramentas. Não haverá um classificador de intenção separado.
- A base de conhecimento (busca atual) continua sendo consultada em todas as mensagens, como hoje.
- Os limites padrão (100 linhas por resultado, 5 consultas por mensagem, 30 segundos por consulta) são configuráveis globalmente na aplicação.
- O agente selecionado no admin segue o mesmo mecanismo de seleção já usado pelas áreas "Bot Telegram" e "WhatsApp".
- A autenticação do painel administrativo existente é reutilizada.
- Fora de escopo nesta versão: consultas salvas (templates de consulta parametrizados) e o bloqueio de consulta livre por dataset. Ficam para uma versão futura, com base nos logs de consultas reais.
