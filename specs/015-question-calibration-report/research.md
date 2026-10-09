# Research: Relatório de calibração de perguntas

Decisões técnicas da feature 015. Cada item: decisão, motivo e alternativas descartadas.

## R1. Onde capturar as rodadas do modelo

**Decision**: o loop de ferramentas não-streaming (`OpenAIService.ChatCompletionWithToolsAsync`, e `ChatCompletionAsync` quando o agente não tem ferramentas) recebe um parâmetro opcional `ChatCompletionTrace? trace`. Quando ele não é nulo, cada ida ao modelo é registrada nele: as mensagens enviadas, a decisão, os tokens e a duração. Cada chamada de ferramenta também é registrada, com argumentos, resultado e duração. O tipo `ChatCompletionTrace` fica em `AvaBot.Infra.Interfaces/AppServices`, ao lado de `ChatToolCall`/`ChatToolDefinition`.

**Rationale**: só o loop vê as mensagens reais montadas para o SDK, incluindo `AssistantChatMessage` com tool calls e `ToolChatMessage`. O que o `ChatService` monta antes é só a primeira rodada. Como o parâmetro é opcional e nulo por padrão, o chat de produção e o streaming não mudam. O teste de agente já usa o caminho não-streaming (`TestMessageAsync` → `ChatCompletionWithToolsAsync`).

**Alternatives considered**:
- *Reconstruir as rodadas a partir de `PowerBIToolset.ExecutedQueries`*: perde a ordem real das mensagens, os tokens, as chamadas que não são de BI e o texto exato que o modelo recebeu.
- *Logar as rodadas no `ILogger` e o teste ler o log*: o teste roda contra a API remota e não tem acesso ao log.
- *Instrumentar também o streaming*: o teste de agente não usa streaming. Fica fora do escopo, para não tocar no caminho de produção.

## R2. Tokens por rodada

**Decision**: ler `ChatCompletion.Usage.InputTokenCount` e `OutputTokenCount` de cada resposta do SDK OpenAI 2.10 e gravar os dois na rodada. Quando `Usage` vier nulo, os campos ficam `null` e o relatório escreve "não informado".

**Rationale**: o dado vem em toda resposta não-streaming, sem chamada extra (clarificação Q3).

**Alternatives considered**: estimar com tokenizador local, que traz dependência nova e dá número aproximado; custo em dinheiro, que a Q3 descartou.

## R3. Histórico de conversa no teste de agente

**Decision**: `AgentTestQuestionInfo` ganha `history: [{ role: "user" | "assistant", content }]`, opcional. O `TestMessageAsync` converte o histórico em `ChatMessage` e corta as mais antigas com o mesmo `Chat:MaxHistoryMessages` do chat real (padrão 20). Depois chama o mesmo `BuildMessages`. O resultado informa quantas mensagens ficaram de fora (`historyOmittedCount`).

**Rationale**: atende FR-021 sem criar sessão e sem persistir mensagens. Como o histórico passa pelo mesmo `BuildMessages`, ele entra no modelo exatamente como numa conversa real. O teste mantém o histórico do lado dele: pergunta enviada e `assistantResponse` recebida.

**Alternatives considered**: criar uma `ChatSession` real e usar o fluxo WebSocket, o que poluiria o histórico de sessões, exigiria streaming e faria o rastreamento passar pelo caminho de produção; um endpoint novo de calibração, que duplicaria o caminho do teste de agente (a spec diz para ampliar o existente).

## R4. Rastreamento sempre presente e compatibilidade do painel

**Decision**: `AgentTestResultInfo` ganha `trace` (sempre preenchido) e `historyOmittedCount`. Os campos atuais (`searchQuery`, `searchResults`, `systemPrompt`, `messages`, `assistantResponse`, `powerBIQueries`) continuam iguais. O frontend ignora campos desconhecidos, então o painel não muda (clarificação Q2, FR-022).

**Rationale**: só um caminho de execução, e o endpoint já tem `[Authorize]`.

**Alternatives considered**: parâmetro de "modo calibração" (a Q2 descartou a opção C).

## R5. Falha no meio do fluxo

**Decision**: se o provedor de IA ou o loop lançar exceção depois que o rastreamento começou, o `TestMessageAsync` devolve o resultado parcial com `trace.error` preenchido. O controller responde **HTTP 500** com `Result<AgentTestResultInfo>`, com `sucesso: false`, `mensagem: <erro>` e `dados: <resultado parcial>`. Falhas anteriores ao rastreamento (agente sem chave OpenAI, agente inexistente) seguem como hoje, 500 sem `dados` ou 404.

