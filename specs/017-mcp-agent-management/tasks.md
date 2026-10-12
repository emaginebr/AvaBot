# Tasks: MCP de gestão de agentes

**Input**: Design documents from `/specs/017-mcp-agent-management/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/mcp-server.md, contracts/mcp-tools.md, contracts/access-keys-api.md, quickstart.md

**Tests**: o plano lista os testes xUnit e os testes de API como parte da entrega (`Testing` no Technical Context, research R10 e os arquivos em `AvaBot.Tests/` e `AvaBot.Tests.API/`), então cada fase inclui suas tarefas de teste. O frontend não tem suíte; a validação é `npm run lint` + `npm run build`.

**Organization**: tarefas agrupadas por história de usuário, na ordem de prioridade da spec (US1 e US2 são P1; US3 e US4 são P2; US5 é P3). A ordem de implementação segue as "Design notes" do plano.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa pendente)
- **[Story]**: história a que a tarefa pertence (US1..US5)
- Caminhos relativos à raiz do repositório

## Path Conventions

Backend .NET em Clean Architecture na raiz (`AvaBot.API`, `AvaBot.Application`, `AvaBot.Domain`, `AvaBot.DTO`, `AvaBot.Infra`, `AvaBot.Infra.Interfaces`, `AvaBot.Tests`, `AvaBot.Tests.API`, `AvaBot.Calibration`); frontend React em `frontend/src/`. Tudo que depende do SDK MCP fica em `AvaBot.API/Mcp/` e `AvaBot.API/Program.cs`; Application, Domain, DTO e Infra não referenciam o SDK.

Convenções a respeitar em todas as tarefas (constituição + skill `dotnet-architecture`): DTOs com `[JsonPropertyName]` camelCase; namespaces file-scoped; campos `_camelCase`; mensagens técnicas em português sem acento (`"Agente nao encontrado"`); controllers com `try/catch → StatusCode(500, Result.Failure(...))`; nomes das ferramentas MCP em `snake_case`; **nunca** logar o valor de uma chave de acesso nem o conteúdo dos argumentos de uma ferramenta; **nenhum** DTO em `McpDTOs.cs` pode ter campo da lista proibida do research R5.

---

## Phase 1: Setup (dependência e contratos de dados)

**Purpose**: o pacote do SDK e os DTOs que todas as fases usam.

- [X] T001 Adicionar `<PackageReference Include="ModelContextProtocol.AspNetCore" Version="2.2.0" />` em `AvaBot.API/AvaBot.API.csproj` e `<PackageReference Include="ModelContextProtocol" Version="2.2.0" />` em `AvaBot.Tests.API/AvaBot.Tests.API.csproj`; rodar `dotnet restore AvaBot.sln` e conferir que resolve do cache local (research R1); nenhum outro projeto referencia o SDK
- [X] T002 [P] Criar `AvaBot.DTO/McpAccessKeyDTOs.cs` com `McpAccessKeyInfo { mcpAccessKeyId, name, keyPrefix, status, createdAt, lastUsedAt?, revokedAt? }`, `McpAccessKeyInsertInfo { name }` e `McpAccessKeyCreatedInfo { key, accessKey: McpAccessKeyInfo }`, todos com `[JsonPropertyName]` camelCase no estilo de `AvaBot.DTO/UserDTOs.cs` (data-model.md, seção "Painel: chaves")
- [X] T003 [P] Criar `AvaBot.DTO/McpDTOs.cs` com `McpIdentityInfo`, `AgentMcpSummaryInfo`, `AgentMcpInfo`, `AgentMcpCreateInfo`, `AgentProfileUpdateInfo` (todas as propriedades anuláveis), `ChatSessionMcpInfo` (`channel` como string `web`/`telegram`, sem `resumeToken`), `ChatMessageMcpInfo` (`role` como string `visitante`/`agente`), `PowerBIMcpInfo` (sem `tenantId`, `clientId`, `clientSecretMasked`), `TelegramMcpInfo`, `WhatsappMcpInfo` e `KnowledgeDocumentMcpInsertInfo`, com `[JsonPropertyName]` camelCase e `[Description]` em português nas propriedades de entrada (o SDK usa a descrição no `inputSchema`); campos exatos na tabela "MCP: entrada e saída" de data-model.md

---

## Phase 2: Foundational (bloqueia todas as histórias)

**Purpose**: entidade e tabela da chave, service e handler de autenticação, esquema/política no ASP.NET, infraestrutura do servidor MCP (JSON, instruções, identidade do chamador, os dois filtros) e o registro em `Program.cs` sem nenhuma ferramenta ainda. Nada de história começa antes desta fase.

**⚠️ CRITICAL**: a migração (T006) precisa ser gerada depois de T004–T005 e aplicada no banco local antes de testar qualquer história. `Program.cs` (T017) é tocado de novo em cada história só para acrescentar uma linha `WithTools<...>`.

- [X] T004 Criar `AvaBot.Domain/Models/McpAccessKey.cs` com `McpAccessKeyId`, `UserId`, `Name`, `KeyHash`, `KeyPrefix`, `Status` (int, 1 ativa, 0 revogada), `CreatedAt`, `LastUsedAt?`, `RevokedAt?`, navegação `User`, `bool IsActive => Status == 1`, `const string Prefix = "avabot_mcp_"`, `static string GenerateKey()` (`Prefix` + base64url de `RandomNumberGenerator.GetBytes(32)`, sem `=`), `static string ComputeHash(string key)` (SHA-256 em hexa minúsculo) e `static bool HasExpectedFormat(string key)` (prefixo + comprimento total 54) (data-model.md)
- [X] T005 Mapear em `AvaBot.Infra/Context/AvaBotContext.cs`: `DbSet<McpAccessKey> McpAccessKeys`; tabela `avabot_mcp_access_keys` com `mcp_access_key_id` identity PK `avabot_mcp_access_keys_pkey`, `user_id bigint NOT NULL`, `name varchar(100)`, `key_hash varchar(64)` com índice único `avabot_mcp_access_keys_key_hash_key`, `key_prefix varchar(20)`, `status integer DEFAULT 1`, `created_at`/`last_used_at`/`revoked_at` `timestamp without time zone`; índice `ix_avabot_mcp_access_keys_user_id`; FK `avabot_fk_users_mcp_access_keys` → `avabot_users.user_id` com `OnDelete(DeleteBehavior.Restrict)` (desvio justificado no Complexity Tracking do plano; nunca `Cascade`)
- [X] T006 Gerar a migração com `dotnet ef migrations add AddMcpAccessKeys --project AvaBot.Infra --startup-project AvaBot.API`, conferir que o `Up()` em `AvaBot.Infra/Migrations/<timestamp>_AddMcpAccessKeys.cs` corresponde ao pseudo-SQL de data-model.md (tabela, índice único, índice de `user_id`, FK `RESTRICT`) e aplicar com `dotnet ef database update --project AvaBot.Infra --startup-project AvaBot.API`
- [X] T007 [P] Criar `AvaBot.Infra.Interfaces/Repository/IMcpAccessKeyRepository.cs` (`IMcpAccessKeyRepository<T>`) com `Task<T?> GetByIdAsync(long id, long userId)`, `Task<T?> GetByHashAsync(string keyHash)` (inclui `User`), `Task<List<T>> GetAllByUserAsync(long userId)` (mais recentes primeiro), `Task<int> CountActiveByUserAsync(long userId)`, `Task<T> CreateAsync(T key)`, `Task<T> UpdateAsync(T key)`
- [X] T008 Criar `AvaBot.Infra/Repository/McpAccessKeyRepository.cs` implementando `IMcpAccessKeyRepository<McpAccessKey>` sobre `AvaBotContext`, no estilo de `AvaBot.Infra/Repository/UserRepository.cs` (`GetByHashAsync` com `Include(k => k.User)`; `CreateAsync` preenche `CreatedAt = DateTime.UtcNow`)
- [X] T009 [P] Acrescentar em `AvaBot.API/Auth/ClaimsPrincipalExtensions.cs` as extensões `long GetMcpAccessKeyId(this ClaimsPrincipal user)` (claim `mcp_key_id`, lança `UnauthorizedAccessException("Chave de acesso sem identificador")` se ausente) e `string GetMcpAccessKeyName(this ClaimsPrincipal user)` (claim `mcp_key_name`, vazio se ausente)
- [X] T010 [P] Criar `AvaBot.Application/Profiles/McpAccessKeyProfile.cs` (`McpAccessKey → McpAccessKeyInfo`) e `AvaBot.Application/Profiles/McpProfile.cs` (`Agent → AgentMcpSummaryInfo` e `Agent → AgentMcpInfo` com `active = Status == 1`, `hasOpenAIApiKey = !string.IsNullOrEmpty(OpenAIApiKeyEncrypted)`, `telegramBotConfigured = !string.IsNullOrEmpty(TelegramBotToken)`, `whatsappConfigured = !string.IsNullOrEmpty(WhatsappToken)`; `ChatMessage → ChatMessageMcpInfo` com `role` = `visitante` para `SenderType.User` e `agente` para `Assistant`; `ChatSession → ChatSessionMcpInfo` ignorando `channel` e `messageCount`, preenchidos pelo service); conferir que os perfis entram no registro do `IMapper` em `AvaBot.Application/DependencyInjection.cs`
- [X] T011 Criar `AvaBot.Application/Services/McpAccessKeyService.cs` com `Task<(McpAccessKey Key, string PlainKey)> CreateAsync(long userId, string name)` (trim; `CountActiveByUserAsync >= 20` → `InvalidOperationException("Limite de 20 chaves ativas por conta. Revogue uma chave antes de criar outra")`; gera a chave, grava só `KeyHash` e `KeyPrefix` = 8 primeiros caracteres após o prefixo), `Task<List<McpAccessKey>> ListAsync(long userId)`, `Task<McpAccessKey> RevokeAsync(long userId, long keyId)` (`KeyNotFoundException("Chave nao encontrada")`; idempotente se já revogada; `Status = 0`, `RevokedAt = UtcNow`) e `Task<McpAccessKey?> AuthenticateAsync(string plainKey)` (`HasExpectedFormat` → `ComputeHash` → `GetByHashAsync`; `null` se inexistente, `!IsActive` ou `User.Status != 1`; toca `LastUsedAt` só se nulo ou mais antigo que 1 minuto); nunca logar `plainKey` (contrato access-keys-api.md, research R3)
- [X] T012 Criar `AvaBot.API/Auth/McpAccessKeyAuthenticationHandler.cs` (`AuthenticationHandler<AuthenticationSchemeOptions>`, esquema `"McpAccessKey"`): lê `Authorization: Bearer <chave>`, chama `McpAccessKeyService.AuthenticateAsync`, em sucesso emite `ClaimsPrincipal` com `ClaimTypes.NameIdentifier`/`sub` = `UserId`, `email`, `name`, `mcp_key_id`, `mcp_key_name`; em qualquer falha `AuthenticateResult.Fail` e `HandleChallengeAsync` respondendo `401` com `WWW-Authenticate: Bearer` e corpo JSON `{ "sucesso": false, "mensagem": "Chave de acesso invalida ou revogada" }` (sempre o mesmo, US1 cenário 5; contrato mcp-server.md)
- [X] T013 Registrar em `AvaBot.Application/DependencyInjection.cs`: `IMcpAccessKeyRepository<McpAccessKey> → McpAccessKeyRepository` (scoped) e `McpAccessKeyService` (scoped), nas seções de repositórios e services (os services `SessionQueryService` e `KnowledgeFileService` são registrados nas histórias US3 e US5)
- [X] T014 [P] Criar em `AvaBot.API/Mcp/`: `McpJson.cs` (`static JsonSerializerOptions Options` com `PropertyNamingPolicy = CamelCase`, `DefaultIgnoreCondition = WhenWritingNull`, `JsonStringEnumConverter`), `McpServerInstructions.cs` (`const string Text` com o texto exato de `initialize` em contracts/mcp-server.md) e `McpCaller.cs` (`record McpCaller(long UserId, long KeyId, string KeyName)` com `static McpCaller From(RequestContext<CallToolRequestParams> context)` lendo `context.User` via as extensões de T009; `context.User == null` → `McpException("Chave de acesso invalida ou revogada")`) (research R4)
- [X] T015 [P] Criar `AvaBot.API/Mcp/McpSecretGuardFilter.cs` com `static readonly HashSet<string> ForbiddenKeys` (`openAIApiKey`, `removeOpenAIApiKey`, `apiKey`, `telegramBotToken`, `telegramWebhookSecret`, `whatsappToken`, `tenantId`, `directoryId`, `clientId`, `appId`, `clientSecret`, `secret`, `token`, `password`, `senha`, comparação `OrdinalIgnoreCase`) e `static McpRequestFilter<CallToolRequestParams, CallToolResult> Create()` (assinatura conforme `AddCallToolFilter` do SDK): percorre as chaves de primeiro nível de `context.Params.Arguments`; na primeira proibida devolve `CallToolResult { IsError = true }` com o texto do contrato mcp-server.md ("Credenciais (...) sao informadas apenas pelo usuario no painel ... Remova o campo '<nome>' e repita a operacao.") **sem** chamar `next`; senão `await next(context, ct)` (research R5)
- [X] T016 [P] Criar `AvaBot.API/Mcp/McpToolExecutionFilter.cs` com `Create()` que: mede `Stopwatch`; `try { return await next(...) }`; mapeia `KeyNotFoundException`, `InvalidOperationException`, `ArgumentException`, `PowerBIApiException` → `IsError = true` com `ex.Message`; `FluentValidation.ValidationException` → mensagens dos erros unidas por `"; "`; `AgentTestFailedException` → mensagem + resultado parcial serializado com `McpJson.Options` no mesmo `content`; `McpException` relança; qualquer outra → `"Erro interno ao executar {tool}. Tente de novo; se persistir, avise o usuario."` com `LogError` do detalhe; em `finally` escreve `MCP tool={name} userId={id} mcpKeyId={id} agentId={id|-} sucesso={bool} durationMs={n}` em `Information` quando `context.MatchedPrimitive` tem `ReadOnly != true` e em `Debug` caso contrário, lendo `agentId` só se existe em `Arguments` e **nunca** logando outros argumentos (research R6, FR-019)
- [X] T017 Em `AvaBot.API/Program.cs`: encadear `.AddScheme<AuthenticationSchemeOptions, McpAccessKeyAuthenticationHandler>("McpAccessKey", null)` no `AddAuthentication` existente (sem mudar o padrão `Bearer` JWT); `AddAuthorization` com a política `McpAccessKeyPolicy` (`AddAuthenticationSchemes("McpAccessKey").RequireAuthenticatedUser()`); `AddMcpServer(o => { ServerInfo = new() { Name = "avabot", Title = "AvaBot", Version = <versão do assembly da API> }; ServerInstructions = McpServerInstructions.Text; }).WithHttpTransport().WithRequestFilters(f => { f.AddCallToolFilter(McpSecretGuardFilter.Create()); f.AddCallToolFilter(McpToolExecutionFilter.Create()); }).AddAuthorizationFilters()` (as linhas `WithTools<...>(McpJson.Options)` entram em cada história); `app.MapMcp("/mcp").RequireAuthorization("McpAccessKeyPolicy")` depois de `UseAuthentication`/`UseAuthorization`; conferir que `[Authorize]` do painel continua só com JWT (contrato mcp-server.md, snippet "Configuração do servidor")
- [X] T018 [P] Criar `AvaBot.Tests/Domain/McpAccessKeyTest.cs`: `GenerateKey` começa com `avabot_mcp_`, tem 54 caracteres, só `[A-Za-z0-9_-]` depois do prefixo e duas chamadas diferem; `ComputeHash` é determinístico, 64 hexa minúsculos; `HasExpectedFormat` falso para vazio, JWT, prefixo certo com tamanho errado
- [X] T019 [P] Criar `AvaBot.Tests/Application/Services/McpAccessKeyServiceTest.cs` (Moq do repositório, estilo de `UserServiceTest.cs`): `CreateAsync` devolve o valor em claro uma vez e grava só hash e prefixo (nunca a chave); limite de 20 ativas lança `InvalidOperationException` com a mensagem do contrato; `AuthenticateAsync` aceita chave ativa com dono ativo, recusa (`null`) chave revogada, inexistente, formato errado e dono inativo; atualiza `LastUsedAt` quando nulo ou > 1 min e não chama `UpdateAsync` quando < 1 min; `RevokeAsync` de id de outro usuário lança `KeyNotFoundException` e é idempotente
- [X] T020 [P] Criar `AvaBot.Tests/API/Mcp/McpSecretGuardFilterTest.cs`: para cada nome da lista proibida, em minúsculas, maiúsculas e camelCase, o filtro devolve `IsError = true` com a mensagem contendo o nome do campo e **não** invoca `next`; argumentos permitidos (`agentId`, `name`, `systemPrompt`, `telegramBotName`) invocam `next`; `Arguments` nulo invoca `next`
- [X] T021 [P] Criar `AvaBot.Tests/API/Mcp/McpToolExecutionFilterTest.cs`: cada exceção do mapa vira `IsError = true` com a mensagem esperada; `ValidationException` com dois erros une por `"; "`; exceção desconhecida vira a mensagem genérica sem o detalhe; sucesso repassa o `CallToolResult` de `next`; log (via `ILogger` fake) contém `tool=`, `userId=`, `mcpKeyId=`, `sucesso=` e **não** contém o valor de `systemPrompt` passado nos argumentos
- [X] T022 [P] Criar `AvaBot.Tests/API/Mcp/McpOutputDtoTest.cs`: por reflexão sobre todos os tipos públicos de `AvaBot.DTO/McpDTOs.cs` (e sobre `PowerBIDatasetInfo`, `PowerBIConnectionTestInfo`, `KnowledgeFileInfo`, `AgentTestResultInfo`, `PowerBIQueryLogInfo`, reutilizados), nenhuma propriedade nem `[JsonPropertyName]` casa com `McpSecretGuardFilter.ForbiddenKeys` nem contém `secret`, `token`, `apikey`, `tenant`, `clientid` (sem diferenciar caixa); serializar uma instância preenchida de `AgentMcpInfo` e `PowerBIMcpInfo` com `McpJson.Options` e conferir ausência das mesmas palavras (SC-002)

**Checkpoint**: `dotnet build AvaBot.sln` verde; migração aplicada; `dotnet test AvaBot.Tests --filter McpAccessKey` verde; `curl -X POST http://localhost:5000/mcp` sem cabeçalho → `401` com o corpo do contrato; com uma chave válida inserida à mão no banco, `tools/list` responde lista vazia.

