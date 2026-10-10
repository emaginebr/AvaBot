# Research: Contas de usuário e autenticação

**Feature**: 016-user-accounts-auth | **Date**: 2026-10-10

Nenhum item do contexto técnico ficou como NEEDS CLARIFICATION: a stack é a do repositório e as três decisões de escopo foram tomadas na spec (sem confirmação de e-mail, sem recuperação de senha, sem administrador global). As pesquisas abaixo fecham as escolhas de implementação.

## R1. Hash de senha

**Decisão**: PBKDF2-HMAC-SHA256 via `Rfc2898DeriveBytes.Pbkdf2` (`System.Security.Cryptography`), 210.000 iterações, salt de 16 bytes, hash de 32 bytes. Formato armazenado: `pbkdf2-sha256$<iterações>$<salt base64>$<hash base64>`. Comparação com `CryptographicOperations.FixedTimeEquals`. Implementado em `AvaBot.Infra/AppServices/Pbkdf2PasswordHasher.cs` atrás de `IPasswordHasher` (`AvaBot.Infra.Interfaces/AppServices`), no mesmo molde do `AesGcmSecretProtector`/`ISecretProtector`.

**Justificativa**: zero dependência nova (regra das features anteriores); PBKDF2 com esse número de iterações é a recomendação OWASP vigente para SHA-256; o formato com prefixo e iterações permite subir o custo no futuro sem migrar dados (re-hash no próximo login).

**Alternativas**: `BCrypt.Net-Next` (dependência nova, descartada); `PasswordHasher<T>` do ASP.NET Identity (precisaria do pacote `Microsoft.Extensions.Identity.Core` no `AvaBot.Infra`, que é biblioteca de classes; também descartada); Argon2 (sem implementação na BCL).

## R2. Emissão e validação do JWT

**Decisão**: manter HS256 com `Auth:JwtSecret` e a configuração atual do `AddJwtBearer` (só muda a validade). O token passa a carregar `sub` = `UserId`, `email` e `name`; `ClaimTypes.NameIdentifier` é mapeado a partir de `sub`. Validade lida de `Auth:TokenExpirationMinutes`, com padrão **43200** (30 dias) no `appsettings.json`. A emissão sai do controller para `AvaBot.API/Auth/JwtTokenIssuer.cs` (classe pequena, registrada como singleton no `Program.cs`), porque `System.IdentityModel.Tokens.Jwt` já vem com o pacote `JwtBearer` da API e não existe nas camadas de baixo.

**Justificativa**: "continue usando JWT" é restrição do pedido; mudar o algoritmo ou o segredo invalidaria tokens à toa. Colocar o emissor na API evita puxar o pacote JWT para `AvaBot.Infra`.

**Alternativas**: refresh token com revogação (fora do escopo: a spec aceita que "sair" só esqueça o token local); emitir na camada Application (exigiria pacote novo no Infra).

## R3. Identificação do usuário autenticado nas camadas

**Decisão**: o controller lê o id do usuário com uma extensão `User.GetUserId()` (`AvaBot.API/Auth/ClaimsPrincipalExtensions.cs`) e passa `ownerUserId` como parâmetro explícito aos services. Sem `IHttpContextAccessor` na Application.

**Justificativa**: mantém `AvaBot.Application` sem dependência de HTTP, testável com Moq como hoje (os testes de `AgentService` passam o id direto). Segue o estilo de `PowerBIService`, que já recebe `slug` e resolve o agente internamente.

**Alternativas**: `ICurrentUser` escopado injetado nos services (acopla Application ao pipeline HTTP, e a calibração e os testes teriam de simulá-lo).

## R4. Isolamento por dono (FR-010, FR-011)

