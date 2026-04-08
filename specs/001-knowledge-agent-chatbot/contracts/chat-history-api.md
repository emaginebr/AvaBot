# Contrato de API — Histórico de Chat

> Versão: 1.0  
> Base URL: `/api`  
> Autenticação: todos os endpoints requerem JWT no header `Authorization: Bearer <token>` (Admin)

---

## Modelos de Dados (DTOs)

### ChatSessionInfo — Leitura de Sessão

Representa uma sessão de chat iniciada por um usuário.

```csharp
public class ChatSessionInfo
{
    [JsonPropertyName("chatSessionId")] public Guid      ChatSessionId { get; set; }
    [JsonPropertyName("agentId")]       public Guid      AgentId       { get; set; }
    [JsonPropertyName("userName")]      public string?   UserName      { get; set; }
    [JsonPropertyName("userEmail")]     public string?   UserEmail     { get; set; }
    [JsonPropertyName("userPhone")]     public string?   UserPhone     { get; set; }
    [JsonPropertyName("startedAt")]     public DateTime  StartedAt     { get; set; }
    [JsonPropertyName("endedAt")]       public DateTime? EndedAt       { get; set; }
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `chatSessionId` | `uuid` | Identificador único da sessão |
| `agentId` | `uuid` | Identificador do agente associado |
| `userName` | `string?` | Nome informado pelo usuário (nulo se não coletado) |
| `userEmail` | `string?` | E-mail informado pelo usuário (nulo se não coletado) |
| `userPhone` | `string?` | Telefone informado pelo usuário (nulo se não coletado) |
| `startedAt` | `datetime` | Data/hora de início da sessão (ISO 8601 UTC) |
| `endedAt` | `datetime?` | Data/hora de encerramento da sessão; nulo se ainda ativa |

---

### ChatMessageInfo — Leitura de Mensagem

Representa uma mensagem individual dentro de uma sessão de chat.

```csharp
public class ChatMessageInfo
{
    [JsonPropertyName("chatMessageId")]  public Guid     ChatMessageId { get; set; }
    [JsonPropertyName("chatSessionId")] public Guid     ChatSessionId { get; set; }
    [JsonPropertyName("senderType")]    public int      SenderType    { get; set; }
    [JsonPropertyName("content")]       public string   Content       { get; set; }
    [JsonPropertyName("createdAt")]     public DateTime CreatedAt     { get; set; }
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `chatMessageId` | `uuid` | Identificador único da mensagem |
| `chatSessionId` | `uuid` | Identificador da sessão à qual pertence |
| `senderType` | `int` | Tipo do remetente (ver tabela abaixo) |
| `content` | `string` | Conteúdo textual da mensagem |
| `createdAt` | `datetime` | Data/hora de criação (ISO 8601 UTC) |

#### Valores de senderType

| Valor | Constante | Descrição |
|---|---|---|
| `0` | `User` | Mensagem enviada pelo usuário |
| `1` | `Assistant` | Mensagem gerada pelo agente de IA |

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

### GET /api/agents/{agentId}/sessions

Lista todas as sessões de chat de um agente, ordenadas da mais recente para a mais antiga.

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
| `dataInicio` | `date` | Não | Filtrar sessões iniciadas a partir desta data (`yyyy-MM-dd`) |
| `dataFim` | `date` | Não | Filtrar sessões iniciadas até esta data (`yyyy-MM-dd`) |
| `ativas` | `bool` | Não | `true` = apenas sessões ainda abertas; `false` = apenas encerradas |

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Sessões listadas com sucesso.",
  "erros": [],
  "dados": {
    "items": [
      {
        "chatSessionId": "c1d2e3f4-a5b6-7890-cdef-012345678901",
        "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userName": "Maria Silva",
        "userEmail": "maria@empresa.com",
        "userPhone": null,
        "startedAt": "2026-04-08T10:15:00Z",
        "endedAt": "2026-04-08T10:32:45Z"
      },
      {
        "chatSessionId": "d2e3f4a5-b6c7-8901-defa-123456789012",
        "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userName": null,
        "userEmail": null,
        "userPhone": null,
        "startedAt": "2026-04-08T11:00:00Z",
        "endedAt": null
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

### GET /api/sessions/{sessionId}/messages

Retorna todas as mensagens de uma sessão de chat, ordenadas cronologicamente (mais antigas primeiro), reconstruindo a conversa completa.

**Acesso:** Admin

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `sessionId` | `uuid` | Identificador da sessão |

**Parâmetros de query:**

| Parâmetro | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `pagina` | `int` | Não | Página atual (padrão: `1`) |
| `tamanhoPagina` | `int` | Não | Itens por página (padrão: `50`, máx.: `200`) |

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Mensagens listadas com sucesso.",
  "erros": [],
  "dados": {
    "session": {
      "chatSessionId": "c1d2e3f4-a5b6-7890-cdef-012345678901",
      "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userName": "Maria Silva",
      "userEmail": "maria@empresa.com",
      "userPhone": null,
      "startedAt": "2026-04-08T10:15:00Z",
      "endedAt": "2026-04-08T10:32:45Z"
    },
    "messages": {
      "items": [
        {
          "chatMessageId": "e3f4a5b6-c7d8-9012-efab-234567890123",
          "chatSessionId": "c1d2e3f4-a5b6-7890-cdef-012345678901",
          "senderType": 0,
          "content": "Como faço para resetar minha senha?",
          "createdAt": "2026-04-08T10:15:30Z"
        },
        {
          "chatMessageId": "f4a5b6c7-d8e9-0123-fabc-345678901234",
          "chatSessionId": "c1d2e3f4-a5b6-7890-cdef-012345678901",
          "senderType": 1,
          "content": "Para resetar sua senha, acesse o portal em https://portal.empresa.com/reset e informe seu e-mail corporativo. Você receberá um link de redefinição em até 5 minutos.",
          "createdAt": "2026-04-08T10:15:35Z"
        },
        {
          "chatMessageId": "a5b6c7d8-e9f0-1234-abcd-456789012345",
          "chatSessionId": "c1d2e3f4-a5b6-7890-cdef-012345678901",
          "senderType": 0,
          "content": "Não recebi o e-mail. O que faço?",
          "createdAt": "2026-04-08T10:16:10Z"
        },
        {
          "chatMessageId": "b6c7d8e9-f0a1-2345-bcde-567890123456",
          "chatSessionId": "c1d2e3f4-a5b6-7890-cdef-012345678901",
          "senderType": 1,
          "content": "Verifique a pasta de spam ou lixo eletrônico. Caso o problema persista, entre em contato com o helpdesk pelo ramal 1234.",
          "createdAt": "2026-04-08T10:16:18Z"
        }
      ],
      "total": 4,
      "pagina": 1,
      "tamanhoPagina": 50
    }
  }
}
```

**Resposta 404 Not Found:**

```json
{
  "sucesso": false,
  "mensagem": "Sessão não encontrada.",
  "erros": [],
  "dados": null
}
```

---

## Resumo dos Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/agents/{agentId}/sessions` | Admin | Lista sessões de chat de um agente |
| `GET` | `/api/sessions/{sessionId}/messages` | Admin | Retorna todas as mensagens de uma sessão |

---

## Regras de Negócio

| Regra | Descrição |
|---|---|
| Isolamento por agente | Sessões de um agente não são visíveis via endpoints de outro agente |
| Sessões ativas | `endedAt = null` indica sessão com conexão WebSocket ainda aberta |
| Encerramento automático | O servidor define `endedAt` ao fechar a conexão WebSocket (por timeout ou pelo cliente) |
| Retenção de dados | O período de retenção do histórico é definido por configuração no `appsettings.json` |
| Dados anonimizados | Campos `userName`, `userEmail` e `userPhone` são nulos quando o agente não os coleta |

---

## Códigos HTTP Utilizados

| Código | Situação |
|---|---|
| `200 OK` | Listagem bem-sucedida |
| `401 Unauthorized` | Token ausente ou inválido |
| `403 Forbidden` | Permissão insuficiente |
| `404 Not Found` | Agente ou sessão não encontrado |
| `500 Internal Server Error` | Erro interno inesperado |
