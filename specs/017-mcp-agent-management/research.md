# Research: MCP de gestão de agentes

**Feature**: 017-mcp-agent-management | **Date**: 2026-10-12

Nenhum item do contexto técnico ficou como NEEDS CLARIFICATION: a stack é a do repositório e as quatro decisões de escopo foram tomadas em `/speckit.clarify` (só servidor remoto; consultas BI de teste continuam no log sem marcação; auditoria só no log técnico; chave com nível único de permissão). As pesquisas abaixo fecham as escolhas de implementação.

## R1. Biblioteca do protocolo MCP

**Decisão**: usar o SDK oficial C# do protocolo, pacote NuGet `ModelContextProtocol.AspNetCore` **2.2.0** (traz `ModelContextProtocol` e `ModelContextProtocol.Core` na mesma versão, mais `Microsoft.Extensions.AI.Abstractions` 10.8.3 e pacotes `System.*` 10.0.10, todos com build para `net9.0`). Os três pacotes já estão no cache NuGet da máquina, então o restore funciona offline. É a **primeira dependência nova** desde a feature 011; a justificativa está no Constitution Check do plano.

**Justificativa**: o SDK implementa o transporte Streamable HTTP (incluindo o modo sem estado, negociação de versão do protocolo, cabeçalhos `MCP-Protocol-Version` e `Mcp-Session-Id`), o JSON-RPC, a geração do JSON Schema das ferramentas a partir das assinaturas C#, filtros de requisição e integração com autenticação/autorização do ASP.NET Core. Escrever isso à mão seria mais código do que a feature inteira e ficaria defasado a cada revisão do protocolo (o SDK já fala as revisões 2025-11-25 e 2026-07-28).

**Alternativas**: implementação própria de JSON-RPC sobre um controller (descartada: reimplementa o protocolo e os clientes mudam de comportamento entre revisões); pacote local em Node (descartado na clarificação 1).

**Superfície do SDK que a feature usa** (confirmada no XML de documentação do pacote em cache):
- `services.AddMcpServer(options)` → `.WithHttpTransport(o => ...)` → `.WithTools<T>(jsonOptions)` → `.WithRequestFilters(f => f.AddCallToolFilter(...))` → `.AddAuthorizationFilters()`.
- `app.MapMcp("/mcp")` devolve um `IEndpointConventionBuilder`, então aceita `.RequireAuthorization(policy)`.
- `[McpServerToolType]` na classe e `[McpServerTool(Name = "...", Title = "...", ReadOnly = true|false, Idempotent = ..., Destructive = ...)]` + `[Description]` nos métodos e parâmetros. Parâmetros `CancellationToken`, `IServiceProvider`, `McpServer` e `RequestContext<CallToolRequestParams>` são injetados e ficam fora do schema; os demais viram o `inputSchema`.
- `RequestContext<CallToolRequestParams>` expõe `User` (`ClaimsPrincipal` da requisição HTTP), `Services`, `Params.Arguments` (`IReadOnlyDictionary<string, JsonElement>`) e `MatchedPrimitive` (a ferramenta casada, com as anotações).
- Erro de negócio dentro de uma ferramenta: lançar `McpException(mensagem)`; o SDK devolve `CallToolResult` com `isError = true` e a mensagem no conteúdo, em vez de erro JSON-RPC. É o que faz o assistente ler a orientação e repassar ao usuário (FR-020).
- `McpServerOptions.ServerInfo` (`Implementation { Name, Title, Version }`) e `ServerInstructions` (texto enviado ao cliente no `initialize`): usados para o nome "avabot" e as instruções em português sobre o que o assistente não pode fazer.
- `HttpServerTransportOptions.Stateless` é `true` por padrão nesta versão; `ScopeRequests` (escopo de DI por requisição) é `true` por padrão, então os services `Scoped` do AvaBot (DbContext, `AgentService`...) funcionam dentro das ferramentas sem registro extra.

## R2. Transporte e modo de sessão

**Decisão**: Streamable HTTP em modo **sem estado** (`Stateless = true`, padrão), mapeado em `/mcp` na própria API. Sem SSE legado (`EnableLegacySse = false`, padrão). Sem `EventStreamStore`.

