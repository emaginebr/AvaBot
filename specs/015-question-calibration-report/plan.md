# Implementation Plan: Relatório de calibração de perguntas

**Branch**: `015-question-calibration-report` | **Date**: 2026-10-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/015-question-calibration-report/spec.md`

## Summary

Um teste E2E no projeto `AvaBot.Tests.API` executa perguntas ou conversas contra o teste de agente (`POST /agents/{id}/test`) e gera um relatório em markdown com o fluxo completo, para uma IA analisar. O fluxo inclui pergunta, base de conhecimento, disponibilidade do BI, prompt de sistema, cada rodada do modelo com as mensagens enviadas, decisão, DAX, resultado ou erro e tokens, o prompt final e a resposta.

O backend precisa de três mudanças:
1. O loop OpenAI não-streaming passa a registrar as rodadas num `ChatCompletionTrace` opcional.
2. O teste de agente aceita `history` e devolve `trace`, junto com metadados de BI e de histórico. Os campos atuais não mudam.
3. Numa falha no meio do fluxo, a API devolve o resultado parcial no corpo do 500.

Um script PowerShell roda o teste filtrado e imprime o relatório limpo no console.

## Technical Context

**Language/Version**: C# / .NET 9.0 (backend e testes); PowerShell 7 (script de execução)  
**Primary Dependencies**: ASP.NET Core 9, OpenAI SDK 2.10 (`ChatCompletion.Usage`), xUnit 2.9 + Flurl.Http 4 (já no `AvaBot.Tests.API`). **Nenhuma dependência nova.**  
**Storage**: N/A. Sem tabelas nem migração; o rastreamento vive só durante a requisição. As consultas BI continuam indo para `avabot_powerbi_query_logs`, como no teste de agente atual.  
**Testing**: xUnit em `AvaBot.Tests` (unidade: loop com rastreamento, `TestMessageAsync` com histórico e parcial) e em `AvaBot.Tests.API` (o próprio teste de calibração, mais testes de unidade do `CalibrationReportBuilder`, que não precisam de rede)  
**Target Platform**: API Linux/Docker existente; teste e script em qualquer máquina com .NET 9 e acesso à API  
**Project Type**: Web service (backend .NET) + projeto de testes de API  
**Performance Goals**: o relatório fica pronto em ≤ tempo da resposta do agente + 10 s (SC-002). O rastreamento só acrescenta cronômetros e listas em memória  
**Constraints**: chat de produção e streaming inalterados; painel inalterado (FR-022); nada truncado (SC-001); nenhum segredo no relatório (FR-016); fora da suíte padrão (FR-018)  
**Scale/Scope**: até ~6 rodadas por mensagem, prompts de dezenas de KB com schema e resultado de 100 linhas; até dezenas de conversas por execução

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I. Skills obrigatórias | As mudanças de DTO e service seguem os padrões da `dotnet-architecture`: DTOs em `AvaBot.DTO` com `[JsonPropertyName]`, mapeamento manual no service, sem entidade nova. Não há frontend. ✅ |
| II. Stack | Nenhuma lib nova; EF Core intocado; sem docker. A constituição cita .NET 8, mas o repositório já está em .NET 9 (desvio preexistente, não introduzido aqui). ✅ |
| III. Case sensitivity | Pastas novas: `AvaBot.Tests.API/Calibration/` e `scripts/`. Sem `Contexts`/`Services` novos. ✅ |
| IV. Convenções de código | PascalCase, `_camelCase`, namespaces file-scoped, `camelCase` em JSON. ✅ |
| V. Banco | Não se aplica (sem schema). ✅ |
| VI. Autenticação | O endpoint segue com `[Authorize]`; o teste faz login com o `TestBase` existente. Os segredos são redigidos no backend e no relatório. ✅ |
| VII. Variáveis de ambiente | As variáveis `CALIBRATION_*` são só do processo de teste; a aplicação não ganha variável nova. ✅ |
| VIII. Tratamento de erros | O controller mantém `try/catch → StatusCode(500, Result.Failure)`; a única extensão é anexar `dados` parciais (research R5). ✅ |

**Resultado**: aprovado, sem violações. A reavaliação depois do design (Fase 1) também passou: os contratos não introduzem dependência, tabela nem mudança de UI.

## Project Structure

### Documentation (this feature)

```text
specs/015-question-calibration-report/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── agent-test-api.md        # POST /agents/{id}/test estendido
│   └── calibration-report.md    # execução (script/env) e layout do markdown
├── checklists/requirements.md
└── tasks.md                     # /speckit.tasks
```

### Source Code (repository root)

```text
AvaBot.Infra.Interfaces/AppServices/
├── ChatCompletionTrace.cs          # NOVO: Trace, Round, Message, ToolCall (data-model §1)
└── IOpenAIService.cs               # ChatCompletionAsync / ChatCompletionWithToolsAsync ganham `ChatCompletionTrace? trace = null`

AvaBot.Infra/AppServices/
└── OpenAIService.cs                # registra rodadas, tokens (Usage), durações, resultados; grava Error e relança

