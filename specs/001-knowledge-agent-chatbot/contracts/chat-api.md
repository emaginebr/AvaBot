# Contrato de API — Chat

> Versão: 1.0  
> Autenticação: endpoint REST é público; WebSocket não requer autenticação (dados do usuário coletados na sessão)

---

## Visão Geral do Fluxo

```
Página de Chat (público)
  │
  ├─ 1. GET /api/agents/{slug}/chat-config
  │       Obtém configurações do agente (nome, campos a coletar, etc.)
  │
  ├─ 2. Exibe formulário de coleta de dados (se configurado)
  │
  ├─ 3. Abre conexão WebSocket: ws://host/ws/chat/{slug}
  │
  ├─ 4. Cliente envia  → { type: "identify", name?, email?, phone? }
  │
  ├─ 5. Servidor responde → { type: "ready" }   (ou { type: "collect_data", fields: [...] })
  │
  └─ 6. Loop de mensagens:
         Cliente → { type: "message", content: "..." }
         Servidor → { type: "chunk", content: "..." }  (N vezes, streaming)
         Servidor → { type: "done" }
```

---

## REST — Configuração de Chat

### GET /api/agents/{slug}/chat-config

Retorna as configurações públicas do agente necessárias para montar a interface de chat. Não expõe o `systemPrompt`.

**Acesso:** Público

**Parâmetros de rota:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `slug` | `string` | Slug único do agente |

**DTO de Resposta — AgentChatConfigInfo:**