**Justificativa**: sem estado dispensa afinidade de sessão e cache distribuído, não guarda nada em memória por cliente e sobrevive a reinícios da API sem o assistente perceber. Os clientes-alvo (Claude Desktop/claude.ai, Claude Code, Cursor, VS Code, ChatGPT) falam Streamable HTTP; nenhum exige o transporte SSE antigo.

**Risco de deploy**: as respostas de `tools/call` podem vir como `text/event-stream`. O proxy reverso de produção (que já serve `https://avabot.net/api/...`, ver `TelegramService.WEBHOOK_BASE_URL`) precisa repassar `/api/mcp` sem buffer de resposta e com timeout de leitura compatível com uma pergunta de teste (até ~2 min, igual ao teste do painel). Isso vai para o quickstart como verificação de deploy; nenhum arquivo de infra está no repositório para alterar.

**Alternativas**: `Stateful` (exige a mesma instância durante a sessão e limpeza de sessões ociosas; sem ganho aqui); endpoint em processo separado (duplicaria DI, configuração e deploy).

## R3. Autenticação do MCP: chave de acesso dedicada

**Decisão**: esquema de autenticação próprio, `McpAccessKey`, implementado como `AuthenticationHandler<AuthenticationSchemeOptions>` em `AvaBot.API/Auth/McpAccessKeyAuthenticationHandler.cs`. Lê `Authorization: Bearer <chave>`; a chave tem o formato `avabot_mcp_` + 43 caracteres base64url (32 bytes de `RandomNumberGenerator`). O handler calcula SHA-256 da chave e busca `avabot_mcp_access_keys.key_hash`; aceita só chave com `status = 1` e dono com `status = 1`; emite um `ClaimsPrincipal` com `sub` = `UserId`, `email`, `name` e `mcp_key_id` = id da chave, e atualiza `last_used_at` (no máximo uma vez por minuto por chave, para não gravar a cada chamada). Falha de qualquer tipo (chave ausente, formato errado, inexistente, revogada, dono inativo) responde `401` com o corpo `{ "sucesso": false, "mensagem": "Chave de acesso invalida ou revogada" }` e `WWW-Authenticate: Bearer`; a mensagem é a mesma em todos os casos (US1 cenário 5).

O endpoint `/mcp` exige a política `McpAccessKeyPolicy` (`RequireAuthenticatedUser` + `AuthenticationSchemes = "McpAccessKey"`), então o JWT do painel **não** vale em `/mcp`, e a chave MCP **não** vale nas rotas do painel (que continuam no esquema `Bearer` JWT, inalterado). `User.GetUserId()` já lê `sub`/`NameIdentifier`, e a extensão nova `GetMcpAccessKeyId()` lê `mcp_key_id`.

**Hash**: SHA-256 simples (`System.Security.Cryptography.SHA256`) e não PBKDF2 como a senha: a chave tem 256 bits aleatórios, então não há o que forçar por dicionário, e o hash precisa ser determinístico para servir de índice de busca. Implementado como método estático em `AvaBot.Domain/Models/McpAccessKey.cs` (`ComputeHash`), só BCL, no mesmo espírito de `User.NormalizeEmail`.

**Sem cache**: a validação consulta o banco a cada requisição (índice único em `key_hash`); é o que garante revogação imediata (SC-006) sem invalidação de cache.

**Justificativa**: a spec pede chave dedicada, revogável, sem expor senha nem o token do navegador; um esquema separado impede que um assistente use a chave no painel e vice-versa. O handler é a forma padrão do ASP.NET Core de plugar isso sem middleware manual.

**Alternativas**: OAuth 2.1 com `AddMcp` (o SDK traz um handler de *resource metadata* para servidores que delegam a um provedor de identidade; o AvaBot não tem provedor OAuth, e a spec quer chave copiada da página "Minha conta"); reutilizar o JWT do painel (descartado na spec: expiraria em 30 dias e não é revogável por assistente); chave em query string (vaza em logs de proxy).

## R4. Identidade do chamador dentro das ferramentas

**Decisão**: cada ferramenta recebe `RequestContext<CallToolRequestParams> context` e obtém o usuário com `context.User.GetUserId()` por meio de um helper `McpCaller.From(context)` (`AvaBot.API/Mcp/McpCaller.cs`), que também expõe o id da chave. O `ownerUserId` é passado explicitamente aos services da Application, como o painel faz (research R3 da feature 016). Nada de `IHttpContextAccessor` nem de `ICurrentUser` nas camadas de baixo.

