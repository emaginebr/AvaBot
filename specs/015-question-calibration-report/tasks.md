# Tasks: Relatório de calibração de perguntas

**Input**: Design documents from `specs/015-question-calibration-report/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/agent-test-api.md`, `contracts/calibration-report.md`, `quickstart.md`

**Tests**: incluídos. O `plan.md` define testes de unidade para o rastreamento (`AvaBot.Tests`) e para o gerador do relatório (`AvaBot.Tests.API`, sem rede). Na entrega, o próprio teste de calibração é o teste E2E.

**Organization**: tarefas agrupadas por história de usuário. A fundação, que é o rastreamento no backend e os DTOs, bloqueia todas as histórias.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: história de usuário da tarefa (US1, US2, US3)

---

## Phase 1: Setup

**Purpose**: estrutura de pastas do teste. Solução, projetos e pacotes já existem; não há dependência nova.

- [X] T001 Criar a pasta `AvaBot.Tests.API/Calibration/` e a pasta `scripts/` na raiz, e confirmar que `AvaBot.Tests.API/AvaBot.Tests.API.csproj` compila a pasta nova (glob padrão do SDK) sem alteração de pacotes

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: o teste de agente passa a devolver o rastreamento completo de cada rodada do modelo (FR-012, FR-023) e o resultado parcial em falha (research R5). Todas as histórias dependem disso.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase.

- [X] T002 [P] Criar `ChatCompletionTrace`, `ChatTraceRound`, `ChatTraceMessage` e `ChatTraceToolCall`, com os campos e regras do data-model §1, em `AvaBot.Infra.Interfaces/AppServices/ChatCompletionTrace.cs`. Classes simples, listas inicializadas, `Number` a partir de 1 e `InputTokens`/`OutputTokens` como `int?`
- [X] T003 Acrescentar o parâmetro opcional `ChatCompletionTrace? trace = null` como último argumento de `ChatCompletionAsync` e `ChatCompletionWithToolsAsync`, depois de `cancellationToken`, em `AvaBot.Infra.Interfaces/AppServices/IOpenAIService.cs`. Os métodos de streaming não mudam
- [X] T004 Implementar o registro em `AvaBot.Infra/AppServices/OpenAIService.cs`. Em `ChatCompletionWithToolsAsync`, antes de cada `CompleteChatAsync`:
  - criar uma `ChatTraceRound` com uma cópia do conjunto completo de `chatMessages`, convertida por um helper `ToTraceMessage` (System/User/Assistant com texto ou `ToolCalls`/Tool com `ToolCallId`), e com `ToolsOffered` e `ToolChoiceNone`;
  - cronometrar a chamada e gravar `FinishReason` (string minúscula), `ResponseText`, `ToolCalls` (id, nome, argumentos) e `Usage.InputTokenCount`/`OutputTokenCount` (null se `Usage` for nulo);
  - após cada `ExecuteToolAsync`, gravar `Result` e `DurationMs` no `ChatTraceToolCall` correspondente;
  - em exceção que não seja cancelamento do usuário, preencher `trace.Error` e relançar.

  Em `ChatCompletionAsync`, registrar uma única rodada com `ToolsOffered = false`. Com `trace == null`, nada muda
- [X] T005 [P] Adicionar os DTOs `AgentTestTraceInfo` (`rounds`, `error`), `AgentTestTraceRoundInfo`, `AgentTestTraceMessageInfo` e `AgentTestTraceToolCallInfo`, com `[JsonPropertyName]` camelCase conforme data-model §2. Estender `AgentTestResultInfo` com `chatModel`, `powerBIAvailable`, `powerBIDatasets`, `maxQueryAttempts` (`int?`), `historyOmittedCount` e `trace` (inicializado), sem alterar os campos existentes. Tudo em `AvaBot.DTO/AgentDTOs.cs`
- [X] T006 [P] Expor `public IReadOnlyList<string> DatasetNames` (nomes dos datasets oferecidos ao modelo, na ordem do catálogo) em `PowerBIToolset`, em `AvaBot.Application/Services/PowerBIToolProvider.cs`
- [X] T007 Alterar `ChatService.TestMessageAsync` em `AvaBot.Application/Services/ChatService.cs`:
  - criar um `ChatCompletionTrace` e passá-lo a `ChatCompletionAsync`/`ChatCompletionWithToolsAsync`;
  - mapear trace → `AgentTestTraceInfo` manualmente, num método privado `MapTrace`;
  - preencher `ChatModel`, `PowerBIAvailable`, `PowerBIDatasets` (`toolset.DatasetNames`) e `MaxQueryAttempts` (`toolset?.MaxQueryAttempts`);
  - em exceção do provedor depois de pelo menos uma rodada registrada, lançar `AgentTestFailedException` (nova, no mesmo arquivo ou em `AvaBot.Application/Services/AgentTestFailedException.cs`), carregando o `AgentTestResultInfo` parcial com `AssistantResponse = ""` e `Trace.Error`. Sem rodada registrada, relançar a exceção original
