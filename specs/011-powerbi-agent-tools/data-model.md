# Data Model: Integração do Agente com Power BI

**Feature**: 011-powerbi-agent-tools | **Date**: 2026-10-07

Convenções seguem o `AvaBotContext` existente: prefixo de tabela `avabot_`, colunas em snake_case, PK `bigint` identity `{entidade}_id`, `timestamp without time zone`. O delete behavior é `ClientSetNull` nas FKs novas, e a remoção dos filhos é feita explicitamente em `AgentRepository.DeleteAsync`, como já ocorre com sessões e arquivos.

## Alteração: `Agent` (`avabot_agents`)

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `PowerBIEnabled` | `powerbi_enabled` | `boolean NOT NULL DEFAULT false` | Só pode virar `true` se houver `AgentPowerBIConfig` com segredo e ao menos 1 dataset com `SchemaStatus` = Generated ou Partial (FR-006). |

Navegações novas: `PowerBIConfig` (0..1), `PowerBIDatasets` (0..N).

## Nova: `AgentPowerBIConfig` (`avabot_agent_powerbi_configs`)

Credenciais do Power BI do agente. Relação 1:1 com `Agent`.

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `AgentPowerBIConfigId` | `agent_powerbi_config_id` | `bigint identity PK` | |
| `AgentId` | `agent_id` | `bigint NOT NULL` | FK → `avabot_agents`, índice único |
| `TenantId` | `tenant_id` | `varchar(64) NOT NULL` | GUID válido |
| `ClientId` | `client_id` | `varchar(64) NOT NULL` | GUID válido |
| `ClientSecretEncrypted` | `client_secret_encrypted` | `varchar(1000) NOT NULL` | AES-256-GCM em base64. Nunca exposto. |
| `ClientSecretHint` | `client_secret_hint` | `varchar(8) NOT NULL` | 4 últimos caracteres, usados na máscara `••••XYZW` |
| `LastTestAt` | `last_test_at` | `timestamp NULL` | |
| `LastTestSuccess` | `last_test_success` | `boolean NULL` | |
| `LastTestMessage` | `last_test_message` | `varchar(2000) NULL` | |
| `CreatedAt` / `UpdatedAt` | `created_at` / `updated_at` | `timestamp NOT NULL` | |

Regras:
- Em um update com `clientSecret` vazio ou nulo, o segredo existente é mantido (FR-003). Na criação, o segredo é obrigatório.
- Alterar `TenantId`, `ClientId` ou o segredo invalida o cache de token dessa credencial.

## Nova: `PowerBIDataset` (`avabot_powerbi_datasets`)

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `PowerBIDatasetId` | `powerbi_dataset_id` | `bigint identity PK` | |
| `AgentId` | `agent_id` | `bigint NOT NULL` | FK → `avabot_agents` |
| `WorkspaceId` | `workspace_id` | `varchar(64) NOT NULL` | GUID |
| `DatasetId` | `dataset_id` | `varchar(64) NOT NULL` | GUID. Único por `(agent_id, dataset_id)`. |
| `Name` | `name` | `varchar(120) NOT NULL` | Obrigatório |
| `Description` | `description` | `varchar(1000) NULL` | Descrição de negócio, enviada ao modelo |
| `ToolKey` | `tool_key` | `varchar(60) NOT NULL` | Slug de `Name` (`[a-z0-9_]`), único por agente. É o valor do enum nas tools. |
| `SchemaJson` | `schema_json` | `jsonb NULL` | Ver formato abaixo |
| `SchemaStatus` | `schema_status` | `integer NOT NULL DEFAULT 0` | Enum `PowerBISchemaStatus` |
| `SchemaGeneratedAt` | `schema_generated_at` | `timestamp NULL` | Data da última geração bem-sucedida |
| `SchemaError` | `schema_error` | `varchar(2000) NULL` | Erro da última tentativa |
| `CreatedAt` / `UpdatedAt` | `created_at` / `updated_at` | `timestamp NOT NULL` | |