**Justificativa**: o SDK preenche `MessageContext.User` a partir do `HttpContext.User` (é o que `AddAuthorizationFilters` usa para avaliar `[Authorize]` por ferramenta); usar o mesmo princípio mantém as ferramentas testáveis com um `ClaimsPrincipal` montado à mão.

## R5. Bloqueio de segredos (FR-009, FR-010)

**Decisão** em três camadas, todas na API:
1. **Schema**: os DTOs de entrada das ferramentas (`AvaBot.DTO/McpDTOs.cs`) não têm nenhum campo de segredo; o JSON Schema publicado em `tools/list` nunca menciona chave, token ou segredo.
2. **Filtro de chamada** (`AvaBot.API/Mcp/McpSecretGuardFilter.cs`, registrado com `AddCallToolFilter`): antes de qualquer ferramenta rodar, percorre as chaves de primeiro nível de `Params.Arguments` e, se alguma bater (sem diferenciar caixa) com a lista proibida — `openAIApiKey`, `removeOpenAIApiKey`, `apiKey`, `telegramBotToken`, `telegramWebhookSecret`, `whatsappToken`, `tenantId`, `directoryId`, `clientId`, `appId`, `clientSecret`, `secret`, `token`, `password`, `senha` —, devolve `CallToolResult { IsError = true }` com a mensagem "Credenciais (chave de API, Directory ID, App ID, segredo do Power BI, tokens de Telegram e WhatsApp) sao informadas apenas pelo usuario no painel, em Agentes > {agente} > ... . Nenhum campo foi gravado." A ferramenta não é executada, então nada é gravado (edge case da spec).
3. **Saída**: as ferramentas devolvem DTOs próprios (`AgentMcpInfo`, `PowerBIMcpInfo`, `TelegramMcpInfo`) que só carregam indicadores (`hasOpenAIApiKey`, `credentialConfigured`, `botConfigured`); `AgentInfo` e `PowerBIConfigInfo` do painel (que trazem `telegramBotToken`, `tenantId`, `clientId`, `clientSecretMasked`) nunca são serializados pelo MCP. Um teste unitário serializa cada DTO de saída e verifica que nenhum nome de propriedade da lista proibida aparece (SC-002).

**Justificativa**: só o schema não basta: um assistente pode mandar campos extras, e o deserializador do SDK os ignoraria em silêncio, deixando o assistente achar que a credencial foi salva. O filtro transforma isso em recusa explícita.

## R6. Erros e mensagens para o assistente (FR-018, FR-020)

**Decisão**: um segundo filtro, `McpToolExecutionFilter` (registrado depois do guarda de segredos), envolve a execução e:
- converte exceções conhecidas em `CallToolResult { IsError = true }` com a mensagem em português: `KeyNotFoundException` → "Agente nao encontrado" / "Sessao nao encontrada" / "Documento nao encontrado" (a mensagem da própria exceção, como os controllers); `FluentValidation.ValidationException` → mensagens do validator unidas por "; "; `InvalidOperationException`, `ArgumentException`, `PowerBIApiException` → `ex.Message`; `AgentTestFailedException` → mensagem + resultado parcial serializado; qualquer outra → "Erro interno ao executar {ferramenta}" (detalhe só no log).
- escreve o **log de auditoria** (FR-019, clarificação 3): uma linha `Information` por chamada de ferramenta de escrita (`ReadOnly = false` na anotação) e `Debug` para leitura, com `tool`, `userId`, `mcpKeyId`, `agentId` (quando o argumento existe), `sucesso` e `durationMs`. Nunca loga o conteúdo dos argumentos (um deles pode ser o prompt de sistema ou um documento inteiro).

As ferramentas reutilizam os validators FluentValidation do painel (`IValidator<AgentInsertInfo>`, `IValidator<PowerBIDatasetInsertInfo>`), injetados pelo `IServiceProvider` do contexto, para que as mensagens de validação sejam as mesmas do painel (FR-018).

**Justificativa**: centralizar em um filtro evita `try/catch` repetido em 28 ferramentas e garante que nenhuma exceção vire erro JSON-RPC genérico sem orientação.

## R7. Reaproveitamento da lógica do painel (FR-018, SC-007)

