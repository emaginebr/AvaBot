# Contrato de API — Agentes

> Versão: 1.0  
> Base URL: `/api/agents`  
> Autenticação: endpoints de administração requerem JWT no header `Authorization: Bearer <token>`

---

## Modelos de Dados (DTOs)

### AgentInfo — Leitura

Retornado nas respostas de leitura.

```csharp
public class AgentInfo
{
    [JsonPropertyName("agentId")]    public Guid     AgentId       { get; set; }
    [JsonPropertyName("name")]       public string   Name          { get; set; }
    [JsonPropertyName("slug")]       public string   Slug          { get; set; }
    [JsonPropertyName("description")]public string   Description   { get; set; }
    [JsonPropertyName("systemPrompt")]public string  SystemPrompt  { get; set; }
    [JsonPropertyName("status")]     public int      Status        { get; set; }
    [JsonPropertyName("collectName")] public bool    CollectName   { get; set; }
    [JsonPropertyName("collectEmail")]public bool    CollectEmail  { get; set; }
    [JsonPropertyName("collectPhone")]public bool    CollectPhone  { get; set; }
    [JsonPropertyName("createdAt")] public DateTime  CreatedAt     { get; set; }
    [JsonPropertyName("updatedAt")] public DateTime  UpdatedAt     { get; set; }
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `agentId` | `uuid` | Identificador único do agente |
| `name` | `string` | Nome de exibição do agente |
| `slug` | `string` | Identificador amigável para URL (único, ex.: `suporte-ti`) |
| `description` | `string` | Descrição pública do agente |
| `systemPrompt` | `string` | Prompt de sistema enviado ao modelo de linguagem |
| `status` | `int` | `0` = inativo, `1` = ativo |
| `collectName` | `bool` | Solicita nome do usuário antes do chat |
| `collectEmail` | `bool` | Solicita e-mail do usuário antes do chat |
| `collectPhone` | `bool` | Solicita telefone do usuário antes do chat |
| `createdAt` | `datetime` | Data de criação (ISO 8601 UTC) |
| `updatedAt` | `datetime` | Data da última atualização (ISO 8601 UTC) |

---

### AgentInsertInfo — Criação / Atualização

Enviado no corpo das requisições POST e PUT.

```csharp
public class AgentInsertInfo
{
    [JsonPropertyName("name")]        public string Name         { get; set; }
    [JsonPropertyName("slug")]        public string Slug         { get; set; }
    [JsonPropertyName("description")] public string Description  { get; set; }
    [JsonPropertyName("systemPrompt")]public string SystemPrompt { get; set; }
    [JsonPropertyName("collectName")] public bool   CollectName  { get; set; }
    [JsonPropertyName("collectEmail")]public bool   CollectEmail { get; set; }
    [JsonPropertyName("collectPhone")]public bool   CollectPhone { get; set; }
}
```

| Campo | Tipo | Obrigatório | Regras |
|---|---|---|---|
| `name` | `string` | Sim | 2–100 caracteres |
| `slug` | `string` | Sim | Único; apenas letras minúsculas, números e hífens |
| `description` | `string` | Não | Máx. 500 caracteres |
| `systemPrompt` | `string` | Sim | Máx. 8 000 caracteres |
| `collectName` | `bool` | Sim | Padrão: `false` |
| `collectEmail` | `bool` | Sim | Padrão: `false` |
| `collectPhone` | `bool` | Sim | Padrão: `false` |

---

### Result\<T\> — Envelope de Resposta

Todas as respostas seguem este envelope:

```csharp
public class Result<T>
{
    [JsonPropertyName("sucesso")] public bool     Sucesso  { get; set; }
    [JsonPropertyName("mensagem")]public string   Mensagem { get; set; }
    [JsonPropertyName("erros")]   public string[] Erros    { get; set; }
    [JsonPropertyName("dados")]   public T        Dados    { get; set; }
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `sucesso` | `bool` | `true` se a operação foi bem-sucedida |
| `mensagem` | `string` | Mensagem descritiva da operação |
| `erros` | `string[]` | Lista de erros de validação (vazia em sucesso) |
| `dados` | `T` | Payload da resposta (nulo em erro) |

---

## Endpoints

### GET /api/agents

Lista todos os agentes cadastrados.

**Acesso:** Admin

**Parâmetros de query:**

| Parâmetro | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `pagina` | `int` | Não | Página atual (padrão: `1`) |
| `tamanhoPagina` | `int` | Não | Itens por página (padrão: `20`, máx.: `100`) |
| `status` | `int` | Não | Filtrar por status: `0` = inativo, `1` = ativo |

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Agentes listados com sucesso.",
  "erros": [],
  "dados": {
    "items": [
      {
        "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "Suporte TI",
        "slug": "suporte-ti",
        "description": "Agente de suporte técnico interno.",
        "systemPrompt": "Você é um assistente de suporte de TI...",
        "status": 1,
        "collectName": true,
        "collectEmail": true,
        "collectPhone": false,
        "createdAt": "2026-01-15T10:00:00Z",
        "updatedAt": "2026-03-20T14:30:00Z"
      }
    ],
    "total": 1,
    "pagina": 1,
    "tamanhoPagina": 20
  }
}
```

---

### GET /api/agents/{slug}

Retorna um agente pelo seu slug. Usado pela página pública de chat.

**Acesso:** Público

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `slug` | `string` | Slug único do agente |

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Agente encontrado.",
  "erros": [],
  "dados": {
    "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Suporte TI",
    "slug": "suporte-ti",
    "description": "Agente de suporte técnico interno.",
    "systemPrompt": "Você é um assistente de suporte de TI...",
    "status": 1,
    "collectName": true,
    "collectEmail": true,
    "collectPhone": false,
    "createdAt": "2026-01-15T10:00:00Z",
    "updatedAt": "2026-03-20T14:30:00Z"
  }
}
```

