# Feature Specification: Relatório de calibração de perguntas

**Feature Branch**: `015-question-calibration-report`  
**Created**: 2026-10-08  
**Status**: Draft  
**Input**: User description: "Crie um teste de API E2E para calibrar as perguntas. O teste deve exibir em formato markdown, podendo ser executado por console, um relatorio completo (pode se basear no teste de agente). Ele deve fornecer todo o fluxo de chamadas daquela pergunta, Pergunta - Resposta -> Base de conhecimento acessada -> Consulta BI -> Scherma -> Prompt enviado para Modelo para montar a consulta -> cada consulta feita -> Novo prompt -> repete o loop até 5 consultar -> Prompt final -> Resposta da IA. Deve gerar o relatorio no console para que uma IA pode executar e analizar o q pode ser melhorado"

## Contexto

Hoje, quando um agente responde mal a uma pergunta que depende de dados do BI, quem faz a calibração precisa juntar as partes à mão. O histórico do Power BI mostra as consultas, e o teste de agente mostra o prompt inicial e a resposta. Nenhum dos dois mostra o que foi enviado ao modelo em cada rodada, nem a ordem em que o modelo decidiu consultar, recebeu o resultado e decidiu de novo. Sem essa sequência, nem uma pessoa nem uma IA conseguem apontar com segurança onde a resposta desandou: na base de conhecimento, no schema, no prompt, na consulta ou na interpretação do resultado.

## Clarifications

### Session 2026-10-08

- Q: O teste calibra só perguntas isoladas ou também conversas com várias mensagens (ex.: o agente pede esclarecimento e o usuário responde)? → A: Cada entrada pode ser uma conversa com uma ou mais mensagens do usuário, enviadas em sequência com o histórico; o relatório mostra o fluxo completo de cada mensagem.
- Q: O rastreamento por rodada fica só para o teste de console ou também aparece no teste de agente do painel? → A: Passa a fazer parte do retorno do teste de agente (restrito a administradores); só o relatório de console o exibe agora, e o painel não muda.
- Q: O relatório mostra consumo de tokens? → A: Sim; tokens de entrada e saída por rodada e total por mensagem, com destaque da rodada mais cara no resumo; sem estimativa de custo em dinheiro.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Gerar o relatório completo de uma pergunta (Priority: P1)

Quem calibra o agente executa, por linha de comando, o teste ponta a ponta com um agente e uma pergunta. O teste roda a pergunta contra a aplicação em execução, pelo mesmo caminho do teste de agente, e escreve no console um relatório em markdown com todo o fluxo, na ordem em que aconteceu:

1. a pergunta e a resposta final, para leitura rápida;
2. o que foi buscado e encontrado na base de conhecimento;
3. se as ferramentas do BI estavam disponíveis e quais datasets;
4. o schema entregue ao modelo;
5. o prompt de sistema e as mensagens enviadas ao modelo na primeira rodada;
6. para cada rodada: o que o modelo decidiu (ferramenta e argumentos, incluindo a consulta DAX), o resultado devolvido (sucesso com linhas, ou erro com o diagnóstico) e o novo conjunto de mensagens enviado na rodada seguinte;
7. o prompt final enviado ao modelo;
8. a resposta da IA.

**Why this priority**: é o objetivo da feature. Com isso, uma IA ou uma pessoa consegue ler o relatório e apontar o que melhorar.

**Independent Test**: com a aplicação rodando e um agente com Power BI configurado, executar o teste para uma pergunta conhecida (por exemplo, "Qual foi o volume de exportação da tilápia em 2024?") e conferir que o relatório traz todas as seções acima, com uma subseção por rodada do modelo, na ordem real.

**Acceptance Scenarios**:

1. **Given** um agente com base de conhecimento e Power BI, **When** o teste é executado com uma pergunta que exige dados do BI, **Then** o console exibe um relatório em markdown com pergunta, resposta, base de conhecimento, schema, o prompt de cada rodada, cada consulta com seu resultado e a resposta final.
2. **Given** uma pergunta em que o modelo faz três consultas, **When** o relatório é gerado, **Then** ele mostra três rodadas de consulta numeradas, cada uma com a mensagem enviada ao modelo antes da decisão, a ferramenta chamada, os argumentos completos e o resultado devolvido, seguidas da rodada que produziu a resposta.
3. **Given** uma consulta que falhou, **When** o relatório é gerado, **Then** a rodada mostra o diagnóstico de erro completo, exatamente como foi devolvido ao modelo, e a rodada seguinte mostra como o modelo reagiu.
4. **Given** uma pergunta que não aciona o BI, **When** o relatório é gerado, **Then** as seções de BI dizem explicitamente que nenhuma consulta foi feita, e o relatório continua mostrando a base de conhecimento, o prompt e a resposta.

