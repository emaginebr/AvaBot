# API Contracts: Autenticação JWT e Painel Administrativo

**Branch**: `007-admin-auth-panel` | **Date**: 2026-04-09

## Base URL

Configurada via `VITE_API_URL` (env). Todas as respostas seguem o formato `Result<T>`:

```typescript
interface Result<T> {
  sucesso: boolean;
  mensagem: string;
  erros: string[];
  dados: T;
}
```

## Endpoints

### Autenticação

#### POST /auth/login
- **Auth**: Nenhuma (público)
- **Request**:
  ```json
  { "username": "string", "password": "string" }
  ```
- **Response (200)**:
  ```json
  { "sucesso": true, "token": "eyJhbGci..." }
  ```
- **Response (401)**:
  ```json
  { "sucesso": false, "mensagem": "Credenciais inválidas" }
  ```

---

### Agentes

#### GET /agents
- **Auth**: Bearer token
- **Response**: `Result<AgentInfo[]>`

#### GET /agents/{slug}
- **Auth**: Nenhuma (público)
- **Response**: `Result<AgentInfo>`

#### GET /agents/{slug}/chat-config
- **Auth**: Nenhuma (público)
- **Response**: `Result<ChatConfig>` (name, description, collectName, collectEmail, collectPhone)

#### POST /agents
- **Auth**: Bearer token
- **Request**: `{ name, description, systemPrompt, collectName, collectEmail, collectPhone }`
- **Response (201)**: `Result<AgentInfo>`

#### PUT /agents/{id}
- **Auth**: Bearer token
- **Request**: `{ name, description, systemPrompt, collectName, collectEmail, collectPhone }`
- **Response**: `Result<AgentInfo>`

#### DELETE /agents/{id}
- **Auth**: Bearer token
- **Response**: `Result<null>`

#### PATCH /agents/{id}/status
- **Auth**: Bearer token
- **Response**: `Result<AgentInfo>` (status toggleado)

---

### Sessões

#### POST /sessions/agents/{slug}
- **Auth**: Nenhuma (público — criação de sessão pelo visitante)
- **Request**: `{ userName, userEmail, userPhone }`
- **Response**: `Result<ChatSessionInfo>`

#### GET /sessions/agents/{agentId}
- **Auth**: Bearer token
- **Query**: `page` (default 1), `maxPage` (default 20, max 100)
- **Response**: `Result<PaginatedResult<ChatSessionInfo>>`

#### GET /sessions/{sessionId}/messages
- **Auth**: Bearer token
- **Query**: `page` (default 1), `maxPage` (default 50, max 200)
- **Response**: `Result<PaginatedResult<ChatMessageInfo>>`

---

### Arquivos (Base de Conhecimento)

#### GET /files/{agentId}
- **Auth**: Bearer token
- **Response**: `Result<KnowledgeFileInfo[]>`

#### POST /files/{agentId}
- **Auth**: Bearer token
- **Content-Type**: multipart/form-data
- **Body**: field `file` (.md, max 10MB)
- **Response**: `Result<KnowledgeFileInfo>` (processingStatus=0)

#### DELETE /files/{agentId}/{fileId}
- **Auth**: Bearer token
- **Response**: `Result<null>`

#### POST /files/{agentId}/{fileId}/reprocess
- **Auth**: Bearer token
- **Response**: `Result<null>`

---

### Busca

#### GET /agents/{id}/search
- **Auth**: Bearer token
- **Query**: `query` (obrigatório), `topK` (default 5)
- **Response**: `Result<string[]>` (chunks de texto)

---

### WebSocket

#### WS /ws/chat/{slug}?sessionId={sessionId}
- **Auth**: Nenhuma (público)
- **Messages**:
  - Server → Client: `{ type: "ready" }`
  - Client → Server: `{ type: "message", content: "..." }`
  - Server → Client: `{ type: "chunk", content: "..." }` (streaming)
  - Server → Client: `{ type: "done" }`
  - Server → Client: `{ type: "error", message: "..." }`