**Rationale**: o painel trata qualquer não-200 lendo `mensagem`, então continua igual. O teste de calibração lê `dados` e mostra as rodadas que terminaram (edge case "erro do provedor de IA no meio do fluxo").

**Alternatives considered**: responder 200 com erro embutido, o que mudaria o painel (resposta vazia sem aviso); descartar o parcial, o que contraria o edge case.

## R6. Onde fica o teste e como ele é chamado

**Decision**: o teste fica no projeto existente `AvaBot.Tests.API` (xUnit + Flurl, login em `TestBase`), na pasta nova `Calibration/`:
- `CalibrationTest`: um único teste com `[CalibrationFact]`, um atributo derivado de `FactAttribute` que define `Skip` quando `CALIBRATION_AGENT` não está definida. Também tem `[Trait("Category", "Calibration")]`.
- `CalibrationReportBuilder`: monta o markdown a partir das respostas. É uma classe pura e testável.
- Entrada por variáveis de ambiente: `CALIBRATION_AGENT` (slug), `CALIBRATION_QUESTION` (uma pergunta) ou `CALIBRATION_FILE` (arquivo JSON de conversas), e `CALIBRATION_OUTPUT` (arquivo opcional).
- Um script `scripts/calibrate-questions.ps1` define as variáveis, roda `dotnet test --filter Category=Calibration` e imprime o markdown limpo no console.

**Rationale**: o usuário pediu explicitamente um "teste de API E2E", e o projeto `AvaBot.Tests.API` já tem login e configuração de URL. Com o `Skip` condicional, o teste fica fora da suíte padrão (FR-018). O xUnit só mostra a saída do teste com `--logger "console;verbosity=detailed"` e a mistura com o texto do runner. Por isso o relatório é gravado sempre num arquivo (padrão: `calibration-report.md` no diretório temporário), e o script o imprime puro, que é o que uma IA precisa ler. O teste também escreve o markdown no `ITestOutputHelper`.

**Alternatives considered**: um subcomando no `AvaBot.Console`, que daria uma saída de console mais limpa, mas não é um "teste E2E" como pedido e duplicaria a configuração de login; um projeto de testes novo, desnecessário.

## R7. Segredos no relatório

**Decision**: o backend já não devolve segredos no teste de agente. O `ClientSecret` é redigido pelo `PowerBIToolset.Sanitize`, e a chave OpenAI nunca entra em mensagens. Como defesa adicional, o `CalibrationReportBuilder` aplica redação de padrões conhecidos antes de escrever: `sk-[A-Za-z0-9_-]{20,}`, `Bearer\s+[A-Za-z0-9._-]+` e `eyJ[A-Za-z0-9._-]{20,}` (JWT).

**Rationale**: FR-016 e SC-004. Um trecho da base de conhecimento ou uma resposta do Power BI podem conter algo parecido com um token.

**Alternatives considered**: confiar só no backend. Rejeitado, porque o relatório sai do servidor e pode ser colado em outra IA.

## R8. Formato markdown e blocos

**Decision**: todo conteúdo livre (prompts, mensagens, DAX, JSON de resultado, respostas) vai num bloco cercado, com a cerca calculada para ser maior que a maior sequência de crases do conteúdo (mínimo de 3). A DAX vai em bloco `dax` e o JSON em bloco `json`. Nada é truncado (SC-001).

**Rationale**: evita que uma resposta com markdown ou bloco de código quebre a estrutura (edge case da spec).

## R9. Sinais de atenção (FR-013)

**Decision**: são regras determinísticas, calculadas no `CalibrationReportBuilder`:
- **Pede informação ao usuário**: a resposta final termina com "?" ou contém "me confirme", "me diga", "qual ... você", "poderia informar".
- **Nenhuma consulta com linhas**: houve `consultar_bi`, mas nenhuma teve sucesso com `rowCount > 0`.
- **Limite atingido**: algum resultado de ferramenta contém `"category":"attempt_limit"`.
- **Consulta repetida**: dois argumentos `dax` iguais depois de normalizar espaços. Indica as rodadas.
- **Consulta com erro**: lista as rodadas.
- **BI indisponível**: o agente não tinha ferramentas, mas a pergunta foi enviada mesmo assim (informativo).

**Rationale**: são sinais objetivos, sem interpretação (US2). A análise de qualidade fica para a IA que lê o relatório (FR-019).
