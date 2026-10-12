# Contrato: ferramentas do MCP

**Feature**: 017-mcp-agent-management | Classes em `AvaBot.API/Mcp/Tools/` (uma por grupo), registradas com `WithTools<T>`.

Convenções:
- Nomes em português, `snake_case`, como a ferramenta `consultar_bi` que o chat já expõe ao modelo. Nenhuma começa com `excluir_`/`remover_agente`.
- Todas recebem `RequestContext<CallToolRequestParams>` (fora do schema) para obter o dono; `agentId` identifica o agente em todas as que operam um agente (research R8).
- `ReadOnly` = anotação `readOnlyHint`; `Idempotent` = `idempotentHint`; `Destructive` só em `desvincular_dataset_powerbi` e `remover_documento`.
- Erros: ver [mcp-server.md](./mcp-server.md). "Agente nao encontrado" vale para id inexistente e para agente de outra conta.
- Saída: o DTO indicado, serializado em JSON no `content` e em `structuredContent`. DTOs em [data-model.md](../data-model.md).

## Conta (`AccountTools`)

| # | Ferramenta | Entrada | Saída | Anotações | Faz |
|---|---|---|---|---|---|
| 1 | `quem_sou_eu` | — | `McpIdentityInfo` | ReadOnly | `UserService.GetAsync(userId)` + nome da chave do claim. |

## Agentes (`AgentTools`)

| # | Ferramenta | Entrada | Saída | Anotações | Faz |
|---|---|---|---|---|---|
| 2 | `listar_agentes` | — | `AgentMcpSummaryInfo[]` | ReadOnly | `AgentService.GetAllAsync(owner)`; projeta sem segredos. |
| 3 | `obter_agente` | `agentId` | `AgentMcpInfo` | ReadOnly | `GetOwnedByIdAsync`; `telegramBotConfigured = token != null`, `whatsappConfigured = whatsappToken != null`, `hasOpenAIApiKey`. |
| 4 | `criar_agente` | `name`, `systemPrompt`, `description?`, `chatModel?`, `collectName?`, `collectEmail?`, `collectPhone?`, `telegramBotName?` | `AgentMcpInfo` | — | Monta `AgentInsertInfo` (sem `openAIApiKey`, `telegramBotToken`), valida com `AgentInsertInfoValidator`, `AgentService.CreateAsync(info, owner)`. Slug gerado como no painel. |
| 5 | `atualizar_agente` | `agentId`, `name?`, `description?`, `systemPrompt?`, `chatModel?`, `collectName?`, `collectEmail?`, `collectPhone?`, `telegramBotName?` | `AgentMcpInfo` | Idempotent | Parcial: `AgentService.UpdateProfileAsync(agentId, info, owner)`; campo ausente mantém o valor; nome novo regenera o slug; valida nome ≤ 260 e prompt não vazio quando informados. Credenciais, status, Power BI e tokens não são tocados. |
| 6 | `definir_agente_ativo` | `agentId`, `active: bool` | `AgentMcpInfo` | Idempotent | `AgentService.SetActiveAsync`; inativo deixa de atender nos canais públicos (comportamento atual de `Status = 0`). |

Descrição de `atualizar_agente` (texto do schema) já avisa: "Nao aceita chave de API nem tokens; esses campos sao recusados e nada e gravado."

## Conversas e teste (`ConversationTools`)

