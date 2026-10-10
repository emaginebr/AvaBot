# Tasks: Contas de usuário e autenticação

**Input**: Design documents from `/specs/016-user-accounts-auth/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/auth-api.md, contracts/ownership.md, quickstart.md

**Tests**: o plano lista os testes xUnit como parte da entrega (`Testing` no Technical Context e arquivos em `AvaBot.Tests/`), então cada história inclui suas tarefas de teste. O frontend não tem suíte; a validação é `npm run lint` + `npm run build`.

**Organization**: tarefas agrupadas por história de usuário, na ordem de prioridade da spec (US1 e US2 são P1; US3 e US4 são P2; US5 é P3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa pendente)
- **[Story]**: história a que a tarefa pertence (US1..US5)
- Caminhos relativos à raiz do repositório

## Path Conventions

Backend .NET em Clean Architecture na raiz (`AvaBot.API`, `AvaBot.Application`, `AvaBot.Domain`, `AvaBot.DTO`, `AvaBot.Infra`, `AvaBot.Infra.Interfaces`, `AvaBot.Tests`, `AvaBot.Tests.API`, `AvaBot.Calibration`); frontend React em `frontend/src/`.

Convenções a respeitar em todas as tarefas (constituição + skill `dotnet-architecture`): DTOs com `[JsonPropertyName]` camelCase; namespaces file-scoped; campos `_camelCase`; mensagens técnicas em português sem acento (`"Agente nao encontrado"`); controllers com `try/catch → StatusCode(500, Result.Failure(...))`; FKs novas com `ClientSetNull`; nunca logar senha nem hash.

---

## Phase 1: Setup (configuração e contratos de dados)

**Purpose**: ajustes de configuração e DTOs que todas as fases usam.

- [X] T001 Alterar `Auth:TokenExpirationMinutes` de `480` para `43200` em `AvaBot.API/appsettings.json` (30 dias; contrato `auth-api.md` seção Configuração)
- [X] T002 [P] Criar `AvaBot.DTO/UserDTOs.cs` com os records/classes `UserInfo { userId, name, email, createdAt }`, `UserRegisterInfo { name, email, password }`, `UserLoginInfo { email, password }`, `UserUpdateInfo { name }`, `UserPasswordChangeInfo { currentPassword, newPassword }` e `AuthResultInfo { token, expiresAt, user: UserInfo }`, todos com `[JsonPropertyName]` camelCase no estilo de `AvaBot.DTO/AgentDTOs.cs` (tabela DTOs em `data-model.md`)

---

## Phase 2: Foundational (bloqueia todas as histórias)

**Purpose**: entidade `User`, dono no `Agent`, schema, hash de senha, repositórios, emissão/leitura do JWT e DI. Nada de história começa antes desta fase.

**⚠️ CRITICAL**: a migração (T006) precisa ser gerada depois de T003–T005 e aplicada no banco local antes de testar qualquer história.

- [X] T003 Criar `AvaBot.Domain/Models/User.cs` com `UserId`, `Name`, `Email`, `PasswordHash`, `Status` (int, 1 ativo), `CreatedAt`, `UpdatedAt`, navegação `Agents` (`ICollection<Agent>`), `static string NormalizeEmail(string email)` (`Trim().ToLowerInvariant()`) e `bool IsActive => Status == 1` (data-model.md seção `User`)
- [X] T004 Adicionar `long? OwnerUserId` e navegação `User? Owner` em `AvaBot.Domain/Models/Agent.cs`
- [X] T005 Mapear em `AvaBot.Infra/Context/AvaBotContext.cs`: `DbSet<User> Users`; tabela `avabot_users` com colunas snake_case (`user_id` identity PK `avabot_users_pkey`, `name varchar(260)`, `email varchar(260)` com índice único `avabot_users_email_key`, `password_hash varchar(500)`, `status integer DEFAULT 1`, `created_at`/`updated_at` `timestamp without time zone`); em `avabot_agents`, coluna `owner_user_id bigint NULL`, índice `ix_avabot_agents_owner_user_id` e FK `avabot_fk_users_agents` → `avabot_users.user_id` com `OnDelete(DeleteBehavior.ClientSetNull)` (seguir o estilo das demais entidades do contexto)
- [X] T006 Gerar a migração `AddUsersAndAgentOwner` com `dotnet ef migrations add AddUsersAndAgentOwner --project AvaBot.Infra --startup-project AvaBot.API`, conferir que o `Up()` gerado em `AvaBot.Infra/Migrations/<timestamp>_AddUsersAndAgentOwner.cs` corresponde ao pseudo-SQL de `data-model.md` (coluna nula, índice, FK `SET NULL`) e aplicar com `dotnet ef database update --project AvaBot.Infra --startup-project AvaBot.API`
- [X] T007 [P] Criar `AvaBot.Infra.Interfaces/AppServices/IPasswordHasher.cs` com `string Hash(string password)` e `bool Verify(string password, string storedHash)` (mesmo molde de `ISecretProtector.cs`)
- [X] T008 [P] Criar `AvaBot.Infra/AppServices/Pbkdf2PasswordHasher.cs` implementando `IPasswordHasher` com `Rfc2898DeriveBytes.Pbkdf2` (HMAC-SHA256, 210.000 iterações, salt de 16 bytes aleatório, hash de 32 bytes), formato `pbkdf2-sha256$<iter>$<salt base64>$<hash base64>`, `Verify` lendo as iterações do próprio hash e comparando com `CryptographicOperations.FixedTimeEquals`; devolver `false` (sem exceção) para hash malformado (research R1)
- [X] T009 [P] Criar `AvaBot.Infra.Interfaces/Repository/IUserRepository.cs` (`IUserRepository<T>`, padrão genérico do repositório) com `Task<T?> GetByIdAsync(long id)`, `Task<T?> GetByEmailAsync(string normalizedEmail)`, `Task<bool> EmailExistsAsync(string normalizedEmail)`, `Task<int> CountAsync()`, `Task<T> CreateAsync(T user)`, `Task<T> UpdateAsync(T user)`
- [X] T010 Criar `AvaBot.Infra/Repository/UserRepository.cs` implementando `IUserRepository<User>` sobre `AvaBotContext`, no estilo de `AvaBot.Infra/Repository/AgentRepository.cs` (busca por e-mail compara com o valor já normalizado; `CreateAsync`/`UpdateAsync` preenchem `CreatedAt`/`UpdatedAt` com `DateTime.UtcNow` como o restante do repo faz)
- [X] T011 Adicionar em `AvaBot.Infra.Interfaces/Repository/IAgentRepository.cs`: `Task<List<T>> GetAllByOwnerAsync(long ownerUserId)`, `Task<T?> GetByIdAsync(long id, long ownerUserId)`, `Task<T?> GetBySlugAsync(string slug, long ownerUserId)`, `Task<int> CountWithoutOwnerAsync()`, `Task<int> AssignOwnerToOrphansAsync(long ownerUserId)` (contrato `ownership.md`; os métodos sem dono continuam existindo para rotas públicas)
- [X] T012 Implementar os cinco métodos de T011 em `AvaBot.Infra/Repository/AgentRepository.cs` (filtro `OwnerUserId == ownerUserId` nas consultas; `AssignOwnerToOrphansAsync` via `ExecuteUpdateAsync` ou carregamento + `SaveChangesAsync`, devolvendo a quantidade atribuída)
- [X] T013 [P] Implementar os métodos novos de `IAgentRepository<Agent>` em `AvaBot.Calibration/Local/LocalRepositories.cs` (devolver o agente único em memória ignorando o dono; `CountWithoutOwnerAsync` → 0; `AssignOwnerToOrphansAsync` → 0) para o projeto de calibração continuar compilando (research R9)
- [X] T014 [P] Em `AvaBot.Application/Profiles/AgentProfile.cs` ignorar `OwnerUserId` e `Owner` no mapa `AgentInsertInfo → Agent`; criar `AvaBot.Application/Profiles/UserProfile.cs` com o mapa `User → UserInfo`
- [X] T015 Criar `AvaBot.API/Auth/JwtTokenIssuer.cs` (classe pública, registrada como singleton) que recebe `IConfiguration`, lê `Auth:JwtSecret` e `Auth:TokenExpirationMinutes` (padrão 43200) e expõe `(string token, DateTime expiresAt) Issue(User user)` emitindo HS256 com claims `sub` = `UserId`, `email`, `name` e `exp`, reaproveitando o código de emissão que hoje está em `AvaBot.API/Controllers/AuthController.cs` (research R2)
- [X] T016 [P] Criar `AvaBot.API/Auth/ClaimsPrincipalExtensions.cs` com `public static long GetUserId(this ClaimsPrincipal user)` lendo `ClaimTypes.NameIdentifier` ou `sub` e lançando `UnauthorizedAccessException("Token sem identificador de usuario")` quando ausente (snippet em `ownership.md`)
- [X] T017 Registrar em `AvaBot.Application/DependencyInjection.cs`: `IUserRepository<User> → UserRepository` (scoped), `IPasswordHasher → Pbkdf2PasswordHasher` (singleton), `UserService` (scoped) e `UserBootstrapService` (scoped), nas seções correspondentes (repositórios, AppServices, services) — as classes `UserService`/`UserBootstrapService` são criadas em T020/T047; registrar junto com elas se preferir, mas a DI final fica neste arquivo
- [X] T018 Em `AvaBot.API/Program.cs` registrar `JwtTokenIssuer` como singleton e garantir que o `sub` do token chega como `ClaimTypes.NameIdentifier` (manter o mapeamento padrão do `JwtSecurityTokenHandler` ou definir `TokenValidationParameters.NameClaimType`), sem mudar `ValidateIssuer`/`ValidateAudience`/segredo
- [X] T019 [P] Criar `AvaBot.Tests/Infra/Pbkdf2PasswordHasherTest.cs` (estilo de `AesGcmSecretProtectorTest.cs`): hash começa com `pbkdf2-sha256$210000$`; dois hashes da mesma senha diferem (salt); `Verify` verdadeiro para a senha certa e falso para senha errada, hash vazio e hash malformado

**Checkpoint**: `dotnet build` verde, migração aplicada, `dotnet test AvaBot.Tests --filter Pbkdf2` verde.

---

## Phase 3: User Story 1 - Criar conta e entrar (Priority: P1) 🎯 MVP

**Goal**: cadastro público com nome/e-mail/senha que já entra no painel; login por e-mail e senha com mensagem genérica em falha e bloqueio após 5 erros.

**Independent Test**: abrir `/register`, criar uma conta, cair no painel vazio; sair; entrar em `/login` com o mesmo e-mail/senha. Repetir o cadastro com o e-mail em maiúsculas → "Este e-mail ja esta em uso". Senha de 5 caracteres bloqueada pela página. Errar a senha 5 vezes → 6ª recusada com aviso de 15 minutos.

### Backend

- [X] T020 [US1] Criar `AvaBot.Application/Services/UserService.cs` recebendo `IUserRepository<User>`, `IPasswordHasher`, `IMemoryCache`, `IMapper` e `ILogger<UserService>`, com: `Task<User> RegisterAsync(UserRegisterInfo info)` (normaliza e-mail com `User.NormalizeEmail`, `EmailExistsAsync` → `InvalidOperationException("Este e-mail ja esta em uso")`, grava `PasswordHash = Hash(password)`, `Status = 1`); `Task<User?> AuthenticateAsync(string email, string password)` (normaliza; se `login-lock:{email}` existe no cache lança `InvalidOperationException("Credenciais invalidas. Muitas tentativas; aguarde 15 minutos")` sem consultar senha; busca por e-mail; se não existe, hash não confere ou `!IsActive` → incrementa `login-fail:{email}` (sliding 15 min), ao chegar a 5 grava `login-lock:{email}` (absoluto 15 min) e zera o contador, devolve `null`; sucesso remove as duas chaves); `Task<User?> GetAsync(long userId)`. Nunca logar senha (research R6, data-model seção Bloqueio)
- [X] T021 [P] [US1] Criar `AvaBot.API/Validators/UserRegisterInfoValidator.cs` (nome 2–260 após trim; e-mail obrigatório, `EmailAddress()`, ≤ 260; senha 8–128, mensagem "Senha deve ter ao menos 8 caracteres") e `AvaBot.API/Validators/UserLoginInfoValidator.cs` (e-mail e senha obrigatórios), seguindo `AgentInsertInfoValidator.cs`; confirmar que o registro automático de validators em `Program.cs` os cobre
- [X] T022 [US1] Reescrever `AvaBot.API/Controllers/AuthController.cs`: injetar `UserService`, `JwtTokenIssuer` e `IMapper`; remover a comparação com `Auth:Username`/`Auth:Password` e o `LoginRequest` antigo; `POST /auth/register` (`[AllowAnonymous]`) → `RegisterAsync` → `JwtTokenIssuer.Issue` → `201 Created` com `Location: /auth/me` e `Result<AuthResultInfo>.Success("Conta criada com sucesso")`, `InvalidOperationException` → `409 Result.Failure(mensagem)`; `POST /auth/login` (`[AllowAnonymous]`) → `AuthenticateAsync` → `200 Result<AuthResultInfo>.Success("Login realizado com sucesso")` ou `401 Result.Failure("Credenciais invalidas")` (`null`) / `401 Result.Failure(ex.Message)` (bloqueio); `ValidationException → 400`; demais → `500` (contrato `auth-api.md`)
- [X] T023 [US1] Reescrever `AvaBot.Tests/API/Controllers/AuthControllerTest.cs` com `UserService` mockado (ou `IUserRepository`/`IPasswordHasher`/`IMemoryCache` mockados por baixo, conforme a classe for mockável) e `JwtTokenIssuer` real com configuração em memória: registro devolve 201 com token e `user.email` normalizado; e-mail duplicado devolve 409; login correto devolve 200 com token; senha errada devolve 401 "Credenciais invalidas"; bloqueio devolve 401 com a mensagem de 15 minutos; token emitido contém claim `sub` igual ao `UserId`
- [X] T024 [US1] Criar `AvaBot.Tests/Application/Services/UserServiceTest.cs` com Moq + `MemoryCache` real (`new MemoryCache(new MemoryCacheOptions())`): cadastro grava e-mail normalizado e hash diferente da senha; cadastro com e-mail já existente (`"Ana@Exemplo.com "` vs `"ana@exemplo.com"`) lança `InvalidOperationException`; login correto devolve usuário e limpa contador; senha errada devolve `null`; e-mail inexistente devolve `null`; usuário com `Status = 0` devolve `null`; 5 falhas seguidas → 6ª tentativa lança a exceção de bloqueio mesmo com a senha certa

### Frontend

- [X] T025 [P] [US1] Atualizar `frontend/src/types/auth.ts`: `AuthCredentials { email; password }`, `RegisterInfo { name; email; password }`, `UserInfo { userId; name; email; createdAt }`, `AuthResultInfo { token; expiresAt; user: UserInfo }` (remover `username`)
- [X] T026 [US1] Atualizar `frontend/src/Services/AuthService.ts`: `login(credentials)` envia `{ email, password }` para `/auth/login` e lê `Result<AuthResultInfo>` via `handleResponse`, guardando `dados.token` em `avabot:auth-token` e `dados.user` em `avabot:auth-user`; novo `register(info)` para `/auth/register` com o mesmo tratamento; `getUser()` lê `avabot:auth-user`; `logout()` limpa as duas chaves; erros 401/409 devem propagar a `mensagem` do envelope para a página exibir
- [X] T027 [US1] Atualizar `frontend/src/stores/useAuthStore.ts`: estado `user: UserInfo | null` inicializado de `AuthService.getUser()`; `login(email, password)`; novo `register(info)`; `logout` zera `user`
- [X] T028 [US1] Atualizar `frontend/src/pages/auth/LoginPage.tsx`: campo "E-mail" (`type="email"`) no lugar de usuário; link "Criar conta" para `/register`; texto "Esqueceu a senha? Fale com o suporte" sem link de recuperação (FR-017); exibir a mensagem retornada pela API em caso de 401 (inclusive a de bloqueio)
- [X] T029 [US1] Criar `frontend/src/pages/auth/RegisterPage.tsx` (mesma estrutura visual da `LoginPage`): campos nome, e-mail, senha com validação local (nome ≥ 2, e-mail válido, senha ≥ 8 com texto explicando a regra); chama `register` do store e navega para `/admin` em sucesso; mostra a mensagem da API em 409/400; se `isAuthenticated()` já for verdadeiro, redireciona para `/admin` (edge case "criar conta já autenticado"); link "Já tenho conta" para `/login`
- [X] T030 [US1] Registrar a rota pública `/register` → `RegisterPage` em `frontend/src/App.tsx`
- [X] T031 [P] [US1] Adicionar links "Entrar" (`/login`) e "Criar conta" (`/register`) no cabeçalho de `frontend/src/pages/LandingPage.tsx` (FR-003)

**Checkpoint**: `dotnet test AvaBot.Tests` verde; `npm run lint && npm run build` verdes; roteiro do Independent Test passa com a API rodando.

---

## Phase 4: User Story 2 - Cada usuário vê e administra só os próprios agentes (Priority: P1)

**Goal**: toda rota `[Authorize]` que toca um agente filtra pelo dono do token; agente alheio responde `404 "Agente nao encontrado"` igual a inexistente; rotas públicas inalteradas.

**Independent Test**: com duas contas, criar um agente em cada. Cada lista mostra só o próprio. Logado como A, chamar com o id/slug do agente de B: `PUT /agents/{id}`, `DELETE`, `PATCH .../status`, `GET .../search`, `POST .../test`, `POST .../openai/diagnose`, `GET /sessions/agents/{id}`, `GET /files/{id}`, `GET /powerbi/{slug}/config`, `GET /telegram/{id}/webhook-info`, `GET /whatsapp/{slug}/status` → todos `404` com "Agente nao encontrado". `GET /agents/{slug}` e `/chat/{slug}` sem login continuam funcionando.

### Services (Application)

- [X] T032 [US2] Atualizar `AvaBot.Application/Services/AgentService.cs` conforme `contracts/ownership.md`: `GetAllAsync(long ownerUserId)` → `GetAllByOwnerAsync`; novos `GetOwnedByIdAsync(id, ownerUserId)` e `GetOwnedBySlugAsync(slug, ownerUserId)`; `CreateAsync(info, ownerUserId)` grava `OwnerUserId`; `UpdateAsync(id, info, ownerUserId)`, `DeleteAsync(id, ownerUserId)`, `ToggleStatusAsync(id, ownerUserId)` resolvem o agente pelo método filtrado e devolvem `null`/`false` quando não é do dono; `GetOpenAIApiKeyAsync(agentId, ownerUserId)` lança `KeyNotFoundException("Agente nao encontrado")` quando não é do dono; manter `GetByIdAsync(id)` e `GetBySlugAsync(slug)` sem dono para chat/webhooks/`ChatService`
- [X] T033 [US2] Atualizar `AvaBot.Tests/Application/Services/AgentServiceTest.cs` para as novas assinaturas e acrescentar casos: `GetAllAsync(ownerId)` chama `GetAllByOwnerAsync`; `CreateAsync` grava `OwnerUserId`; `UpdateAsync`/`DeleteAsync`/`ToggleStatusAsync` com agente de outro dono (repositório filtrado devolve `null`) devolvem `null`/`false` sem tocar o repositório; `GetOpenAIApiKeyAsync` de outro dono lança `KeyNotFoundException`
- [X] T034 [P] [US2] Atualizar `AvaBot.Application/Services/PowerBIService.cs`: todo método público por `slug` ganha `long ownerUserId` logo após o slug e `GetAgentBySlugOrThrowAsync(slug, ownerUserId)` passa a usar `_agentRepo.GetBySlugAsync(slug, ownerUserId)`, mantendo a `KeyNotFoundException("Agente nao encontrado")`; o uso interno pelo `PowerBIToolProvider`/chat (sem usuário) continua pelo caminho sem dono
- [X] T035 [P] [US2] Atualizar `AvaBot.Application/Services/TelegramService.cs`: `SetupWebhookAsync(id, ownerUserId)`, `GetWebhookInfoAsync(id, ownerUserId)`, `RegenerateWebhookSecretAsync(id, ownerUserId)` resolvem o agente com `_agentRepo.GetByIdAsync(id, ownerUserId)` e lançam `KeyNotFoundException("Agente nao encontrado")`; `ProcessUpdateAsync` (webhook público) não muda
- [X] T036 [P] [US2] Atualizar `AvaBot.Application/Services/WhatsappService.cs`: `StartSessionAsync(slug, ownerUserId)`, `GetQrCodeAsync(slug, ownerUserId)`, `GetStatusAsync(slug, ownerUserId)`, `DisconnectAsync(slug, ownerUserId)` resolvem o agente com `_agentService.GetOwnedBySlugAsync` e lançam `KeyNotFoundException("Agente nao encontrado")`; `ProcessWebhookAsync` não muda
- [X] T037 [US2] Atualizar `AvaBot.Tests/Application/Services/PowerBIServiceTest.cs` para as novas assinaturas e acrescentar: slug de agente de outro dono (repositório devolve `null`) → `KeyNotFoundException` em `GetConfigAsync` e em uma rota de dataset

### Controllers (API)

- [X] T038 [US2] Atualizar `AvaBot.API/Controllers/AgentController.cs`: em todas as rotas `[Authorize]` ler `var ownerUserId = User.GetUserId();` e passar aos métodos de T032; `Search`, `TestQuestion` e `DiagnoseOpenAI` chamam `GetOwnedByIdAsync` antes de delegar e devolvem `404 Result.Failure("Agente nao encontrado")` quando `null`; `UnauthorizedAccessException` → `401`; rotas `[AllowAnonymous]` (`GET /agents/{slug}`, `chat-config`) intactas
- [X] T039 [P] [US2] Atualizar `AvaBot.API/Controllers/SessionController.cs`: `GetSessions(agentId)` valida `GetOwnedByIdAsync(agentId, User.GetUserId())` e devolve `404 "Agente nao encontrado"` quando `null`; `GetMessages(sessionId)` carrega a sessão, confere `GetOwnedByIdAsync(session.AgentId, ownerUserId)` e devolve `404 "Sessao nao encontrada"` quando a sessão não existe ou o agente não é do dono; rotas públicas de iniciar/retomar sessão intactas
- [X] T040 [P] [US2] Atualizar `AvaBot.API/Controllers/FileController.cs`: todas as rotas validam o agente com `GetOwnedByIdAsync(agentId, User.GetUserId())` antes de tocar `KnowledgeFile`/`IngestionService`, devolvendo `404 "Agente nao encontrado"`; manter a checagem existente `file.AgentId == agentId`
- [X] T041 [P] [US2] Atualizar `AvaBot.API/Controllers/PowerBIController.cs`: todas as 13 rotas passam `User.GetUserId()` ao `PowerBIService`; `Failure()` já converte `KeyNotFoundException` em `404` (conferir que `UnauthorizedAccessException` vira `401`)
- [X] T042 [P] [US2] Atualizar `AvaBot.API/Controllers/TelegramController.cs`: `setup-webhook`, `webhook-info`, `regenerate-secret` passam `User.GetUserId()`; webhook público intacto
- [X] T043 [P] [US2] Atualizar `AvaBot.API/Controllers/WhatsappController.cs`: `start-session`, `qrcode`, `status`, `disconnect` passam `User.GetUserId()`; webhook público intacto
- [X] T044 [US2] Atualizar `AvaBot.Tests/API/Controllers/AgentControllerTest.cs`, `FileControllerTest.cs` e `SessionControllerTest.cs` para as novas assinaturas: montar `ControllerContext` com `ClaimsPrincipal` contendo `ClaimTypes.NameIdentifier` (helper compartilhado, por exemplo `AvaBot.Tests/API/Controllers/ControllerTestHelpers.cs`) e acrescentar um caso por controller em que o service devolve `null` para agente de outro dono → `404` com "Agente nao encontrado" (`SessionController.GetMessages` → "Sessao nao encontrada")

**Checkpoint**: `dotnet build` da solução inteira (inclui `AvaBot.Calibration`) e `dotnet test AvaBot.Tests` verdes; roteiro do Independent Test passa.

---

## Phase 5: User Story 3 - Acesso que dura 30 dias (Priority: P2)

**Goal**: token válido por 30 dias; painel reconhece token expirado sem chamar a API, redireciona ao login com aviso; "Sair" esquece o acesso.

**Independent Test**: entrar, fechar e reabrir o navegador → continua no painel. Subir a API com `Auth__TokenExpirationMinutes=1`, entrar, esperar 1 minuto, clicar em qualquer menu → `/login` com "Sessão expirada, entre novamente". Clicar em "Sair" → `/login` e `localStorage` sem `avabot:auth-token` nem `avabot:auth-user`.

- [X] T045 [US3] Acrescentar em `AvaBot.Tests/API/Controllers/AuthControllerTest.cs` um caso em que `JwtTokenIssuer` configurado com `Auth:TokenExpirationMinutes = 43200` devolve `expiresAt` entre 29,9 e 30,1 dias à frente de `DateTime.UtcNow`, e outro em que a chave ausente usa o padrão 43200
- [X] T046 [US3] Atualizar `frontend/src/Services/AuthService.ts`: `isAuthenticated()` decodifica o segundo segmento do JWT com `atob` (base64url → base64), lê `exp` e, se vencido ou ilegível, limpa `avabot:auth-token`/`avabot:auth-user` e devolve `false`; `handleUnauthorized()` limpa o storage e redireciona para `/login?expired=1` (research R7)
- [X] T047 [US3] Atualizar `frontend/src/pages/auth/LoginPage.tsx` para ler `expired=1` de `useSearchParams` e exibir "Sessão expirada, entre novamente" acima do formulário; conferir que `frontend/src/components/auth/ProtectedRoute.tsx` (ou onde estiver) continua usando `isAuthenticated()` e que o botão "Sair" em `AdminNavbar.tsx` chama `logout` do store e navega para `/login`

**Checkpoint**: `npm run lint && npm run build` verdes; roteiro do Independent Test passa.

---

## Phase 6: User Story 4 - Agentes existentes continuam com dono (Priority: P2)

**Goal**: na primeira subida, criar a conta do administrador a partir de `Auth:Username`/`Auth:Password`, atribuir a ela os agentes órfãos e falhar a subida de forma visível se restar órfão.

**Independent Test**: base com agentes e `AVABOT_USERNAME`/`AVABOT_PASSWORD` no `.env`: subir a API, ver no log "Conta do administrador criada (email=...) e N agente(s) atribuído(s)", entrar com essas credenciais no painel e ver todos os agentes antigos; criar uma conta nova e confirmar que ela não vê nenhum. Subir de novo: nada muda (idempotente). Base com agentes e sem credenciais e sem usuários: a API não sobe e o log traz a mensagem do contrato.

- [X] T048 [US4] Criar `AvaBot.Application/Services/UserBootstrapService.cs` com `Task EnsureAdminAccountAsync()` recebendo `IUserRepository<User>`, `IAgentRepository<Agent>`, `IPasswordHasher`, `IConfiguration` e `ILogger<UserBootstrapService>`: se `CountAsync() == 0` e `Auth:Username` e `Auth:Password` não vazios, cria `User { Name = username, Email = NormalizeEmail(username), PasswordHash = Hash(password), Status = 1 }` e chama `AssignOwnerToOrphansAsync(userId)`, logando e-mail e quantidade (nunca a senha); em seguida, se `CountWithoutOwnerAsync() > 0`, lança `InvalidOperationException` com a mensagem exata do contrato `auth-api.md` seção "Erros do bootstrap"; com usuários já existentes e sem órfãos, não faz nada (research R5)
- [X] T049 [US4] Em `AvaBot.API/Program.cs`, logo após o `esService.CreateIndexAsync()` existente e antes de `app.Run()`, abrir `app.Services.CreateScope()`, resolver `UserBootstrapService` e aguardar `EnsureAdminAccountAsync()` (a exceção deve derrubar a subida, sem `catch`)
- [X] T050 [US4] Criar `AvaBot.Tests/Application/Services/UserBootstrapServiceTest.cs` com Moq e `ConfigurationBuilder().AddInMemoryCollection`: sem usuários + credenciais → cria usuário com e-mail normalizado e hash, chama `AssignOwnerToOrphansAsync`; sem usuários + credenciais + base sem agentes → cria a conta e não lança; usuários existentes → não cria nada; sem usuários, sem credenciais e com órfãos → lança `InvalidOperationException` contendo "sem dono"; usuários existentes e ainda com órfãos → lança
- [X] T051 [US4] Em `AvaBot.API/Controllers/AuthController.cs` e `Program.cs`, confirmar que nenhum caminho de login ainda lê `Auth:Username`/`Auth:Password` (FR-013); remover `using`/campos mortos e manter as chaves apenas no `UserBootstrapService`

**Checkpoint**: `dotnet test AvaBot.Tests` verde; roteiro do Independent Test passa em uma cópia local da base.

---

## Phase 7: User Story 5 - Gerenciar a própria conta (Priority: P3)

**Goal**: ver nome e e-mail, alterar nome e trocar senha confirmando a atual.

**Independent Test**: em `/admin/account`, trocar o nome e ver o cabeçalho atualizar. Trocar a senha com a atual errada → aviso, nada muda. Trocar com a atual certa, sair e entrar com a nova; a antiga dá "Credenciais invalidas".

### Backend

- [X] T052 [US5] Acrescentar em `AvaBot.Application/Services/UserService.cs`: `Task<User> UpdateNameAsync(long userId, string name)` (trim, `UpdatedAt`) e `Task ChangePasswordAsync(long userId, string currentPassword, string newPassword)` (`Verify` da atual → `InvalidOperationException("Senha atual incorreta")`; grava novo hash e `UpdatedAt`); ambos lançam `KeyNotFoundException` se o usuário não existe
- [X] T053 [P] [US5] Criar `AvaBot.API/Validators/UserUpdateInfoValidator.cs` (nome 2–260) e `AvaBot.API/Validators/UserPasswordChangeInfoValidator.cs` (senha atual obrigatória; nova 8–128)
- [X] T054 [US5] Acrescentar em `AvaBot.API/Controllers/AuthController.cs` as rotas `[Authorize]`: `GET /auth/me` → `GetAsync(User.GetUserId())` → `200 Result<UserInfo>`; `PUT /auth/me` → `UpdateNameAsync` → `200 Result<UserInfo>.Success("Conta atualizada com sucesso")`; `PUT /auth/me/password` → `ChangePasswordAsync` → `200 Result.Success("Senha alterada com sucesso")`, `InvalidOperationException` → `400 Result.Failure("Senha atual incorreta")`; `KeyNotFoundException` → `404`; `UnauthorizedAccessException` → `401` (contrato `auth-api.md`)
- [X] T055 [US5] Acrescentar casos em `AvaBot.Tests/Application/Services/UserServiceTest.cs` (senha atual errada lança e não grava; senha certa grava hash novo que verifica a nova e não a antiga; `UpdateNameAsync` faz trim) e em `AvaBot.Tests/API/Controllers/AuthControllerTest.cs` (`GET /me` devolve `UserInfo` sem hash; `PUT /me/password` com atual errada → 400 "Senha atual incorreta")

### Frontend

- [X] T056 [US5] Acrescentar em `frontend/src/types/auth.ts` `PasswordChangeInfo { currentPassword; newPassword }` e em `frontend/src/Services/AuthService.ts` os métodos `me()`, `updateMe(name)` (atualiza `avabot:auth-user`) e `changePassword(info)`, todos com `Authorization: Bearer` e `handleResponse`
- [X] T057 [US5] Acrescentar em `frontend/src/stores/useAuthStore.ts` as ações `refreshUser()`, `updateName(name)` (atualiza `user` no estado) e `changePassword(info)`
- [X] T058 [US5] Criar `frontend/src/pages/admin/AccountPage.tsx` dentro do `AdminLayout`: bloco com nome e e-mail; formulário "Alterar nome" (validação ≥ 2, toast sonner em sucesso); formulário "Alterar senha" com senha atual, nova e confirmação (nova ≥ 8, confirmação igual; em 400 mostra a mensagem da API, por exemplo "Senha atual incorreta"); nunca logar senhas no console
- [X] T059 [P] [US5] Adicionar o item "Minha conta" (`/admin/account`) em `frontend/src/components/admin/AdminSidebar.tsx` e mostrar `user.name` com link para `/admin/account` em `frontend/src/components/admin/AdminNavbar.tsx`
- [X] T060 [US5] Registrar a rota protegida `/admin/account` → `AccountPage` em `frontend/src/App.tsx`

**Checkpoint**: todas as histórias funcionam de ponta a ponta; `dotnet test AvaBot.Tests` e `npm run lint && npm run build` verdes.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: consumidores do contrato antigo de login, segurança do repositório, documentação e validação final.

- [X] T061 [P] Atualizar `AvaBot.Tests.API/Support/ApiSettings.cs` (`Email` no lugar de `Username`) e `AvaBot.Tests.API/Support/TestBase.cs` (login envia `{ email, password }` e lê `dados.token` do envelope `Result`), conferindo que os demais testes em `AvaBot.Tests.API/Controllers/` continuam a compilar
- [X] T062 Corrigir a regra `Avachat.Tests.API/appsettings.json` → `AvaBot.Tests.API/appsettings.json` em `.gitignore`, remover o arquivo do índice com `git rm --cached AvaBot.Tests.API/appsettings.json` (o arquivo local permanece) e criar `AvaBot.Tests.API/appsettings.Example.json` com `ApiSettings.BaseUrl`, `Email` e `Password` preenchidos com placeholders, nunca com valores reais (research R8; apontar isso ao usuário no relatório final)
- [X] T063 [P] Atualizar `bruno/Auth/Login.bru` (corpo `email`/`password`, `post-response` salvando `res.body.dados.token`), conferir a variável de token em `bruno/collection.bru`, e criar `bruno/Auth/Register.bru`, `bruno/Auth/Me.bru`, `bruno/Auth/Update Me.bru`, `bruno/Auth/Change Password.bru` conforme `contracts/auth-api.md`
- [X] T064 [P] Documentar em `README.md` (seção de autenticação/variáveis) o cadastro de contas, o bootstrap do administrador, a validade de 30 dias, a mudança de contrato de `POST /auth/login`, o comando da migração e a mensagem de erro do bootstrap; manter `.env.example`, `docker-compose*.yml` e `deploy-prod.yml` como estão (nenhuma variável nova), apenas comentando que `AVABOT_USERNAME`/`AVABOT_PASSWORD` só servem ao bootstrap
- [X] T065 Rodar `dotnet build AvaBot.sln` e `dotnet test AvaBot.Tests` na raiz; corrigir qualquer aviso novo ou teste quebrado
- [X] T066 [P] Rodar `npm run lint && npm run build` em `frontend/`; corrigir avisos novos
- [X] T067 Executar os 7 cenários de validação manual de `specs/016-user-accounts-auth/quickstart.md` com a API e o frontend locais e registrar o resultado no relatório final (inclui SC-005: inspecionar `avabot_users.password_hash` e o log da API para confirmar que nenhuma senha aparece em texto)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende de T002 (DTOs) apenas para `UserProfile` (T014); bloqueia todas as histórias. Dentro da fase: T003 → T004 → T005 → T006 (migração) em sequência; T007/T008/T009/T013/T014/T016/T019 em paralelo; T010 depois de T009; T012 depois de T011; T015 depois de T003; T017/T018 por último.
- **US1 (Phase 3)**: depende da Phase 2. Backend T020 → T022 → T023/T024; frontend T025 → T026 → T027 → T028/T029 → T030.
- **US2 (Phase 4)**: depende da Phase 2; **não** depende de US1 (pode ser testada com contas criadas por T022 ou direto no banco). T032 → T033; T034/T035/T036 em paralelo após T032 (WhatsappService usa `GetOwnedBySlugAsync`); controllers T038–T043 após os services; T044 por último.
- **US3 (Phase 5)**: depende da Phase 2 e de T026 (AuthService novo). T045 depende de T023.
- **US4 (Phase 6)**: depende da Phase 2 (T010, T012, T008). T048 → T049 → T050/T051.
- **US5 (Phase 7)**: depende de US1 (T020, T022, T026, T027). T052 → T054 → T055; T053 em paralelo; frontend T056 → T057 → T058 → T060; T059 em paralelo.
- **Polish (Phase 8)**: depende de todas as histórias desejadas; T061 e T063 dependem do contrato de login (T022).

### User Story Dependencies

- **US1 (P1)**: só Phase 2.
- **US2 (P1)**: só Phase 2. Independente de US1.
- **US3 (P2)**: Phase 2 + `AuthService` de US1 (T026).
- **US4 (P2)**: só Phase 2. Independente das demais.
- **US5 (P3)**: US1 (service, controller, store).

### Parallel Opportunities

- Phase 2: T007, T008, T009, T013, T014, T016, T019 em paralelo.
- US1: T021, T025, T031 em paralelo com T020; T023 e T024 em paralelo após T022.
- US2: T034, T035, T036 em paralelo; T039, T040, T041, T042, T043 em paralelo após os services.
- US5: T053 e T059 em paralelo com o restante.
- Polish: T061, T063, T064, T066 em paralelo.
- Entre histórias: US2 e US4 podem avançar enquanto US1 está em andamento (arquivos distintos, exceto `AuthController`/`Program.cs`, que US4 só toca em T049/T051 e US1 em T022).

---

## Parallel Example: Phase 2

```bash
# Depois de T003–T006 (modelo, contexto, migração):
Task: "T007 Criar IPasswordHasher em AvaBot.Infra.Interfaces/AppServices/IPasswordHasher.cs"
Task: "T008 Criar Pbkdf2PasswordHasher em AvaBot.Infra/AppServices/Pbkdf2PasswordHasher.cs"
Task: "T009 Criar IUserRepository em AvaBot.Infra.Interfaces/Repository/IUserRepository.cs"
Task: "T013 Métodos novos em AvaBot.Calibration/Local/LocalRepositories.cs"
Task: "T014 AgentProfile ignora dono + UserProfile em AvaBot.Application/Profiles/"
Task: "T016 ClaimsPrincipalExtensions em AvaBot.API/Auth/ClaimsPrincipalExtensions.cs"
Task: "T019 Pbkdf2PasswordHasherTest em AvaBot.Tests/Infra/Pbkdf2PasswordHasherTest.cs"
```

## Parallel Example: User Story 2

```bash
# Depois de T032 (AgentService):
Task: "T034 PowerBIService com ownerUserId em AvaBot.Application/Services/PowerBIService.cs"
Task: "T035 TelegramService com ownerUserId em AvaBot.Application/Services/TelegramService.cs"
Task: "T036 WhatsappService com ownerUserId em AvaBot.Application/Services/WhatsappService.cs"

