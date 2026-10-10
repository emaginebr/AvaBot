# Data Model: Contas de usuário e autenticação

**Feature**: 016-user-accounts-auth | **Date**: 2026-10-10

Convenções do `AvaBotContext`: prefixo `avabot_`, colunas snake_case, PK `bigint` identity `{entidade}_id`, `timestamp without time zone`, FKs novas com `ClientSetNull`, strings `varchar` com tamanho.

## Nova: `User` (`avabot_users`)

Pessoa com acesso ao painel. Dona de zero ou mais agentes.

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `UserId` | `user_id` | `bigint identity PK` (`avabot_users_pkey`) | |
| `Name` | `name` | `varchar(260) NOT NULL` | 2 a 260 caracteres, sem espaços nas pontas |
| `Email` | `email` | `varchar(260) NOT NULL` | Normalizado (`Trim().ToLowerInvariant()`) antes de gravar e de comparar (FR-002). Índice único `avabot_users_email_key`. Formato validado no cadastro; na conta migrada do administrador aceita-se o valor de `Auth:Username` como está. |
| `PasswordHash` | `password_hash` | `varchar(500) NOT NULL` | Formato `pbkdf2-sha256$<iter>$<salt>$<hash>` (research R1). Nunca sai da camada de dados. |
| `Status` | `status` | `integer NOT NULL DEFAULT 1` | 1 = ativo, 0 = inativo. Inativo não consegue entrar. Nesta versão nada o altera (reservado para suporte via banco). |
| `CreatedAt` | `created_at` | `timestamp NOT NULL` | |
| `UpdatedAt` | `updated_at` | `timestamp NOT NULL` | Atualizado em troca de nome ou senha |

Navegação: `Agents` (`ICollection<Agent>`).

Comportamento no modelo (entidade rica, sem dependência de infra):
- `static string NormalizeEmail(string email)`: `Trim().ToLowerInvariant()`.
- `bool IsActive => Status == 1`.

## Alteração: `Agent` (`avabot_agents`)

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `OwnerUserId` | `owner_user_id` | `bigint NULL` | FK `avabot_fk_users_agents` → `avabot_users.user_id`, `ClientSetNull`. Índice `ix_avabot_agents_owner_user_id`. Nula só entre a migração de schema e o bootstrap (research R5); o bootstrap falha se restar agente sem dono. Toda criação pelo painel grava o dono (FR-009). |

Navegação: `Owner` (`User?`).

O `AgentProfile` ignora `OwnerUserId` e `Owner` no mapa `AgentInsertInfo → Agent` (o dono vem do service, não do corpo). `AgentInfo` não expõe o dono: o painel só vê os próprios.

## Sem alteração

`KnowledgeFile`, `ChatSession`, `ChatMessage`, `TelegramChat`, `AgentPowerBIConfig`, `PowerBIDataset`, `PowerBIQueryLog`: continuam ligados ao agente e, por ele, ao dono. Nenhuma coluna nova.

## Migração

`AddUsersAndAgentOwner` (`AvaBot.Infra/Migrations`):

```text
Up:
  CREATE TABLE avabot_users (...)            -- conforme tabela acima
  ALTER TABLE avabot_agents ADD owner_user_id bigint NULL
  CREATE INDEX ix_avabot_agents_owner_user_id ON avabot_agents (owner_user_id)
  ALTER TABLE avabot_agents ADD CONSTRAINT avabot_fk_users_agents FOREIGN KEY (owner_user_id) REFERENCES avabot_users (user_id) ON DELETE SET NULL
Down:
  inverso
```

Bootstrap de dados (não é migração EF; roda na subida da API, research R5):

```text
se COUNT(avabot_users) = 0 e Auth:Username e Auth:Password configurados:
    inserir usuário { name = Auth:Username, email = NormalizeEmail(Auth:Username), password_hash = Hash(Auth:Password), status = 1 }
    UPDATE avabot_agents SET owner_user_id = <novo id> WHERE owner_user_id IS NULL
se existir agente com owner_user_id IS NULL:
    falhar a subida com mensagem explicando que faltam credenciais de administrador para migrar N agentes
```

## Estado de "acesso" (não persistido)

O comprovante de login é o JWT. Não há tabela de sessões ou de tokens.

| Claim | Valor |
|---|---|
| `sub` | `UserId` |
| `email` | e-mail normalizado |
| `name` | nome |
| `exp` | emissão + `Auth:TokenExpirationMinutes` (padrão 43200 = 30 dias) |

Ciclo: emitido no cadastro ou login → válido até `exp` → qualquer chamada autenticada depois de `exp` recebe `401` → o painel limpa o storage e volta ao login. "Sair" apaga o token do navegador; o token em si continua válido até `exp` (premissa da spec).

## Bloqueio de login (em memória, research R6)

| Chave `IMemoryCache` | Valor | Expiração |
|---|---|---|
| `login-fail:{email}` | contador de falhas seguidas | deslizante, 15 min |
| `login-lock:{email}` | marcador de bloqueio | absoluta, 15 min |

Transições: falha → contador+1; contador = 5 → cria `login-lock` e zera o contador; sucesso → remove os dois; tentativa com `login-lock` presente → recusada sem consultar a senha.

## DTOs (`AvaBot.DTO/UserDTOs.cs`)

| DTO | Campos (JSON camelCase) | Uso |
|---|---|---|
| `UserInfo` | `userId`, `name`, `email`, `createdAt` | resposta de `/auth/me`, dentro de `AuthResultInfo` |
| `UserRegisterInfo` | `name`, `email`, `password` | `POST /auth/register` |
| `UserLoginInfo` | `email`, `password` | `POST /auth/login` |
| `UserUpdateInfo` | `name` | `PUT /auth/me` |
| `UserPasswordChangeInfo` | `currentPassword`, `newPassword` | `PUT /auth/me/password` |
| `AuthResultInfo` | `token`, `expiresAt`, `user: UserInfo` | resposta de login e cadastro |

Validação (FluentValidation em `AvaBot.API/Validators`): nome 2–260; e-mail obrigatório, formato válido, ≤ 260; senha ≥ 8 caracteres e ≤ 128 (FR-004). Senha nunca aparece em `UserInfo`, em logs do backend nem no console do frontend.