| # | Ferramenta | Entrada | Saída | Anotações | Faz |
|---|---|---|---|---|---|
| 7 | `listar_sessoes` | `agentId`, `page?=1`, `pageSize?=20` (máx. 100) | `PaginatedResult<ChatSessionMcpInfo>` | ReadOnly | `SessionQueryService.GetSessionsAsync`; mais recentes primeiro; `channel` = `telegram` se há `avabot_telegram_chats` para a sessão, senão `web`. |
| 8 | `listar_mensagens` | `sessionId`, `page?=1`, `pageSize?=50` (máx. 200) | `PaginatedResult<ChatMessageMcpInfo>` | ReadOnly | `SessionQueryService.GetMessagesAsync`; ordem cronológica; `role` = `visitante`/`agente`; "Sessao nao encontrada" para sessão de outra conta. |
| 9 | `listar_consultas_bi` | `agentId`, `page?=1`, `pageSize?=20` (máx. 100), `status?` (`success` \| `error` \| `timeout`) | `PowerBIQueryLogPageInfo` | ReadOnly | Resolve slug; `PowerBIService.GetQueryLogsAsync(slug, owner, page, pageSize, status)`. Inclui consultas de perguntas de teste (clarificação 2). |
| 10 | `testar_agente` | `agentId`, `question`, `history?: [{ role: user\|assistant, content }]` | `AgentTestResultInfo` | ReadOnly* | Valida histórico com as mesmas mensagens de `POST /agents/{id}/test`; `ChatService.TestMessageAsync`. Não cria sessão nem mensagem. (*Marcada ReadOnly porque não altera configuração; a descrição avisa que as consultas BI entram no log.) |

## Power BI (`PowerBITools`)

Todas resolvem o `slug` por `agentId` e delegam ao `PowerBIService`. As que dependem de credencial (teste, workspaces, vincular, gerar schema) devolvem a mensagem "O Power BI deste agente ainda nao tem credencial..." quando `AgentPowerBIConfig` não existe, antes de chamar a Microsoft.

| # | Ferramenta | Entrada | Saída | Anotações | Faz |
|---|---|---|---|---|---|
| 11 | `obter_powerbi` | `agentId` | `PowerBIMcpInfo` | ReadOnly | `GetConfigAsync` + `GetDatasetsAsync`, projetados para `PowerBIMcpInfo` (sem `tenantId`, `clientId`, `clientSecretMasked`). |
| 12 | `definir_powerbi_ativo` | `agentId`, `enabled: bool` | `PowerBIMcpInfo` | Idempotent | `SetEnabledAsync`. Ligar sem credencial → recusa com orientação (US4 cenário 2). |
| 13 | `testar_conexao_powerbi` | `agentId` | `PowerBIConnectionTestInfo` | ReadOnly | `TestConnectionAsync` (resultado por etapa, igual ao painel; grava o último teste). |
| 14 | `listar_workspaces_powerbi` | `agentId` | `PowerBIWorkspaceInfo[]` (cada um com seus datasets disponíveis) | ReadOnly | `ListWorkspacesAsync`. |
| 15 | `listar_datasets_powerbi` | `agentId` | `PowerBIDatasetInfo[]` (vinculados) | ReadOnly | `GetDatasetsAsync`. |
| 16 | `vincular_dataset_powerbi` | `agentId`, `workspaceId`, `datasetId`, `name`, `description?` | `PowerBIDatasetInfo` | — | Valida com `PowerBIDatasetInsertInfoValidator`; `CreateDatasetAsync`. |
| 17 | `atualizar_dataset_powerbi` | `agentId`, `powerBIDatasetId`, `name?`, `description?` | `PowerBIDatasetInfo` | Idempotent | Carrega o dataset, preserva `workspaceId`/`datasetId`, `UpdateDatasetAsync` com nome/descrição novos. |
| 18 | `desvincular_dataset_powerbi` | `agentId`, `powerBIDatasetId` | `{ message }` | Destructive | `DeleteDatasetAsync`. |
| 19 | `gerar_schema_powerbi` | `agentId`, `powerBIDatasetId` | `PowerBIDatasetSchemaInfo` | — | `GenerateSchemaAsync` (preserva descrições do usuário, como o painel). |
| 20 | `obter_schema_powerbi` | `agentId`, `powerBIDatasetId` | `PowerBIDatasetSchemaInfo` | ReadOnly | `GetSchemaAsync`. |
| 21 | `descrever_schema_powerbi` | `agentId`, `powerBIDatasetId`, `items: [{ kind: table\|column\|measure, table, name?, userDescription }]` | `PowerBIDatasetSchemaInfo` | Idempotent | `UpdateSchemaDescriptionsAsync` com `PowerBISchemaDescriptionUpdateInfo`. |