```csharp
public class AgentChatConfigInfo
{
    [JsonPropertyName("agentId")]     public Guid   AgentId     { get; set; }
    [JsonPropertyName("name")]        public string Name        { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("collectName")] public bool   CollectName { get; set; }
    [JsonPropertyName("collectEmail")]public bool   CollectEmail{ get; set; }
    [JsonPropertyName("collectPhone")]public bool   CollectPhone{ get; set; }
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `agentId` | `uuid` | Identificador do agente |
| `name` | `string` | Nome de exibição do agente |
| `description` | `string` | Descrição pública exibida no cabeçalho do chat |
| `collectName` | `bool` | Se `true`, solicitar nome do usuário antes do chat |
| `collectEmail` | `bool` | Se `true`, solicitar e-mail do usuário antes do chat |
| `collectPhone` | `bool` | Se `true`, solicitar telefone do usuário antes do chat |

**Resposta 200 OK:**

```json
{
  "sucesso": true,
  "mensagem": "Configuração obtida com sucesso.",
  "erros": [],
  "dados": {
    "agentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Suporte TI",
    "description": "Assistente de suporte técnico disponível 24h.",
    "collectName": true,
    "collectEmail": true,
    "collectPhone": false
  }
}
```

**Resposta 404 Not Found:**

```json
{
  "sucesso": false,
  "mensagem": "Agente não encontrado ou inativo.",
  "erros": [],
  "dados": null
}
```

> **Nota:** Agentes com `status = 0` (inativo) retornam `404` para usuários públicos.

---

## WebSocket — Protocolo de Chat

### Conexão

```
ws://host/ws/chat/{slug}
wss://host/ws/chat/{slug}   (produção com TLS)
```

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `slug` | `string` | Slug único do agente |

O servidor aceita a conexão WebSocket e aguarda a mensagem `identify` antes de liberar o chat.

---

### Mensagens Cliente → Servidor

Todas as mensagens do cliente são objetos JSON com o campo obrigatório `type`.

#### identify

Enviada imediatamente após a conexão ser estabelecida. Inicia a sessão e fornece os dados do usuário.

```json
{
  "type": "identify",
  "name": "Maria Silva",
  "email": "maria@empresa.com",
  "phone": "11987654321"
}
```

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `type` | `string` | Sim | Valor fixo: `"identify"` |
| `name` | `string?` | Condicional | Obrigatório se `collectName = true` |
| `email` | `string?` | Condicional | Obrigatório se `collectEmail = true` |
| `phone` | `string?` | Condicional | Obrigatório se `collectPhone = true` |

> O servidor cria um registro de `ChatSession` ao processar esta mensagem.

---

#### message

Enviada pelo usuário para iniciar uma pergunta ou continuar a conversa.

```json
{
  "type": "message",
  "content": "Como faço para resetar minha senha?"
}
```

| Campo | Tipo | Obrigatório | Regras |
|---|---|---|---|
| `type` | `string` | Sim | Valor fixo: `"message"` |
| `content` | `string` | Sim | Máx. 4 000 caracteres; não pode ser vazio |

> Mensagens enviadas antes do recebimento de `ready` são ignoradas pelo servidor.

---

### Mensagens Servidor → Cliente

Todas as mensagens do servidor são objetos JSON com o campo `type`.

#### ready

Enviado após o `identify` ser aceito. Indica que o chat está pronto para receber mensagens.

```json
{
  "type": "ready"
}
```

---

#### collect_data

Enviado quando os dados obrigatórios não foram fornecidos no `identify`. Lista os campos ainda necessários.

```json
{
  "type": "collect_data",
  "fields": ["name", "email"]
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `type` | `string` | Valor fixo: `"collect_data"` |
| `fields` | `string[]` | Campos faltantes: `"name"`, `"email"`, `"phone"` |

> O cliente deve exibir um formulário solicitando os campos listados e reenviar `identify` com os dados completos.

---

#### chunk

Enviado repetidamente durante o streaming da resposta do modelo de linguagem. Cada mensagem contém um fragmento do texto gerado.

```json
{
  "type": "chunk",
  "content": "Para resetar sua senha, acesse o portal "
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `type` | `string` | Valor fixo: `"chunk"` |
| `content` | `string` | Fragmento de texto a ser concatenado pelo cliente |

> O cliente deve acumular os fragmentos e exibi-los progressivamente (efeito de digitação).

---

#### done

Enviado ao final do streaming, indicando que a resposta está completa.

```json
{
  "type": "done"
}
```

> Após `done`, o servidor persiste a mensagem completa no histórico (`ChatMessage`).

---

#### error

Enviado em caso de falha durante o processamento da mensagem.

```json
{
  "type": "error",
  "message": "Não foi possível processar sua mensagem. Tente novamente."
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `type` | `string` | Valor fixo: `"error"` |
| `message` | `string` | Descrição do erro para exibição ao usuário |

> Em erros críticos, o servidor pode fechar a conexão WebSocket após enviar esta mensagem.

---

## Diagrama de Sequência

```
Cliente                          Servidor
  │                                  │
  ├──── WS connect ────────────────► │
  │◄─── (conexão aceita) ───────────┤
  │                                  │
  ├──── { type: "identify", ... } ──► │  cria ChatSession
  │◄─── { type: "ready" } ──────────┤
  │                                  │
  ├──── { type: "message", ... } ───► │  persiste ChatMessage (user)
  │◄─── { type: "chunk", ... } ─────┤  streaming
  │◄─── { type: "chunk", ... } ─────┤  streaming
  │◄─── { type: "chunk", ... } ─────┤  streaming
  │◄─── { type: "done" } ───────────┤  persiste ChatMessage (assistant)
  │                                  │
  ├──── { type: "message", ... } ───► │  próxima pergunta
  │◄─── { type: "chunk", ... } ─────┤
  │◄─── { type: "done" } ───────────┤
  │                                  │
  ├──── WS close ───────────────────► │  atualiza ChatSession.endedAt
```

---

## Regras de Negócio

| Regra | Descrição |
|---|---|
| Agente inativo | Conexão WebSocket é recusada com código `1008` (Policy Violation) se o agente estiver inativo |
| Ordem obrigatória | Nenhuma mensagem `message` é processada antes de `ready` ser enviado |
| Tamanho máximo | Mensagens do usuário limitadas a 4 000 caracteres |
| Contexto de conversa | O histórico da sessão atual é enviado ao modelo para manter contexto |
| Persistência | Mensagens do usuário e do assistente são salvas em `ChatMessage` após cada troca |
| Timeout de sessão | Conexões inativas por mais de 30 minutos são encerradas automaticamente pelo servidor |

---

## Códigos de Fechamento WebSocket

| Código | Situação |
|---|---|
| `1000` | Encerramento normal pelo cliente |
| `1001` | Servidor em processo de desligamento |
| `1008` | Agente inativo ou slug inválido |
| `1011` | Erro interno inesperado no servidor |
