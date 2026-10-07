# Implementation Plan: Integração do Agente com Power BI

**Branch**: `011-powerbi-agent-tools` | **Date**: 2026-10-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/011-powerbi-agent-tools/spec.md`

## Summary

Permitir que o agente responda com dados reais do Power BI sem alterar o fluxo RAG existente. Cada agente guarda as próprias credenciais de service principal (com o segredo criptografado) e uma lista de datasets, cujo schema é gerado automaticamente por consultas DAX `INFO.VIEW.*` e armazenado em `jsonb`.

Quando a flag `PowerBIEnabled` do agente está ligada, o `ChatService` envia ao modelo duas tools dinâmicas, `listar_schema` e `consultar_bi`, e usa um novo loop de *function calling* com streaming no `OpenAIService`. Com a flag desligada, o código executado é exatamente o atual.

O admin ganha a área **Power BI**, com credenciais e teste de conexão, flag com aviso, datasets, visualização e descrição do schema, e histórico de consultas com retenção de 30 dias.

## Technical Context

**Language/Version**: C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend)
**Primary Dependencies**: ASP.NET Core 9, EF Core 9 + Npgsql, OpenAI SDK 2.10 (já instalado; usa `ChatTool`/`ToolChatMessage`), `HttpClient` nativo (Entra ID + Power BI REST), `IMemoryCache` (cache de token), `System.Security.Cryptography.AesGcm`, AutoMapper; frontend com React Router 7, Zustand 5, Tailwind 4 e sonner 2. **Nenhuma dependência nova.**
**Storage**: PostgreSQL. Uma coluna nova em `avabot_agents` e três tabelas novas, com o schema em `jsonb`. O Elasticsearch não é afetado.
**Testing**: xUnit + Moq (`AvaBot.Tests`), `npm run lint` (frontend)
**Target Platform**: Linux container (Docker), browser moderno para o admin
**Project Type**: Web application (backend Clean Architecture + SPA React)
**Performance Goals**: 90% das perguntas com dados respondidas em até 20 s (SC-006); geração de schema de até 50 tabelas em menos de 30 s (SC-002); nenhuma latência extra para agentes com a flag desligada (SC-005)
**Constraints**: até 100 linhas por resultado, até 5 tool calls por mensagem, 30 s por consulta (configuráveis); segredo nunca exposto em respostas ou logs; limites da API do Power BI (120 req/min por principal, 100k linhas)
**Scale/Scope**: dezenas de agentes, poucos datasets por agente (1 a 10), schemas de até cerca de 50 tabelas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

A constituição (v1.0.0) está desatualizada em relação à stack real: cita .NET 8, React 18, Bootstrap e proíbe Zustand. Seguindo o precedente das specs 009 e 010, os gates são avaliados contra a stack real documentada em `CLAUDE.md` (ver research R11).

| Princípio | Status | Notas |
|---|---|---|
| I. Skills obrigatórias | PASS | Entidades backend (`AgentPowerBIConfig`, `PowerBIDataset`, `PowerBIQueryLog`) via skill `dotnet-architecture`. No frontend, um novo `PowerBIService` + `types/powerbi.ts`; o estado da página é local, como em `TelegramBotPage` e `WhatsappPage` (sem store global novo). |
| II. Stack | PASS | Nenhum pacote novo. EF Core continua sendo o único ORM. |
| III. Case sensitivity | PASS | `Services/PowerBIService.ts`, `types/powerbi.ts`, `pages/admin/PowerBIPage.tsx`, `components/admin/powerbi/` |
| IV. Convenções de código | PASS | `[JsonPropertyName]` camelCase nos DTOs; `interface` no TS; arrow functions |
| V. Banco de dados | PASS | snake_case, PK `{entidade}_id` identity, FKs `ClientSetNull` com remoção explícita no `AgentRepository.DeleteAsync`, timestamps sem timezone, `varchar` com tamanho. `jsonb` para o schema está justificado em R6 (não é proibido). |
| VI. Autenticação e segurança | PASS | `[Authorize]` em todo o `PowerBIController`; segredo criptografado (AES-GCM) e nunca retornado; chave por variável de ambiente |
| VII. Variáveis de ambiente | PASS | Nova `PowerBI__SecretEncryptionKey` (env `POWERBI_SECRET_ENCRYPTION_KEY`) |
| VIII. Tratamento de erros | PASS | Controllers com `try/catch` e o envelope `Result<T>` existente; falhas de tool viram `{error}` para o modelo e não quebram a conversa |

**Gate Result (pré-pesquisa)**: PASS.
**Re-check pós-design**: PASS. O design não introduziu projeto, ORM nem biblioteca nova.

## Arquitetura do fluxo de chat

```
ChatService.ProcessMessageAsync(agentId, sessionId, ...)
  ├─ SearchService.SearchAsync (Elastic)                 ← inalterado
  ├─ BuildFullSystemPrompt / BuildMessages               ← inalterado
  ├─ toolset = PowerBIToolProvider.GetToolsetAsync(agent)  → null se flag off ou sem dataset utilizável
  ├─ toolset == null → OpenAIService.StreamChatCompletionAsync (caminho atual, idêntico)
  └─ toolset != null → prompt += bloco "DADOS DO POWER BI"
                       OpenAIService.StreamChatCompletionWithToolsAsync(model, prompt, messages,
                            toolset.Definitions, toolset.ExecuteAsync, maxToolCalls, ct)
                         loop: stream → (tool calls?) → executor → ToolChatMessage → stream ...
