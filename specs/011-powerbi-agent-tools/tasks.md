---
description: "Task list for 011-powerbi-agent-tools"
---

# Tasks: Integração do Agente com Power BI

**Input**: Design documents from `C:\repos\AvaBot\specs\011-powerbi-agent-tools\`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/powerbi-api.md, contracts/llm-tools.md, quickstart.md

**Tests**: A spec não exige TDD. Estão incluídos apenas os testes unitários listados na estrutura do `plan.md` (xUnit + Moq em `AvaBot.Tests`), posicionados depois da implementação de cada história.

**Organization**: Tarefas agrupadas por user story (US1–US4 da spec). A US5 foi removida no clarify.

**Regras gerais para quem executar**:
- Backend: usar a skill `dotnet-architecture` (constituição, princípio I) e seguir os padrões existentes: `WhatsappController`, `AgentRepository`, `DependencyInjection.cs`, envelope `Result<T>`, `[JsonPropertyName]` em camelCase e tabelas `avabot_*` em snake_case.
- Frontend: seguir o padrão de `pages/admin/WhatsappPage.tsx` e `Services/AgentService.ts` (fetch + `AuthService.getAuthHeaders()` + `handleResponse`), `toast` do sonner, Tailwind, `interface` e arrow functions.
- Nunca retornar nem logar o client secret em claro.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: história a que a tarefa pertence (US1, US2, US3, US4)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Configuração e enums compartilhados.

- [X] T001 Adicionar seção `PowerBI` em `AvaBot.API/appsettings.json` (`SecretEncryptionKey: ""`, `MaxRows: 100`, `MaxToolCallsPerMessage: 5`, `QueryTimeoutSeconds: 30`, `QueryLogRetentionDays: 30`, `ApiBaseUrl: "https://api.powerbi.com/v1.0/myorg"`, `AuthorityBaseUrl: "https://login.microsoftonline.com"`), conforme data-model.md § Configuração
- [X] T002 [P] Adicionar `PowerBI__SecretEncryptionKey: ${POWERBI_SECRET_ENCRYPTION_KEY}` ao serviço da API em `docker-compose.yml` e `docker-compose-prod.yml`, e `POWERBI_SECRET_ENCRYPTION_KEY=` (vazio, com comentário de como gerar) em `.env.example` ou `.env.prod.example`, se existirem
- [X] T003 [P] Criar enum `PowerBISchemaStatus` (`NotGenerated = 0`, `Generated = 1`, `Partial = 2`, `Error = 3`) em `AvaBot.Domain/Enums/PowerBISchemaStatus.cs`
- [X] T004 [P] Criar enum `PowerBIQueryStatus` (`Success = 1`, `Error = 2`, `Timeout = 3`) em `AvaBot.Domain/Enums/PowerBIQueryStatus.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modelo de dados, migração, cliente Power BI, proteção de segredo e casca do admin. Bloqueia todas as histórias.

**⚠️ CRITICAL**: Nenhuma história começa antes desta fase.

### Domínio e persistência