**Resposta 404 Not Found:**

```json
{
  "sucesso": false,
  "mensagem": "Agente não encontrado.",
  "erros": [],
  "dados": null
}
```

---

### POST /api/agents

Cria um novo agente.

**Acesso:** Admin

**Content-Type:** `application/json`

**Corpo da requisição:**

```json
{
  "name": "Suporte TI",
  "slug": "suporte-ti",
  "description": "Agente de suporte técnico interno.",
  "systemPrompt": "Você é um assistente de suporte de TI...",
  "collectName": true,
  "collectEmail": true,
  "collectPhone": false
}
```

**Resposta 201 Created:**

```json
{
  "sucesso": true,
  "mensagem": "Agente criado com sucesso.",
  "erros": [],
  "dados": {
    "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Suporte TI",
    "slug": "suporte-ti",
    "description": "Agente de suporte técnico interno.",
    "systemPrompt": "Você é um assistente de suporte de TI...",
    "status": 1,
    "collectName": true,
    "collectEmail": true,
    "collectPhone": false,
    "createdAt": "2026-04-08T12:00:00Z",
    "updatedAt": "2026-04-08T12:00:00Z"
  }
}
```

**Resposta 400 Bad Request (validação):**

```json
{
  "sucesso": false,
  "mensagem": "Dados inválidos.",
  "erros": [
    "O campo 'name' é obrigatório.",
    "O slug 'suporte-ti' já está em uso."
  ],
  "dados": null
}
```

---

### PUT /api/agents/{id}

Atualiza todos os campos de um agente existente.

**Acesso:** Admin

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `id` | `uuid` | Identificador do agente |

**Content-Type:** `application/json`

**Corpo da requisição:** mesmo esquema de `AgentInsertInfo`.

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Agente atualizado com sucesso.",
  "erros": [],
  "dados": { /* AgentInfo atualizado */ }
}
```

**Resposta 404 Not Found:**

```json
{
  "sucesso": false,
  "mensagem": "Agente não encontrado.",
  "erros": [],
  "dados": null
}
```

---

### DELETE /api/agents/{id}

Remove um agente e todos os seus recursos associados (arquivos de conhecimento, sessões de chat).

**Acesso:** Admin

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `id` | `uuid` | Identificador do agente |

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Agente removido com sucesso.",
  "erros": [],
  "dados": null
}
```

**Resposta 404 Not Found:**

```json
{
  "sucesso": false,
  "mensagem": "Agente não encontrado.",
  "erros": [],
  "dados": null
}
```

---

### PATCH /api/agents/{id}/status

Alterna o status do agente entre ativo (`1`) e inativo (`0`).

**Acesso:** Admin

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `id` | `uuid` | Identificador do agente |

**Corpo da requisição:**

```json
{
  "status": 0
}
```

| Campo | Tipo | Valores |
|---|---|---|
| `status` | `int` | `0` = inativo, `1` = ativo |

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Status do agente atualizado com sucesso.",
  "erros": [],
  "dados": {
    "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": 0
  }
}
```

---

## Resumo dos Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/agents` | Admin | Lista todos os agentes |
| `GET` | `/api/agents/{slug}` | Público | Busca agente por slug |
| `POST` | `/api/agents` | Admin | Cria novo agente |
| `PUT` | `/api/agents/{id}` | Admin | Atualiza agente completo |
| `DELETE` | `/api/agents/{id}` | Admin | Remove agente |
| `PATCH` | `/api/agents/{id}/status` | Admin | Alterna status ativo/inativo |

---

## Códigos HTTP Utilizados

| Código | Situação |
|---|---|
| `200 OK` | Leitura, atualização ou remoção bem-sucedida |
| `201 Created` | Recurso criado com sucesso |
| `400 Bad Request` | Dados inválidos ou regra de negócio violada |
| `401 Unauthorized` | Token ausente ou inválido |
| `403 Forbidden` | Permissão insuficiente |
| `404 Not Found` | Recurso não encontrado |
| `500 Internal Server Error` | Erro interno inesperado |