Hoje parte da lógica de sessões e de arquivos vive nos controllers, não em services. Para o MCP e o painel fazerem exatamente a mesma coisa, a feature extrai essa lógica para a Application e faz os dois chamarem o mesmo código:

| Hoje | Passa a existir | Quem usa |
|---|---|---|
| `SessionController.GetSessions/GetMessages` (paginação, contagem, validação de dono) | `SessionQueryService.GetSessionsAsync(agentId, ownerUserId, page, pageSize)` e `GetMessagesAsync(sessionId, ownerUserId, page, pageSize)` devolvendo `PaginatedResult<...>`; `KeyNotFoundException` para agente/sessão de outro dono | `SessionController` (refatorado) e ferramentas `listar_sessoes`, `listar_mensagens` |
| `FileController.GetByAgent/Upload/Delete/Reprocess` (limite 10 MB, só `.md`, processamento em background com `IServiceScopeFactory`) | `KnowledgeFileService.ListAsync`, `AddTextAsync(agentId, ownerUserId, fileName, content)`, `DeleteAsync`, `ReprocessAsync` | `FileController` (refatorado; o upload continua lendo o `IFormFile` e chamando `AddTextAsync`) e ferramentas `listar_documentos`, `adicionar_documento`, `reprocessar_documento`, `remover_documento` |
| `AgentService.UpdateAsync(id, AgentInsertInfo, owner)` (substitui todos os campos, inclusive `telegramBotToken`, e trata chave OpenAI) | `AgentService.UpdateProfileAsync(id, AgentProfileUpdateInfo, owner)`: atualização **parcial** só de nome, descrição, prompt, modelo, coleta e nome do bot; `null` mantém o valor; renomear regenera o slug como hoje. `AgentService.SetActiveAsync(id, active, owner)` ao lado do `ToggleStatusAsync` | só o MCP (o painel continua com `UpdateAsync`) |
| `AgentService.CreateAsync(AgentInsertInfo, owner)` | inalterado; a ferramenta `criar_agente` monta um `AgentInsertInfo` sem campos de segredo e passa pelo `AgentInsertInfoValidator` | painel e MCP |
| `PowerBIService.GetQueryLogsAsync(slug, owner, page, pageSize)` e `IPowerBIQueryLogRepository.GetPagedByAgentAsync` | ganham o parâmetro opcional `PowerBIQueryStatus? status` (FR-012); `GET /powerbi/{slug}/query-logs?status=` passa a aceitar o filtro também | painel (opcional) e `listar_consultas_bi` |
| `PowerBIService.GetConfigAsync` devolve `PowerBIConfigInfo` com `tenantId`, `clientId`, `clientSecretMasked` | a ferramenta `obter_powerbi` projeta para `PowerBIMcpInfo` (só `enabled`, `credentialConfigured`, último teste e datasets); o service não muda | MCP |
| `TelegramService.GetWebhookInfoAsync` lança se não há token | a ferramenta `obter_telegram` verifica `TelegramBotToken` vazio antes e responde `botConfigured = false` sem chamar o Telegram; o service não muda | MCP |
| `ChatService.TestMessageAsync` | inalterado (não cria sessão nem mensagens; as consultas BI continuam entrando no log, clarificação 2) | painel e `testar_agente` |

A ferramenta `canal` em `listar_sessoes`: a sessão não guarda o canal. A feature devolve `"telegram"` quando existe `avabot_telegram_chats` apontando para a sessão (método novo `ITelegramChatRepository.GetSessionIdsAsync(IEnumerable<long>)`) e `"web"` nos demais casos (site, widget e WhatsApp, que hoje são indistinguíveis). Fica documentado como limitação; adicionar uma coluna de canal é fora do escopo.

**Justificativa**: evita duas implementações das mesmas regras e faz o painel se beneficiar (filtro por situação nos logs). A refatoração é mecânica: os controllers mantêm rotas, respostas e mensagens.

## R8. Identificação do agente nas ferramentas

**Decisão**: todas as ferramentas recebem `agentId` (inteiro). `listar_agentes` devolve id e slug; as ferramentas de Power BI e WhatsApp, cujos services trabalham por `slug`, resolvem o agente com `AgentService.GetOwnedByIdAsync(agentId, ownerUserId)` e passam o `slug` adiante. Agente de outra conta ou inexistente lança `KeyNotFoundException("Agente nao encontrado")` em todos os casos (FR-004, SC-004).