- [X] T008 Em `AvaBot.API/Controllers/AgentController.cs` (`TestQuestion`), capturar `AgentTestFailedException` antes do `catch (Exception)` e responder `StatusCode(500, new Result<AgentTestResultInfo> { Sucesso = false, Mensagem = ex.Message, Dados = ex.PartialResult })`, conforme `contracts/agent-test-api.md`. Os demais caminhos ficam iguais
- [X] T009 [P] Atualizar os mocks de `IOpenAIService` em `AvaBot.Tests/Application/Services/ChatServiceTest.cs` para a nova assinatura (acrescentar `It.IsAny<ChatCompletionTrace?>()` onde há `ChatCompletionAsync`/`ChatCompletionWithToolsAsync`). Acrescentar testes:
  - (a) `TestMessageAsync` repassa um trace e devolve `trace.rounds` mapeado com mensagens, chamadas de ferramenta, resultado e tokens, usando `Callback` que preenche o trace;
  - (b) `PowerBIAvailable`/`PowerBIDatasets`/`MaxQueryAttempts` preenchidos com toolset e vazios sem toolset;
  - (c) exceção depois de uma rodada → `AgentTestFailedException` com parcial e `Trace.Error`; exceção sem rodada → exceção original
- [X] T010 Rodar `dotnet build AvaBot.sln` e `dotnet test AvaBot.Tests` e corrigir quebras nos mocks que usam a assinatura antiga de `IOpenAIService` em qualquer arquivo de `AvaBot.Tests/`

**Checkpoint**: `POST /agents/{id}/test` devolve `trace` completo e o painel continua funcionando (campos antigos intactos).

---

## Phase 3: User Story 1 - Gerar o relatório completo de uma pergunta (Priority: P1) 🎯 MVP

**Goal**: um comando gera no console o relatório markdown de uma pergunta, com o fluxo completo na ordem real: base de conhecimento, BI, prompt de sistema, rodadas com mensagens, decisão, DAX, resultado e tokens, prompt final e resposta.

**Independent Test**: com a API rodando, executar `./scripts/calibrate-questions.ps1 -Agent abipesca -Question "Qual foi o volume de exportação da tilápia em 2024?"` e conferir as seções 1 a 5 de `contracts/calibration-report.md`, com uma subseção por rodada e nada truncado.

### Tests for User Story 1

- [X] T011 [P] [US1] Criar `AvaBot.Tests.API/Calibration/CalibrationReportBuilderTest.cs`. São testes de unidade sem rede, que rodam na suíte padrão, e montam um `CalibrationRun` em memória com 3 rodadas (schema, consulta com erro, resposta final). Verificar:
  - os títulos das seções 1 a 5;
  - a rodada 1 com todas as mensagens e as rodadas 2 e 3 só com o delta;
  - o prompt final completo;
  - a DAX em bloco `dax`;
  - o diagnóstico de erro integral;
  - tokens "não informado" quando `null`;
  - a cerca maior que uma sequência de 4 crases no conteúdo;
  - a redação de `sk-...`, `Bearer xyz` e JWT para `[redacted]`;
  - "Nenhum trecho encontrado." sem resultados de busca;
  - "Ferramentas de BI não disponíveis para este agente." sem BI.

### Implementation for User Story 1