---

### User Story 2 - Resumo objetivo para orientar a análise (Priority: P2)

Além do fluxo completo, o relatório traz um resumo com indicadores objetivos da execução: número de rodadas do modelo, consultas ao BI feitas e com erro, consultas repetidas com o mesmo texto, se o limite de tentativas foi atingido, tempo de cada etapa e tempo total. O resumo também lista sinais de atenção detectáveis sem interpretação, como "a resposta final pede informação ao usuário", "nenhuma consulta retornou linhas" ou "o limite de consultas foi atingido".

**Why this priority**: acelera a leitura. A IA que analisa começa pelos sinais e depois vai ao trecho correspondente. Sem o resumo, o relatório da US1 já cumpre o objetivo.

**Independent Test**: executar o teste com uma pergunta que gera erro de consulta e conferir que o resumo mostra a contagem correta de consultas, de erros e os sinais de atenção correspondentes.

**Acceptance Scenarios**:

1. **Given** uma execução com 4 consultas, 2 com erro, **When** o relatório é gerado, **Then** o resumo mostra 4 consultas, 2 erros e o tempo total.
2. **Given** uma execução em que o modelo repete a mesma consulta, **When** o relatório é gerado, **Then** o resumo sinaliza a repetição e indica as rodadas envolvidas.

---

### User Story 3 - Calibrar conversas e várias perguntas numa execução (Priority: P3)

Quem calibra informa um conjunto de conversas. Cada conversa tem uma ou mais mensagens do usuário; uma pergunta simples é uma conversa de uma mensagem. As mensagens de uma conversa são enviadas em sequência, e cada uma leva o histórico das anteriores, incluindo as respostas do agente. Isso permite calibrar o caso em que o agente pede um esclarecimento e o usuário responde. O teste gera, para cada mensagem, o fluxo completo da US1, agrupado por conversa. No fim, uma tabela consolidada mostra, por mensagem, o número de consultas, os erros, os sinais de atenção e o tempo.

**Why this priority**: útil para medir o efeito de uma mudança de prompt ou de schema num conjunto fixo de perguntas e para reproduzir diálogos de esclarecimento, mas o fluxo de uma pergunta por vez já resolve o caso principal.

**Independent Test**: executar o teste com duas conversas, uma com uma mensagem e outra com duas (pergunta e resposta ao esclarecimento), e conferir três fluxos completos agrupados por conversa, seguidos da tabela consolidada.

**Acceptance Scenarios**:

1. **Given** um conjunto com três perguntas simples, **When** o teste é executado, **Then** o console mostra três relatórios completos e uma tabela final com uma linha por pergunta.
2. **Given** uma conversa com duas mensagens ("Qual foi o volume de exportação da tilápia em 2024?" e "Considere todos os produtos de tilápia"), **When** o teste é executado, **Then** o relatório da segunda mensagem mostra, entre as mensagens enviadas ao modelo, a primeira pergunta e a resposta do agente a ela.
3. **Given** um conjunto em que a segunda conversa falha por erro da aplicação, **When** o teste é executado, **Then** o relatório dessa conversa registra a falha e as demais conversas são processadas normalmente.

---

### Edge Cases

