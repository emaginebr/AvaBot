# Data Model: MCP de gestão de agentes

**Feature**: 017-mcp-agent-management | **Date**: 2026-10-12

Convenções do `AvaBotContext`: prefixo `avabot_`, colunas snake_case, PK `bigint` identity `{entidade}_id`, `timestamp without time zone`, strings `varchar` com tamanho, status `integer`.

## Nova: `McpAccessKey` (`avabot_mcp_access_keys`)

Chave criada pelo usuário para conectar um assistente. Pertence a exatamente um usuário.

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `McpAccessKeyId` | `mcp_access_key_id` | `bigint identity PK` (`avabot_mcp_access_keys_pkey`) | |
| `UserId` | `user_id` | `bigint NOT NULL` | FK `avabot_fk_users_mcp_access_keys` → `avabot_users.user_id`, `Restrict` (ver nota). Índice `ix_avabot_mcp_access_keys_user_id`. |
| `Name` | `name` | `varchar(100) NOT NULL` | 1 a 100 caracteres, sem espaços nas pontas. Repetido permitido. |
| `KeyHash` | `key_hash` | `varchar(64) NOT NULL` | SHA-256 em hexa minúsculo do valor em claro. Índice único `avabot_mcp_access_keys_key_hash_key`. É por ele que o handler de autenticação acha a chave. |
| `KeyPrefix` | `key_prefix` | `varchar(20) NOT NULL` | Os 8 primeiros caracteres depois de `avabot_mcp_`, só para o usuário reconhecer a chave na lista (`avabot_mcp_Ab3kQ9xZ…`). Não permite reconstruir a chave. |
| `Status` | `status` | `integer NOT NULL DEFAULT 1` | 1 = ativa, 0 = revogada. Única transição: 1 → 0 (revogação). Não volta. |
| `CreatedAt` | `created_at` | `timestamp NOT NULL` | |
| `LastUsedAt` | `last_used_at` | `timestamp NULL` | Nulo = "nunca usada". Atualizado pelo handler de autenticação no máximo uma vez por minuto por chave (research R3). |
| `RevokedAt` | `revoked_at` | `timestamp NULL` | Preenchido na revogação. |

Navegação: `User` (`User`).

Comportamento no modelo (só BCL):
- `static string GenerateKey()`: `"avabot_mcp_" + Base64Url(RandomNumberGenerator.GetBytes(32))` (43 caracteres após o prefixo).
- `static string ComputeHash(string key)`: SHA-256 → hexa minúsculo.
- `static bool HasExpectedFormat(string key)`: prefixo `avabot_mcp_` e comprimento total 54; evita consultar o banco para um JWT do painel ou lixo.
- `bool IsActive => Status == 1`.

Nota sobre a FK: a constituição pede `ClientSetNull` para FKs novas, o que exigiria `user_id` nulo. Uma chave sem dono não tem significado (nenhuma operação poderia ser atribuída a uma conta), então `user_id` é `NOT NULL` com `Restrict`; nunca `Cascade`. Registrado no Complexity Tracking do plano. Não existe exclusão de usuário no produto, então o `Restrict` nunca dispara na prática.

## Alterações em entidades existentes

Nenhuma coluna nova. `Agent`, `ChatSession`, `ChatMessage`, `KnowledgeFile`, `AgentPowerBIConfig`, `PowerBIDataset`, `PowerBIQueryLog`, `TelegramChat` e `User` ficam como estão.

Alterações só de leitura (métodos de repositório):
- `IPowerBIQueryLogRepository<T>.GetPagedByAgentAsync(agentId, page, pageSize, PowerBIQueryStatus? status = null)`: filtro opcional por situação (FR-012).
- `ITelegramChatRepository<T>.GetSessionIdsAsync(IEnumerable<long> chatSessionIds)`: devolve quais sessões vieram do Telegram, para o campo `canal` de `listar_sessoes` (research R7).
- `IMcpAccessKeyRepository<T>` (novo): `GetByIdAsync(id, userId)`, `GetByHashAsync(keyHash)` (inclui `User`), `GetAllByUserAsync(userId)`, `CountActiveByUserAsync(userId)`, `CreateAsync`, `UpdateAsync`.