## Canais (`ChannelTools`)

| # | Ferramenta | Entrada | Saída | Anotações | Faz |
|---|---|---|---|---|---|
| 22 | `obter_telegram` | `agentId` | `TelegramMcpInfo` | ReadOnly | Se `TelegramBotToken` vazio: `{ botName, botConfigured: false, webhookRegistered: false }` sem chamar o Telegram. Senão `TelegramService.GetWebhookInfoAsync` → `webhookRegistered`, `webhookUrl`. Nunca token nem segredo. |
| 23 | `registrar_webhook_telegram` | `agentId` | `TelegramMcpInfo` | Idempotent | `SetupWebhookAsync`; sem token → mensagem de orientação. Não existe ferramenta para regenerar o segredo (FR-015). |
| 24 | `obter_whatsapp` | `agentId` | `WhatsappMcpInfo` | ReadOnly | `WhatsappService.GetStatusAsync(slug, owner)`; falha do WPP Connect vira mensagem de erro legível. Sem start-session, QR code ou disconnect (FR-016). |

## Base de conhecimento (`KnowledgeTools`)

| # | Ferramenta | Entrada | Saída | Anotações | Faz |
|---|---|---|---|---|---|
| 25 | `listar_documentos` | `agentId` | `KnowledgeFileInfo[]` | ReadOnly | `KnowledgeFileService.ListAsync`. |
| 26 | `adicionar_documento` | `agentId`, `fileName`, `content` | `KnowledgeFileInfo` | — | `KnowledgeFileService.AddTextAsync`: acrescenta `.md` se faltar, limite 10 MB (UTF-8), cria com `Processing` e dispara a ingestão em background, igual ao upload. |
| 27 | `reprocessar_documento` | `agentId`, `knowledgeFileId` | `KnowledgeFileInfo` | Idempotent | `KnowledgeFileService.ReprocessAsync`. |
| 28 | `remover_documento` | `agentId`, `knowledgeFileId` | `{ message }` | Destructive | `KnowledgeFileService.DeleteAsync` (apaga chunks no Elasticsearch e o registro). |

## O que não existe de propósito

| Operação do painel | Por que não está no MCP |
|---|---|
| `DELETE /agents/{id}` | FR-008 |
| `openAIApiKey` / `removeOpenAIApiKey` em criar/atualizar; `POST /agents/{id}/openai/diagnose` | FR-009/FR-010 (o diagnóstico aceita chave no corpo) |
| `PUT /powerbi/{slug}/config` (tenant, client, secret) | FR-010 |
| `telegramBotToken` em criar/atualizar; `POST /telegram/{id}/regenerate-secret` | FR-015 |
| `POST /whatsapp/{slug}/start-session`, `GET .../qrcode`, `POST .../disconnect` | FR-016 |
| Upload de arquivo binário | premissa da spec (só texto) |
| Cadastro, login, troca de senha, gestão de chaves | fora do escopo |

## Exemplo de chamada

```json
{ "jsonrpc": "2.0", "id": 3, "method": "tools/call",
  "params": { "name": "atualizar_agente",
              "arguments": { "agentId": 12, "systemPrompt": "Voce e o assistente da Loja X. Responda so sobre trocas e devolucoes." } } }
```

Resposta:
```json
{ "jsonrpc": "2.0", "id": 3,
  "result": { "isError": false,
              "content": [{ "type": "text", "text": "{\"agentId\":12,\"name\":\"Suporte Loja\",\"slug\":\"suporte-loja\",...,\"hasOpenAIApiKey\":true,\"telegramBotConfigured\":false}" }],
              "structuredContent": { "agentId": 12, "name": "Suporte Loja", "...": "..." } } }
```

Mesma chamada com `"openAIApiKey": "sk-..."` nos argumentos: resposta com `isError: true` e a mensagem do guarda de segredos; o prompt **não** é alterado.
