# Contrato de API — Arquivos de Conhecimento

> Versão: 1.0  
> Base URL: `/api/agents/{agentId}/files`  
> Autenticação: todos os endpoints requerem JWT no header `Authorization: Bearer <token>` (Admin)

---

## Modelos de Dados (DTOs)

### KnowledgeFileInfo — Leitura

Retornado nas respostas de leitura.

```csharp
public class KnowledgeFileInfo
{
    [JsonPropertyName("knowledgeFileId")]   public Guid     KnowledgeFileId   { get; set; }
    [JsonPropertyName("agentId")]           public Guid     AgentId           { get; set; }
    [JsonPropertyName("fileName")]          public string   FileName          { get; set; }
    [JsonPropertyName("fileSize")]          public long     FileSize          { get; set; }
    [JsonPropertyName("processingStatus")] public int      ProcessingStatus  { get; set; }
    [JsonPropertyName("errorMessage")]      public string?  ErrorMessage      { get; set; }
    [JsonPropertyName("createdAt")]         public DateTime CreatedAt         { get; set; }
    [JsonPropertyName("updatedAt")]         public DateTime UpdatedAt         { get; set; }
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `knowledgeFileId` | `uuid` | Identificador único do arquivo |
| `agentId` | `uuid` | Identificador do agente ao qual o arquivo pertence |
| `fileName` | `string` | Nome original do arquivo enviado |
| `fileSize` | `long` | Tamanho do arquivo em bytes |
| `processingStatus` | `int` | Status do processamento (ver tabela abaixo) |
| `errorMessage` | `string?` | Mensagem de erro quando `processingStatus = 2`; nulo caso contrário |
| `createdAt` | `datetime` | Data de upload (ISO 8601 UTC) |
| `updatedAt` | `datetime` | Data da última atualização de status (ISO 8601 UTC) |

#### Valores de processingStatus

| Valor | Constante | Descrição |
|---|---|---|
| `0` | `Processing` | Arquivo recebido; ingestão em andamento |
| `1` | `Ready` | Ingestão concluída; chunks indexados |
| `2` | `Error` | Falha durante o processamento |

---

### Result\<T\> — Envelope de Resposta

Todas as respostas seguem o envelope padrão do projeto:

```csharp
public class Result<T>
{
    [JsonPropertyName("sucesso")] public bool     Sucesso  { get; set; }
    [JsonPropertyName("mensagem")]public string   Mensagem { get; set; }
    [JsonPropertyName("erros")]   public string[] Erros    { get; set; }
    [JsonPropertyName("dados")]   public T        Dados    { get; set; }
}
```

---

## Endpoints

### GET /api/agents/{agentId}/files

Lista todos os arquivos de conhecimento associados a um agente.

**Acesso:** Admin

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `agentId` | `uuid` | Identificador do agente |

**Parâmetros de query:**

| Parâmetro | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `pagina` | `int` | Não | Página atual (padrão: `1`) |
| `tamanhoPagina` | `int` | Não | Itens por página (padrão: `20`, máx.: `100`) |
| `status` | `int` | Não | Filtrar por `processingStatus` |

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Arquivos listados com sucesso.",
  "erros": [],
  "dados": {
    "items": [
      {
        "knowledgeFileId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "fileName": "manual-produto.md",
        "fileSize": 45312,
        "processingStatus": 1,
        "errorMessage": null,
        "createdAt": "2026-04-01T09:00:00Z",
        "updatedAt": "2026-04-01T09:02:15Z"
      },
      {
        "knowledgeFileId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
        "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "fileName": "politica-suporte.md",
        "fileSize": 12048,
        "processingStatus": 2,
        "errorMessage": "Arquivo malformado: seção sem título encontrada na linha 47.",
        "createdAt": "2026-04-05T11:30:00Z",
        "updatedAt": "2026-04-05T11:30:45Z"
      }
    ],
    "total": 2,
    "pagina": 1,
    "tamanhoPagina": 20
  }
}
```

**Resposta 404 Not Found** (agente inexistente):

```json
{
  "sucesso": false,
  "mensagem": "Agente não encontrado.",
  "erros": [],
  "dados": null
}
```

---

### POST /api/agents/{agentId}/files

Faz upload de um arquivo `.md` para ingestão como base de conhecimento do agente.

**Acesso:** Admin

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `agentId` | `uuid` | Identificador do agente |

**Content-Type:** `multipart/form-data`

**Campos do formulário:**

| Campo | Tipo | Obrigatório | Regras |
|---|---|---|---|
| `file` | `file` | Sim | Extensão `.md`; tamanho máximo: **10 MB** |

**Exemplo de requisição (curl):**