`AvaBot.Calibration/Local/LocalRepositories.cs` implementa as assinaturas novas (`GetPagedByAgentAsync` com o parâmetro extra; `GetSessionIdsAsync` devolvendo vazio).

## Migração

`AddMcpAccessKeys` (`AvaBot.Infra/Migrations`), aplicada automaticamente na subida pelo `StartupMigrator`:

```text
Up:
  CREATE TABLE avabot_mcp_access_keys (
    mcp_access_key_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id bigint NOT NULL,
    name varchar(100) NOT NULL,
    key_hash varchar(64) NOT NULL,
    key_prefix varchar(20) NOT NULL,
    status integer NOT NULL DEFAULT 1,
    created_at timestamp NOT NULL,
    last_used_at timestamp NULL,
    revoked_at timestamp NULL,
    CONSTRAINT avabot_fk_users_mcp_access_keys FOREIGN KEY (user_id) REFERENCES avabot_users (user_id) ON DELETE RESTRICT
  )
  CREATE UNIQUE INDEX avabot_mcp_access_keys_key_hash_key ON avabot_mcp_access_keys (key_hash)
  CREATE INDEX ix_avabot_mcp_access_keys_user_id ON avabot_mcp_access_keys (user_id)
Down:
  DROP TABLE avabot_mcp_access_keys
```

## Ciclo de vida da chave

```text
[criar no painel] → ativa (last_used_at = null, "nunca usada")
ativa --(assistente chama /mcp)--> ativa (last_used_at = agora, no máximo 1x/min)
ativa --(revogar no painel)--> revogada (status 0, revoked_at = agora)
revogada --(qualquer chamada /mcp)--> 401 "Chave de acesso invalida ou revogada"
dono com status 0 --(qualquer chamada /mcp)--> 401, mesma mensagem (todas as chaves da conta)
```

Não existe expiração nem renovação; troca de senha não afeta chaves (premissas da spec).

## Identidade do chamador (não persistida)

O `McpAccessKeyAuthenticationHandler` monta o `ClaimsPrincipal` da requisição em `/mcp`:

| Claim | Valor |
|---|---|
| `sub` / `NameIdentifier` | `User.UserId` |
| `email` | e-mail do dono |
| `name` | nome do dono |
| `mcp_key_id` | `McpAccessKeyId` |
| `mcp_key_name` | `Name` (para `quem_sou_eu`) |

Lido pelas ferramentas via `McpCaller.From(context)` → `{ UserId, KeyId, KeyName }`.

## DTOs

### Painel: chaves (`AvaBot.DTO/McpAccessKeyDTOs.cs`)

| DTO | Campos (JSON camelCase) | Uso |
|---|---|---|
| `McpAccessKeyInfo` | `mcpAccessKeyId`, `name`, `keyPrefix`, `status`, `createdAt`, `lastUsedAt`, `revokedAt` | `GET /auth/me/mcp-keys` e dentro de `McpAccessKeyCreatedInfo` |
| `McpAccessKeyInsertInfo` | `name` | `POST /auth/me/mcp-keys` |
| `McpAccessKeyCreatedInfo` | `key` (valor em claro, **só aqui**), `accessKey: McpAccessKeyInfo` | resposta do `POST` |

Validação (FluentValidation, `McpAccessKeyInsertInfoValidator`): nome obrigatório, 1–100 após `Trim`.

### MCP: entrada e saída das ferramentas (`AvaBot.DTO/McpDTOs.cs`)

Nenhum DTO abaixo tem campo de segredo. `AgentInfo` e `PowerBIConfigInfo` do painel **não** são usados pelo MCP.