- [X] T012 [P] [US1] Criar `AvaBot.Tests.API/Calibration/CalibrationFactAttribute.cs`. É um `FactAttribute` que, no construtor, define `Skip = "Defina CALIBRATION_AGENT (ou use scripts/calibrate-questions.ps1) para rodar a calibração."` quando `CALIBRATION_AGENT` está vazia e `CALIBRATION_FILE` também está vazia ou o arquivo não tem `agent`
- [X] T013 [P] [US1] Criar `AvaBot.Tests.API/Calibration/CalibrationModels.cs` com:
  - as classes de resposta do lado do teste, espelhando `contracts/agent-test-api.md` (`AgentTestResult`, `AgentTestTrace`, `AgentTestTraceRound`, `AgentTestTraceMessage`, `AgentTestTraceToolCall`, `AgentTestPowerBIQuery`, com propriedades PascalCase desserializadas por case-insensitive);
  - o modelo do relatório: `CalibrationRun`, `CalibrationConversation`, `CalibrationTurn` com `Status` (`Concluida` | `Falhou` | `NaoEnviada`), `Reason`, `ClientDurationMs` e `History`.
- [X] T014 [US1] Criar `AvaBot.Tests.API/Calibration/CalibrationInput.cs`. Ele lê `CALIBRATION_AGENT`, `CALIBRATION_QUESTION` e `CALIBRATION_OUTPUT`; com só a pergunta, gera uma conversa `conversa-1` de uma mensagem. O caminho padrão de saída é `Path.Combine(Path.GetTempPath(), $"avabot-calibration-{DateTime.Now:yyyyMMdd-HHmmss}.md")`. Erros claros para pergunta ausente. O suporte a `CALIBRATION_FILE` fica para a US3 (T027)
- [X] T015 [US1] Criar `AvaBot.Tests.API/Calibration/CalibrationReportBuilder.cs`, que gera o markdown de `contracts/calibration-report.md`. Inclui:
  - cabeçalho com agente, slug, data, API, modelo e contagens;
  - por mensagem: pergunta e resposta;
  - seções de 1. Base de conhecimento, 2. Power BI, 3. Histórico enviado, 4. Prompt de sistema e 5. Rodadas;
  - em cada rodada: título com `finishReason`, duração e tokens; mensagens completas na rodada 1 e delta nas seguintes (data-model §4); decisão do modelo; por chamada, os argumentos (`json`), a DAX (`dax`, quando `consultar_bi`) e o resultado devolvido ao modelo, com status, dataset, duração e linhas obtidos de `powerBIQueries` pela ordem de execução;
  - na última rodada, o "Prompt final enviado ao modelo" completo e a "Resposta da IA";
  - o bloco de erro quando `trace.error`;
  - o turno não enviado com o motivo;
  - a seção final "Instruções para a IA que analisar este relatório" (FR-019).

  Helpers: `Fence(content, lang)` com cerca dinâmica (research R8) e `Redact(text)` com as regex de research R7, aplicada a todo conteúdo antes de escrever. Nenhum truncamento.
- [X] T016 [US1] Criar `AvaBot.Tests.API/Calibration/CalibrationTest.cs`, uma classe que herda `TestBase` com `[Trait("Category", "Calibration")]`, `ITestOutputHelper` e um método `[CalibrationFact] Calibrate()`. O método:
  1. lê `CalibrationInput`;
  2. resolve o agente por `GET agents/{slug}`, falhando com "Agente '<slug>' não encontrado" em 404;
  3. para cada conversa e mensagem, faz `POST agents/{id}/test` com `{ query }` e cronometra no cliente;
  4. em não-200, tenta ler `dados` parcial e marca o turno como `Falhou`, com `mensagem` como motivo;
  5. monta o `CalibrationRun`, gera o markdown, grava em `CALIBRATION_OUTPUT` (UTF-8) e escreve no `ITestOutputHelper` e em `Console.Out`, terminando com a linha `Relatório gravado em: <caminho>`.

  O teste falha só por login, API inacessível, agente inexistente ou quando nenhum turno foi enviado (FR-017).
- [X] T017 [US1] Criar `scripts/calibrate-questions.ps1` com os parâmetros `-Agent`, `-Question`, `-File` e `-Output` (padrão: arquivo temporário com timestamp). O script:
  - define `CALIBRATION_*` no processo;
  - roda `dotnet test AvaBot.Tests.API --filter "Category=Calibration" --nologo --verbosity quiet`;
  - imprime o conteúdo do `.md` com `Get-Content -Raw -Encoding UTF8` e o caminho do arquivo;
  - sai com o código do `dotnet test`;
  - valida que `-Question` ou `-File` foi informado.