- **Agente inexistente ou sem permissão**: o teste encerra com mensagem clara, sem relatório parcial enganoso.
- **Aplicação fora do ar ou credenciais inválidas**: o teste falha logo no início, informando o motivo.
- **Base de conhecimento sem resultados**: a seção informa explicitamente "nenhum trecho encontrado", em vez de ficar vazia.
- **Agente sem Power BI ou com Power BI desligado**: as seções de BI dizem que as ferramentas não estavam disponíveis e por quê, quando o motivo for conhecido.
- **Limite de consultas atingido**: o relatório mostra a última tentativa bloqueada e o diagnóstico de limite devolvido ao modelo.
- **Conteúdo muito longo** (schema extenso, resultado com muitas linhas, prompts grandes): o relatório não trunca prompts, schema, consultas nem diagnósticos, porque a análise depende do texto exato. Resultados de consulta aparecem como o modelo os recebeu, que já são limitados pela aplicação.
- **Conteúdo com marcação markdown ou blocos de código**: textos de prompt, consultas e respostas aparecem em blocos delimitados, de forma que não quebrem a estrutura do relatório.
- **Segredos**: chaves de API, segredos de cliente e tokens nunca aparecem no relatório, nem dentro de prompts ou diagnósticos.
- **Erro do provedor de IA no meio do fluxo**: o relatório mostra as rodadas concluídas e o erro, em vez de descartar tudo.
- **Falha no meio de uma conversa**: as mensagens seguintes da mesma conversa aparecem como "não enviadas" com o motivo, e a conversa seguinte começa com histórico vazio.
- **Histórico longo**: o histórico enviado segue o mesmo limite de mensagens de uma conversa real, e o relatório indica quando mensagens antigas ficaram de fora.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O teste MUST poder ser executado por linha de comando, sem interface gráfica, contra uma instância da aplicação em execução.
- **FR-002**: O teste MUST receber como entrada o agente (identificador público) e a pergunta, ou um conjunto de conversas (FR-014). A URL da aplicação e as credenciais de administrador são obtidas da configuração do projeto de testes de API.
- **FR-003**: O teste MUST executar a pergunta pelo mesmo caminho do teste de agente do painel, com o modelo, o prompt e as ferramentas configurados no agente, sem criar sessão de chat de usuário final. O histórico de uma conversa (FR-021) é mantido pelo próprio teste e enviado a cada mensagem.
- **FR-004**: O relatório MUST ser escrito no console em markdown válido, com títulos por seção e blocos delimitados para prompts, consultas, resultados e respostas.
- **FR-005**: O relatório MUST começar com cabeçalho (agente, modelo, data/hora da execução, pergunta) e com a resposta final.
- **FR-006**: O relatório MUST mostrar a busca na base de conhecimento: o texto pesquisado e cada trecho retornado, na ordem de relevância, ou a indicação de que nada foi encontrado.
- **FR-007**: O relatório MUST indicar se as ferramentas de BI estavam disponíveis e quais datasets foram oferecidos ao modelo.
- **FR-008**: O relatório MUST mostrar o prompt de sistema completo e as mensagens enviadas ao modelo na primeira rodada.
- **FR-009**: Para cada rodada do modelo, o relatório MUST mostrar, na ordem real: as mensagens acrescentadas desde a rodada anterior, a decisão do modelo (ferramentas chamadas com argumentos completos, ou resposta em texto), e para cada ferramenta o resultado exato devolvido ao modelo, com status, duração e número de linhas quando houver.
- **FR-010**: O schema entregue ao modelo MUST aparecer completo na rodada em que foi solicitado.
- **FR-011**: O relatório MUST mostrar o conjunto final de mensagens enviado ao modelo na rodada que produziu a resposta e a resposta da IA.
- **FR-012**: A aplicação MUST disponibilizar ao teste a sequência completa das interações com o modelo durante o teste de agente: cada rodada, as mensagens enviadas, as chamadas de ferramenta e os resultados. Hoje ela devolve só o prompt inicial, a busca, as consultas e a resposta. O rastreamento passa a fazer parte do retorno do teste de agente, sempre, continua restrito a administradores autenticados e mantém os campos atuais, para que o painel siga funcionando sem alteração.
- **FR-013**: O relatório MUST incluir um resumo com: rodadas do modelo, consultas ao BI (total, com erro, repetidas), se o limite de tentativas foi atingido, duração por etapa e total, tokens totais e a rodada com mais tokens de entrada, e sinais de atenção objetivos (resposta pede informação ao usuário, nenhuma consulta com linhas, limite atingido, consulta repetida).
- **FR-014**: O teste MUST aceitar um conjunto de conversas, cada uma com uma ou mais mensagens do usuário, e gerar o fluxo completo de cada mensagem agrupado por conversa, seguido de uma tabela consolidada com uma linha por mensagem.
- **FR-015**: Uma falha numa conversa MUST ficar registrada no relatório daquela conversa, sem interromper as demais. Dentro da conversa, as mensagens seguintes à falha não são enviadas, porque o histórico ficaria incompleto.
- **FR-016**: O relatório MUST omitir segredos (chaves de API, segredos de cliente do Power BI, tokens de acesso), mesmo quando aparecerem dentro de mensagens ou diagnósticos.
- **FR-017**: O teste MUST NOT reprovar por causa da qualidade da resposta, porque serve para calibração. Ele reprova apenas quando não consegue executar: aplicação indisponível, autenticação, agente inexistente.
- **FR-018**: O teste MUST NOT ser executado na suíte padrão de testes; roda apenas quando chamado explicitamente, porque consome o provedor de IA e o Power BI reais.
- **FR-019**: O relatório MUST terminar com uma instrução curta para a IA que for analisá-lo, pedindo que aponte a etapa onde a resposta se degradou e sugira melhorias de prompt, schema, descrições ou base de conhecimento.
- **FR-020**: Quem executa MUST poder, opcionalmente, gravar o mesmo relatório em arquivo, além de exibi-lo no console.
- **FR-021**: Cada mensagem de uma conversa MUST ser enviada ao agente com o histórico das mensagens anteriores da mesma conversa (perguntas do usuário e respostas do agente), na mesma forma em que o histórico entra numa conversa real, e o relatório MUST mostrar esse histórico entre as mensagens enviadas ao modelo.
- **FR-022**: O teste de agente do painel administrativo MUST continuar com a mesma apresentação; exibir o rastreamento no painel fica fora de escopo nesta feature.
- **FR-023**: Para cada rodada do modelo, o relatório MUST mostrar os tokens de entrada e de saída informados pelo provedor de IA; para cada mensagem, o total. Quando o provedor não informar, o relatório diz "não informado". Estimativa de custo em dinheiro fica fora de escopo.

