# Contrato: API de chaves de acesso (painel)

**Feature**: 017-mcp-agent-management | Base: `McpAccessKeyController` (`/auth/me/mcp-keys`), JWT do painel (`[Authorize]`, esquema `Bearer`).

Todas as respostas usam o envelope `Result<T>`: `{ "sucesso": bool, "mensagem": string, "erros": string[], "dados": T|null }`. Mensagens em português sem acento nas técnicas, como o resto da API. A chave MCP **não** autentica nestas rotas (`401`).

## GET /auth/me/mcp-keys

Lista as chaves da conta do token, mais recentes primeiro, ativas e revogadas.

- `200`:
  ```json
  { "sucesso": true, "mensagem": "Chaves listadas com sucesso",
    "dados": [
      { "mcpAccessKeyId": 3, "name": "Claude do notebook", "keyPrefix": "Ab3kQ9xZ", "status": 1,
        "createdAt": "2026-10-12T14:00:00Z", "lastUsedAt": "2026-10-12T14:35:10Z", "revokedAt": null },
      { "mcpAccessKeyId": 2, "name": "Cursor", "keyPrefix": "p0Lm2Vq8", "status": 0,
        "createdAt": "2026-10-11T09:00:00Z", "lastUsedAt": null, "revokedAt": "2026-10-12T10:00:00Z" } ] }
  ```
  `lastUsedAt = null` → o painel mostra "nunca usada" (US1 cenário 1 e 4).
- `401` sem JWT ou expirado.

## POST /auth/me/mcp-keys

Cria uma chave. **Única** resposta que contém o valor em claro.

Request:
```json
{ "name": "Claude do notebook" }
```

- `201 Created`, `Location: /auth/me/mcp-keys/3`:
  ```json
  { "sucesso": true, "mensagem": "Chave criada. Copie o valor agora: ele nao sera exibido de novo",
    "dados": { "key": "avabot_mcp_Ab3kQ9xZ...43 caracteres...",
               "accessKey": { "mcpAccessKeyId": 3, "name": "Claude do notebook", "keyPrefix": "Ab3kQ9xZ", "status": 1,
                              "createdAt": "2026-10-12T14:00:00Z", "lastUsedAt": null, "revokedAt": null } } }
  ```
- `400` validação: `{ "sucesso": false, "mensagem": "Nome e obrigatorio", "erros": ["Nome e obrigatorio"] }` (nome vazio ou > 100).
- `400` limite: `{ "sucesso": false, "mensagem": "Limite de 20 chaves ativas por conta. Revogue uma chave antes de criar outra" }`.
- `401` sem JWT.

O servidor nunca loga o valor da chave; o frontend não escreve `dados.key` no console (mesma regra das senhas no `AuthService`).

## DELETE /auth/me/mcp-keys/{id}

Revoga a chave (não apaga o registro).

- `200`: `{ "sucesso": true, "mensagem": "Chave revogada", "dados": { ...McpAccessKeyInfo com status 0 e revokedAt... } }`.
- `200` idempotente se já estava revogada (devolve o registro como está).
- `404` id inexistente **ou** de outra conta: `{ "sucesso": false, "mensagem": "Chave nao encontrada" }` (sem distinguir).
- `401` sem JWT.

Efeito: a próxima requisição em `/mcp` com essa chave recebe `401` (SC-006). Não há cache a invalidar.

## Service (`AvaBot.Application/Services/McpAccessKeyService.cs`)

```csharp
Task<(McpAccessKey Key, string PlainKey)> CreateAsync(long userId, string name);   // InvalidOperationException no limite
Task<List<McpAccessKey>> ListAsync(long userId);
Task<McpAccessKey> RevokeAsync(long userId, long keyId);                             // KeyNotFoundException
Task<McpAccessKey?> AuthenticateAsync(string plainKey);                              // null para qualquer falha; toca LastUsedAt (<= 1x/min)
```

`AuthenticateAsync` é o único método usado pelo `McpAccessKeyAuthenticationHandler`; devolve a chave com `User` carregado só quando `key.IsActive && key.User.IsActive`.

## Frontend

| Arquivo | Conteúdo |
|---|---|
| `types/mcpAccessKey.ts` | `McpAccessKeyInfo`, `McpAccessKeyCreatedInfo` |
| `Services/McpAccessKeyService.ts` | `list()`, `create(name)`, `revoke(id)` com `AuthService.getAuthHeaders()` e tratamento de 401 |
| `stores/useMcpAccessKeyStore.ts` | `keys`, `loading`, `error`, `loadKeys()`, `createKey(name)` (devolve o `Result` com a chave em claro, não guarda no estado), `revokeKey(id)` |
| `components/admin/McpAccessKeysPanel.tsx` | Lista (nome, prefixo, criada em, último uso ou "nunca usada", badge "revogada"), formulário de nome + "Criar chave", botão "Revogar" com confirmação |
| `components/admin/McpKeyCreatedModal.tsx` | Chave em claro com botão copiar, aviso de exibição única, endereço `${VITE_API_URL}/mcp`, abas com o trecho de configuração de Claude Code, Claude Desktop / claude.ai, Cursor e VS Code |
| `pages/admin/AccountPage.tsx` | Nova seção "Assistentes de IA (MCP)" abaixo dos cards atuais, usando o painel |

Rota: nenhuma nova (fica em `/admin/account`).

## Coleção Bruno (`bruno/MCP/`)

`Create Key.bru` (guarda `dados.key` em variável de ambiente `mcpKey`), `List Keys.bru`, `Revoke Key.bru`, `Tools List.bru` e `Call Tool.bru` (POST JSON-RPC em `/mcp` com `Authorization: Bearer {{mcpKey}}`).