**Decisão**: o filtro de dono fica **no repositório e no service**, nunca só no controller:
- `IAgentRepository<T>` ganha `GetAllByOwnerAsync(ownerUserId)`, `GetByIdAsync(id, ownerUserId)` e `GetBySlugAsync(slug, ownerUserId)`.
- `AgentService` ganha `GetAllAsync(ownerUserId)`, `GetOwnedByIdAsync(id, ownerUserId)`, `GetOwnedBySlugAsync(slug, ownerUserId)`; `CreateAsync(info, ownerUserId)` grava o dono; `UpdateAsync`, `DeleteAsync`, `ToggleStatusAsync` e `GetOpenAIApiKeyAsync` recebem `ownerUserId` e devolvem `null`/`KeyNotFoundException` quando o agente não é do usuário, exatamente como quando não existe.
- `PowerBIService` (todos os métodos por `slug`), `TelegramService` (`SetupWebhookAsync`, `GetWebhookInfoAsync`, `RegenerateWebhookSecretAsync`) e `WhatsappService` (`StartSessionAsync`, `GetQrCodeAsync`, `GetStatusAsync`, `DisconnectAsync`) recebem `ownerUserId` e resolvem o agente pelo método filtrado.
- `SessionController.GetSessions` e `FileController` validam o agente com `GetOwnedByIdAsync` antes de tocar nos repositórios; `SessionController.GetMessages` carrega a sessão e confere o dono do agente dela.
- `AgentController.Search`, `TestQuestion` e `DiagnoseOpenAI` validam o dono antes de delegar.
- A resposta é sempre `404` com a mensagem `"Agente nao encontrado"` já usada hoje (SC-002).

Rotas públicas que **não** mudam: `GET /agents/{slug}`, `GET /agents/{slug}/chat-config`, `POST /sessions/agents/{slug}`, `GET /sessions/resume/{slug}`, webhooks do Telegram e WhatsApp, WebSocket `/ws/chat/{slug}`. Elas continuam usando `GetBySlugAsync(slug)` sem dono.

**Justificativa**: fazer o filtro no banco evita esquecer uma rota; manter a mensagem de 404 igual impede enumeração de agentes alheios.

**Alternativas**: query filter global do EF (`HasQueryFilter`) com o usuário corrente (exige `ICurrentUser` no DbContext, e quebraria webhooks e o chat público, que não têm usuário); checagem só no controller (frágil).

## R5. Migração de schema e migração de dados (FR-013, US4)

**Decisão**: duas etapas separadas.
1. **Migração EF** `AddUsersAndAgentOwner`: cria `avabot_users` e adiciona `owner_user_id bigint NULL` em `avabot_agents` com FK `ClientSetNull` e índice. A coluna nasce nula porque a migração não tem como criar a conta: a senha vem da configuração e precisa ser hasheada pelo app.
2. **Bootstrap na subida** (`UserBootstrapService` em `AvaBot.Application/Services`, chamado no `Program.cs` logo depois do `CreateIndexAsync` do Elasticsearch): se não existe nenhum usuário e `Auth:Username` + `Auth:Password` estão configurados, cria a conta (e-mail = `Auth:Username` normalizado, nome = `Auth:Username`, senha hasheada) e atribui a ela todos os agentes com `owner_user_id IS NULL`. Depois, se **ainda** existir agente sem dono (por exemplo, credenciais não configuradas numa base com agentes), lança `InvalidOperationException` com mensagem clara e a API não sobe. Com usuários já existentes, não faz nada (idempotente). Numa base sem agentes a conta é criada do mesmo modo, para que o administrador continue entrando com as credenciais de hoje.

A aplicação da migração continua manual (`dotnet ef database update`), como em `docs/POWERBI_INTEGRATION.md`; o `Program.cs` não chama `Migrate()`.

**Justificativa**: atende "a migração falha de forma visível se houver agente que não possa ser atribuído" sem deixar a coluna `NOT NULL` antes de ter o usuário. Depois do bootstrap, `Auth:Username`/`Auth:Password` deixam de ser lidos para login (FR-013) e podem ser removidos do ambiente.

**Alternativas**: `owner_user_id NOT NULL` com usuário placeholder inserido em SQL na migração (senha impossível de hashear na migração; placeholder ficaria sem senha válida); migração de dados dentro do `Up()` lendo configuração (migrations não têm acesso ao `IConfiguration` de forma limpa).

## R6. Bloqueio após falhas de login (FR-007)

**Decisão**: contador em `IMemoryCache` por e-mail normalizado: chave `login-fail:{email}` com expiração deslizante de 15 min; ao chegar a 5, grava `login-lock:{email}` por 15 min. Login bloqueado responde `401` com a mesma mensagem genérica de credenciais inválidas mais o aviso de bloqueio temporário (sem dizer se o e-mail existe). Um login bem-sucedido zera o contador. Implementado dentro de `UserService` (Application), que recebe `IMemoryCache` (já registrado por `AddMemoryCache()` e disponível transitivamente via EF Core).