| DTO | Campos | Observação |
|---|---|---|
| `McpIdentityInfo` | `userId`, `name`, `email`, `accessKeyName` | `quem_sou_eu` |
| `AgentMcpSummaryInfo` | `agentId`, `name`, `slug`, `chatModel`, `active`, `hasOpenAIApiKey`, `powerBIEnabled`, `telegramBotName`, `updatedAt` | `listar_agentes` |
| `AgentMcpInfo` | tudo de `AgentMcpSummaryInfo` + `description`, `systemPrompt`, `collectName`, `collectEmail`, `collectPhone`, `telegramBotConfigured`, `whatsappConfigured`, `createdAt` | `obter_agente`, `criar_agente`, `atualizar_agente`, `definir_agente_ativo` |
| `AgentMcpCreateInfo` | `name`, `systemPrompt`, `description?`, `chatModel?` (padrão `gpt-4o`), `collectName?`, `collectEmail?`, `collectPhone?`, `telegramBotName?` | entrada de `criar_agente`; convertido em `AgentInsertInfo` sem segredos |
| `AgentProfileUpdateInfo` | `name?`, `description?`, `systemPrompt?`, `chatModel?`, `collectName?`, `collectEmail?`, `collectPhone?`, `telegramBotName?` | entrada de `atualizar_agente`; `null` = manter; usado por `AgentService.UpdateProfileAsync` |
| `ChatSessionMcpInfo` | `chatSessionId`, `startedAt`, `endedAt`, `channel` (`web`/`telegram`), `userName`, `userEmail`, `userPhone`, `messageCount` | `listar_sessoes` (sem `resumeToken`) |
| `ChatMessageMcpInfo` | `chatMessageId`, `role` (`visitante`/`agente`), `content`, `createdAt` | `listar_mensagens` |
| `PowerBIMcpInfo` | `enabled`, `credentialConfigured`, `lastTestAt`, `lastTestSuccess`, `lastTestMessage`, `datasets: PowerBIDatasetInfo[]` | `obter_powerbi`, `definir_powerbi_ativo` |
| `TelegramMcpInfo` | `botName`, `botConfigured`, `webhookRegistered`, `webhookUrl` | `obter_telegram`, `registrar_webhook_telegram` |
| `WhatsappMcpInfo` | `status`, `isConnected` | `obter_whatsapp` |
| `KnowledgeDocumentMcpInsertInfo` | `fileName`, `content` | entrada de `adicionar_documento` |

Reutilizados do painel sem alteração (já não têm segredo): `PaginatedResult<T>`, `PowerBIQueryLogPageInfo`/`PowerBIQueryLogInfo`, `PowerBIConnectionTestInfo`, `PowerBIWorkspaceInfo`, `PowerBIDatasetInfo`, `PowerBIDatasetInsertInfo`, `PowerBIDatasetSchemaInfo`, `PowerBISchemaDescriptionUpdateInfo`, `AgentTestQuestionInfo`/`AgentTestMessageInfo`/`AgentTestResultInfo`, `KnowledgeFileInfo`.

## Limites

| Item | Valor | Origem |
|---|---|---|
| Chaves ativas por conta | 20 | research R9 |
| `listar_sessoes` pageSize | padrão 20, máximo 100 | igual a `GET /sessions/agents/{id}` |
| `listar_mensagens` pageSize | padrão 50, máximo 200 | igual a `GET /sessions/{id}/messages` |
| `listar_consultas_bi` pageSize | padrão 20, máximo 100 | igual a `GET /powerbi/{slug}/query-logs` |
| Documento de texto | 10 MB em UTF-8; nome termina em `.md` (acrescentado se faltar) | igual a `POST /files/{agentId}` |
| Histórico de `testar_agente` | papéis `user`/`assistant`, conteúdo não vazio; corte em `Chat:MaxHistoryMessages` | igual a `POST /agents/{id}/test` |
