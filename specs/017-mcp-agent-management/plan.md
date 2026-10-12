# Implementation Plan: MCP de gestão de agentes

**Branch**: `017-mcp-agent-management` | **Date**: 2026-10-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/017-mcp-agent-management/spec.md`

## Summary

Expor o AvaBot como servidor MCP (Model Context Protocol) remoto, em `/mcp` na própria API, para que um assistente de IA opere a conta do usuário: criar, editar, ativar/desativar agentes; ler sessões, mensagens e logs de consulta Power BI; fazer perguntas de teste; configurar Power BI (datasets, schema, descrições), Telegram (webhook) e a base de conhecimento. O assistente se autentica com uma **chave de acesso dedicada**, criada e revogada em "Minha conta". Duas regras são o núcleo: **nenhuma ferramenta exclui agente** e **nenhum segredo é lido ou gravado** (chave de API do modelo, Directory ID, App ID e segredo do Power BI, tokens de Telegram e WhatsApp, segredo do webhook). Abordagem técnica: SDK oficial `ModelContextProtocol.AspNetCore` 2.2.0 com Streamable HTTP sem estado; esquema de autenticação próprio `McpAccessKey`; dois filtros de chamada (guarda de segredos e execução/erros/auditoria); DTOs de saída sem campos de segredo; e extração da lógica de sessões e arquivos dos controllers para services da Application, para que painel e MCP executem o mesmo código. Decisões da clarificação: só remoto; consultas BI de teste continuam no log sem marcação; auditoria só no log técnico; chave com nível único.

## Technical Context

**Language/Version**: C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend)  
**Primary Dependencies**: ASP.NET Core 9, EF Core 9 + Npgsql, FluentValidation (já na API), AutoMapper, `System.Security.Cryptography` (SHA-256, `RandomNumberGenerator`, BCL), `Microsoft.AspNetCore.Authentication` (handler próprio, já na API via JwtBearer). **Dependência nova**: `ModelContextProtocol.AspNetCore` **2.2.0** na API (transitivamente `ModelContextProtocol`, `ModelContextProtocol.Core`, `Microsoft.Extensions.AI.Abstractions` 10.8.3) e `ModelContextProtocol` 2.2.0 só em `AvaBot.Tests.API` (cliente para os testes). Todos já no cache NuGet local. Frontend: React Router 7, Zustand 5, Tailwind 4, sonner 2; nada novo.  
**Storage**: PostgreSQL. Uma tabela nova (`avabot_mcp_access_keys`). Nenhuma coluna nova em tabelas existentes. Elasticsearch não é afetado (os documentos adicionados pelo MCP seguem a ingestão atual).  
**Testing**: xUnit 2.9 + Moq em `AvaBot.Tests` (service de chaves, filtros, DTOs de saída, services extraídos, ferramentas); `AvaBot.Tests.API` com Flurl e o cliente do SDK MCP contra a API no ar; frontend `npm run lint` + `npm run build`.  
**Target Platform**: API Linux/Docker existente atrás do proxy de `https://avabot.net/api`; painel SPA; clientes MCP (Claude Code, Claude Desktop/claude.ai, Cursor, VS Code, ChatGPT) na máquina ou nuvem do usuário.  
**Project Type**: Web service (backend .NET) + frontend React; o MCP é um endpoint a mais na API.  
**Performance Goals**: validação da chave em < 10 ms (uma consulta por índice único); ferramentas de leitura com o mesmo custo das rotas do painel equivalentes; `testar_agente` com o mesmo tempo do teste do painel (dominado pelo modelo e pelo Power BI).  
**Constraints**: só servidor remoto (clarificação 1); chave dedicada, nunca o JWT do painel, em `/mcp`; mesma mensagem `401` para chave inválida e revogada; "Agente nao encontrado" idêntico para alheio e inexistente (FR-004); nenhum valor de segredo em nenhuma resposta ou schema (FR-009, SC-002); recusa integral de chamadas com campo de segredo (FR-010); nenhuma rota/resposta do painel muda (SC-007); auditoria só em log técnico, sem conteúdo dos argumentos (FR-019); mensagens em português (FR-020); proxy de produção sem buffer em `/api/mcp` (research R2).  
**Scale/Scope**: dezenas de contas, poucas chaves por conta (limite de 20 ativas); 28 ferramentas em 6 classes; 1 tabela, 1 migração, 3 endpoints REST novos, 1 esquema de autenticação, 2 filtros, 3 services novos + 1 método novo em 2 services existentes, 2 controllers refatorados sem mudança de contrato, 1 seção nova na página de conta + 2 componentes, 1 documento de setup, 5 requisições Bruno.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I. Skills obrigatórias | Backend segue a `dotnet-architecture`: DTOs em `AvaBot.DTO` com `[JsonPropertyName]`, interface de repositório genérica em `Infra.Interfaces`, modelo rico em `Domain` (`McpAccessKey.GenerateKey/ComputeHash`), repositório em `Infra`, services em `Application`, DI em `DependencyInjection.cs`, migração via `dotnet ef`. O que é específico do ASP.NET (handler de autenticação, filtros e classes de ferramentas do SDK) fica em `AvaBot.API/Auth` e `AvaBot.API/Mcp`, como o `JwtTokenIssuer` ficou na API. Frontend segue o padrão do repositório (`Services/` + `stores/` Zustand), desvio preexistente das features 007–016, não ampliado. ✅ |
| II. Stack | **Uma dependência nova**: `ModelContextProtocol.AspNetCore` 2.2.0 (SDK oficial do protocolo, mantido por Microsoft/Anthropic). A constituição não proíbe bibliotecas novas (veta só ORM alternativo, state management e bundler); as features anteriores evitaram dependências por escolha, não por regra. Justificativa em research R1 e no Complexity Tracking. .NET 9 e JWT são desvios preexistentes já registrados na 016. ✅ |
| III. Case sensitivity | Backend: `AvaBot.API/Mcp/`, `AvaBot.API/Mcp/Tools/`, `AvaBot.API/Auth/`. Frontend: `Services/` (S maiúsculo), `stores/`, `types/`, `components/admin/`. ✅ |
| IV. Convenções de código | PascalCase, `_camelCase`, namespaces file-scoped, JSON camelCase; nomes das ferramentas em `snake_case` por serem identificadores do protocolo (como `consultar_bi` já existente); frontend `interface`, arrow functions, `const`. ✅ |
| V. Banco | `avabot_mcp_access_keys` snake_case, PK `mcp_access_key_id bigint identity`, `avabot_mcp_access_keys_pkey`, FK `avabot_fk_users_mcp_access_keys`, `timestamp without time zone`, `varchar` com tamanho, `status integer DEFAULT 1`. **Desvio**: FK com `Restrict` e `user_id NOT NULL` em vez de `ClientSetNull` (ver Complexity Tracking). Nunca `Cascade`. ⚠️ justificado |
| VI. Autenticação | Rotas novas do painel (`/auth/me/mcp-keys`) com `[Authorize]` JWT; `/mcp` com política própria do esquema `McpAccessKey`; token do painel continua em `localStorage`; a chave MCP não é guardada pelo frontend (só exibida uma vez); nenhum segredo no frontend; CORS inalterado. ✅ |
| VII. Variáveis de ambiente | Nenhuma variável nova. O endereço do MCP mostrado no painel deriva de `VITE_API_URL`. ✅ |
| VIII. Tratamento de erros | Controllers novos mantêm `try/catch → StatusCode(500, Result.Failure)` com `KeyNotFoundException → 404`, `InvalidOperationException → 400`, `ValidationException → 400`. No MCP o equivalente é o `McpToolExecutionFilter`, que converte as mesmas exceções em `isError` com mensagem (research R6). ✅ |