**Justificativa**: sem tabela nova e sem dependência; o deploy é de instância única, então o contador em memória cumpre o requisito. Se um dia houver várias réplicas, troca-se por uma tabela ou Redis sem mudar a interface.

**Alternativas**: coluna `failed_attempts`/`locked_until` em `avabot_users` (persistente, mas não cobre e-mails inexistentes e exige escrita a cada falha); rate limiting do ASP.NET por IP (não é por e-mail).

## R7. Frontend: sessão de 30 dias e expiração

**Decisão**:
- `AuthService` guarda o token em `avabot:auth-token` (como hoje) e o usuário em `avabot:auth-user` (`{ userId, name, email }`).
- `isAuthenticated()` passa a decodificar o `exp` do payload do JWT (`atob` do segundo segmento, sem biblioteca) e considera expirado o token vencido, limpando o storage. Assim o painel não abre com token de 31 dias só para receber 401 na primeira chamada.
- `handleUnauthorized` redireciona para `/login?expired=1`; a `LoginPage` lê o parâmetro e mostra "Sessão expirada, entre novamente" (edge case "acesso expira no meio de uma ação").
- Login por `email` + `password`; nova `RegisterPage` em `/register`; nova `AccountPage` em `/admin/account`; `AdminNavbar` mostra o nome do usuário com link para a conta; `LandingPage` ganha links "Entrar" e "Criar conta" no cabeçalho; `LoginPage` ganha link "Criar conta" e o aviso "Esqueceu a senha? Fale com o suporte" (FR-017).
- Padrão de arquivos: o repositório usa `Services/*.ts` (objeto com funções) + `stores/use*Store.ts` (Zustand), não o par Context/Hook descrito na skill `react-architecture`. As features 007 a 011 seguiram o padrão do repositório; esta também segue.

**Justificativa**: nada de dependência nova; a decodificação do `exp` é trivial e evita uma chamada perdida; o padrão Zustand já é o do painel.

**Alternativas**: cookies `HttpOnly` (a constituição proíbe tokens em cookies); `jwt-decode` (dependência para 3 linhas de código).

## R8. Testes de API e coleção Bruno

**Decisão**: `AvaBot.Tests.API/Support/TestBase.cs` passa a enviar `{ email, password }` e a ler `dados.token` do envelope `Result`; `ApiSettings` ganha `Email` no lugar de `Username`. `bruno/Auth/Login.bru` é atualizado para o novo corpo e ganham-se `Register.bru`, `Me.bru`, `Update Me.bru`, `Change Password.bru`. O `collection.bru` que guarda o token precisa apontar para `dados.token`.

**Nota de segurança (pré-existente)**: `AvaBot.Tests.API/appsettings.json` está rastreado com credenciais reais porque a regra do `.gitignore` usa o nome antigo `Avachat.Tests.API`. A feature corrige a regra para `AvaBot.Tests.API/appsettings.json`, tira o arquivo do índice (`git rm --cached`) e adiciona `appsettings.Example.json`. Isso é uma tarefa pequena de polish, destacada para o usuário.

## R9. Programa de calibração

**Decisão**: `AvaBot.Calibration/Local/LocalRepositories.cs` implementa os três métodos novos de `IAgentRepository<Agent>` (filtram o agente único em memória pelo dono, ou devolvem o agente ignorando o dono, já que a calibração não tem usuário). `ChatService.TestMessageAsync` não muda de assinatura, então o `Program.cs` da calibração não é afetado.

## R10. Resposta de login e registro

**Decisão**: `POST /auth/login` e `POST /auth/register` devolvem o envelope padrão `Result<AuthResultInfo>` (`sucesso`, `mensagem`, `dados`), com `dados = { token, expiresAt, user: { userId, name, email, createdAt } }`. É uma mudança de formato em relação ao `{ sucesso, token }` atual, absorvida pelo frontend, pelo `TestBase` e pela coleção Bruno nesta mesma feature.

**Justificativa**: todos os outros endpoints usam `Result<T>`; o frontend já tem `handleResponse` para esse envelope.