```

- `IOpenAIService` ganha `StreamChatCompletionWithToolsAsync(...)` e `ChatCompletionWithToolsAsync(...)` (este para a página de teste). Os tipos ficam neutros em `Infra.Interfaces`: `ChatToolDefinition { Name, Description, ParametersJsonSchema }`, `ChatToolCall { Id, Name, ArgumentsJson }` e um executor `Func<ChatToolCall, CancellationToken, Task<string>>`. Nenhum tipo da OpenAI vaza para a camada Application.
- `PowerBIToolProvider` (Application) monta as definições a partir dos datasets do agente e executa: valida o dataset, lê o schema salvo ou chama `IPowerBIClient.ExecuteQueryAsync`, trunca, serializa e grava o `PowerBIQueryLog` (com `sessionId` e `userQuestion` do contexto).
- `TelegramService`, `WhatsappService` e `ChatWebSocketHandler` não mudam, porque todos já chamam `ChatService.ProcessMessageAsync` (FR-024).
- `TestMessageAsync` usa `ChatCompletionWithToolsAsync` e devolve `powerBIQueries` (FR-028).

## Project Structure

### Documentation (this feature)

```text
specs/011-powerbi-agent-tools/
├── plan.md              # Este arquivo
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── powerbi-api.md   # Endpoints REST do admin
│   └── llm-tools.md     # Tools enviadas ao modelo + bloco de prompt
├── checklists/requirements.md
└── tasks.md             # Phase 2 (/speckit.tasks)
```

### Source Code (repository root)

```text
AvaBot.Domain/
├── Models/
│   ├── Agent.cs                          # + PowerBIEnabled, navegações
│   ├── AgentPowerBIConfig.cs             # novo
│   ├── PowerBIDataset.cs                 # novo
│   ├── PowerBIQueryLog.cs                # novo
│   └── PowerBISchema.cs                  # novo (POCO do SchemaJson + Merge)
└── Enums/
    ├── PowerBISchemaStatus.cs            # novo
    └── PowerBIQueryStatus.cs             # novo

AvaBot.DTO/
├── AgentDTOs.cs                          # + PowerBIEnabled em AgentInfo; + PowerBIQueries em AgentTestResultInfo
└── PowerBIDTOs.cs                        # novo (Config, Test, Workspace, Dataset, Schema, QueryLog)

AvaBot.Infra.Interfaces/
├── AppServices/
│   ├── IOpenAIService.cs                 # + métodos com tools e tipos neutros
│   ├── IPowerBIClient.cs                 # novo: GetTokenAsync, ListWorkspacesAsync, ExecuteQueryAsync
│   └── ISecretProtector.cs               # novo: Protect / Unprotect
└── Repository/
    ├── IAgentPowerBIConfigRepository.cs  # novo
    ├── IPowerBIDatasetRepository.cs      # novo
    └── IPowerBIQueryLogRepository.cs     # novo (inclui DeleteOlderThanAsync, GetPagedByAgentAsync)

