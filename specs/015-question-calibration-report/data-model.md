# Data Model: Relatório de calibração de perguntas

Nenhuma tabela nova e nenhuma migração. Os modelos abaixo existem em memória durante a requisição ou como DTO de resposta.

## 1. Rastreamento do loop (Infra.Interfaces)

`AvaBot.Infra.Interfaces/AppServices/ChatCompletionTrace.cs`. O `OpenAIService` preenche esse objeto quando recebe uma instância.

### ChatCompletionTrace
| Campo | Tipo | Regra |
|---|---|---|
| `Rounds` | `List<ChatTraceRound>` | Em ordem; uma entrada por chamada ao modelo |
| `Error` | `string?` | Mensagem da exceção que interrompeu o loop; `null` se concluiu |

### ChatTraceRound
| Campo | Tipo | Regra |
|---|---|---|
| `Number` | `int` | Começa em 1 |
| `Messages` | `List<ChatTraceMessage>` | Conjunto **completo** enviado nesta rodada, incluindo o system prompt |
| `ToolsOffered` | `bool` | `true` se havia ferramentas na chamada |
| `ToolChoiceNone` | `bool` | `true` quando o limite foi atingido e a rodada forçou resposta final |
| `FinishReason` | `string` | `stop`, `tool_calls`, `length`, `content_filter`… |
| `ResponseText` | `string?` | Texto devolvido pelo modelo, se houver |
| `ToolCalls` | `List<ChatTraceToolCall>` | Decisões de ferramenta desta rodada |
| `InputTokens` / `OutputTokens` | `int?` | `Usage` do SDK; `null` se ausente |
| `DurationMs` | `long` | Tempo da chamada ao modelo (sem as ferramentas) |

### ChatTraceMessage
| Campo | Tipo | Regra |
|---|---|---|
| `Role` | `string` | `system`, `user`, `assistant`, `tool` |
| `Content` | `string` | Texto integral; vazio para assistant que só chamou ferramenta |
| `ToolCalls` | `List<ChatTraceToolCall>?` | Em `assistant` com tool calls |
| `ToolCallId` | `string?` | Em `tool` |

### ChatTraceToolCall
| Campo | Tipo | Regra |
|---|---|---|
| `Id` | `string` | Id da chamada no provedor |
| `Name` | `string` | `listar_schema`, `consultar_bi`… |
| `ArgumentsJson` | `string` | Argumentos integrais |
| `Result` | `string?` | Texto exato devolvido ao modelo (preenchido após executar) |
| `DurationMs` | `long?` | Tempo da execução da ferramenta |

**Ciclo de vida**: a rodada é criada antes de chamar o modelo, com as mensagens. Depois da resposta, recebe a decisão e os tokens. Cada ferramenta é executada e o resultado é gravado no `ChatTraceToolCall` da mesma rodada. A rodada seguinte já começa com o assistant e os tool messages entre as suas mensagens. Se houver exceção, o loop grava `Error` e relança a exceção.

**Volume**: o padrão é de até 6 rodadas por pergunta (5 consultas mais o schema, mais a resposta final). As mensagens se repetem de rodada a rodada, por isso o relatório mostra o delta (ver §4). O DTO leva o conjunto completo, para que o relatório não dependa de reconstruir nada.

## 2. DTOs do teste de agente (AvaBot.DTO/AgentDTOs.cs)

### AgentTestQuestionInfo (alterado)
| Campo JSON | Tipo | Regra |
|---|---|---|
| `query` | `string` | Obrigatório (como hoje) |
| `history` | `AgentTestMessageInfo[]?` | **Novo**. Opcional. `role` ∈ {`user`,`assistant`}; mensagens inválidas → 400 |

### AgentTestResultInfo (estendido; campos atuais intactos)
| Campo JSON | Tipo | Regra |
|---|---|---|
| `searchQuery`, `searchResults`, `systemPrompt`, `messages`, `assistantResponse`, `powerBIQueries` | — | Inalterados |
| `chatModel` | `string` | **Novo**. Modelo usado |
| `powerBIAvailable` | `bool` | **Novo**. Havia toolset |
| `powerBIDatasets` | `string[]` | **Novo**. Nomes dos datasets oferecidos |
| `maxQueryAttempts` | `int?` | **Novo**. Orçamento de `consultar_bi` (null sem BI) |
| `historyOmittedCount` | `int` | **Novo**. Mensagens do histórico cortadas por `Chat:MaxHistoryMessages` |
| `trace` | `AgentTestTraceInfo` | **Novo**. Sempre presente |

### AgentTestTraceInfo / AgentTestTraceRoundInfo / AgentTestTraceMessageInfo / AgentTestTraceToolCallInfo
O formato JSON espelha o §1 em camelCase (`rounds`, `error`, `number`, `messages`, `toolsOffered`, `toolChoiceNone`, `finishReason`, `responseText`, `toolCalls`, `inputTokens`, `outputTokens`, `durationMs`, `role`, `content`, `toolCallId`, `id`, `name`, `argumentsJson`, `result`). O mapeamento é manual, de `ChatCompletionTrace` para o DTO, no `ChatService`.

**Segurança**: nenhum campo carrega chave OpenAI nem `ClientSecret`. Os resultados de ferramenta já chegam redigidos pelo `PowerBIToolset`.

## 3. Arquivo de conversas (entrada do teste)

`CALIBRATION_FILE` aponta para um JSON:

```json
{
  "agent": "abipesca",
  "conversations": [
    { "name": "tilapia-2024", "messages": ["Qual foi o volume de exportação da tilápia em 2024?"] },
    { "name": "tilapia-esclarecimento", "messages": [
        "Qual foi o volume de exportação da tilápia em 2024?",
        "Considere todos os produtos de tilápia"
    ]}
  ]
}
```

| Campo | Regra |
|---|---|
| `agent` | Opcional; `CALIBRATION_AGENT` tem precedência |
| `conversations[].name` | Opcional; padrão `conversa-N` |
| `conversations[].messages` | ≥ 1 string não vazia |

Uma pergunta isolada via `CALIBRATION_QUESTION` equivale a uma conversa de uma mensagem.

## 4. Modelo do relatório (AvaBot.Tests.API/Calibration)

- **CalibrationRun**: agente (slug, id, nome), data/hora, lista de `CalibrationConversation`, duração total.
- **CalibrationConversation**: nome, status (`concluida` | `falhou`) e lista de `CalibrationTurn`.
- **CalibrationTurn**: índice, pergunta, histórico enviado, `AgentTestResultInfo` (ou o parcial), status (`concluida` | `falhou` | `nao_enviada`), motivo, duração medida pelo cliente.
- **CalibrationSignals**: calculados por turno (research R9). Contagens: rodadas, consultas, erros, repetidas, limite, tokens totais e a rodada com mais tokens de entrada.

**Delta de mensagens**: a rodada 1 mostra todas as mensagens. A rodada N > 1 mostra só as mensagens além das que a rodada N-1 enviou, que são o assistant com tool calls e os tool results. A rodada final mostra o conjunto completo (FR-011).

**Transições do turno**: `pendente → concluida`, `pendente → falhou`; quando um turno falha, os seguintes da mesma conversa vão para `nao_enviada`.