- [X] T018 [US1] Rodar `dotnet test AvaBot.Tests.API --filter "FullyQualifiedName~CalibrationReportBuilderTest"` e `dotnet test AvaBot.Tests.API --filter "Category=Calibration"` sem variáveis, para confirmar que a calibração fica Skipped. Corrigir falhas

**Checkpoint**: uma pergunta gera o relatório completo no console e em arquivo.

---

## Phase 4: User Story 2 - Resumo objetivo para orientar a análise (Priority: P2)

**Goal**: cada mensagem ganha a tabela de indicadores e os sinais de atenção objetivos (FR-013, research R9).

**Independent Test**: executar o teste com uma pergunta que gera erro de consulta e conferir no resumo: consultas, erros, limite, tokens e rodada mais cara, tempos e sinais.

### Tests for User Story 2

- [X] T019 [P] [US2] Em `AvaBot.Tests.API/Calibration/CalibrationSignalsTest.cs`, testes de unidade para cada regra de research R9:
  - resposta que pede informação ao usuário (`?` final, "me confirme", "me diga");
  - nenhuma consulta com linhas;
  - limite atingido (`"category":"attempt_limit"` no resultado);
  - DAX repetida com espaços diferentes, indicando as rodadas;
  - consulta com erro, com as rodadas;
  - BI indisponível;
  - contagem de tokens totais e rodada com mais tokens de entrada, ignorando `null`.

### Implementation for User Story 2

- [X] T020 [US2] Criar `AvaBot.Tests.API/Calibration/CalibrationSignals.cs`, que calcula, a partir de um `CalibrationTurn`: rodadas; consultas (total, com erro, repetidas); limite atingido e uso (x/`maxQueryAttempts`); tokens de entrada e saída totais; rodada com mais tokens de entrada; duração do modelo (soma de `round.durationMs`); duração das ferramentas (soma de `toolCall.durationMs`); duração total no cliente; e a lista de sinais com texto em português. Regras conforme research R9
- [X] T021 [US2] Integrar `CalibrationSignals` em `AvaBot.Tests.API/Calibration/CalibrationReportBuilder.cs`: a seção "#### Resumo" (tabela) e "**Sinais de atenção**" logo após pergunta e resposta, com "Nenhum sinal de atenção." quando a lista vier vazia, conforme `contracts/calibration-report.md`
- [X] T022 [US2] Estender `AvaBot.Tests.API/Calibration/CalibrationReportBuilderTest.cs` para verificar a tabela de resumo e os sinais no markdown gerado

**Checkpoint**: o relatório de uma pergunta tem fluxo completo e resumo com sinais.

---

## Phase 5: User Story 3 - Calibrar conversas e várias perguntas numa execução (Priority: P3)

**Goal**: um conjunto de conversas (arquivo JSON), cada uma com uma ou mais mensagens enviadas com histórico (FR-014, FR-015, FR-021), gera um relatório por conversa e a tabela consolidada.

**Independent Test**: executar com `calibration/abipesca.json` do quickstart (duas conversas, a segunda com duas mensagens) e conferir três fluxos, o histórico da mensagem 2 com a pergunta 1 e a resposta do agente, e a tabela "Consolidado".

### Tests for User Story 3

- [X] T023 [P] [US3] Em `AvaBot.Tests/Application/Services/ChatServiceTest.cs`, testes de `TestMessageAsync` com histórico:
  - (a) o histórico entra em `messages`, antes do contexto da base e da pergunta, na mesma forma do chat real;
  - (b) histórico maior que `Chat:MaxHistoryMessages` é cortado nas mais recentes, com `HistoryOmittedCount` correto;
  - (c) o resultado sem histórico fica igual ao atual.

### Implementation for User Story 3