AvaBot.Infra/
├── AppServices/
│   ├── OpenAIService.cs                  # + loop de function calling (streaming e não streaming)
│   ├── PowerBIClient.cs                  # novo (HttpClient "PowerBI" e "EntraId" + IMemoryCache)
│   └── AesGcmSecretProtector.cs          # novo
├── Context/AvaBotContext.cs              # + 3 entidades + coluna powerbi_enabled
├── Repository/                           # + 3 repositórios; AgentRepository.DeleteAsync remove filhos Power BI
└── Migrations/<timestamp>_AddPowerBIIntegration.cs

AvaBot.Application/
├── Services/
│   ├── ChatService.cs                    # bifurcação por toolset; TestMessageAsync com tools
│   ├── PowerBIService.cs                 # novo: config, teste, flag, datasets, schema, descrições, logs
│   ├── PowerBIToolProvider.cs            # novo: definições + executor das tools
│   ├── PowerBISchemaBuilder.cs           # novo: INFO.VIEW.* → PowerBISchema (fallback COLUMNSTATISTICS)
│   └── PowerBIQueryLogCleanupService.cs  # novo: BackgroundService (retenção 30 dias)
├── Profiles/PowerBIProfile.cs            # novo (AutoMapper)
└── DependencyInjection.cs                # registros + HttpClients + MemoryCache + HostedService

AvaBot.API/
├── Controllers/PowerBIController.cs      # novo (contracts/powerbi-api.md)
├── Validators/                           # PowerBIConfigUpdateInfoValidator, PowerBIDatasetInsertInfoValidator
└── appsettings*.json                     # seção PowerBI

AvaBot.Tests/Application/Services/
├── PowerBIToolProviderTest.cs
├── PowerBIServiceTest.cs
├── PowerBISchemaBuilderTest.cs           # parsing + merge de descrições
└── ChatServiceTest.cs                    # + flag off ⇒ caminho sem tools; flag on ⇒ tools
AvaBot.Tests/Infra/AesGcmSecretProtectorTest.cs

frontend/src/
├── types/powerbi.ts                      # novo
├── types/agent.ts                        # + powerBIEnabled, powerBIQueries
├── Services/PowerBIService.ts            # novo
├── pages/admin/PowerBIPage.tsx           # novo: abas Credenciais | Datasets | Histórico
├── components/admin/powerbi/
│   ├── PowerBICredentialsForm.tsx        # credenciais, testar conexão, resultado por passo
│   ├── PowerBIEnableToggle.tsx           # flag + diálogo de aviso (FR-024a)
│   ├── PowerBIDatasetList.tsx            # lista, status do schema, gerar schema
│   ├── PowerBIDatasetForm.tsx            # selects de workspace/dataset via /workspaces
│   ├── PowerBISchemaViewer.tsx           # tabelas expansíveis + edição de descrições
│   └── PowerBIQueryLogTable.tsx          # histórico paginado
├── components/admin/AdminSidebar.tsx     # + item "Power BI" após "WhatsApp"
├── pages/admin/AgentTestPage.tsx         # + seção "Consultas Power BI"
└── App.tsx                               # + rota /admin/powerbi
```

**Structure Decision**: Web application com as camadas existentes do backend (API, Application, Domain, DTO, Infra, Infra.Interfaces) e o SPA em `frontend/`. Nenhum projeto novo.

## Riscos e mitigação

| Risco | Mitigação |
|---|---|
| `INFO.VIEW.*` indisponível para o service principal | Fallback para `COLUMNSTATISTICS()` com status `Partial` (R5). Validar assim que a permissão do tenant de teste for liberada. |
| Permissão Viewer impede consultas | "Testar conexão" por dataset com mensagem orientativa; quickstart documenta Contributor ou Build |
| Modelo gera DAX incorreto | O erro volta ao modelo, que pode corrigir dentro do limite de 5 chamadas. Descrições do schema e histórico ajudam o ajuste. |
| Alucinação de números | Bloco de prompt explícito (contracts/llm-tools.md) e respostas sempre baseadas no resultado das tools; validação via SC-003 e SC-004 |
| Streaming com texto antes de tool call | Aceitável. O texto parcial ("vou consultar...") é entregue normalmente e persistido na resposta final. |
| Rate limit (429) do Power BI | Tratado como erro amigável de tool; não há retry automático na v1 |

## Complexity Tracking

Sem violações da constituição a justificar.