**Resultado**: aprovado com dois itens justificados no Complexity Tracking. Reavaliação após a Fase 1: os contratos não introduzem variável, cookie nem rota pública nova; `/mcp` é autenticado; o painel não muda de contrato (só ganha o filtro opcional `status` nos logs BI).

## Project Structure

### Documentation (this feature)

```text
specs/017-mcp-agent-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── mcp-server.md        # endpoint /mcp, autenticação, initialize, filtros, erros, Program.cs
│   ├── mcp-tools.md         # as 28 ferramentas: entrada, saída, anotações, service chamado
│   └── access-keys-api.md   # /auth/me/mcp-keys, service, frontend, Bruno
├── checklists/requirements.md
└── tasks.md                 # /speckit.tasks
```

### Source Code (repository root)

```text
AvaBot.Domain/Models/
└── McpAccessKey.cs                          # NOVO: entidade + GenerateKey, ComputeHash, HasExpectedFormat, IsActive

AvaBot.DTO/
├── McpAccessKeyDTOs.cs                      # NOVO: McpAccessKeyInfo, McpAccessKeyInsertInfo, McpAccessKeyCreatedInfo
├── McpDTOs.cs                               # NOVO: McpIdentityInfo, AgentMcpSummaryInfo, AgentMcpInfo, AgentMcpCreateInfo, AgentProfileUpdateInfo,
│                                            #       ChatSessionMcpInfo, ChatMessageMcpInfo, PowerBIMcpInfo, TelegramMcpInfo, WhatsappMcpInfo, KnowledgeDocumentMcpInsertInfo
└── PowerBIDTOs.cs                           # sem mudança (PowerBIQueryLogPageInfo reutilizado)

AvaBot.Infra.Interfaces/Repository/
├── IMcpAccessKeyRepository.cs               # NOVO: GetByIdAsync(id,userId), GetByHashAsync, GetAllByUserAsync, CountActiveByUserAsync, CreateAsync, UpdateAsync
├── IPowerBIQueryLogRepository.cs            # GetPagedByAgentAsync(+ PowerBIQueryStatus? status)
└── ITelegramChatRepository.cs               # + GetSessionIdsAsync(IEnumerable<long>)

AvaBot.Infra/
├── Context/AvaBotContext.cs                 # DbSet<McpAccessKey>, mapeamento avabot_mcp_access_keys
├── Repository/McpAccessKeyRepository.cs     # NOVO
├── Repository/PowerBIQueryLogRepository.cs  # filtro por status
├── Repository/TelegramChatRepository.cs     # GetSessionIdsAsync
└── Migrations/<ts>_AddMcpAccessKeys.cs      # gerada por dotnet ef

AvaBot.Application/
├── Services/McpAccessKeyService.cs          # NOVO: CreateAsync, ListAsync, RevokeAsync, AuthenticateAsync (research R3, R9)
├── Services/SessionQueryService.cs          # NOVO: GetSessionsAsync, GetMessagesAsync (extraído do SessionController, research R7)
├── Services/KnowledgeFileService.cs         # NOVO: ListAsync, AddTextAsync, DeleteAsync, ReprocessAsync (extraído do FileController)
├── Services/AgentService.cs                 # + UpdateProfileAsync (parcial, sem segredos), SetActiveAsync
├── Services/PowerBIService.cs               # GetQueryLogsAsync(+ status)
├── Profiles/McpAccessKeyProfile.cs          # NOVO: McpAccessKey → McpAccessKeyInfo
├── Profiles/McpProfile.cs                   # NOVO: Agent → AgentMcpSummaryInfo/AgentMcpInfo, ChatSession → ChatSessionMcpInfo, ChatMessage → ChatMessageMcpInfo
└── DependencyInjection.cs                   # registra repositório e os 3 services novos

AvaBot.API/
├── Auth/McpAccessKeyAuthenticationHandler.cs  # NOVO: esquema "McpAccessKey" (research R3)
├── Auth/ClaimsPrincipalExtensions.cs          # + GetMcpAccessKeyId(), GetMcpAccessKeyName()
├── Mcp/McpCaller.cs                           # NOVO: From(RequestContext) → UserId, KeyId, KeyName (research R4)
├── Mcp/McpSecretGuardFilter.cs                # NOVO: lista proibida + recusa (research R5)
├── Mcp/McpToolExecutionFilter.cs              # NOVO: exceções → isError, auditoria no log (research R6)
├── Mcp/McpServerInstructions.cs               # NOVO: texto do initialize
├── Mcp/McpJson.cs                             # NOVO: JsonSerializerOptions das ferramentas
├── Mcp/Tools/AccountTools.cs                  # NOVO: quem_sou_eu
├── Mcp/Tools/AgentTools.cs                    # NOVO: listar/obter/criar/atualizar agente, definir_agente_ativo
├── Mcp/Tools/ConversationTools.cs             # NOVO: listar_sessoes, listar_mensagens, listar_consultas_bi, testar_agente
├── Mcp/Tools/PowerBITools.cs                  # NOVO: 11 ferramentas de Power BI
├── Mcp/Tools/ChannelTools.cs                  # NOVO: obter_telegram, registrar_webhook_telegram, obter_whatsapp
├── Mcp/Tools/KnowledgeTools.cs                # NOVO: listar/adicionar/reprocessar/remover documento
├── Controllers/McpAccessKeyController.cs      # NOVO: GET/POST/DELETE /auth/me/mcp-keys
├── Controllers/SessionController.cs           # GetSessions/GetMessages delegam ao SessionQueryService (mesmas respostas)
├── Controllers/FileController.cs              # delega ao KnowledgeFileService (mesmas respostas)
├── Controllers/PowerBIController.cs           # GetQueryLogs aceita ?status=
├── Validators/McpAccessKeyInsertInfoValidator.cs  # NOVO
├── Program.cs                                 # AddScheme McpAccessKey, política, AddMcpServer(...), MapMcp("/mcp")
└── AvaBot.API.csproj                          # + ModelContextProtocol.AspNetCore 2.2.0

AvaBot.Calibration/Local/LocalRepositories.cs  # assinaturas novas de IPowerBIQueryLogRepository e ITelegramChatRepository

AvaBot.Tests/
├── Domain/McpAccessKeyTest.cs                            # NOVO: formato, hash, HasExpectedFormat
├── Application/Services/McpAccessKeyServiceTest.cs       # NOVO
├── Application/Services/SessionQueryServiceTest.cs       # NOVO
├── Application/Services/KnowledgeFileServiceTest.cs      # NOVO
├── Application/Services/AgentServiceTest.cs              # + UpdateProfileAsync, SetActiveAsync
├── API/Mcp/McpSecretGuardFilterTest.cs                   # NOVO
├── API/Mcp/McpToolExecutionFilterTest.cs                 # NOVO
├── API/Mcp/McpOutputDtoTest.cs                           # NOVO: nenhum campo de segredo nos DTOs de saída (SC-002)
├── API/Mcp/AgentToolsTest.cs, PowerBIToolsTest.cs, ChannelToolsTest.cs  # NOVOS: lógica própria das ferramentas com services mockados
└── API/Controllers/McpAccessKeyControllerTest.cs         # NOVO

AvaBot.Tests.API/
├── Controllers/McpAccessKeyControllerTest.cs  # NOVO: criar, listar, revogar, 401
├── Controllers/McpServerTest.cs               # NOVO: cliente do SDK contra /mcp (research R10)
└── AvaBot.Tests.API.csproj                    # + ModelContextProtocol 2.2.0

frontend/src/
├── types/mcpAccessKey.ts                      # NOVO
├── Services/McpAccessKeyService.ts            # NOVO
├── stores/useMcpAccessKeyStore.ts             # NOVO
├── components/admin/McpAccessKeysPanel.tsx    # NOVO
├── components/admin/McpKeyCreatedModal.tsx    # NOVO
└── pages/admin/AccountPage.tsx                # + seção "Assistentes de IA (MCP)"

docs/MCP_SETUP.md                              # NOVO: criar chave, conectar cada assistente, ferramentas, limites, revogar
README.md                                      # seção "MCP para assistentes de IA" + link
bruno/MCP/                                     # NOVO: Create Key, List Keys, Revoke Key, Tools List, Call Tool
```