- [X] T024 [US3] Adicionar `history` (`List<AgentTestMessageInfo>?`, `[JsonPropertyName("history")]`) a `AgentTestQuestionInfo` em `AvaBot.DTO/AgentDTOs.cs`
- [X] T025 [US3] Em `AvaBot.Application/Services/ChatService.cs`, mudar a assinatura para `TestMessageAsync(long agentId, string chatModel, string systemPrompt, string userMessage, IReadOnlyList<AgentTestMessageInfo>? history = null)`. O método converte o histórico em `ChatMessage` (`user` → `SenderType.User`, `assistant` → `SenderType.Assistant`), mantém as `_maxHistoryMessages` mais recentes, preenche `HistoryOmittedCount` e passa o histórico ao `BuildMessages` existente
- [X] T026 [US3] Em `AvaBot.API/Controllers/AgentController.cs` (`TestQuestion`), validar `info.History`: um `role` fora de `user`/`assistant` ou um `content` vazio responde `400` com "Historico invalido: item {i} ...". Passar `info.History` ao `TestMessageAsync`
- [X] T027 [US3] Estender `AvaBot.Tests.API/Calibration/CalibrationInput.cs` para ler `CALIBRATION_FILE`, no formato do data-model §3 (`agent` opcional, `conversations[].name` com padrão `conversa-N`, `messages` com ao menos uma string não vazia). `CALIBRATION_AGENT` tem precedência sobre `agent`. Arquivo inexistente ou inválido gera erro claro. Atualizar `CalibrationFactAttribute.cs` para não pular quando o agente vem do arquivo
- [X] T028 [US3] Em `AvaBot.Tests.API/Calibration/CalibrationTest.cs`, manter o histórico por conversa: após cada turno concluído, acrescentar `{user: pergunta}` e `{assistant: assistantResponse}` e enviar `history` no próximo `POST`. Quando um turno falha, marcar os seguintes da conversa como `NaoEnviada`, com o motivo "falha na mensagem N", e seguir para a próxima conversa
- [X] T029 [US3] Em `AvaBot.Tests.API/Calibration/CalibrationReportBuilder.cs`:
  - agrupar por `## Conversa N — <nome>` e `### Mensagem i de n`;
  - na seção "3. Histórico enviado", mostrar o histórico com a nota de omitidas (`historyOmittedCount`);
  - acrescentar a seção "## Consolidado", com uma linha por mensagem: conversa, msg, rodadas, consultas, erros, nº de sinais, tokens, tempo e status, usando `CalibrationSignals`.
- [X] T030 [US3] Criar o exemplo `calibration/abipesca.example.json`, com as duas conversas do quickstart, e estender `CalibrationReportBuilderTest.cs` com um run de duas conversas (uma com turno `NaoEnviada`) para verificar o agrupamento e a tabela consolidada

**Checkpoint**: conversas com histórico e lote de perguntas funcionam; uma falha não interrompe as outras conversas.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T031 [P] Criar `docs/CALIBRATION.md` (skill `doc-manager`), cobrindo:
  - objetivo;
  - pré-requisitos (`AvaBot.Tests.API/appsettings.json`);
  - uso do script, com uma pergunta e com arquivo;
  - formato do JSON;
  - variáveis `CALIBRATION_*`;
  - como ler o relatório (seções, delta de mensagens, sinais);
  - como pedir a análise a uma IA;
  - a ressalva de que as consultas vão para o histórico do Power BI.
- [X] T032 [P] Acrescentar uma linha em `README.md` (seção de testes) apontando para `docs/CALIBRATION.md` e para `scripts/calibrate-questions.ps1`
- [X] T033 Rodar `dotnet build AvaBot.sln`, `dotnet test AvaBot.Tests` e `dotnet test AvaBot.Tests.API --filter "FullyQualifiedName~Calibration&Category!=Calibration"` (testes de unidade do builder e dos sinais). Confirmar que a calibração aparece Skipped sem variáveis
- [X] T034 Executar os cenários de validação de `specs/015-question-calibration-report/quickstart.md` contra uma API em execução, quando houver uma disponível; se não houver, registrar o que não foi executado. Incluir a busca por `sk-`, `Bearer ` e `eyJ` no `.md` gerado (SC-004) e a conferência de que o teste de agente do painel continua igual (FR-022)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup e bloqueia todas as histórias. A ordem interna é T002 → T003 → T004; T005 e T006 em paralelo com T002–T004; T007 depois de T004, T005 e T006; T008 depois de T007; T009 depois de T007; T010 por último.
- **US1 (Phase 3)**: depende da fundação.
- **US2 (Phase 4)**: depende da US1, porque usa `CalibrationTurn` e integra no `CalibrationReportBuilder`.
- **US3 (Phase 5)**: o lado do teste (T027–T030) depende da US1 (teste, input e builder), e o T029 depende do T020 (`CalibrationSignals`, usado na tabela consolidada). O lado backend (T023–T026) só depende da fundação e pode ser feito em paralelo com a US1 ou a US2.
- **Polish (Phase 6)**: depois das histórias desejadas.