# Depois dos services:
Task: "T039 SessionController valida dono em AvaBot.API/Controllers/SessionController.cs"
Task: "T040 FileController valida dono em AvaBot.API/Controllers/FileController.cs"
Task: "T041 PowerBIController passa ownerUserId em AvaBot.API/Controllers/PowerBIController.cs"
Task: "T042 TelegramController passa ownerUserId em AvaBot.API/Controllers/TelegramController.cs"
Task: "T043 WhatsappController passa ownerUserId em AvaBot.API/Controllers/WhatsappController.cs"
```

---

## Implementation Strategy

### MVP First (US1 + US2)

As duas histórias P1 formam o MVP: cadastro aberto sem isolamento seria um risco (spec, US2 "Why this priority"), e isolamento sem cadastro não tem segundo usuário para testar.

1. Phase 1 + Phase 2 (migração aplicada).
2. Phase 3 (US1) → validar o Independent Test.
3. Phase 4 (US2) → validar com duas contas.
4. **Parar e validar**: neste ponto o produto já é multiusuário e seguro, mas um deploy em produção ainda precisa de US4 (bootstrap) para não deixar os agentes atuais órfãos.

### Incremental Delivery

1. Setup + Foundational → compila, migração aplicada.
2. US1 → cadastro e login (MVP parcial).
3. US2 → isolamento (MVP completo para ambiente novo).
4. US4 → bootstrap; **obrigatório antes do primeiro deploy em produção** (SC-004).
5. US3 → expiração e redirecionamento no painel.
6. US5 → página da conta.
7. Polish → testes de API, Bruno, `.gitignore`, docs, validação do quickstart.

### Ordem recomendada para um único desenvolvedor

Phase 1 → Phase 2 → US1 → US2 → US4 → US3 → US5 → Polish. US4 antes de US3 porque é pré-requisito do deploy; US3 é pequena e só toca o frontend e um teste.

---

## Notes

- Toda mensagem de "não encontrado" para agente alheio deve ser **idêntica** à de agente inexistente (FR-011, SC-002): não criar mensagens novas do tipo "sem permissão".
- Nenhuma tarefa adiciona pacote NuGet ou npm.
- O arquivo `AvaBot.Tests.API/appsettings.json` e o `.env` contêm credenciais reais: nunca copiá-las para exemplos, docs, testes ou commits.
- Commit a cada fase concluída (Phase 2, cada história, Polish).