- [X] T005 [P] Criar `AgentPowerBIConfig` (campos de data-model.md: `AgentPowerBIConfigId`, `AgentId`, `TenantId`, `ClientId`, `ClientSecretEncrypted`, `ClientSecretHint`, `LastTestAt`, `LastTestSuccess`, `LastTestMessage`, `CreatedAt`, `UpdatedAt`, navegação `Agent`) em `AvaBot.Domain/Models/AgentPowerBIConfig.cs`
- [X] T006 [P] Criar `PowerBIDataset` (campos de data-model.md, incluindo `ToolKey`, `SchemaJson` como `string?`, `SchemaStatus`, `SchemaGeneratedAt`, `SchemaError`, navegação `Agent`) em `AvaBot.Domain/Models/PowerBIDataset.cs`
- [X] T007 [P] Criar `PowerBIQueryLog` (campos de data-model.md) em `AvaBot.Domain/Models/PowerBIQueryLog.cs`
- [X] T008 [P] Criar os POCOs do schema `PowerBISchema { List<PowerBISchemaTable> Tables }`, `PowerBISchemaTable { Name, Description, UserDescription, Columns, Measures }`, `PowerBISchemaColumn { Name, DataType, Description, UserDescription }` e `PowerBISchemaMeasure { Name, Description, UserDescription }`, com `[JsonPropertyName]` camelCase e métodos estáticos `Serialize`/`Deserialize` (System.Text.Json) em `AvaBot.Domain/Models/PowerBISchema.cs`
- [X] T009 Adicionar `public bool PowerBIEnabled { get; set; }` e as navegações `AgentPowerBIConfig? PowerBIConfig` e `ICollection<PowerBIDataset> PowerBIDatasets` em `AvaBot.Domain/Models/Agent.cs`
- [X] T010 Mapear no `AvaBotContext`: coluna `powerbi_enabled` (`boolean`, default `false`) em `Agent`; tabelas `avabot_agent_powerbi_configs` (índice único em `agent_id`), `avabot_powerbi_datasets` (índice único `(agent_id, dataset_id)` e `(agent_id, tool_key)`, `schema_json` com `HasColumnType("jsonb")`) e `avabot_powerbi_query_logs` (índice `(agent_id, created_at)`). Usar PKs `{tabela}_pkey`, FKs `OnDelete(DeleteBehavior.ClientSetNull)` e tamanhos de varchar de data-model.md, em `AvaBot.Infra/Context/AvaBotContext.cs`
- [X] T011 Gerar a migração `AddPowerBIIntegration` com `dotnet ef migrations add AddPowerBIIntegration --project AvaBot.Infra --startup-project AvaBot.API` e revisar o arquivo gerado em `AvaBot.Infra/Migrations/`
- [X] T012 [P] Criar a interface `IAgentPowerBIConfigRepository<T>` (`GetByAgentIdAsync`, `UpsertAsync`) em `AvaBot.Infra.Interfaces/Repository/IAgentPowerBIConfigRepository.cs` e a implementação em `AvaBot.Infra/Repository/AgentPowerBIConfigRepository.cs`
- [X] T013 [P] Criar a interface `IPowerBIDatasetRepository<T>` (`GetByAgentIdAsync`, `GetByIdAsync(agentId, id)`, `ExistsAsync(agentId, datasetId, excludeId)`, `ToolKeyExistsAsync(agentId, toolKey, excludeId)`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`) em `AvaBot.Infra.Interfaces/Repository/IPowerBIDatasetRepository.cs` e a implementação em `AvaBot.Infra/Repository/PowerBIDatasetRepository.cs`
- [X] T014 [P] Criar a interface `IPowerBIQueryLogRepository<T>` (`CreateAsync`, `GetPagedByAgentAsync(agentId, page, pageSize)` retornando itens e total ordenados por `CreatedAt DESC`, `DeleteOlderThanAsync(DateTime)`) em `AvaBot.Infra.Interfaces/Repository/IPowerBIQueryLogRepository.cs` e a implementação em `AvaBot.Infra/Repository/PowerBIQueryLogRepository.cs`
- [X] T015 Atualizar `AgentRepository.DeleteAsync` para remover, antes do agente, os `PowerBIQueryLogs`, `PowerBIDatasets` e `AgentPowerBIConfig` do agente, em `AvaBot.Infra/Repository/AgentRepository.cs`

### Serviços de infraestrutura

- [X] T016 [P] Criar a interface `ISecretProtector` (`string Protect(string plain)`, `string Unprotect(string cipher)`) em `AvaBot.Infra.Interfaces/AppServices/ISecretProtector.cs` e a implementação `AesGcmSecretProtector` em `AvaBot.Infra/AppServices/AesGcmSecretProtector.cs`: AES-256-GCM com chave base64 de 32 bytes de `PowerBI:SecretEncryptionKey`, formato `base64(nonce(12)|tag(16)|ciphertext)`. Lança `InvalidOperationException("PowerBI:SecretEncryptionKey não configurada")` no uso se a chave estiver ausente ou inválida; não lança no construtor, para não derrubar a API de quem não usa a feature (research R7)
- [X] T017 [P] Criar a interface `IPowerBIClient` em `AvaBot.Infra.Interfaces/AppServices/IPowerBIClient.cs` com os tipos neutros `PowerBICredentials { TenantId, ClientId, ClientSecret }`, `PowerBIQueryResult { List<string> Columns, List<List<object?>> Rows }`, `PowerBIWorkspace { Id, Name, List<PowerBIDatasetRef> Datasets }` e a exceção `PowerBIApiException { StatusCode, ErrorCode, Message }`. Métodos: `GetAccessTokenAsync(creds, ct)`, `ListWorkspacesWithDatasetsAsync(creds, ct)`, `ExecuteQueryAsync(creds, workspaceId, datasetId, dax, ct)` e `InvalidateToken(tenantId, clientId)`
- [X] T018 Implementar `PowerBIClient` em `AvaBot.Infra/AppServices/PowerBIClient.cs`:
  - token via `POST {AuthorityBaseUrl}/{tenantId}/oauth2/v2.0/token` (client_credentials, scope `https://analysis.windows.net/powerbi/api/.default`), cacheado em `IMemoryCache` com chave `pbi-token:{tenantId}:{clientId}` até `expires_in - 300s`;
  - `GET {ApiBaseUrl}/groups` e `GET /groups/{id}/datasets`;
  - `POST /groups/{ws}/datasets/{ds}/executeQueries` com body `{"queries":[{"query":dax}],"serializerSettings":{"includeNulls":true}}`, convertendo `results[0].tables[0].rows` em colunas (união ordenada das chaves) e linhas;
  - erros HTTP viram `PowerBIApiException` com o código `pbi.error.code` (ex.: `PowerBIEntityNotFound`) e a mensagem `details[].detail.value` quando existir;
  - HttpClients nomeados `"PowerBI"` e `"EntraId"` (research R3/R4).
- [X] T019 Registrar em `AvaBot.Application/DependencyInjection.cs`: `services.AddMemoryCache()`, HttpClients `"PowerBI"` e `"EntraId"`, `ISecretProtector` → `AesGcmSecretProtector` (singleton), `IPowerBIClient` → `PowerBIClient` (singleton) e os três repositórios novos (scoped)

### DTOs, API e casca do frontend

- [X] T020 [P] Criar `AvaBot.DTO/PowerBIDTOs.cs` com todos os DTOs de contracts/powerbi-api.md: `PowerBIConfigInfo`, `PowerBIConfigUpdateInfo`, `PowerBIEnabledUpdateInfo`, `PowerBIConnectionTestInfo`, `PowerBIConnectionTestStepInfo`, `PowerBIWorkspaceInfo`, `PowerBIWorkspaceDatasetInfo`, `PowerBIDatasetInfo`, `PowerBIDatasetInsertInfo`, `PowerBIDatasetSchemaInfo`, `PowerBISchemaTableInfo`, `PowerBISchemaColumnInfo`, `PowerBISchemaMeasureInfo`, `PowerBISchemaDescriptionUpdateInfo`, `PowerBISchemaDescriptionItemInfo`, `PowerBIQueryLogInfo`, `PowerBIQueryLogPageInfo` e `AgentTestPowerBIQueryInfo`, todos com `[JsonPropertyName]` camelCase
- [X] T021 Adicionar `PowerBIEnabled` (`[JsonPropertyName("powerBIEnabled")]`) em `AgentInfo` e `List<AgentTestPowerBIQueryInfo> PowerBIQueries` (`"powerBIQueries"`) em `AgentTestResultInfo` em `AvaBot.DTO/AgentDTOs.cs`. Conferir que o `AgentProfile` mapeia `PowerBIEnabled` em `AvaBot.Application/Profiles/AgentProfile.cs` e que `AgentService` não sobrescreve `PowerBIEnabled` no update do agente
- [X] T022 Criar o esqueleto de `PowerBIService` (construtor com os repositórios de agente, config, dataset e log, `IPowerBIClient`, `ISecretProtector`, `IConfiguration`, `ILogger` e um helper privado `GetAgentBySlugOrThrowAsync` que lança `KeyNotFoundException`) em `AvaBot.Application/Services/PowerBIService.cs` e registrá-lo como scoped em `AvaBot.Application/DependencyInjection.cs`
- [X] T023 Criar o esqueleto de `PowerBIController` (`[ApiController]`, injeção de `PowerBIService`, padrão de try/catch do `WhatsappController`: `KeyNotFoundException` → 404, `InvalidOperationException`/`ArgumentException`/`PowerBIApiException` → 400, demais → 500) em `AvaBot.API/Controllers/PowerBIController.cs`
- [X] T024 [P] Criar `frontend/src/types/powerbi.ts` com interfaces espelhando os DTOs de T020 (camelCase) e adicionar `powerBIEnabled: boolean` em `AgentInfo` e `powerBIQueries` em `AgentTestResult` em `frontend/src/types/agent.ts`
- [X] T025 [P] Criar `frontend/src/Services/PowerBIService.ts` (mesmo padrão de `getApiUrl`/`handleResponse`/`AuthService.getAuthHeaders()` de `AgentService.ts`) com stubs para todos os endpoints de contracts/powerbi-api.md: `getConfig`, `saveConfig`, `testConnection`, `setEnabled`, `getWorkspaces`, `getDatasets`, `createDataset`, `updateDataset`, `deleteDataset`, `generateSchema`, `getSchema`, `updateSchemaDescriptions` e `getQueryLogs(slug, page, pageSize)`
- [X] T026 Criar `frontend/src/pages/admin/PowerBIPage.tsx` com cabeçalho "Power BI", mensagem "Selecione um agente" quando `useAgentStore().selectedAgent` for nulo, e abas Credenciais | Datasets | Histórico (conteúdo vazio por enquanto). Adicionar a rota `/admin/powerbi` em `frontend/src/App.tsx` e o item `{ label: 'Power BI', path: '/admin/powerbi', requiresAgent: true }` logo após "WhatsApp" em `frontend/src/components/admin/AdminSidebar.tsx`

**Checkpoint**: `dotnet build` ok, migração aplicada, página "Power BI" acessível no menu.

---

## Phase 3: User Story 1 - Configurar Credenciais do Power BI no Agente (Priority: P1) 🎯 MVP

**Goal**: O administrador salva as credenciais por agente (segredo criptografado e mascarado) e testa a conexão com diagnóstico por etapa.

**Independent Test**: Em `/admin/powerbi`, salvar tenant, client e secret, recarregar e ver o secret mascarado; clicar em "Testar conexão" e ver auth ✓, workspaces ✓ e o status por dataset (com o papel Viewer atual, o passo do dataset deve falhar com a orientação de permissão).

### Implementation for User Story 1

- [X] T027 [P] [US1] Criar `PowerBIConfigUpdateInfoValidator` (FluentValidation: `tenantId` e `clientId` obrigatórios e GUID válidos; `clientSecret` até 500 caracteres) em `AvaBot.API/Validators/PowerBIConfigUpdateInfoValidator.cs`, seguindo `AgentInsertInfoValidator.cs`
- [X] T028 [US1] Implementar `PowerBIService.GetConfigAsync(slug)`, retornando `PowerBIConfigInfo` (com `isConfigured: false` se não houver config; `clientSecretMasked = "••••" + ClientSecretHint`; nunca o segredo), em `AvaBot.Application/Services/PowerBIService.cs`
- [X] T029 [US1] Implementar `PowerBIService.SaveConfigAsync(slug, PowerBIConfigUpdateInfo)` em `AvaBot.Application/Services/PowerBIService.cs`:
  - criação exige segredo; no update, segredo vazio mantém o atual (FR-003);
  - criptografa com `ISecretProtector` e grava `ClientSecretHint` com os 4 últimos caracteres;
  - chama `IPowerBIClient.InvalidateToken` com os valores antigos e novos;
  - limpa `LastTest*` quando as credenciais mudam.
- [X] T030 [US1] Implementar `PowerBIService.TestConnectionAsync(slug)` em `AvaBot.Application/Services/PowerBIService.cs`:
  - passo `auth` (`GetAccessTokenAsync`; em erro, mensagem "Credenciais inválidas" com detalhe do Entra ID, sem o segredo);
  - passo `workspaces` (`ListWorkspacesWithDatasetsAsync`, informando quantos workspaces);
  - para cada dataset cadastrado, passo `dataset:{Name}` com `EVALUATE ROW("ok", 1)`. Se vier `PowerBIEntityNotFound` ou 401/403, a mensagem é: "Credenciais válidas, mas o aplicativo não tem permissão para consultar este dataset. Conceda papel Contributor no workspace ou permissão Build no dataset e verifique a configuração de tenant 'Dataset Execute Queries REST API'.";
  - persiste `LastTestAt`/`LastTestSuccess`/`LastTestMessage` e retorna `PowerBIConnectionTestInfo`.
- [X] T031 [US1] Adicionar ao `PowerBIController` os endpoints `GET /powerbi/{slug}/config`, `PUT /powerbi/{slug}/config` e `POST /powerbi/{slug}/test`, todos com `[Authorize]`, em `AvaBot.API/Controllers/PowerBIController.cs`
- [X] T032 [US1] Criar `frontend/src/components/admin/powerbi/PowerBICredentialsForm.tsx`:
  - campos Tenant ID, Client ID e Client Secret (input `password`, placeholder com `clientSecretMasked` quando já configurado e texto "deixe em branco para manter");
  - botões "Salvar" e "Testar conexão";
  - lista de passos do teste com ✓/✗ e mensagem, e último teste (data e status);
  - toasts de sucesso e erro; estados de loading nos botões.
- [X] T033 [US1] Integrar `PowerBICredentialsForm` na aba "Credenciais" de `frontend/src/pages/admin/PowerBIPage.tsx`, carregando `getConfig` ao trocar de agente

**Checkpoint**: US1 funcional e testável sozinha.

---

## Phase 4: User Story 2 - Cadastrar Datasets e Gerar o Schema (Priority: P1)

**Goal**: O administrador vincula vários datasets ao agente (escolhendo-os em selects), gera o schema automaticamente e o visualiza.

**Independent Test**: Com credenciais válidas e permissão de consulta, adicionar o dataset "ABIPESCA - Comércio Internacional", clicar em "Gerar schema" e ver as tabelas, colunas (com tipo) e medidas e a data da geração; gerar de novo com o dataset inacessível e ver o erro com o schema anterior preservado.

### Implementation for User Story 2

- [X] T034 [P] [US2] Criar `PowerBIDatasetInsertInfoValidator` (`workspaceId` e `datasetId` GUID; `name` com 1 a 120 caracteres; `description` até 1000) em `AvaBot.API/Validators/PowerBIDatasetInsertInfoValidator.cs`
- [X] T035 [P] [US2] Criar `PowerBISchemaBuilder` em `AvaBot.Application/Services/PowerBISchemaBuilder.cs` (registrar como scoped em `DependencyInjection.cs`):
  - método `BuildAsync(creds, workspaceId, datasetId, ct)` que executa `EVALUATE INFO.VIEW.TABLES()`, `EVALUATE INFO.VIEW.COLUMNS()` e `EVALUATE INFO.VIEW.MEASURES()` via `IPowerBIClient` e monta o `PowerBISchema`;
  - localiza as colunas do resultado pelo sufixo do nome (`[Name]`, `[Table]`, `[DataType]`, `[IsHidden]`, `[Description]`) e ignora itens com `IsHidden = true` (FR-013);
  - fallback: se `INFO.VIEW.*` falhar com erro de função ou permissão, executa `EVALUATE COLUMNSTATISTICS()` (colunas `Table Name` e `Column Name`) e retorna o status `Partial`;
  - retorna `(PowerBISchema schema, PowerBISchemaStatus status)` (research R5).
- [X] T036 [P] [US2] Adicionar o método estático `PowerBISchema.MergeUserDescriptions(PowerBISchema previous, PowerBISchema fresh)`, que copia `UserDescription` por chave (tabela, tabela+coluna, tabela+medida, sem diferenciar maiúsculas) e descarta itens inexistentes, em `AvaBot.Domain/Models/PowerBISchema.cs`
- [X] T037 [US2] Implementar em `PowerBIService` `ListWorkspacesAsync(slug)`, que exige config e retorna `PowerBIWorkspaceInfo[]`, em `AvaBot.Application/Services/PowerBIService.cs`
- [X] T038 [US2] Implementar em `PowerBIService` o CRUD de datasets, em `AvaBot.Application/Services/PowerBIService.cs`:
  - `GetDatasetsAsync`, com `tableCount`/`columnCount`/`measureCount` calculados a partir do `SchemaJson`;
  - `CreateDatasetAsync`, que impede duplicar `datasetId` no agente e gera `ToolKey` com o slug de `Name` (minúsculas, sem acentos, `[a-z0-9_]`, até 60 caracteres, sufixo `_2`, `_3`... se repetido);
  - `UpdateDatasetAsync`: se `workspaceId` ou `datasetId` mudar, zera `SchemaJson` e volta o status para `NotGenerated`; se `Name` mudar, recalcula `ToolKey`;
  - `DeleteDatasetAsync`: se o agente ficar sem dataset utilizável e `PowerBIEnabled` for `true`, desliga a flag e informa isso na mensagem de retorno.
- [X] T039 [US2] Implementar `PowerBIService.GenerateSchemaAsync(slug, datasetId)` em `AvaBot.Application/Services/PowerBIService.cs`:
  - descriptografa as credenciais e chama `PowerBISchemaBuilder` com timeout de 60 s;
  - em sucesso, faz o merge com o schema anterior (T036) e grava `SchemaJson`, `SchemaStatus`, `SchemaGeneratedAt = UtcNow` e `SchemaError = null`;
  - em falha, mantém `SchemaJson`, grava `SchemaStatus = Error` e `SchemaError`, e lança `InvalidOperationException` com mensagem amigável (permissão vira o mesmo texto de orientação de T030) (FR-010).

  Implementar também `GetSchemaAsync(slug, datasetId)`.
- [X] T040 [US2] Adicionar ao `PowerBIController` os endpoints `GET /powerbi/{slug}/workspaces`, `GET/POST /powerbi/{slug}/datasets`, `PUT/DELETE /powerbi/{slug}/datasets/{id}`, `POST /powerbi/{slug}/datasets/{id}/schema/generate` e `GET /powerbi/{slug}/datasets/{id}/schema`, em `AvaBot.API/Controllers/PowerBIController.cs`
- [X] T041 [P] [US2] Criar `frontend/src/components/admin/powerbi/PowerBIDatasetForm.tsx`, um formulário em modal ou painel inline com Tailwind:
  - select de workspace e select de dataset preenchidos por `getWorkspaces`, com fallback para digitar os GUIDs manualmente se a listagem falhar;
  - campos nome (pré-preenchido com o nome do dataset) e descrição de negócio (textarea, com dica "descreva o que o dataset contém; o agente usa isso para escolher o dataset");
  - modos criar e editar.
- [X] T042 [P] [US2] Criar `frontend/src/components/admin/powerbi/PowerBISchemaViewer.tsx`, só leitura nesta fase: cabeçalho com status e data de geração, aviso se `Partial` ("schema parcial: medidas e tipos indisponíveis"), lista de tabelas expansíveis com colunas (nome e tipo) e medidas, e campo de busca por nome
- [X] T043 [US2] Criar `frontend/src/components/admin/powerbi/PowerBIDatasetList.tsx`:
  - cards ou linhas por dataset com nome, descrição, badge de status do schema (Não gerado, Gerado em dd/mm hh:mm, Parcial, Erro) e contagem de tabelas, colunas e medidas;
  - botões "Gerar schema" (com loading), "Ver schema" (abre `PowerBISchemaViewer`), "Editar" e "Remover" (confirmação em modal Tailwind, sem `window.confirm`);
  - exibe `schemaError` quando houver.
- [X] T044 [US2] Integrar `PowerBIDatasetList` e `PowerBIDatasetForm` na aba "Datasets" de `frontend/src/pages/admin/PowerBIPage.tsx`, com o botão "Adicionar dataset" desabilitado se as credenciais não estiverem configuradas

**Checkpoint**: US1 + US2 funcionais; schema gerado e visível.

---

## Phase 5: User Story 3 - Ativar o Power BI no Agente e Responder com Dados (Priority: P1)

**Goal**: Com a flag ligada, o agente usa as tools `listar_schema` e `consultar_bi` em todos os canais; com a flag desligada, o comportamento é idêntico ao atual. As consultas são registradas e exibidas no histórico.

**Independent Test**: Ativar a flag (com aviso), perguntar "Quanto exportamos de tilápia?" e ver o agente pedir o período; perguntar com o período e conferir o valor no relatório; ver o registro no Histórico; desligar a flag e confirmar que a resposta vem só da base de conhecimento, sem registro novo.

### Implementation for User Story 3

- [X] T045 [P] [US3] Adicionar a `AvaBot.Infra.Interfaces/AppServices/IOpenAIService.cs` os tipos neutros `ChatToolDefinition { Name, Description, string ParametersJsonSchema }` e `ChatToolCall { Id, Name, ArgumentsJson }`, e os métodos:
  - `IAsyncEnumerable<string> StreamChatCompletionWithToolsAsync(string model, string systemPrompt, List<ChatCompletionMessage> messages, IReadOnlyList<ChatToolDefinition> tools, Func<ChatToolCall, CancellationToken, Task<string>> toolExecutor, int maxToolCalls, CancellationToken ct = default)`;
  - `Task<string> ChatCompletionWithToolsAsync(...)`, com os mesmos parâmetros e sem streaming.
- [X] T046 [US3] Implementar os dois métodos em `AvaBot.Infra/AppServices/OpenAIService.cs` (research R2), sem alterar os métodos existentes:
  - extrair a conversão de mensagens atual para um helper privado reutilizado;
  - criar `ChatCompletionOptions` com `ChatTool.CreateFunctionTool(name, description, BinaryData.FromString(schema))`;
  - no streaming, fazer `yield return` do `ContentUpdate` e acumular `ToolCallUpdates` por `Index` (`ToolCallId`, `FunctionName`, `FunctionArgumentsUpdate`);
  - quando `FinishReason == ToolCalls`, adicionar `new AssistantChatMessage(toolCalls)` (`ChatToolCall.CreateFunctionToolCall`), executar cada call com o executor e adicionar `new ToolChatMessage(id, result)`, depois repetir;
  - contar as chamadas; ao atingir `maxToolCalls`, a próxima iteração usa `ToolChoice = ChatToolChoice.CreateNoneChoice()`;
  - se o executor lançar exceção, enviar `{"error":"..."}` como resultado.
- [X] T047 [US3] Criar `PowerBIToolProvider` em `AvaBot.Application/Services/PowerBIToolProvider.cs` (scoped, registrar em `DependencyInjection.cs`):
  - `Task<PowerBIToolset?> GetToolsetAsync(Agent agent, long? sessionId, string userQuestion)` retorna `null` se `!agent.PowerBIEnabled`, se não houver config ou se não houver dataset com `SchemaJson`;
  - `PowerBIToolset` expõe `Definitions` (duas tools, JSON schema e descrições exatamente como em contracts/llm-tools.md, com enum das `ToolKey` e catálogo "chave: nome — descrição"), `SystemPromptAddendum` (bloco "DADOS DO POWER BI" de contracts/llm-tools.md), `ExecuteAsync(ChatToolCall, ct)` e `List<AgentTestPowerBIQueryInfo> ExecutedQueries`, este último para a página de teste.
- [X] T048 [US3] Implementar `PowerBIToolset.ExecuteAsync` em `AvaBot.Application/Services/PowerBIToolProvider.cs`, seguindo as regras do executor em contracts/llm-tools.md:
  - parsear os argumentos JSON e validar que o `dataset` pertence ao agente;
  - `listar_schema` devolve o texto compacto do `SchemaJson`, incluindo `userDescription`;
  - `consultar_bi` valida o início com `EVALUATE`/`DEFINE` (ignorando espaços e comentários `//`, `--`, `/* */`), descriptografa as credenciais e executa com timeout `QueryTimeoutSeconds` via `CancellationTokenSource.CreateLinkedTokenSource`;
  - truncar o resultado em `MaxRows` e serializar `{columns, rows, rowCount, truncated}`; em erro, `{error}` sanitizado;
  - medir a duração com `Stopwatch`, gravar `PowerBIQueryLog` (`AgentId`, `ChatSessionId`, `PowerBIDatasetId`, `DatasetName`, `ToolName`, `UserQuestion`, `Query`, `DurationMs`, `RowCount`, `Truncated`, `Status`, `ErrorMessage`) e logar com `ILogger` no mesmo estilo de caixas do `ChatService`, sem segredo (FR-027);
  - nunca lançar exceção para o chamador.
- [X] T049 [US3] Alterar `ChatService.ProcessMessageAsync` em `AvaBot.Application/Services/ChatService.cs` para a bifurcação:
  - carregar o agente (injetar `IAgentRepository<Agent>` e `PowerBIToolProvider`) e chamar `GetToolsetAsync(agent, sessionId, userMessage)`;
  - se o toolset for `null`, manter **exatamente** a chamada atual a `StreamChatCompletionAsync`;
  - senão, concatenar `SystemPromptAddendum` ao `fullSystemPrompt` e chamar `StreamChatCompletionWithToolsAsync(..., toolset.Definitions, toolset.ExecuteAsync, MaxToolCallsPerMessage, ct)`.

  O acúmulo de `fullResponse`, o log e o `SaveMessageAsync` continuam iguais.
- [X] T050 [US3] Ajustar `BuildFullSystemPrompt` em `AvaBot.Application/Services/ChatService.cs` para receber `bool hasDataTools`. Quando for `true`, trocar a frase "Responda SOMENTE com base no contexto fornecido" por "Responda SOMENTE com base no contexto fornecido ou nos dados retornados pelas ferramentas" e acrescentar "NAO use tabelas" à instrução de formatação (FR-022a). Quando for `false`, o texto atual fica inalterado.
- [X] T051 [US3] Alterar `ChatService.TestMessageAsync` em `AvaBot.Application/Services/ChatService.cs` para usar o toolset (com `sessionId` nulo) e `ChatCompletionWithToolsAsync` quando disponível, preenchendo `AgentTestResultInfo.PowerBIQueries` com `toolset.ExecutedQueries` (`resultPreview` com até 2000 caracteres) (FR-028)
- [X] T052 [US3] Implementar `PowerBIService.SetEnabledAsync(slug, bool)` em `AvaBot.Application/Services/PowerBIService.cs`. Ao ativar, validar FR-006 (config com segredo e ao menos 1 dataset com `SchemaJson`) e lançar `InvalidOperationException` com a mensagem do que falta. Implementar também `GetQueryLogsAsync(slug, page, pageSize)`, com `pageSize` limitado a 100
- [X] T053 [US3] Adicionar ao `PowerBIController` `PUT /powerbi/{slug}/enabled` e `GET /powerbi/{slug}/query-logs?page&pageSize` em `AvaBot.API/Controllers/PowerBIController.cs`
- [X] T054 [P] [US3] Criar `PowerBIQueryLogCleanupService : BackgroundService` em `AvaBot.Application/Services/PowerBIQueryLogCleanupService.cs`: a cada 24 h, cria um scope e chama `DeleteOlderThanAsync(UtcNow - QueryLogRetentionDays)`, logando a quantidade removida e capturando exceções. Registrar com `services.AddHostedService<...>()` em `AvaBot.Application/DependencyInjection.cs` (FR-027b)
- [X] T055 [P] [US3] Criar `frontend/src/components/admin/powerbi/PowerBIEnableToggle.tsx`: switch "Power BI ativo"; ao ativar, abre um modal Tailwind com o aviso "Os dados dos datasets vinculados ficarão acessíveis a qualquer usuário que converse com este agente, em todos os canais (web, Telegram, WhatsApp)" e os botões Cancelar/Ativar (FR-024a); mostra a mensagem do backend (FR-006) em toast de erro; desativar não pede confirmação
- [X] T056 [P] [US3] Criar `frontend/src/components/admin/powerbi/PowerBIQueryLogTable.tsx`: lista paginada (20 por página) com data/hora, pergunta, dataset, tool, status (badge), duração e linhas (indicando "truncado"); a linha expande para mostrar o DAX em `<pre>` e a mensagem de erro; botão "Atualizar"
- [X] T057 [US3] Integrar `PowerBIEnableToggle` no topo de `frontend/src/pages/admin/PowerBIPage.tsx` (visível em todas as abas) e `PowerBIQueryLogTable` na aba "Histórico". Ao mudar a flag, atualizar `selectedAgent.powerBIEnabled` no `useAgentStore` (recarregar com `loadAgents` ou fazer um update local)
- [X] T058 [US3] Adicionar a seção "Consultas Power BI" em `frontend/src/pages/admin/AgentTestPage.tsx`, exibida quando `powerBIQueries.length > 0`, com tool, dataset, DAX em `<pre>`, duração, linhas, sucesso ou erro e prévia do resultado, no mesmo estilo das seções existentes de resultados da busca

**Checkpoint**: Fluxo completo de ponta a ponta. Validar o cenário US3 do quickstart.md em web, Telegram e WhatsApp.

---

## Phase 6: User Story 4 - Complementar o Schema com Descrições de Negócio (Priority: P2)

**Goal**: O administrador descreve tabelas, colunas e medidas; as descrições vão para o modelo e sobrevivem à regeração do schema.

**Independent Test**: Descrever uma medida, salvar, gerar o schema de novo e confirmar que a descrição continua lá e aparece no resultado de `listar_schema` (visível na página de teste).

### Implementation for User Story 4

- [X] T059 [US4] Implementar `PowerBIService.UpdateSchemaDescriptionsAsync(slug, datasetId, PowerBISchemaDescriptionUpdateInfo)` em `AvaBot.Application/Services/PowerBIService.cs`: localiza cada item por `kind` + `table` + `name` (sem diferenciar maiúsculas), define `UserDescription` (string vazia vira `null`; limite de 1000 caracteres) e retorna 400 (`ArgumentException`) se algum item não existir
- [X] T060 [US4] Adicionar `PUT /powerbi/{slug}/datasets/{id}/schema/descriptions` ao `PowerBIController` em `AvaBot.API/Controllers/PowerBIController.cs`
- [X] T061 [US4] Tornar editáveis as descrições em `frontend/src/components/admin/powerbi/PowerBISchemaViewer.tsx`: ícone ou botão "Descrever" por tabela, coluna e medida abre um textarea inline; o viewer mostra a descrição do modelo (`description`, em cinza) e a do administrador (`userDescription`); botão "Salvar descrições" envia só os itens alterados e exibe toast; aviso de alterações não salvas ao fechar

**Checkpoint**: Todas as histórias funcionais.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T062 [P] Testes unitários de `AesGcmSecretProtector` (ida e volta, chave inválida, ciphertext adulterado lança exceção) em `AvaBot.Tests/Infra/AesGcmSecretProtectorTest.cs`
- [X] T063 [P] Testes de `PowerBISchemaBuilder` (parsing das colunas `INFO.VIEW.*` por sufixo, filtro `IsHidden`, fallback `COLUMNSTATISTICS` → `Partial`) e de `PowerBISchema.MergeUserDescriptions` em `AvaBot.Tests/Application/Services/PowerBISchemaBuilderTest.cs`
- [X] T064 [P] Testes de `PowerBIToolProvider` (toolset nulo com flag off ou sem schema; enum só com datasets do agente; dataset de outro agente vira `{error}`; DAX sem `EVALUATE`/`DEFINE` é rejeitado; truncamento em `MaxRows`; log gravado em sucesso e erro) em `AvaBot.Tests/Application/Services/PowerBIToolProviderTest.cs`
- [X] T065 [P] Testes de `PowerBIService` (segredo mantido em update sem segredo; `GetConfig` nunca retorna o segredo; `SetEnabled` bloqueado sem schema; delete do último dataset desliga a flag; erro de geração preserva o schema) em `AvaBot.Tests/Application/Services/PowerBIServiceTest.cs`
- [X] T066 Atualizar `AvaBot.Tests/Application/Services/ChatServiceTest.cs`: ajustar o construtor para as novas dependências; flag off ⇒ `StreamChatCompletionAsync` chamado e `StreamChatCompletionWithToolsAsync` nunca chamado (SC-005); flag on ⇒ método com tools chamado e prompt contém "DADOS DO POWER BI"
- [X] T067 [P] Garantir que nenhum log do `ChatService`, do `PowerBIClient` ou do `PowerBIToolProvider` inclua o client secret ou o access token: revisar `AvaBot.Infra/AppServices/PowerBIClient.cs` e `AvaBot.Application/Services/PowerBIToolProvider.cs` (SC-007)
- [X] T068 [P] Adicionar requests Bruno da pasta Power BI (config, test, enabled, workspaces, datasets, schema, query-logs) em `bruno/PowerBI/`, seguindo o formato de `bruno/WhatsApp/`
- [X] T069 [P] Documentar a feature (pré-requisitos no Entra ID e Power BI, variável `POWERBI_SECRET_ENCRYPTION_KEY`, uso da área Power BI) em `docs/POWERBI_INTEGRATION.md`
- [ ] T070 Rodar `dotnet build`, `dotnet test AvaBot.Tests` e `cd frontend && npm run lint && npm run build`, corrigir falhas e executar o roteiro de `specs/011-powerbi-agent-tools/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup e bloqueia todas as histórias.
- **US1 (Phase 3)**: depende só da Foundational.
- **US2 (Phase 4)**: depende da Foundational. Para teste real, precisa de credenciais salvas (US1); para desenvolvimento, só da Foundational.
- **US3 (Phase 5)**: depende da US2 (precisa de datasets com schema para gerar tools e permitir ativar a flag).
- **US4 (Phase 6)**: depende da US2 (viewer e schema). Independente da US3, mas as descrições só têm efeito no chat com a US3 pronta.
- **Polish (Phase 7)**: depois das histórias desejadas.

### Dentro das fases

- T005–T009 → T010 → T011 (migração) → T012–T015.
- T016/T017 → T018 → T019.
- T020 → T021, T022 → T023; T024 → T025 → T026.
- US1: T027 em paralelo com T028 → T029 → T030 → T031 → T032 → T033.
- US2: T034/T035/T036 em paralelo → T037 → T038 → T039 → T040; T041/T042 em paralelo → T043 → T044.
- US3: T045 → T046; T047 → T048 → T049 → T050 → T051; T052 → T053; T054/T055/T056 em paralelo → T057; T058 depois de T051.
- US4: T059 → T060 → T061.

### Parallel Opportunities

- Phase 1: T002, T003 e T004.
- Phase 2: entidades T005–T008; repositórios T012–T014; T016 e T017; T020, T024 e T025.
- US2: T034, T035 e T036 (backend) junto com T041 e T042 (frontend).
- US3: T045–T046 (OpenAI) em paralelo com T052–T053 (flag e logs) e com T054–T056.
- Polish: T062–T065 e T067–T069.

## Parallel Example: User Story 3

```text
Dev A (LLM loop):   T045 → T046
Dev B (tools):      T047 → T048 → T049 → T050 → T051
Dev C (admin/API):  T052 → T053
Dev D (frontend):   T055, T056 → T057 → T058
Qualquer um:        T054
```

## Implementation Strategy

### MVP

1. Phases 1 e 2 (fundação).
2. US1: credenciais e teste de conexão. Já entrega valor ao diagnosticar permissões no tenant do cliente.
3. US2: datasets e schema.
4. US3: agente respondendo com dados. **Este é o MVP funcional da feature** (as três histórias P1).
5. **Parar e validar** com o quickstart (conferir os números contra os relatórios: SC-003 e SC-004).

### Incremental Delivery

- US1 → deploy (diagnóstico de credenciais).
- US2 → deploy (catálogo e schema visíveis).
- US3 → deploy (feature em produção atrás da flag, desligada por padrão).
- US4 → deploy (refino da precisão com descrições).

### Notas

- A flag nasce desligada para todos os agentes existentes. O deploy de qualquer fase não altera o comportamento atual do chat.
- O formato de retorno de `INFO.VIEW.*` (T035) deve ser confirmado assim que o aplicativo tiver permissão de consulta no workspace de teste.

## Status de execução (2026-10-07)

T001–T069 concluídas. Verificado:

- `dotnet build AvaBot.sln`: 0 erros.
- `dotnet test AvaBot.Tests`: **140 aprovados, 0 falhas** (48 novos: `AesGcmSecretProtectorTest`, `PowerBISchemaBuilderTest`, `PowerBIToolProviderTest`, `PowerBIServiceTest` + 2 novos casos em `ChatServiceTest`).
- `dotnet ef migrations add AddPowerBIIntegration`: gerada e revisada (`20261007213101_AddPowerBIIntegration.cs`), com o índice `(agent_id, created_at DESC)`.
- `npx tsc -b` + `npm run build` no frontend: ok.
- ESLint nos arquivos novos da feature: 0 erros (o `npm run lint` do repo continua com 8 erros pré-existentes em `AgentForm`, `AvatarBubble`, `TelegramDemo`, `useChat`, `useChatWidget`, `useWebSocket`, `SessionDetailPage` e `SessionListPage`, que esta feature não toca).

**T070 pendente de execução** — não roda nesta sessão por depender de ambiente:

- `dotnet ef database update` (o Postgres local em `127.0.0.1:5432` estava inacessível).
- `POWERBI_SECRET_ENCRYPTION_KEY` ausente no `.env` local.
- O roteiro do `quickstart.md` (US1–US4, SC-003/SC-004) exige permissão de consulta no workspace: com o papel atual do service principal, o passo `dataset:*` do "Testar conexão" falha com `PowerBIEntityNotFound` (research R3), então nenhum DAX real foi executado contra o dataset da ABIPESCA.
- O parser de `INFO.VIEW.*` (T035) ainda não foi validado contra a resposta real do Power BI — está protegido pelo fallback `COLUMNSTATISTICS` → status `Partial`, mas confirmação pendente pela mesma razão acima.