### User Story Dependencies

```text
Setup → Foundational (T002–T010)
          ├─ US1 (T011–T018) ──► US2 (T019–T022)
          │                  └─► US3 lado do teste (T027–T030; T029 após T020)
          └─ US3 lado backend (T023–T026)  [paralelo a US1/US2]
US1 + US2 + US3 → Polish (T031–T034)
```

### Within Each User Story

- Testes de unidade do builder e dos sinais podem ser escritos antes da implementação. Devem falhar até a implementação existir.
- Modelos (`CalibrationModels`) antes de `CalibrationInput`, do builder e do teste.
- `CalibrationReportBuilder` antes de `CalibrationTest`, e `CalibrationTest` antes do script.

### Parallel Opportunities

- T002, T005 e T006 (arquivos diferentes na fundação).
- T011, T012 e T013 no início da US1.
- T019 com T020 (teste e implementação de sinais em arquivos diferentes).
- T023–T026 (backend de histórico) em paralelo com toda a US1 e a US2.
- T031 e T032 na fase final.

---

## Parallel Example: Foundational

```text
T002: ChatCompletionTrace em AvaBot.Infra.Interfaces/AppServices/ChatCompletionTrace.cs
T005: DTOs de trace em AvaBot.DTO/AgentDTOs.cs
T006: DatasetNames em AvaBot.Application/Services/PowerBIToolProvider.cs
```

## Parallel Example: User Story 1

```text
T011: CalibrationReportBuilderTest.cs (testes, sem rede)
T012: CalibrationFactAttribute.cs
T013: CalibrationModels.cs
```

## Parallel Example: User Story 3 (backend) com User Story 2