---

## Phase 3: User Story 1 - Conectar um assistente de IA à conta (Priority: P1) 🎯 MVP

**Goal**: criar, listar e revogar chaves em "Minha conta"; o assistente conecta em `/mcp`, descobre as ferramentas e responde `quem_sou_eu`; chave revogada ou inválida recebe a mesma recusa.

**Independent Test**: cenário 1 do quickstart.md: criar a chave no painel, configurar o Claude Code com `claude mcp add --transport http avabot http://localhost:5000/mcp --header "Authorization: Bearer avabot_mcp_..."`, perguntar "quem sou eu no AvaBot" → nome e e-mail; ver "último uso" na lista; revogar e repetir → recusa; chave alterada em um caractere → mesma recusa.

### Backend

- [X] T023 [P] [US1] Criar `AvaBot.API/Validators/McpAccessKeyInsertInfoValidator.cs` (`AbstractValidator<McpAccessKeyInsertInfo>`): `name` obrigatório após `Trim`, 1–100 caracteres, mensagens `"Nome e obrigatorio"` / `"Nome deve ter no maximo 100 caracteres"`; registrar na DI junto dos demais validators em `AvaBot.API/Program.cs`
- [X] T024 [US1] Criar `AvaBot.API/Controllers/McpAccessKeyController.cs` (`[ApiController] [Route("auth/me/mcp-keys")] [Authorize]`, esquema JWT padrão): `GET` → `ListAsync(User.GetUserId())` → `200 Result<List<McpAccessKeyInfo>>.Success("Chaves listadas com sucesso")`; `POST` → validator → `CreateAsync` → `201 Created` com `Location` e `Result<McpAccessKeyCreatedInfo>.Success("Chave criada. Copie o valor agora: ele nao sera exibido de novo")`, `InvalidOperationException` → `400`; `DELETE {id:long}` → `RevokeAsync` → `200 Result<McpAccessKeyInfo>.Success("Chave revogada")`, `KeyNotFoundException` → `404 "Chave nao encontrada"`; `UnauthorizedAccessException` → `401`; `try/catch → 500`; nunca logar `dados.key` (contrato access-keys-api.md)
- [X] T025 [US1] Criar `AvaBot.API/Mcp/Tools/AccountTools.cs` (`[McpServerToolType]`, classe `sealed`) com `[McpServerTool(Name = "quem_sou_eu", Title = "Quem sou eu", ReadOnly = true, Idempotent = true)]` e `[Description]` em português: `McpCaller.From(context)` → `UserService.GetAsync(userId)` (obtido de `context.Services`) → `McpIdentityInfo { userId, name, email, accessKeyName }`; usuário nulo → `McpException("Chave de acesso invalida ou revogada")`; registrar `.WithTools<AccountTools>(McpJson.Options)` em `AvaBot.API/Program.cs` (contrato mcp-tools.md #1)
- [X] T026 [P] [US1] Criar `AvaBot.Tests/API/Controllers/McpAccessKeyControllerTest.cs` (estilo de `AuthControllerTest.cs` com `ControllerTestHelpers`): `POST` devolve `201` com `dados.key` e `accessKey.keyPrefix`; `POST` com nome vazio → `400 "Nome e obrigatorio"`; limite → `400` com a mensagem do service; `DELETE` inexistente → `404 "Chave nao encontrada"`; `GET` devolve a lista do service sem campo `keyHash`
- [X] T027 [P] [US1] Criar `AvaBot.Tests.API/Controllers/McpAccessKeyControllerTest.cs` (Flurl, `TestBase`): criar chave e conferir que `dados.key` começa com `avabot_mcp_` e tem 54 caracteres; listar e encontrar a chave com `lastUsedAt = null`; revogar e conferir `status = 0` e `revokedAt` preenchido; `GET /auth/me/mcp-keys` sem JWT → `401`; `GET /auth/me` com `Authorization: Bearer <chave MCP>` → `401` (esquemas não se misturam)
- [X] T028 [US1] Criar `AvaBot.Tests.API/Controllers/McpServerTest.cs` usando `McpClient.CreateAsync` com `HttpClientTransport` (`ModelContextProtocol` 2.2.0) apontando para `{BaseUrl}/mcp` com `Authorization: Bearer <chave criada via POST /auth/me/mcp-keys no setup>`: `initialize` devolve `serverInfo.name == "avabot"` e `instructions` não vazio; `tools/list` contém `quem_sou_eu`; `quem_sou_eu` devolve `name` e `email` iguais a `GET /auth/me`; depois de `DELETE` da chave, a próxima chamada falha com `401`; chave com um caractere trocado → `401` com a mesma mensagem; JWT do painel em `/mcp` → `401`; `GET /mcp` → `405`; revogar a chave no `Dispose` (research R10; os demais testes de ferramentas são acrescentados neste arquivo nas histórias seguintes)

### Frontend

- [X] T029 [P] [US1] Criar `frontend/src/types/mcpAccessKey.ts` com `interface McpAccessKeyInfo { mcpAccessKeyId; name; keyPrefix; status; createdAt; lastUsedAt: string | null; revokedAt: string | null }` e `interface McpAccessKeyCreatedInfo { key: string; accessKey: McpAccessKeyInfo }`
- [X] T030 [US1] Criar `frontend/src/Services/McpAccessKeyService.ts` (objeto no estilo de `frontend/src/Services/AuthService.ts`) com `list(): Promise<Result<McpAccessKeyInfo[]>>`, `create(name): Promise<Result<McpAccessKeyCreatedInfo>>` e `revoke(id): Promise<Result<McpAccessKeyInfo>>` contra `${VITE_API_URL}/auth/me/mcp-keys`, usando `AuthService.getAuthHeaders()` e o tratamento de `401` existente; nunca escrever `dados.key` no console
- [X] T031 [US1] Criar `frontend/src/stores/useMcpAccessKeyStore.ts` (Zustand) com `keys`, `loading`, `error`, `loadKeys()`, `createKey(name)` (devolve o `Result` com a chave em claro ao chamador e recarrega a lista, **sem** guardar `key` no estado) e `revokeKey(id)` (atualiza o item na lista)
- [X] T032 [P] [US1] Criar `frontend/src/components/admin/McpKeyCreatedModal.tsx` (estilo de `OpenAIConnectionDiagnosticModal`): recebe `created: McpAccessKeyCreatedInfo` e `onClose`; mostra a chave em claro com botão "Copiar" (`navigator.clipboard`, toast sonner), aviso "Esta é a única vez que a chave será exibida", o endereço `${import.meta.env.VITE_API_URL}/mcp`, e abas com os trechos de configuração de Claude Code (`claude mcp add --transport http avabot <endereço> --header "Authorization: Bearer <chave>"`), Claude Desktop / claude.ai (conector remoto com cabeçalho), Cursor (`mcp.json` com `url` e `headers`) e VS Code (`.vscode/mcp.json` tipo `http`), já com a chave e o endereço preenchidos
- [X] T033 [US1] Criar `frontend/src/components/admin/McpAccessKeysPanel.tsx`: usa o store; formulário "Nome da chave" + botão "Criar chave" (validação 1–100; em sucesso abre `McpKeyCreatedModal` com o `Result` em estado local do componente); lista com nome, prefixo (`avabot_mcp_<prefixo>…`), criada em, "Último uso" ou "nunca usada", badge "revogada" quando `status === 0`; botão "Revogar" (confirmação, toast "Chave revogada"); estados de loading, vazio ("Nenhuma chave criada") e erro
- [X] T034 [US1] Em `frontend/src/pages/admin/AccountPage.tsx` acrescentar a seção "Assistentes de IA (MCP)" abaixo dos cards atuais, com um parágrafo explicando o que é e o link para `docs/MCP_SETUP.md` no repositório, renderizando `McpAccessKeysPanel`; carregar as chaves no `useEffect` existente junto com `refreshUser`

**Checkpoint**: `dotnet test AvaBot.Tests --filter "McpAccessKey|McpSecretGuard|McpToolExecution|McpOutputDto"` verde; `dotnet test AvaBot.Tests.API --filter "McpAccessKeyController|McpServerTest"` verde com a API no ar; `npm run lint && npm run build` verdes; Independent Test da US1 passa com o Claude Code.

---

## Phase 4: User Story 2 - Criar e editar agentes pelo assistente (Priority: P1)

**Goal**: o assistente lista, lê, cria, edita parcialmente, ativa e desativa agentes da conta; não existe exclusão; chave de API nunca aparece nem é aceita; agente alheio responde "nao encontrado".

**Independent Test**: cenários 2 e 3 do quickstart.md: criar "Suporte Loja" pelo assistente, conferir em `/admin/agents`; "deixe o tom mais formal" e "troque o modelo" → painel reflete; desativar/reativar; "exclua o agente" → não há ferramenta; "configure a chave de API sk-teste" → recusa e prompt intacto; segunda conta → `obter_agente` do id alheio → "Agente nao encontrado".

- [X] T035 [US2] Acrescentar em `AvaBot.Application/Services/AgentService.cs`: `Task<Agent> UpdateProfileAsync(long id, AgentProfileUpdateInfo info, long ownerUserId)` (`GetOwnedByIdAsync` nulo → `KeyNotFoundException("Agente nao encontrado")`; copia só os campos **não nulos** de `name`, `description`, `systemPrompt`, `chatModel`, `collectName`, `collectEmail`, `collectPhone`, `telegramBotName`; `name` novo e diferente → `GenerateUniqueSlugAsync` como `UpdateAsync` faz hoje; `name` em branco → `ArgumentException("Nome e obrigatorio")`, `name` > 260 → `ArgumentException("Nome deve ter no maximo 260 caracteres")`, `systemPrompt` em branco → `ArgumentException("Prompt de sistema e obrigatorio")` (mesmas mensagens de `AgentInsertInfoValidator`); **nunca** toca `OpenAIApiKeyEncrypted`, `TelegramBotToken`, `TelegramWebhookSecret`, `WhatsappToken`, `PowerBIEnabled`, `Status`, `OwnerUserId`; `UpdatedAt = UtcNow`) e `Task<Agent> SetActiveAsync(long id, bool active, long ownerUserId)` (`Status = active ? 1 : 0`, idempotente, mesma exceção para alheio) (research R7)
- [X] T036 [P] [US2] Acrescentar em `AvaBot.Tests/Application/Services/AgentServiceTest.cs`: `UpdateProfileAsync` com só `systemPrompt` mantém nome, modelo, token do Telegram e chave OpenAI criptografada; `name` novo regenera o slug; `name` em branco lança sem gravar; agente de outro dono lança `KeyNotFoundException("Agente nao encontrado")`; `SetActiveAsync(false)` grava `Status = 0` e `SetActiveAsync(true)` duas vezes mantém `1`
- [X] T037 [US2] Criar `AvaBot.API/Mcp/Tools/AgentTools.cs` (`[McpServerToolType]`) com as ferramentas `listar_agentes` (ReadOnly; `AgentService.GetAllAsync(owner)` → `List<AgentMcpSummaryInfo>` via `IMapper`), `obter_agente` (ReadOnly; `GetOwnedByIdAsync` nulo → `KeyNotFoundException("Agente nao encontrado")`), `criar_agente` (recebe os campos de `AgentMcpCreateInfo` como parâmetros individuais com `[Description]`; monta `AgentInsertInfo` **sem** `openAIApiKey`/`telegramBotToken`, `chatModel` padrão `gpt-4o`; valida com `IValidator<AgentInsertInfo>` de `context.Services` e lança `ValidationException`; `AgentService.CreateAsync(info, owner)`), `atualizar_agente` (Idempotent; parâmetros anuláveis → `AgentProfileUpdateInfo` → `UpdateProfileAsync`; descrição do schema avisa "Nao aceita chave de API nem tokens; esses campos sao recusados e nada e gravado") e `definir_agente_ativo` (Idempotent; `SetActiveAsync`); todas devolvem `AgentMcpInfo`; nenhuma com nome começando por `excluir_`/`remover_`/`deletar_`; registrar `.WithTools<AgentTools>(McpJson.Options)` em `AvaBot.API/Program.cs` (contrato mcp-tools.md #2–#6)
- [X] T038 [P] [US2] Criar `AvaBot.Tests/API/Mcp/AgentToolsTest.cs` (services e `IValidator` mockados; `RequestContext` montado com um `ClaimsPrincipal` de teste e um `ServiceProvider` com os mocks): `criar_agente` passa pelo validator e nunca preenche `OpenAIApiKey`/`TelegramBotToken` no `AgentInsertInfo`; `obter_agente` de id inexistente lança `KeyNotFoundException` com `"Agente nao encontrado"`; `obter_agente` devolve `AgentMcpInfo` com `hasOpenAIApiKey = true` e sem nenhuma propriedade de token; `listar_agentes` mapeia `active` a partir de `Status`
- [X] T039 [US2] Acrescentar em `AvaBot.Tests.API/Controllers/McpServerTest.cs`: `tools/list` não contém ferramenta com `excluir`, `delete`, `remover_agente` ou `deletar` no nome e nenhum `inputSchema` contém propriedade da lista proibida (percorrer o JSON de todos os schemas); ciclo `criar_agente` → `listar_agentes` (aparece) → `obter_agente` → `atualizar_agente` só `systemPrompt` (nome mantido) → `definir_agente_ativo(false)` → `(true)`; `atualizar_agente` com `openAIApiKey` nos argumentos → `isError = true` com "Credenciais" na mensagem e `obter_agente` em seguida mostra o prompt inalterado; `obter_agente` com id `999999999` → `isError` "Agente nao encontrado"; limpar o agente criado via `DELETE /agents/{id}` (painel) no fim

**Checkpoint**: MVP completo (US1 + US2). `dotnet test AvaBot.Tests` e `dotnet test AvaBot.Tests.API --filter McpServerTest` verdes; Independent Test da US2 passa com o Claude Code e o painel mostra o agente com os mesmos dados.

---

## Phase 5: User Story 3 - Ler logs e testar o agente (Priority: P2)

**Goal**: sessões, mensagens e logs de consulta BI (com filtro por situação) paginados; pergunta de teste com rastro, sem criar sessão. Painel e MCP passam a executar o mesmo código de sessões.

**Independent Test**: cenário 4 do quickstart.md: num agente com conversas, "o que perguntaram ontem e onde respondeu mal" → `listar_sessoes` + `listar_mensagens`; "liste as consultas com erro" → `listar_consultas_bi` com `status = error`; "teste a pergunta X" → resposta com rastro, nenhuma sessão nova em `/admin/sessions`, consulta do teste visível em `/admin/powerbi` → logs.

- [X] T040 [P] [US3] Acrescentar o parâmetro `PowerBIQueryStatus? status = null` em `GetPagedByAgentAsync(...)` de `AvaBot.Infra.Interfaces/Repository/IPowerBIQueryLogRepository.cs` (manter o tipo de retorno atual) e aplicar `Where(l => l.Status == status)` quando informado em `AvaBot.Infra/Repository/PowerBIQueryLogRepository.cs`
- [X] T041 [P] [US3] Acrescentar `Task<HashSet<long>> GetSessionIdsAsync(IEnumerable<long> chatSessionIds)` em `AvaBot.Infra.Interfaces/Repository/ITelegramChatRepository.cs` e implementar em `AvaBot.Infra/Repository/TelegramChatRepository.cs` (`Where(c => c.ChatSessionId != null && ids.Contains(c.ChatSessionId.Value)).Select(...)`), devolvendo vazio para lista vazia sem ir ao banco
- [X] T042 [US3] Atualizar `AvaBot.Calibration/Local/LocalRepositories.cs` para as assinaturas novas de T040 (ignorar o `status` ou filtrar na lista em memória) e T041 (devolver `HashSet` vazio), mantendo o projeto compilando (data-model.md, "Alterações só de leitura")
- [X] T043 [US3] Acrescentar `PowerBIQueryStatus? status = null` em `PowerBIService.GetQueryLogsAsync(slug, ownerUserId, page, pageSize, status)` em `AvaBot.Application/Services/PowerBIService.cs` repassando ao repositório; em `AvaBot.API/Controllers/PowerBIController.cs` o `GET {slug}/query-logs` ganha `[FromQuery] string? status` aceitando `success` | `error` | `timeout` (outro valor → `400 Result.Failure("Status invalido. Use success, error ou timeout")`); acrescentar em `AvaBot.Tests/Application/Services/PowerBIServiceTest.cs` um caso em que o `status` é repassado ao repositório (mudança aditiva; contrato mcp-server.md, "Compatibilidade")
- [X] T044 [US3] Criar `AvaBot.Application/Services/SessionQueryService.cs` com `Task<PaginatedResult<ChatSessionMcpInfo>> GetSessionsAsync(long agentId, long ownerUserId, int page, int pageSize)` (agente via `IAgentRepository.GetByIdAsync(agentId, ownerUserId)` nulo → `KeyNotFoundException("Agente nao encontrado")`; `pageSize` limitado a 100 e padrão 20, `page` mínimo 1; `GetByAgentIdAsync` + `CountByAgentIdAsync`; `messageCount` = `CountBySessionIdAsync` por sessão; `channel` = `telegram` para ids devolvidos por `ITelegramChatRepository.GetSessionIdsAsync`, senão `web`; `maxPage = ceil(total / pageSize)`) e `Task<PaginatedResult<ChatMessageMcpInfo>> GetMessagesAsync(long sessionId, long ownerUserId, int page, int pageSize)` (sessão via `IChatSessionRepository.GetByIdAsync`; sessão nula ou cujo `AgentId` não é de um agente do dono → `KeyNotFoundException("Sessao nao encontrada")`; `pageSize` máximo 200 e padrão 50; ordem cronológica); mover para cá a lógica de paginação e validação de dono que hoje está em `SessionController.GetSessions/GetMessages`; registrar `SessionQueryService` (scoped) em `AvaBot.Application/DependencyInjection.cs` (research R7)
- [X] T045 [US3] Refatorar `GetSessions` e `GetMessages` em `AvaBot.API/Controllers/SessionController.cs` para delegar ao `SessionQueryService` mantendo rota, parâmetros de query (`page`, `maxPage`), códigos, envelope `Result<PaginatedResult<...>>`, DTOs de resposta e mensagens **exatamente** como hoje (o service pode expor uma sobrecarga que devolve as entidades paginadas e o controller mapeia para os DTOs atuais de `ChatDTOs.cs` como antes; escolher a opção que mantém `AvaBot.Tests/API/Controllers/SessionControllerTest.cs` e `AvaBot.Tests.API/Controllers/ChatSessionControllerTest.cs` passando sem alteração de asserções); `StartSession` e `ResumeSession` não mudam
- [X] T046 [P] [US3] Criar `AvaBot.Tests/Application/Services/SessionQueryServiceTest.cs` (repositórios mockados): agente de outro dono → `KeyNotFoundException("Agente nao encontrado")`; `pageSize = 500` é reduzido a 100 e `maxPage` calculado; `channel = telegram` para a sessão devolvida por `GetSessionIdsAsync` e `web` para as demais; `GetMessagesAsync` de sessão cujo agente pertence a outro dono → `"Sessao nao encontrada"`; `role` = `visitante`/`agente` conforme `SenderType`
- [X] T047 [US3] Criar `AvaBot.API/Mcp/Tools/ConversationTools.cs` (`[McpServerToolType]`) com `listar_sessoes` (ReadOnly; `agentId`, `page = 1`, `pageSize = 20` → `SessionQueryService.GetSessionsAsync`), `listar_mensagens` (ReadOnly; `sessionId`, `page = 1`, `pageSize = 50` → `GetMessagesAsync`), `listar_consultas_bi` (ReadOnly; `agentId`, `page`, `pageSize = 20`, `status?` string → `PowerBIQueryStatus?`, valor inválido → `ArgumentException("Status invalido. Use success, error ou timeout")`; resolve o slug com `AgentService.GetOwnedByIdAsync` → `"Agente nao encontrado"`; `PowerBIService.GetQueryLogsAsync`; descrição avisa que inclui consultas de perguntas de teste) e `testar_agente` (ReadOnly; `agentId`, `question`, `history?` como `List<AgentTestMessageInfo>`; valida `question` não vazia e histórico com papéis `user`/`assistant` e conteúdo não vazio com as **mesmas mensagens** de `AgentController` `POST {id}/test`; `ChatService.TestMessageAsync(id, agent.ChatModel, agent.SystemPrompt, question, history)`; descrição avisa que não cria conversa mas as consultas BI entram no log); registrar `.WithTools<ConversationTools>(McpJson.Options)` em `AvaBot.API/Program.cs` (contrato mcp-tools.md #7–#10)
- [X] T048 [P] [US3] Criar `AvaBot.Tests/API/Mcp/ConversationToolsTest.cs`: `testar_agente` com histórico de papel `system` lança `ArgumentException` com a mesma mensagem do controller e não chama `ChatService`; `listar_consultas_bi` com `status = "falha"` lança `"Status invalido..."`; `status = "error"` repassa `PowerBIQueryStatus.Error`; `listar_sessoes` repassa `page`/`pageSize` ao service
- [X] T049 [US3] Acrescentar em `AvaBot.Tests.API/Controllers/McpServerTest.cs`: `listar_sessoes` do agente criado no teste devolve `items` vazio com `total = 0`; `listar_mensagens` com `sessionId = 999999999` → `isError` "Sessao nao encontrada"; `listar_consultas_bi` com `status = "error"` responde sem erro; `testar_agente` num agente sem chave OpenAI responde `isError` com mensagem legível (não `Erro interno`) **ou**, se a chave do servidor estiver configurada, devolve `answer`; e rodar `dotnet test AvaBot.Tests.API --filter ChatSessionControllerTest` para confirmar a refatoração do painel

**Checkpoint**: `dotnet test AvaBot.Tests` verde incluindo `SessionControllerTest` inalterado; `GET /sessions/agents/{id}` e `GET /sessions/{id}/messages` respondem como antes no Bruno (`bruno/Chat Sessions/`); Independent Test da US3 passa.

---

## Phase 6: User Story 4 - Configurar Power BI e canais sem tocar em segredos (Priority: P2)

**Goal**: as 11 ferramentas de Power BI e as 3 de canais, todas por `agentId`, com saída sem `tenantId`/`clientId`/`clientSecretMasked`/token, e orientação clara quando falta credencial ou token.

**Independent Test**: cenário 5 do quickstart.md: num agente com credenciais BI salvas no painel, "ligue o Power BI, liste os workspaces, vincule o dataset X, gere o schema e descreva as colunas de data" → sequência de ferramentas refletida no painel; "me mostre a configuração do Power BI" → só ligado/credencial configurada/último teste; "grave o Directory ID 1234" → recusa; agente sem credencial → orientação; Telegram: situação sem token e registro do webhook; WhatsApp: situação.

- [X] T050 [US4] Criar `AvaBot.API/Mcp/Tools/PowerBITools.cs` (`[McpServerToolType]`) com um helper privado `ResolveSlugAsync(agentId, owner)` (`AgentService.GetOwnedByIdAsync` → `"Agente nao encontrado"`) e um `RequireCredentialAsync(slug, owner)` (`PowerBIService.GetConfigAsync` com credencial ausente → `InvalidOperationException("O Power BI deste agente ainda nao tem credencial. Peca ao usuario para informar Directory ID, App ID e segredo no painel, em Agentes > <nome> > Power BI, e tente de novo.")`), e as ferramentas `obter_powerbi` (ReadOnly; `GetConfigAsync` + `GetDatasetsAsync` projetados para `PowerBIMcpInfo` **sem** copiar `tenantId`/`clientId`/`clientSecretMasked`), `definir_powerbi_ativo` (Idempotent; `enabled = true` sem credencial → a mensagem acima; `SetEnabledAsync`), `testar_conexao_powerbi` (ReadOnly; credencial obrigatória; `TestConnectionAsync`), `listar_workspaces_powerbi` (ReadOnly; credencial obrigatória; `ListWorkspacesAsync`), `listar_datasets_powerbi` (ReadOnly; `GetDatasetsAsync`), `vincular_dataset_powerbi` (credencial obrigatória; `IValidator<PowerBIDatasetInsertInfo>` → `CreateDatasetAsync`), `atualizar_dataset_powerbi` (Idempotent; carrega o dataset em `GetDatasetsAsync`, preserva `workspaceId`/`datasetId`, aplica `name`/`description` não nulos → `UpdateDatasetAsync`), `desvincular_dataset_powerbi` (Destructive; `DeleteDatasetAsync` → `{ message }`), `gerar_schema_powerbi` (credencial obrigatória; `GenerateSchemaAsync`), `obter_schema_powerbi` (ReadOnly; `GetSchemaAsync`) e `descrever_schema_powerbi` (Idempotent; `items` → `PowerBISchemaDescriptionUpdateInfo` → `UpdateSchemaDescriptionsAsync`); registrar `.WithTools<PowerBITools>(McpJson.Options)` em `AvaBot.API/Program.cs` (contrato mcp-tools.md #11–#21)
- [X] T051 [US4] Criar `AvaBot.API/Mcp/Tools/ChannelTools.cs` (`[McpServerToolType]`) com `obter_telegram` (ReadOnly; agente via `GetOwnedByIdAsync`; `TelegramBotToken` vazio → `TelegramMcpInfo { botName, botConfigured = false, webhookRegistered = false, webhookUrl = null }` **sem** chamar o Telegram; senão `TelegramService.GetWebhookInfoAsync(agentId, owner)` → `webhookRegistered = !string.IsNullOrEmpty(url)`, `webhookUrl`), `registrar_webhook_telegram` (Idempotent; token vazio → `InvalidOperationException("Este agente nao tem bot do Telegram configurado. Peca ao usuario para informar o token no painel, em Agentes > <nome> > Telegram.")`; `SetupWebhookAsync` → `TelegramMcpInfo`) e `obter_whatsapp` (ReadOnly; slug via `GetOwnedByIdAsync`; `WhatsappService.GetStatusAsync(slug, owner)` → `WhatsappMcpInfo { status, isConnected }`; exceção do WPP Connect → `InvalidOperationException("Nao foi possivel consultar o WhatsApp: <motivo>")`); nenhuma ferramenta de token, segredo, QR code, start-session ou disconnect; registrar `.WithTools<ChannelTools>(McpJson.Options)` em `AvaBot.API/Program.cs` (contrato mcp-tools.md #22–#24)
- [X] T052 [P] [US4] Criar `AvaBot.Tests/API/Mcp/PowerBIToolsTest.cs` (`PowerBIService` com repositórios e `IPowerBIClient` mockados, conforme o padrão de `PowerBIServiceTest.cs`): `obter_powerbi` devolve `PowerBIMcpInfo` cujo JSON serializado com `McpJson.Options` não contém `tenantId`, `clientId` nem `clientSecret`; `definir_powerbi_ativo(true)` sem credencial lança a mensagem de orientação e não chama `SetEnabledAsync`; `listar_workspaces_powerbi` sem credencial lança antes de chamar o cliente; `atualizar_dataset_powerbi` preserva `workspaceId`/`datasetId`; agente alheio → `"Agente nao encontrado"` em `obter_powerbi`
- [X] T053 [P] [US4] Criar `AvaBot.Tests/API/Mcp/ChannelToolsTest.cs`: `obter_telegram` com token vazio devolve `botConfigured = false` e não chama `TelegramService`; com token chama `GetWebhookInfoAsync` e o JSON de saída não contém `token` nem `secret`; `registrar_webhook_telegram` sem token lança a mensagem de orientação; `obter_whatsapp` converte exceção do service em `InvalidOperationException` legível
- [X] T054 [US4] Acrescentar em `AvaBot.Tests.API/Controllers/McpServerTest.cs`: `obter_powerbi` do agente de teste devolve `structuredContent` sem as chaves `tenantId`, `clientId`, `clientSecretMasked` e com `credentialConfigured = false`; `definir_powerbi_ativo` com `enabled = true` → `isError` com "credencial" na mensagem; chamada de `obter_powerbi` com argumento extra `tenantId` → `isError` do guarda de segredos; `obter_telegram` → `botConfigured = false` sem `token` no JSON; `registrar_webhook_telegram` → `isError` com "token" na mensagem de orientação; `obter_whatsapp` responde (`isError` legível se o WPP Connect não está no ar)

**Checkpoint**: `dotnet test AvaBot.Tests --filter "PowerBITools|ChannelTools"` verde; Independent Test da US4 passa num agente com credencial real salva pelo painel; nenhuma resposta do MCP contém valor de credencial (conferir no Inspector ou no `Call Tool.bru`).

---

## Phase 7: User Story 5 - Manter a base de conhecimento (Priority: P3)

**Goal**: listar, adicionar por texto, reprocessar e remover documentos da base por `agentId`, com os mesmos limites do painel. Painel e MCP passam a executar o mesmo código de arquivos.

**Independent Test**: cenário 6 do quickstart.md: "adicione na base do agente Suporte Loja a política de trocas: ..." → documento aparece em `/admin/knowledge/files` e passa a "pronto"; `testar_agente` com pergunta coberta usa o texto; remover → some; texto > 10 MB → recusa com o limite.

- [X] T055 [US5] Criar `AvaBot.Application/Services/KnowledgeFileService.cs` com `Task<List<KnowledgeFile>> ListAsync(long agentId, long ownerUserId)` (agente via `IAgentRepository.GetByIdAsync(agentId, ownerUserId)` nulo → `KeyNotFoundException("Agente nao encontrado")`), `Task<KnowledgeFile> AddTextAsync(long agentId, long ownerUserId, string fileName, string content)` (nome obrigatório → `ArgumentException("Nome do arquivo e obrigatorio")`; acrescenta `.md` se não termina em `.md` sem diferenciar caixa; `Encoding.UTF8.GetByteCount(content) > 10 * 1024 * 1024` → `InvalidOperationException("Documento excede o limite de 10MB")`; conteúdo vazio → `ArgumentException("Conteudo e obrigatorio")`; cria com `ProcessingStatus.Processing`, `FileSize` em bytes; dispara a ingestão em background com `IServiceScopeFactory` exatamente como `FileController.Upload` faz hoje), `Task DeleteAsync(long agentId, long ownerUserId, long fileId)` (arquivo nulo ou de outro agente → `KeyNotFoundException("Documento nao encontrado")`; apaga chunks no Elasticsearch e o registro, como o controller) e `Task<KnowledgeFile> ReprocessAsync(long agentId, long ownerUserId, long fileId)` (mesma validação; volta a `Processing` e dispara a ingestão); mover para cá a lógica de `FileController`; registrar `KnowledgeFileService` (scoped) em `AvaBot.Application/DependencyInjection.cs` (research R7)
- [X] T056 [US5] Refatorar `GetByAgent`, `Upload`, `Delete` e `Reprocess` em `AvaBot.API/Controllers/FileController.cs` para delegar ao `KnowledgeFileService` (o `Upload` continua lendo o `IFormFile`, validando extensão `.md` e tamanho como hoje para manter as mesmas mensagens, e chama `AddTextAsync` com o conteúdo lido), mantendo rotas, códigos, envelope, `KnowledgeFileInfo` e mensagens **exatamente** como hoje; `AvaBot.Tests/API/Controllers/FileControllerTest.cs` e `AvaBot.Tests.API/Controllers/KnowledgeFileControllerTest.cs` devem passar sem alteração de asserções (ajustar só a construção do controller nos testes unitários se a injeção mudou)
- [X] T057 [P] [US5] Criar `AvaBot.Tests/Application/Services/KnowledgeFileServiceTest.cs` (repositórios, `IElasticsearchService` e `IServiceScopeFactory` mockados): `AddTextAsync("politica", ...)` grava `politica.md`; conteúdo de 10 MB + 1 byte lança `"Documento excede o limite de 10MB"` sem gravar; agente de outro dono → `"Agente nao encontrado"`; `DeleteAsync` de arquivo de outro agente → `"Documento nao encontrado"`; `ReprocessAsync` volta o status para `Processing`
- [X] T058 [US5] Criar `AvaBot.API/Mcp/Tools/KnowledgeTools.cs` (`[McpServerToolType]`) com `listar_documentos` (ReadOnly; `ListAsync` → `List<KnowledgeFileInfo>` via `IMapper`), `adicionar_documento` (`agentId`, `fileName`, `content` → `AddTextAsync` → `KnowledgeFileInfo`; descrição informa o limite de 10 MB e que o documento é processado em segundo plano), `reprocessar_documento` (Idempotent; `ReprocessAsync`) e `remover_documento` (Destructive; `DeleteAsync` → `{ message = "Documento removido" }`); registrar `.WithTools<KnowledgeTools>(McpJson.Options)` em `AvaBot.API/Program.cs` (contrato mcp-tools.md #25–#28)
- [X] T059 [US5] Acrescentar em `AvaBot.Tests.API/Controllers/McpServerTest.cs`: `adicionar_documento` com `fileName = "politica"` devolve `fileName = "politica.md"` e status de processamento; `listar_documentos` contém o documento; `remover_documento` responde sem erro e a lista fica vazia; `remover_documento` com id inexistente → `isError` "Documento nao encontrado"; `tools/list` agora tem exatamente 28 ferramentas; e rodar `dotnet test AvaBot.Tests.API --filter KnowledgeFileControllerTest` para confirmar a refatoração do painel

**Checkpoint**: todas as histórias funcionam de ponta a ponta; `tools/list` com 28 ferramentas; `dotnet test AvaBot.Tests` e `dotnet test AvaBot.Tests.API` verdes; `GET/POST/DELETE /files/{agentId}` respondem como antes no Bruno (`bruno/Knowledge Files/`).

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: documentação de conexão (FR-021, SC-001), coleção Bruno, revisão das descrições das ferramentas, verificação final de build, testes e cenários manuais do quickstart.

- [X] T060 [P] Criar `docs/MCP_SETUP.md` (mesmo estilo de `docs/TELEGRAM_SETUP.md`): o que é o MCP do AvaBot; criar a chave em "Minha conta"; endereço (`https://avabot.net/api/mcp` em produção, `http://localhost:5000/mcp` em dev); configuração passo a passo de Claude Code (`claude mcp add --transport http ...`), Claude Desktop e claude.ai (conector remoto com cabeçalho), Cursor, VS Code e ChatGPT; primeira pergunta ("quem sou eu no AvaBot"); tabela das 28 ferramentas com uma linha cada; seção "O que o assistente não pode fazer" (exclusão de agente, credenciais, pareamento do WhatsApp, upload binário); revogação; seção "Deploy" com a exigência do proxy (`proxy_buffering off` e timeout de leitura ≥ 120 s em `/api/mcp`); depuração com `curl`, Bruno e MCP Inspector (research R11, quickstart.md)
- [X] T061 [P] Acrescentar em `README.md` a seção "🔌 MCP para assistentes de IA" (entre "📚 API Documentation" e "🤖 CLI Agent Loader") com o resumo em 5 linhas, o endereço, a regra de segurança (sem exclusão, sem segredos) e o link para `docs/MCP_SETUP.md`; adicionar `docs/MCP_SETUP.md` à lista de documentos e `AvaBot.API/Mcp/` à árvore em "📁 Project Structure"; nenhuma variável nova em "⚙️ Environment Configuration" (só a nota do proxy)
- [X] T062 [P] Criar a pasta `bruno/MCP/` com `folder.bru`, `Create Key.bru` (`POST {{baseUrl}}/auth/me/mcp-keys` com `Authorization: Bearer {{token}}`, corpo `{ "name": "Bruno" }`, `post-response` com `bru.setVar("mcpKey", res.body.dados.key)` e `bru.setVar("mcpKeyId", res.body.dados.accessKey.mcpAccessKeyId)`), `List Keys.bru`, `Revoke Key.bru` (`DELETE .../{{mcpKeyId}}`), `Tools List.bru` e `Call Tool.bru` (`POST {{baseUrl}}/mcp`, cabeçalhos `Authorization: Bearer {{mcpKey}}`, `Content-Type: application/json`, `Accept: application/json, text/event-stream`, corpo JSON-RPC `tools/list` e `tools/call` de `quem_sou_eu`), conforme contrato access-keys-api.md
- [X] T063 Revisar os textos de `[Description]` de todas as classes em `AvaBot.API/Mcp/Tools/` e de `McpServerInstructions.Text`: português claro, cada parâmetro com formato/padrão, cada ferramenta dizendo o que devolve e o que **não** faz (FR-021); conferir no MCP Inspector (`npx @modelcontextprotocol/inspector`) que `tools/list` mostra `title`, `description`, `inputSchema` e as anotações `readOnlyHint`/`idempotentHint`/`destructiveHint` esperadas para as 28 ferramentas (contrato mcp-tools.md)
- [X] T064 Rodar `dotnet build AvaBot.sln` e `dotnet test AvaBot.Tests` na raiz; corrigir qualquer aviso novo ou teste quebrado, inclusive em `AvaBot.Calibration`
- [X] T065 [P] Rodar `npm run lint && npm run build` em `frontend/`; corrigir avisos novos
- [ ] T066 Com a API no ar e `AvaBot.Tests.API/appsettings.json` local, rodar `dotnet test AvaBot.Tests.API` completo (inclui `McpServerTest`, `McpAccessKeyControllerTest` e os testes antigos de sessões e arquivos que validam a refatoração)
- [ ] T067 Executar os 8 cenários de validação manual de `specs/017-mcp-agent-management/quickstart.md` com a API, o frontend e o Claude Code locais (incluindo o cenário 3 com duas contas para SC-004 e o cenário 7 lendo o log da API para FR-019); conferir no log que nenhuma linha contém o valor de uma chave `avabot_mcp_` nem o conteúdo de `systemPrompt`/`content` de documento; registrar o resultado e lembrar no relatório final que o proxy de produção precisa da configuração de `docs/MCP_SETUP.md` → "Deploy" antes do primeiro uso em `https://avabot.net/api/mcp`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências. T002 e T003 em paralelo com T001.
- **Foundational (Phase 2)**: depende de T001–T003. Dentro da fase: T004 → T005 → T006 em sequência; T007, T009, T010, T014, T015, T016, T018 em paralelo após T004; T008 depois de T007; T011 depois de T008; T012 depois de T011 e T009; T013 depois de T011; T017 por último (depende de T012, T014, T015, T016); testes T019–T022 em paralelo depois das classes que cobrem. **Bloqueia todas as histórias.**
- **US1 (Phase 3)**: depende da Phase 2. Backend T023 → T024; T025 em paralelo com T024; T026/T027/T028 depois de T024–T025. Frontend T029 → T030 → T031 → T033 → T034; T032 em paralelo com T030/T031.
- **US2 (Phase 4)**: depende da Phase 2 e de T028 (arquivo `McpServerTest.cs` criado) para T039. T035 → T037 → T039; T036 e T038 em paralelo.
- **US3 (Phase 5)**: depende da Phase 2. T040 e T041 em paralelo → T042 → T043; T044 → T045; T046 em paralelo com T045; T047 depois de T043 e T044; T048 em paralelo com T047; T049 depois de T047.
- **US4 (Phase 6)**: depende da Phase 2 e de US2 só para o teste de API (T054 usa o agente criado em T039). T050 e T051 em paralelo; T052/T053 em paralelo; T054 por último.
- **US5 (Phase 7)**: depende da Phase 2. T055 → T056 e T058; T057 em paralelo; T059 por último.
- **Polish (Phase 8)**: depende de todas as histórias desejadas. T060, T061, T062, T065 em paralelo; T063 depois de todas as classes de ferramentas; T064 → T066 → T067.

### User Story Dependencies

- **US1 (P1)**: só Phase 2. É quem cria o `McpServerTest.cs` usado pelas demais.
- **US2 (P1)**: só Phase 2 (o teste de API reaproveita a chave criada pelo setup de T028).
- **US3 (P2)**: só Phase 2. Independente de US2 (opera agentes já existentes).
- **US4 (P2)**: só Phase 2 para o código; o teste de API usa o agente criado em US2.
- **US5 (P3)**: só Phase 2. Independente das demais.

### Within Each User Story

- Services da Application antes das classes de ferramentas; classes de ferramentas antes do registro em `Program.cs`; testes unitários em paralelo com a implementação que cobrem; teste de API por último.
- `Program.cs` é compartilhado: cada história acrescenta só a sua linha `WithTools<...>`; fazer isso em sequência, não em paralelo.
- `McpServerTest.cs` é compartilhado: cada história acrescenta seus métodos de teste; fazer em sequência.

### Parallel Opportunities

- Phase 1: T001, T002, T003.
- Phase 2: T007, T009, T010, T014, T015, T016, T018 após T004; T019–T022 após as classes.
- US1: T023 com T025; T026, T027 após T024; frontend T029 e T032 com o backend.
- US2: T036, T038 com T035/T037.
- US3: T040, T041; T046, T048 com a implementação.
- US4: T050 com T051; T052 com T053.
- US5: T057 com T055/T056.
- Polish: T060, T061, T062, T065.
- Entre histórias: US3, US4 e US5 tocam arquivos distintos (fora `Program.cs` e `McpServerTest.cs`) e podem avançar em paralelo depois da Phase 2; a extração de `SessionQueryService` (US3) e `KnowledgeFileService` (US5) são independentes uma da outra.

---

## Parallel Example: Phase 2

```bash
# Depois de T004 (entidade), com T005/T006 em andamento:
Task: "T007 IMcpAccessKeyRepository em AvaBot.Infra.Interfaces/Repository/IMcpAccessKeyRepository.cs"
Task: "T009 GetMcpAccessKeyId/Name em AvaBot.API/Auth/ClaimsPrincipalExtensions.cs"
Task: "T010 McpAccessKeyProfile e McpProfile em AvaBot.Application/Profiles/"
Task: "T014 McpJson, McpServerInstructions e McpCaller em AvaBot.API/Mcp/"
Task: "T015 McpSecretGuardFilter em AvaBot.API/Mcp/McpSecretGuardFilter.cs"
Task: "T016 McpToolExecutionFilter em AvaBot.API/Mcp/McpToolExecutionFilter.cs"
Task: "T018 McpAccessKeyTest em AvaBot.Tests/Domain/McpAccessKeyTest.cs"

# Depois de T011, T015, T016:
Task: "T019 McpAccessKeyServiceTest em AvaBot.Tests/Application/Services/McpAccessKeyServiceTest.cs"
Task: "T020 McpSecretGuardFilterTest em AvaBot.Tests/API/Mcp/McpSecretGuardFilterTest.cs"
Task: "T021 McpToolExecutionFilterTest em AvaBot.Tests/API/Mcp/McpToolExecutionFilterTest.cs"
Task: "T022 McpOutputDtoTest em AvaBot.Tests/API/Mcp/McpOutputDtoTest.cs"
```

## Parallel Example: User Story 4

```bash
# Depois da Phase 2:
Task: "T050 PowerBITools em AvaBot.API/Mcp/Tools/PowerBITools.cs"
Task: "T051 ChannelTools em AvaBot.API/Mcp/Tools/ChannelTools.cs"

# Em seguida:
Task: "T052 PowerBIToolsTest em AvaBot.Tests/API/Mcp/PowerBIToolsTest.cs"
Task: "T053 ChannelToolsTest em AvaBot.Tests/API/Mcp/ChannelToolsTest.cs"
```

---

## Implementation Strategy

### MVP First (US1 + US2)

As duas histórias P1 formam o MVP: a chave e o endpoint (US1) sem nenhuma ferramenta de agente não têm uso, e as ferramentas de agente (US2) sem a conexão não têm como ser chamadas.

1. Phase 1 + Phase 2 (migração aplicada, `/mcp` respondendo `401` e `tools/list` vazio com chave válida).
2. Phase 3 (US1) → validar o Independent Test com o Claude Code.
3. Phase 4 (US2) → validar criar/editar/ativar, as duas recusas e o isolamento entre contas.
4. **Parar e validar**: neste ponto já dá para um assistente administrar agentes com segurança. US3–US5 acrescentam leitura de logs, Power BI/canais e base de conhecimento.

### Incremental Delivery

1. Setup + Foundational → compila, migração aplicada, autenticação funcionando.
2. US1 → chaves no painel + `quem_sou_eu` (MVP parcial).
3. US2 → agentes (MVP completo).
4. US3 → logs e teste (fecha o ciclo de calibração, SC-005).
5. US4 → Power BI e canais.
6. US5 → base de conhecimento.
7. Polish → docs, Bruno, validação do quickstart.

### Ordem recomendada para um único desenvolvedor

Phase 1 → Phase 2 → US1 → US2 → US3 → US4 → US5 → Polish, que é a ordem das "Design notes" do plano. As refatorações de `SessionController` (US3) e `FileController` (US5) devem ser commitadas separadamente, cada uma com os testes antigos passando, para facilitar o diff da revisão.

---

## Notes

- **Nunca** criar uma ferramenta de exclusão de agente nem aceitar campo de segredo: se uma tarefa parecer exigir isso, a leitura está errada (FR-008, FR-009, FR-010).
- Toda mensagem de "nao encontrado" para agente, sessão ou documento alheio deve ser **idêntica** à de inexistente (FR-004, SC-004).
- O valor da chave de acesso existe só na resposta do `POST /auth/me/mcp-keys` e no modal; nunca no log, no estado do store, no console nem em testes commitados.
- Única dependência nova: `ModelContextProtocol.AspNetCore` 2.2.0 na API e `ModelContextProtocol` 2.2.0 nos testes de API. Nada no frontend.
- `AvaBot.Tests.API/appsettings.json` e `.env` contêm credenciais reais: nunca copiá-las para exemplos, docs, Bruno ou commits.
- Commit a cada fase concluída (Phase 2, cada história, Polish).
