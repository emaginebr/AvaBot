# Contract: API REST Power BI (admin)

**Feature**: 011-powerbi-agent-tools

Todos os endpoints exigem `[Authorize]` (JWT Bearer do admin) e retornam o envelope `Result<T>` existente (`sucesso`, `mensagem`, `erros`, `dados`). O padrão de rota segue o `WhatsappController` (`/{recurso}/{slug}/...`).

Erros: `404` para agente ou dataset não encontrado, `400` para validação ou `InvalidOperationException`, `500` para os demais casos.

## Credenciais e flag

### `GET /powerbi/{slug}/config` → `Result<PowerBIConfigInfo>`

```json
{
  "agentId": 1,
  "enabled": false,
  "isConfigured": true,
  "tenantId": "d375fd58-...",
  "clientId": "e686fb17-...",
  "hasClientSecret": true,
  "clientSecretMasked": "••••CudiY",
  "lastTestAt": "2026-10-07T14:00:00",
  "lastTestSuccess": false,
  "lastTestMessage": "Sem permissão de consulta no dataset ..."
}
```
Quando o agente ainda não tem credenciais, retorna `isConfigured: false` e os demais campos nulos. **Nunca** retorna o segredo.

### `PUT /powerbi/{slug}/config` → `Result<PowerBIConfigInfo>`

```json
{ "tenantId": "guid", "clientId": "guid", "clientSecret": "opcional no update" }
```
Validação: `tenantId` e `clientId` precisam ser GUIDs, e `clientSecret` é obrigatório se ainda não houver segredo salvo.

### `POST /powerbi/{slug}/test` → `Result<PowerBIConnectionTestInfo>`

Executa os passos em ordem e para no primeiro erro de autenticação.

```json
{
  "success": false,
  "steps": [
    { "step": "auth",       "success": true,  "message": "Token obtido" },
    { "step": "workspaces", "success": true,  "message": "1 workspace acessível" },
    { "step": "dataset:Comércio Internacional", "success": false,
      "message": "Credenciais válidas, mas o aplicativo não tem permissão para consultar este dataset. Conceda papel Contributor no workspace ou permissão Build no dataset." }
  ]
}
```
O resultado é gravado em `LastTest*`. O passo por dataset executa `EVALUATE ROW("ok", 1)`.

### `PUT /powerbi/{slug}/enabled` → `Result<PowerBIConfigInfo>`

```json
{ "enabled": true }
```
Retorna `400` com a mensagem do que falta ("Configure as credenciais" ou "Gere o schema de ao menos um dataset") quando FR-006 não é satisfeito.

## Descoberta (apoio ao cadastro)

### `GET /powerbi/{slug}/workspaces` → `Result<PowerBIWorkspaceInfo[]>`

Lista os workspaces e datasets visíveis às credenciais, para preencher selects no cadastro.
```json
[ { "workspaceId": "a9b9...", "name": "BI ABIPESCA",
    "datasets": [ { "datasetId": "eab9...", "name": "ABIPESCA - Comércio Internacional" } ] } ]
```

## Datasets

### `GET /powerbi/{slug}/datasets` → `Result<PowerBIDatasetInfo[]>`

```json
[ {
  "powerBIDatasetId": 10, "workspaceId": "guid", "datasetId": "guid",
  "name": "Comércio Internacional", "description": "Exportações e importações de pescado por país, produto e mês",
  "toolKey": "comercio_internacional",
  "schemaStatus": 1, "schemaGeneratedAt": "2026-10-07T14:05:00", "schemaError": null,
  "tableCount": 8, "columnCount": 64, "measureCount": 21
} ]
```

### `POST /powerbi/{slug}/datasets` → `Result<PowerBIDatasetInfo>`

```json
{ "workspaceId": "guid", "datasetId": "guid", "name": "string (1-120)", "description": "string (0-1000)" }
```
Retorna `400` se o dataset já estiver vinculado ao agente.

### `PUT /powerbi/{slug}/datasets/{id}` → `Result<PowerBIDatasetInfo>`

Mesmo body do POST. Alterar `datasetId` ou `workspaceId` reseta o schema (`NotGenerated`).

### `DELETE /powerbi/{slug}/datasets/{id}` → `Result<bool>`

Se for o último dataset utilizável e a flag estiver ativa, a flag é desligada automaticamente e a mensagem informa isso.

## Schema

### `POST /powerbi/{slug}/datasets/{id}/schema/generate` → `Result<PowerBIDatasetSchemaInfo>`

Gera o schema (síncrono, até 60 s) e faz o merge com as descrições existentes. Em caso de falha retorna `400` com a mensagem e preserva o schema anterior.

### `GET /powerbi/{slug}/datasets/{id}/schema` → `Result<PowerBIDatasetSchemaInfo>`

```json
{
  "powerBIDatasetId": 10, "schemaStatus": 1, "schemaGeneratedAt": "...", "schemaError": null,
  "tables": [ {
    "name": "Exportacoes", "description": null, "userDescription": "...",
    "columns": [ { "name": "Data", "dataType": "DateTime", "description": null, "userDescription": null } ],
    "measures": [ { "name": "Valor FOB (US$)", "description": null, "userDescription": "valor em dólares" } ]
  } ]
}
```

### `PUT /powerbi/{slug}/datasets/{id}/schema/descriptions` → `Result<PowerBIDatasetSchemaInfo>`

```json
{ "items": [
  { "kind": "table",   "table": "Exportacoes", "name": null,              "userDescription": "..." },
  { "kind": "column",  "table": "Exportacoes", "name": "Data",            "userDescription": "..." },
  { "kind": "measure", "table": "Exportacoes", "name": "Valor FOB (US$)", "userDescription": "..." }
] }
```
`userDescription` vazio remove a descrição. Itens inexistentes retornam `400`.

## Histórico de consultas

### `GET /powerbi/{slug}/query-logs?page=1&pageSize=20` → `Result<PowerBIQueryLogPageInfo>`

```json
{
  "page": 1, "pageSize": 20, "total": 134,
  "items": [ {
    "powerBIQueryLogId": 991, "createdAt": "...", "chatSessionId": 55,
    "datasetName": "Comércio Internacional", "toolName": "consultar_bi",
    "userQuestion": "Quanto exportamos de tilápia em 2025?",
    "query": "EVALUATE SUMMARIZECOLUMNS(...)",
    "durationMs": 840, "rowCount": 12, "truncated": false,
    "status": 1, "errorMessage": null
  } ]
}
```
Ordenado por `createdAt DESC`. `pageSize` máximo: 100.

## Alterações em contratos existentes

- `AgentInfo` ganha `powerBIEnabled: boolean` (somente leitura; só é alterado via `PUT /powerbi/{slug}/enabled`).
- `AgentTestResultInfo` (`POST /agents/{id}/test`) ganha `powerBIQueries: [{ toolName, datasetName, query, durationMs, rowCount, truncated, success, error, resultPreview }]` (FR-028). A lista fica vazia quando a flag está desligada.