**Enum `PowerBISchemaStatus`** (`AvaBot.Domain/Enums`): `NotGenerated = 0`, `Generated = 1`, `Partial = 2` (fallback `COLUMNSTATISTICS`, sem medidas e tipos), `Error = 3`.

Transições:
```
NotGenerated --gerar ok--> Generated | Partial
NotGenerated --gerar falha--> Error              (SchemaJson continua null)
Generated|Partial --gerar ok--> Generated | Partial   (merge preserva userDescription)
Generated|Partial --gerar falha--> Error           (SchemaJson anterior preservado, FR-010)
Error --gerar ok--> Generated | Partial
```

Um dataset é **utilizável pelo agente** quando `SchemaJson != null`, mesmo com status `Error`, desde que exista um schema anterior.

### Formato de `SchemaJson`

```json
{
  "tables": [
    {
      "name": "Exportacoes",
      "description": "descrição vinda do modelo (se houver)",
      "userDescription": "descrição do administrador",
      "columns": [
        { "name": "Data", "dataType": "DateTime", "description": null, "userDescription": "use para filtros por período" }
      ],
      "measures": [
        { "name": "Valor FOB (US$)", "description": null, "userDescription": "valor em dólares" }
      ]
    }
  ]
}
```

O merge ao regerar mantém `userDescription` quando a chave (`table`, `table+column`, `table+measure`) já existia. Itens que deixaram de existir são descartados.

## Nova: `PowerBIQueryLog` (`avabot_powerbi_query_logs`)

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `PowerBIQueryLogId` | `powerbi_query_log_id` | `bigint identity PK` | |
| `AgentId` | `agent_id` | `bigint NOT NULL` | FK → `avabot_agents` |
| `ChatSessionId` | `chat_session_id` | `bigint NULL` | FK → `avabot_chat_sessions` (`null` na página de teste) |
| `PowerBIDatasetId` | `powerbi_dataset_id` | `bigint NULL` | FK → `avabot_powerbi_datasets`, `ClientSetNull` |
| `DatasetName` | `dataset_name` | `varchar(120) NULL` | Cópia do nome, para manter o histórico legível após remoção |
| `ToolName` | `tool_name` | `varchar(60) NOT NULL` | `listar_schema` ou `consultar_bi` |
| `UserQuestion` | `user_question` | `text NULL` | Mensagem do usuário que originou a consulta |
| `Query` | `query` | `text NULL` | DAX executado |
| `DurationMs` | `duration_ms` | `integer NOT NULL` | |
| `RowCount` | `row_count` | `integer NULL` | |
| `Truncated` | `truncated` | `boolean NOT NULL DEFAULT false` | |
| `Status` | `status` | `integer NOT NULL` | Enum `PowerBIQueryStatus`: `Success = 1`, `Error = 2`, `Timeout = 3` |
| `ErrorMessage` | `error_message` | `varchar(2000) NULL` | Sem segredos |
| `CreatedAt` | `created_at` | `timestamp NOT NULL` | Índice `(agent_id, created_at DESC)` |

Retenção: remoção automática após `PowerBI:QueryLogRetentionDays` (padrão 30) por `PowerBIQueryLogCleanupService`.

## Exclusão do agente

`AgentRepository.DeleteAsync` passa a remover também, antes do agente: `PowerBIQueryLog`, `PowerBIDataset` e `AgentPowerBIConfig` do agente.

## Configuração (`appsettings` → seção `PowerBI`)

| Chave | Padrão | Uso |
|---|---|---|
| `SecretEncryptionKey` | (obrigatória se a feature for usada) | Chave AES-256 em base64, via env `PowerBI__SecretEncryptionKey` |
| `MaxRows` | 100 | FR-020 |
| `MaxToolCallsPerMessage` | 5 | FR-021 |
| `QueryTimeoutSeconds` | 30 | FR-021 |
| `QueryLogRetentionDays` | 30 | FR-027b |
| `ApiBaseUrl` | `https://api.powerbi.com/v1.0/myorg` | |
| `AuthorityBaseUrl` | `https://login.microsoftonline.com` | |