```text
T023–T026: histórico no teste de agente (AvaBot.DTO, ChatService, AgentController, ChatServiceTest)
T019–T022: sinais e resumo (AvaBot.Tests.API/Calibration/CalibrationSignals*.cs, CalibrationReportBuilder.cs)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 e Phase 2: o teste de agente devolve o rastreamento.
2. Phase 3: o relatório de uma pergunta, pelo script.
3. **Parar e validar** com a pergunta da tilápia: o relatório já permite a uma IA apontar onde a resposta desandou.

### Incremental Delivery

1. Fundação + US1: relatório completo (MVP).
2. US2: resumo e sinais, que aceleram a leitura.
3. US3: conversas com histórico e lote de perguntas.
4. Polish: documentação e validação.

---

## Notes

- `[P]` = arquivos diferentes, sem dependência pendente.
- O chat de produção e o streaming não podem mudar: `trace` é sempre opcional (`null` por padrão).
- O painel não muda (FR-022): nenhum arquivo em `frontend/` é tocado.
- Não truncar nada no relatório; a redação de segredos é obrigatória.
- Commit por tarefa ou grupo lógico.

## Status de execução (2026-10-08)

T001–T034 concluídas. A T034 foi concluída só em parte: não havia API em execução nesta sessão (veja abaixo).

| Tarefa | O que ficou |
|---|---|
| T002–T004 | `ChatCompletionTrace` em `AvaBot.Infra.Interfaces`. `ChatCompletionAsync` e `ChatCompletionWithToolsAsync` ganham `trace` opcional, como último parâmetro. O `OpenAIService` registra, por rodada, a cópia das mensagens, `ToolsOffered`/`ToolChoiceNone`, `FinishReason`, o texto, as chamadas (com resultado e duração da ferramenta), os tokens (`Usage`) e a duração; numa exceção grava `Error` e relança. O streaming não mudou. |
| T005–T008 | DTOs `AgentTestTrace*` e campos novos em `AgentTestResultInfo`; `PowerBIToolset.DatasetNames`. `TestMessageAsync` mapeia o trace e lança `AgentTestFailedException` com o parcial quando a falha ocorre depois de uma rodada; o controller devolve 500 com `dados`. |
| T009–T010 | 4 testes novos no `ChatServiceTest` (trace mapeado, sem BI, parcial, falha antes da 1ª rodada). |
| T011–T018 | `AvaBot.Tests.API/Calibration/`: `CalibrationModels`, `CalibrationInput`, `CalibrationFactAttribute` (Skip sem agente), `CalibrationReportBuilder` (cerca dinâmica, redação, delta de mensagens), `CalibrationTest` (timeout de 10 min por mensagem). `scripts/calibrate-questions.ps1` imprime o `.md` puro; quando falha, mostra só a mensagem de erro do teste. |
| T019–T022 | `CalibrationSignals` + resumo e sinais no relatório. |
| T023–T030 | `history` no teste de agente (validação 400, corte por `Chat:MaxHistoryMessages`, `historyOmittedCount`); conversas no teste, com turnos `NaoEnviada` após falha; tabela consolidada; `calibration/abipesca.example.json`. |
| T031–T032 | `docs/CALIBRATION.md` e seção no `README.md`. |

**Verificação**:
- `dotnet build AvaBot.sln`: 0 erros.
- `dotnet test AvaBot.Tests`: **155 aprovados** (149 anteriores + 6 novos).
- `dotnet test AvaBot.Tests.API --filter FullyQualifiedName~Calibration`: **17 aprovados, 1 ignorado** (a calibração, sem variáveis).
- Script testado sem API: sem `-Agent` recusa; com agente e API fora do ar, mostra `FlurlHttpException ... localhost:5000` e sai com 1.
- Um relatório de exemplo, gerado do fixture de 3 rodadas, foi conferido visualmente contra `contracts/calibration-report.md`.

**Não executado**: os cenários do `quickstart.md` contra uma API real, com OpenAI e Power BI (a pergunta da tilápia, uma conversa de 2 mensagens, a busca de segredos num relatório real e a conferência visual do painel). Rodar depois do deploy.
## Revisão 2026-10-08: programa de console em vez de teste via API

A pedido do usuário, a calibração deixou de ser um teste E2E contra a API em execução (`AvaBot.Tests.API` + `scripts/calibrate-questions.ps1`, removidos). Agora é o programa de console **`AvaBot.Calibration`**:

- Ele lê `AvaBot.Calibration/appsettings.json` (local, no `.gitignore`; modelo em `appsettings.Example.json`) e roda **no próprio processo** o mesmo `ChatService.TestMessageAsync`, com `OpenAIService`, `PowerBIClient`, `PowerBIToolProvider` e `PowerBISchemaBuilder` reais.
- **Sem banco:** repositórios em memória (`Local/LocalRepositories.cs`). O agente e os datasets vêm da configuração, o histórico de consultas é descartado e o `PlainSecretProtector` usa as chaves em texto da configuração local.
- **Sem Elasticsearch:** `LocalKnowledgeBase` (`IElasticsearchService`) lê uma pasta (`agent_input/<agente>/docs` por padrão), divide os documentos com `IngestionService.ChunkText` e busca com BM25 sobre tokens em minúsculas, como o analyzer `standard` do índice.
- **Schema:** vem de `Datasets[].SchemaFile` (editável, aceita `userDescription`). Se o arquivo não existir, ou com `--refresh-schema`, é gerado no Power BI e gravado lá; `calibration/schemas/` fica no `.gitignore`.
- **Saída:** o relatório (o mesmo `CalibrationReportBuilder`/`CalibrationSignals`, agora em `AvaBot.Calibration/Report` e sobre os DTOs `AgentTestResultInfo`) sai no stdout e num `.md`; o progresso sai no stderr.
- **Testes:** os de unidade do gerador, dos sinais, da base local, do arquivo de conversas e da linha de comando ficam em `AvaBot.Tests/Calibration`.
- **O que permanece da implementação anterior:** o rastreamento por rodada (`ChatCompletionTrace`), os campos novos de `AgentTestResultInfo`, o `history`/`AgentTestFailedException` no `TestMessageAsync` e a extensão do `POST /agents/{id}/test`.
