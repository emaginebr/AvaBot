# Data Model: Autenticação JWT e Painel Administrativo

**Branch**: `007-admin-auth-panel` | **Date**: 2026-04-09

## Entidades

### AuthCredentials (NOVA)

Dados enviados para login.

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| username | string | Sim | Nome de usuário |
| password | string | Sim | Senha |

### AuthResponse (NOVA)

Resposta do endpoint de login.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| sucesso | boolean | Indica sucesso da operação |
| token | string | Token JWT (presente apenas quando sucesso=true) |

### AuthState (NOVA — Zustand Store)

Estado de autenticação no frontend.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| token | string \| null | Token JWT armazenado |
| isAuthenticated | boolean | Derivado: token !== null |
| login | (credentials) => Promise\<boolean\> | Ação de login |
| logout | () => void | Ação de logout |
| getAuthHeaders | () => HeadersInit | Retorna headers com Bearer token |

**Storage key**: `"avachat:auth-token"` (localStorage)

### AgentInfo (EXISTENTE — sem alterações)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| agentId | number | ID único |
| name | string | Nome do agente |
| slug | string | Slug único |
| description | string \| null | Descrição |
| systemPrompt | string | Prompt do sistema |
| status | number | 0=inativo, 1=ativo |
| collectName | boolean | Coleta nome do visitante |
| collectEmail | boolean | Coleta email do visitante |
| collectPhone | boolean | Coleta telefone do visitante |
| createdAt | string | Data de criação |
| updatedAt | string | Data de atualização |

### AgentStore (EXISTENTE — adicionar campo)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| agents | AgentInfo[] | Lista de agentes |
| selectedAgent | AgentInfo \| null | **NOVO** — agente selecionado na navbar |
| loading | boolean | Estado de carregamento |
| error | string \| null | Mensagem de erro |
| loadAgents | () => Promise\<void\> | Carregar lista |
| selectAgent | (agent: AgentInfo \| null) => void | **NOVO** — selecionar agente |
| clearError | () => void | Limpar erro |

**Persistência**: `selectedAgent` persiste em localStorage key `"avachat:selected-agent"`

### ChatSessionInfo (EXISTENTE — sem alterações)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| chatSessionId | number | ID único |
| agentId | number | ID do agente |
| userName | string \| null | Nome do visitante |
| userEmail | string \| null | Email do visitante |
| userPhone | string \| null | Telefone do visitante |
| startedAt | string | Início da sessão |
| endedAt | string \| null | Fim da sessão |
| messageCount | number | Quantidade de mensagens |

### PaginatedResult\<T\> (EXISTENTE IMPLÍCITO — formalizar tipo)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| items | T[] | Lista de itens da página |
| total | number | Total de registros |
| page | number | Página atual |
| maxPage | number | Itens por página |

### ChatMessageInfo (EXISTENTE — sem alterações)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| chatMessageId | number | ID único |
| chatSessionId | number | ID da sessão |
| senderType | SenderType | 0=User, 1=Assistant |
| content | string | Conteúdo da mensagem |
| createdAt | string | Data de criação |

### KnowledgeFileInfo (EXISTENTE — sem alterações)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| knowledgeFileId | number | ID único |
| agentId | number | ID do agente |
| fileName | string | Nome do arquivo |
| fileSize | number | Tamanho em bytes |
| processingStatus | ProcessingStatus | 0=Processing, 1=Ready, 2=Error |
| errorMessage | string \| null | Mensagem de erro |
| createdAt | string | Data de criação |
| updatedAt | string | Data de atualização |

## Relacionamentos

```
AuthState (1) ──guards──> AdminLayout (1)
AgentStore.selectedAgent (1) ──context──> SessionListPage, KnowledgeFilesPage, KnowledgeSearchPage, AgentSettingsPage
AgentInfo (1) ──has many──> ChatSessionInfo (N)
ChatSessionInfo (1) ──has many──> ChatMessageInfo (N)
AgentInfo (1) ──has many──> KnowledgeFileInfo (N)
```

## State Transitions

### Autenticação
```
Unauthenticated → [login success] → Authenticated
Authenticated → [logout / token expired / 401] → Unauthenticated
```

### Agente Selecionado
```
None → [select agent] → Selected
Selected → [select another] → Selected
Selected → [delete current agent] → None
```