### Key Entities

- **Conversa de calibração**: sequência de uma ou mais mensagens do usuário executadas contra um agente, cada uma com o histórico das anteriores. Tem agente, modelo, data/hora, status e as execuções de suas mensagens, em ordem.
- **Execução de calibração**: uma mensagem do usuário executada dentro de uma conversa. Tem pergunta, histórico enviado, duração, resposta final e status (concluída, falhou com motivo, ou não enviada por falha anterior na conversa).
- **Busca na base de conhecimento**: o texto pesquisado e os trechos retornados, em ordem.
- **Rodada do modelo**: uma ida ao modelo dentro da execução. Tem número, mensagens enviadas (ou acrescentadas desde a rodada anterior), decisão (chamadas de ferramenta ou texto), duração e tokens de entrada e saída.
- **Chamada de ferramenta**: ferramenta, dataset, argumentos completos (consulta DAX), resultado devolvido ao modelo, status, duração, linhas e se houve truncamento.
- **Resumo da execução**: indicadores e sinais de atenção calculados a partir das rodadas e chamadas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Para qualquer pergunta, 100% das idas ao modelo e das chamadas de ferramenta da execução aparecem no relatório, na ordem real, sem trechos truncados de prompt, consulta ou diagnóstico.
- **SC-002**: Quem calibra obtém o relatório de uma pergunta com um único comando, em no máximo o tempo da própria resposta do agente mais 10 segundos.
- **SC-003**: Uma IA que recebe apenas o relatório consegue identificar a etapa em que a resposta falhou, em pelo menos 4 de 5 casos reais conhecidos (como os erros de nome de tabela, de medida usada como coluna e de desistência), sem precisar consultar outra fonte.
- **SC-004**: Nenhum segredo (chave de API, segredo de cliente, token) aparece em relatórios gerados, verificado em execuções com Power BI e base de conhecimento ativos.
- **SC-005**: Um conjunto de 10 conversas gera o relatório de todas as mensagens e a tabela consolidada numa única execução, mesmo que alguma conversa falhe.

## Assumptions

- Quem executa é um desenvolvedor ou administrador com credenciais de administrador da aplicação e acesso a uma instância em execução (local ou homologação). A execução contra produção é possível, mas a decisão é de quem executa.
- O teste reutiliza o projeto de testes de API existente, com sua configuração de URL e login, e é executado de forma explícita (filtro ou categoria própria), fora da execução padrão de testes.
- O ponto de partida é o teste de agente do painel, que já executa busca, ferramentas e modelo sem criar sessão. A feature amplia o que esse teste devolve, em vez de criar um segundo caminho de execução.
- As consultas ao BI feitas durante a calibração entram no histórico do Power BI do agente, como acontece hoje no teste de agente.
- O limite de consultas por pergunta é o já configurado na aplicação (padrão 5). O relatório o reflete, sem alterá-lo.
- Resultados de consulta aparecem como o modelo os recebe, já limitados pela aplicação (padrão 100 linhas).
- O idioma do relatório é português.
- Análise automática da qualidade da resposta, comparação com respostas esperadas e interface gráfica para o relatório ficam fora de escopo.