**Alternativas**: aceitar id ou slug no mesmo parâmetro (ambíguo no schema; o assistente acabaria chutando).

## R9. Chaves de acesso no painel (US1)

**Decisão**: entidade `McpAccessKey` + service `McpAccessKeyService` (Application) + controller `McpAccessKeyController` em `/auth/me/mcp-keys` (JWT, `[Authorize]`), e a seção "Assistentes de IA (MCP)" na `AccountPage`. O valor em claro só existe na resposta do `POST`; o painel mostra um modal com a chave, o botão copiar, o endereço do servidor (`${VITE_API_URL}/mcp`) e trechos de configuração prontos (Claude Code, Claude Desktop/claude.ai, Cursor, VS Code). Revogar = `DELETE /auth/me/mcp-keys/{id}` → `status = 0`, `revoked_at`; a chave continua na lista como "revogada". Limite de **20 chaves ativas por conta** (guarda contra loop de criação; não está na spec, é regra de implementação). Nome 1 a 100 caracteres, repetido permitido (edge case da spec).

Frontend segue o padrão do repositório: `types/mcpAccessKey.ts`, `Services/McpAccessKeyService.ts`, `stores/useMcpAccessKeyStore.ts`, componente `components/admin/McpAccessKeysPanel.tsx` (lista, criar, revogar) e `McpKeyCreatedModal.tsx` (exibição única). Sem dependência nova; o modal segue o estilo do `OpenAIConnectionDiagnosticModal`.

## R10. Testes

**Decisão**:
- `AvaBot.Tests` (xUnit + Moq, sem API): `McpAccessKeyServiceTest` (cria e devolve o valor uma vez; grava só o hash; autentica chave válida; recusa revogada, inexistente e dono inativo; atualiza último uso com limite de 1 min; limite de 20); `McpAccessKeyTest` (formato e hash); `AgentServiceTest` (+ `UpdateProfileAsync` parcial preserva segredos e regenera slug; `SetActiveAsync`); `SessionQueryServiceTest`; `KnowledgeFileServiceTest` (limite 10 MB, `.md` automático, agente alheio); `McpSecretGuardFilterTest` (cada nome proibido, em qualquer caixa, é recusado sem chamar a ferramenta; nomes permitidos passam); `McpToolExecutionFilterTest` (mapa de exceções → `isError` e mensagens; log de escrita); `McpOutputDtoTest` (serializa os DTOs de saída e garante ausência dos campos de segredo, SC-002); testes das classes de ferramentas com services mockados onde a lógica é mais que delegação (`obter_telegram` sem token; `obter_powerbi` sem credencial; `testar_agente` validando o histórico).
- `AvaBot.Tests.API` (API no ar): `McpAccessKeyControllerTest` (criar, listar, revogar, 401 do painel sem JWT) e `McpServerTest` usando o cliente do próprio SDK (`ModelContextProtocol` 2.2.0, `McpClient` + `HttpClientTransport` apontando para `{BaseUrl}/mcp` com a chave): `initialize` e `tools/list` (28 ferramentas; nenhuma com "excluir"/"delete"/"remover_agente"); `quem_sou_eu`; ciclo criar → listar → atualizar → desativar → reativar; `atualizar_agente` com `openAIApiKey` → `isError` e agente intacto; agente de outra conta → "nao encontrado"; chave revogada → `401` na chamada seguinte. O pacote `ModelContextProtocol` entra só no projeto de testes de API.
- Frontend: `npm run lint` e `npm run build` (não há suíte de testes no frontend).

## R11. Documentação e coleção Bruno

**Decisão**: `docs/MCP_SETUP.md` (criar a chave; endereço; configuração passo a passo de Claude Code, Claude Desktop e claude.ai, Cursor, VS Code e ChatGPT; lista das ferramentas; o que o assistente não pode fazer; como revogar); seção "MCP para assistentes de IA" no `README.md`; `bruno/MCP/` com `Create Key.bru`, `List Keys.bru`, `Revoke Key.bru` e `Tools List.bru` / `Call Tool.bru` (JSON-RPC bruto contra `/mcp`, úteis para depurar sem assistente). O SC-001 (5 minutos até o "quem sou eu") é validado seguindo só o `docs/MCP_SETUP.md`.