```bash
curl -X POST "https://host/api/agents/3fa85f64-5717-4562-b3fc-2c963f66afa6/files" \
  -H "Authorization: Bearer <token>" \
  -F "file=@manual-produto.md"
```

**Resposta 201 Created:**

O arquivo é aceito imediatamente com `processingStatus = 0` (processando). A ingestão ocorre de forma assíncrona em background.

```json
{
  "sucesso": true,
  "mensagem": "Arquivo enviado. Processamento iniciado.",
  "erros": [],
  "dados": {
    "knowledgeFileId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fileName": "manual-produto.md",
    "fileSize": 45312,
    "processingStatus": 0,
    "errorMessage": null,
    "createdAt": "2026-04-08T12:00:00Z",
    "updatedAt": "2026-04-08T12:00:00Z"
  }
}
```

**Resposta 400 Bad Request:**

```json
{
  "sucesso": false,
  "mensagem": "Arquivo inválido.",
  "erros": [
    "Somente arquivos com extensão .md são permitidos.",
    "O arquivo excede o tamanho máximo permitido de 10 MB."
  ],
  "dados": null
}
```

**Resposta 404 Not Found** (agente inexistente):

```json
{
  "sucesso": false,
  "mensagem": "Agente não encontrado.",
  "erros": [],
  "dados": null
}
```

---

### DELETE /api/agents/{agentId}/files/{fileId}

Remove um arquivo de conhecimento e todos os chunks indexados associados a ele.

**Acesso:** Admin

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `agentId` | `uuid` | Identificador do agente |
| `fileId` | `uuid` | Identificador do arquivo |

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Arquivo e chunks removidos com sucesso.",
  "erros": [],
  "dados": null
}
```

**Resposta 404 Not Found:**

```json
{
  "sucesso": false,
  "mensagem": "Arquivo não encontrado.",
  "erros": [],
  "dados": null
}
```

> **Nota:** A exclusão é irreversível. Todos os chunks vetorizados derivados do arquivo são removidos do índice de busca semântica.

---

### POST /api/agents/{agentId}/files/{fileId}/reprocess

Reprocessa um arquivo já existente, recriando seus chunks e atualizando o índice de busca. Útil para arquivos com status `2` (erro) ou para forçar reindexação após mudanças no pipeline de ingestão.

**Acesso:** Admin

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `agentId` | `uuid` | Identificador do agente |
| `fileId` | `uuid` | Identificador do arquivo |

**Corpo da requisição:** nenhum (sem payload).

**Resposta 200 OK:**

O status é redefinido para `0` (processando) e a ingestão é disparada novamente.

```json
{
  "sucesso": true,
  "mensagem": "Reprocessamento iniciado.",
  "erros": [],
  "dados": {
    "knowledgeFileId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fileName": "politica-suporte.md",
    "fileSize": 12048,
    "processingStatus": 0,
    "errorMessage": null,
    "createdAt": "2026-04-05T11:30:00Z",
    "updatedAt": "2026-04-08T13:00:00Z"
  }
}
```

**Resposta 404 Not Found:**

```json
{
  "sucesso": false,
  "mensagem": "Arquivo não encontrado.",
  "erros": [],
  "dados": null
}
```

---

## Resumo dos Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/agents/{agentId}/files` | Admin | Lista arquivos do agente |
| `POST` | `/api/agents/{agentId}/files` | Admin | Faz upload de arquivo `.md` |
| `DELETE` | `/api/agents/{agentId}/files/{fileId}` | Admin | Remove arquivo e chunks |
| `POST` | `/api/agents/{agentId}/files/{fileId}/reprocess` | Admin | Reprocessa ingestão do arquivo |

---

## Regras de Negócio

| Regra | Descrição |
|---|---|
| Formato permitido | Apenas arquivos com extensão `.md` (Markdown) |
| Tamanho máximo | 10 MB por arquivo |
| Processamento assíncrono | O upload retorna imediatamente; a ingestão ocorre em background via `IHostedService` ou worker |
| Reprocessamento | Limpa os chunks existentes antes de reindexar |
| Exclusão em cascata | Remover o agente exclui automaticamente todos os arquivos e chunks associados |

---

## Códigos HTTP Utilizados

| Código | Situação |
|---|---|
| `200 OK` | Listagem, exclusão ou reprocessamento iniciado com sucesso |
| `201 Created` | Arquivo enviado e fila de processamento criada |
| `400 Bad Request` | Formato ou tamanho inválido |
| `401 Unauthorized` | Token ausente ou inválido |
| `403 Forbidden` | Permissão insuficiente |
| `404 Not Found` | Agente ou arquivo não encontrado |
| `500 Internal Server Error` | Erro interno inesperado |
