# Contrato: API de autenticação e conta

**Feature**: 016-user-accounts-auth | Base: `AuthController` (`/auth`)

Todas as respostas usam o envelope `Result<T>`: `{ "sucesso": bool, "mensagem": string|null, "erros": string[]|null, "dados": T|null }`. Mensagens em português, sem acento nas mensagens técnicas, como no resto da API.

## POST /auth/register (público)

Cria a conta e já devolve o acesso (FR-003, FR-005, FR-016).

Request:
```json
{ "name": "Ana Souza", "email": "Ana@Exemplo.com ", "password": "minhasenha123" }
```

Respostas:
- `201 Created`, `Location: /auth/me`
  ```json
  { "sucesso": true, "mensagem": "Conta criada com sucesso",
    "dados": { "token": "<jwt>", "expiresAt": "2026-11-09T12:00:00Z",
               "user": { "userId": 7, "name": "Ana Souza", "email": "ana@exemplo.com", "createdAt": "2026-10-10T12:00:00Z" } } }
  ```
- `400` validação: `{ "sucesso": false, "mensagem": "Senha deve ter ao menos 8 caracteres", "erros": ["..."] }`
- `409 Conflict` e-mail em uso: `{ "sucesso": false, "mensagem": "Este e-mail ja esta em uso" }` (US1 cenário 2)

## POST /auth/login (público)

Request:
```json
{ "email": "ana@exemplo.com", "password": "minhasenha123" }
```

Respostas:
- `200`: mesmo `dados` do cadastro, `mensagem: "Login realizado com sucesso"`.
- `401` credenciais erradas, e-mail inexistente **ou** conta inativa: `{ "sucesso": false, "mensagem": "Credenciais invalidas" }` (FR-006; nunca diz qual campo falhou).
- `401` bloqueado por 5 falhas: `{ "sucesso": false, "mensagem": "Credenciais invalidas. Muitas tentativas; aguarde 15 minutos" }` (FR-007). A tentativa bloqueada não consulta a senha.

Mudança em relação ao contrato atual: o corpo deixa de ser `{ username, password }` e a resposta deixa de ser `{ sucesso, token }` na raiz. Clientes afetados e atualizados nesta feature: frontend (`AuthService`), `AvaBot.Tests.API/Support/TestBase.cs`, `bruno/Auth/Login.bru` e `bruno/collection.bru`.

## GET /auth/me (autenticado)

- `200`: `{ "sucesso": true, "dados": { "userId": 7, "name": "...", "email": "...", "createdAt": "..." } }`
- `401` token ausente, inválido ou expirado (comportamento padrão do `JwtBearer`, corpo vazio).

## PUT /auth/me (autenticado)

Request: `{ "name": "Ana S." }`

- `200`: `UserInfo` atualizado, `mensagem: "Conta atualizada com sucesso"`.
- `400` nome vazio ou > 260.

## PUT /auth/me/password (autenticado)

Request: `{ "currentPassword": "antiga", "newPassword": "nova12345" }`

- `200`: `{ "sucesso": true, "mensagem": "Senha alterada com sucesso", "dados": null }`. O token atual continua válido (sem revogação nesta versão).
- `400` nova senha fora da regra (< 8 ou > 128).
- `400` senha atual errada: `{ "sucesso": false, "mensagem": "Senha atual incorreta" }` (US5 cenário 2).

## Token (JWT)

- Header `Authorization: Bearer <jwt>` em todas as rotas `[Authorize]`, como hoje.
- Claims: `sub` (userId), `email`, `name`, `exp` (30 dias por padrão, `Auth:TokenExpirationMinutes`).
- Assinatura HS256 com `Auth:JwtSecret` (inalterado).

## Configuração

| Chave | Antes | Depois |
|---|---|---|
| `Auth:JwtSecret` | obrigatório | obrigatório (inalterado) |
| `Auth:TokenExpirationMinutes` | 480 | **43200** (padrão no `appsettings.json`) |
| `Auth:Username` / `Auth:Password` | credenciais do único login | lidas **só** pelo bootstrap na primeira subida para criar a conta migrada. Depois, ignoradas para login (FR-013). Podem ser removidas do `.env` após o primeiro deploy. |

Variáveis de ambiente (`docker-compose*.yml`, `.env.example`, `deploy-prod.yml`) continuam as mesmas: `AVABOT_USERNAME`, `AVABOT_PASSWORD`, `AVABOT_JWT_SECRET`. Nenhuma variável nova.

## Erros do bootstrap (subida da API)

Se, depois do bootstrap, houver agente sem dono, a API não sobe e o log mostra:

```
Existem N agente(s) sem dono e nao ha Auth:Username/Auth:Password configurados para criar a conta do administrador. Configure as credenciais ou atribua owner_user_id manualmente.
```