AvaBot.DTO/
└── AgentDTOs.cs                    # AgentTestQuestionInfo.history; AgentTestResultInfo + chatModel, powerBIAvailable,
                                    # powerBIDatasets, maxQueryAttempts, historyOmittedCount, trace; DTOs AgentTestTrace*

AvaBot.Application/Services/
├── ChatService.cs                  # TestMessageAsync(history, trace) → mapeia trace; parcial em falha (exceção com resultado)
└── PowerBIToolProvider.cs          # expor nomes dos datasets do toolset (DatasetNames)

AvaBot.API/Controllers/
└── AgentController.cs              # valida history; 500 com dados parciais (research R5)

AvaBot.Tests/
├── Infra/OpenAIServiceTraceTest.cs # se o SDK permitir fake do ChatClient; senão cobrir via ChatService mockando IOpenAIService
└── Application/Services/ChatServiceTest.cs   # histórico cortado, metadados, parcial

AvaBot.Tests.API/
├── Calibration/
│   ├── CalibrationFactAttribute.cs # Skip sem CALIBRATION_AGENT
│   ├── CalibrationInput.cs         # leitura de env + arquivo JSON (data-model §3)
│   ├── CalibrationModels.cs        # DTOs de resposta do lado do teste + Run/Conversation/Turn
│   ├── CalibrationSignals.cs       # regras R9
│   ├── CalibrationReportBuilder.cs # markdown (contracts/calibration-report.md), cercas R8, redação R7
│   ├── CalibrationTest.cs          # [CalibrationFact][Trait("Category","Calibration")]
│   └── CalibrationReportBuilderTest.cs  # unidade, sem rede (roda na suíte padrão)
└── appsettings.Example.json        # inalterado

scripts/
└── calibrate-questions.ps1         # define env, roda dotnet test filtrado, imprime o .md

docs/
└── CALIBRATION.md                  # como rodar e analisar (via doc-manager)
```

**Structure Decision**: o projeto de testes de API já existente concentra a lógica do teste; o backend ganha só o rastreamento opcional e a extensão do teste de agente. Não há projeto novo. `AvaBot.Console` não é usado (research R6).

## Riscos e mitigação

| Risco | Mitigação |
|---|---|
| A saída do xUnit mistura o texto do runner com o relatório | O relatório é gravado em arquivo e impresso puro pelo script (R6) |
| Mudar a assinatura de `IOpenAIService` quebra mocks em `ChatServiceTest` | Parâmetro opcional no fim, com default `null`; os `It.IsAny` atuais continuam casando ao acrescentar o argumento |
| O payload do teste de agente fica grande (o schema se repete em cada rodada) | Aceito: é uma rota de administrador, chamada sob demanda; o relatório mostra o delta (data-model §4) |
| A resposta 500 com `dados` confunde o painel | O painel lê só `mensagem` em não-200 (verificado em `AgentService.ts`) |
| A calibração em produção grava consultas no histórico do Power BI | Comportamento igual ao teste de agente atual, e está documentado no quickstart |

## Complexity Tracking

Sem violações da constituição.

## Revisão 2026-10-08: programa de console em vez de teste via API

A pedido do usuário, a calibração deixou de ser um teste E2E contra a API em execução (`AvaBot.Tests.API` + `scripts/calibrate-questions.ps1`, removidos). Agora é o programa de console **`AvaBot.Calibration`**:

- Ele lê `AvaBot.Calibration/appsettings.json` (local, no `.gitignore`; modelo em `appsettings.Example.json`) e roda **no próprio processo** o mesmo `ChatService.TestMessageAsync`, com `OpenAIService`, `PowerBIClient`, `PowerBIToolProvider` e `PowerBISchemaBuilder` reais.
- **Sem banco:** repositórios em memória (`Local/LocalRepositories.cs`). O agente e os datasets vêm da configuração, o histórico de consultas é descartado e o `PlainSecretProtector` usa as chaves em texto da configuração local.
- **Sem Elasticsearch:** `LocalKnowledgeBase` (`IElasticsearchService`) lê uma pasta (`agent_input/<agente>/docs` por padrão), divide os documentos com `IngestionService.ChunkText` e busca com BM25 sobre tokens em minúsculas, como o analyzer `standard` do índice.
- **Schema:** vem de `Datasets[].SchemaFile` (editável, aceita `userDescription`). Se o arquivo não existir, ou com `--refresh-schema`, é gerado no Power BI e gravado lá; `calibration/schemas/` fica no `.gitignore`.
- **Saída:** o relatório (o mesmo `CalibrationReportBuilder`/`CalibrationSignals`, agora em `AvaBot.Calibration/Report` e sobre os DTOs `AgentTestResultInfo`) sai no stdout e num `.md`; o progresso sai no stderr.
- **Testes:** os de unidade do gerador, dos sinais, da base local, do arquivo de conversas e da linha de comando ficam em `AvaBot.Tests/Calibration`.
- **O que permanece da implementação anterior:** o rastreamento por rodada (`ChatCompletionTrace`), os campos novos de `AgentTestResultInfo`, o `history`/`AgentTestFailedException` no `TestMessageAsync` e a extensão do `POST /agents/{id}/test`.