**Structure Decision**: web application com backend .NET em Clean Architecture (raiz) e frontend React em `frontend/`, como as features anteriores. Nada de projeto novo: o servidor MCP é um endpoint da `AvaBot.API`, e o que depende do SDK fica confinado em `AvaBot.API/Mcp` e `AvaBot.API/Program.cs`; Application, Domain e Infra não referenciam o SDK.

## Design notes (Fase 1)

- **Ordem de implementação**: (1) entidade, repositório, migração e `McpAccessKeyService` + controller + testes; (2) handler de autenticação e política, com teste de API do `401`; (3) extração de `SessionQueryService` e `KnowledgeFileService` com os controllers refatorados e os testes de API existentes passando sem mudança; (4) `AgentService.UpdateProfileAsync/SetActiveAsync` e filtro de status nos logs; (5) SDK: `Program.cs`, filtros, `AccountTools` + `AgentTools` (MVP = US1 + US2); (6) `ConversationTools`, `PowerBITools`, `ChannelTools`, `KnowledgeTools`; (7) frontend; (8) docs, Bruno, `McpServerTest` completo.
- **Fluxo de uma chamada**: `POST /mcp` → `McpAccessKeyAuthenticationHandler` (claims) → política → SDK despacha `tools/call` → `McpSecretGuardFilter` → `McpToolExecutionFilter` (cronômetro + try/catch + log) → método da ferramenta (`McpCaller.From(context)` → `ownerUserId`) → service da Application → DTO de saída → SDK serializa.
- **Por que DTOs de saída próprios**: `AgentInfo` do painel carrega `telegramBotToken`, `telegramWebhookSecret` e `whatsappToken`; `PowerBIConfigInfo` carrega `tenantId`, `clientId`, `clientSecretMasked`. O MCP nunca serializa esses tipos; o teste `McpOutputDtoTest` garante isso por reflexão nos DTOs de `McpDTOs.cs`.
- **`atualizar_agente` parcial**: `AgentService.UpdateAsync` substitui tudo (inclusive o token do Telegram, que o painel reenvia). O MCP usa `UpdateProfileAsync`, que copia só os campos não nulos de `AgentProfileUpdateInfo` e nunca toca em `OpenAIApiKeyEncrypted`, `TelegramBotToken`, `TelegramWebhookSecret`, `WhatsappToken`, `PowerBIEnabled` ou `Status`.
- **Frontend**: `AccountPage` carrega as chaves no `useEffect` junto com `refreshUser`; "Criar chave" chama `createKey` e abre `McpKeyCreatedModal` com o `Result` em memória de componente (não no store, para o valor não sobreviver à navegação); "Revogar" pede confirmação e atualiza a lista; toasts com sonner (`react-alert`). O modal mostra o endereço `${import.meta.env.VITE_API_URL}/mcp` e trechos de configuração em abas.
- **Compatibilidade**: nenhum contrato do painel muda. `SessionController` e `FileController` mantêm rotas, códigos e mensagens; os testes de API atuais (`ChatSessionControllerTest`, `KnowledgeFileControllerTest`) servem de rede de segurança da refatoração.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Dependência nova `ModelContextProtocol.AspNetCore` 2.2.0 (as features 011–016 não adicionaram pacotes) | O MCP é um protocolo com transporte, negociação de versão, JSON-RPC, geração de schema e filtros de autorização; o SDK oficial implementa tudo e acompanha as revisões do protocolo. | Implementar JSON-RPC + Streamable HTTP num controller: mais código que a feature inteira, sem schema automático, e quebraria conforme os clientes adotassem revisões novas do protocolo. |
| FK `avabot_fk_users_mcp_access_keys` com `Restrict` e `user_id NOT NULL` (constituição V pede `ClientSetNull`) | Uma chave sem dono não tem significado: toda operação do MCP é atribuída a uma conta; `NOT NULL` é o que garante FR-004 no banco. | `user_id` nulo com `ClientSetNull`: permitiria chave órfã que o handler teria de tratar como inválida, trocando uma garantia do banco por uma regra em código. Não existe exclusão de usuário no produto, então o `Restrict` nunca dispara. |
