# Implementation Plan: Contas de usuário e autenticação

**Branch**: `016-user-accounts-auth` | **Date**: 2026-10-10 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/016-user-accounts-auth/spec.md`

## Summary

Substituir o login único por configuração por contas de usuário no banco: entidade `User`, cadastro público no site, login por e-mail e senha, JWT HS256 (como hoje) com validade de 30 dias, e cada agente com um dono. Toda rota do painel passa a filtrar pelo dono; rotas públicas (chat, widget, webhooks) não mudam. Na primeira subida, um bootstrap cria a conta do administrador atual a partir de `Auth:Username`/`Auth:Password` e atribui a ela os agentes existentes. Sem confirmação de e-mail, sem recuperação de senha e sem administrador global nesta versão (decisões da spec).

## Technical Context

**Language/Version**: C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend)  
**Primary Dependencies**: ASP.NET Core 9 + `Microsoft.AspNetCore.Authentication.JwtBearer` (já na API), EF Core 9 + Npgsql, FluentValidation (já na API), `System.Security.Cryptography` (PBKDF2, BCL), `IMemoryCache` (já registrado), AutoMapper; frontend com React Router 7, Zustand 5, Tailwind 4, sonner 2. **Nenhuma dependência nova.**  
**Storage**: PostgreSQL. Uma tabela nova (`avabot_users`) e uma coluna nova em `avabot_agents` (`owner_user_id`). Elasticsearch não é afetado.  
**Testing**: xUnit 2.9 + Moq em `AvaBot.Tests` (UserService, hasher, bootstrap, isolamento em AgentService/PowerBIService, AuthController); `AvaBot.Tests.API` (TestBase atualizado); frontend `npm run lint` + `npm run build` (não há suíte de testes no frontend).  
**Target Platform**: API Linux/Docker existente; painel SPA no navegador  
**Project Type**: Web service (backend .NET) + frontend React  
**Performance Goals**: cadastro e login em < 500 ms no servidor (o hash PBKDF2 com 210k iterações custa ~100 ms); listagens do painel sem custo extra perceptível (o filtro por dono usa índice)  
**Constraints**: JWT obrigatório (pedido); token em `localStorage` (constituição VI); mensagem de 404 idêntica para agente alheio e inexistente (FR-011); nenhuma senha em logs, respostas ou console (SC-005); canais públicos intactos (FR-012); deploy atual continua funcionando após migração + bootstrap (SC-004)  
**Scale/Scope**: dezenas a centenas de contas; 1 tabela, 1 coluna, 5 endpoints novos/alterados em `/auth`, ~25 rotas do painel ganhando filtro de dono, 2 páginas novas e 3 alteradas no frontend

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I. Skills obrigatórias | Backend segue a `dotnet-architecture`: DTO em `AvaBot.DTO` com `[JsonPropertyName]`, interface de repositório genérica em `Infra.Interfaces`, modelo rico em `Domain`, repositório e AppService em `Infra`, DI centralizada em `DependencyInjection.cs`, migração via `dotnet ef`. Frontend segue o padrão do repositório (`Services/` + `stores/` Zustand), que é o que as features 007–011 usaram no lugar do par Context/Hook da skill `react-architecture`; desvio preexistente, não ampliado. ✅ |
| II. Stack | Nenhuma lib nova. A constituição cita NAuth/Basic e .NET 8; o repositório já usa JWT Bearer e .NET 9 desde a feature 007, e o pedido manda "continuar usando JWT". Desvio preexistente e requisito explícito do usuário. ✅ |
| III. Case sensitivity | Backend: pastas novas `AvaBot.API/Auth/`. Frontend: `Services/` (S maiúsculo) e `stores/` como já existem; páginas em `pages/auth/` e `pages/admin/`. ✅ |
| IV. Convenções de código | PascalCase, `_camelCase`, namespaces file-scoped, JSON camelCase; frontend `interface`, arrow functions, `const`. ✅ |
| V. Banco | `avabot_users` snake_case, PK `user_id bigint identity`, `avabot_users_pkey`, FK `avabot_fk_users_agents` com `ClientSetNull` (nunca Cascade), `timestamp without time zone`, `varchar` com tamanho, `status integer DEFAULT 1`. ✅ |
| VI. Autenticação | `[Authorize]` em todos os controllers do painel (inalterado) e nas rotas `/auth/me*`; token em `localStorage`; nenhum segredo no frontend; CORS inalterado. O esquema é JWT, não Basic/NAuth (ver II). ✅ |
| VII. Variáveis de ambiente | Nenhuma variável nova; `AVABOT_USERNAME`/`AVABOT_PASSWORD` passam a alimentar só o bootstrap. ✅ |
| VIII. Tratamento de erros | Controllers mantêm `try/catch → StatusCode(500, Result.Failure)`; `KeyNotFoundException → 404`, `InvalidOperationException → 400/409`, `ValidationException → 400`, no estilo de `PowerBIController.Failure`. ✅ |

**Resultado**: aprovado. Reavaliação após a Fase 1: os contratos não introduzem dependência, cookie, variável nova nem rota pública nova além de `/auth/register`. Sem itens para Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/016-user-accounts-auth/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── auth-api.md        # /auth/register, /auth/login, /auth/me, /auth/me/password, JWT, configuração
│   └── ownership.md       # filtro por dono em cada rota do painel e assinaturas dos services
├── checklists/requirements.md
└── tasks.md               # /speckit.tasks
```

### Source Code (repository root)

```text
AvaBot.DTO/
└── UserDTOs.cs                         # NOVO: UserInfo, UserRegisterInfo, UserLoginInfo, UserUpdateInfo, UserPasswordChangeInfo, AuthResultInfo

AvaBot.Infra.Interfaces/
├── Repository/IUserRepository.cs       # NOVO: GetByIdAsync, GetByEmailAsync, EmailExistsAsync, CountAsync, CreateAsync, UpdateAsync
├── Repository/IAgentRepository.cs      # + GetAllByOwnerAsync, GetByIdAsync(id, ownerUserId), GetBySlugAsync(slug, ownerUserId), CountWithoutOwnerAsync, AssignOwnerToOrphansAsync
└── AppServices/IPasswordHasher.cs      # NOVO: Hash, Verify

AvaBot.Domain/Models/
├── User.cs                             # NOVO (data-model)
└── Agent.cs                            # + OwnerUserId, Owner

AvaBot.Infra/
├── Context/AvaBotContext.cs            # DbSet<User>, mapeamento avabot_users, FK/índice em avabot_agents
├── Repository/UserRepository.cs        # NOVO
├── Repository/AgentRepository.cs       # métodos por dono + órfãos
├── AppServices/Pbkdf2PasswordHasher.cs # NOVO (research R1)
└── Migrations/<ts>_AddUsersAndAgentOwner.cs  # gerada por dotnet ef

AvaBot.Application/
├── Services/UserService.cs             # NOVO: RegisterAsync, AuthenticateAsync (com bloqueio), GetAsync, UpdateNameAsync, ChangePasswordAsync
├── Services/UserBootstrapService.cs    # NOVO: EnsureAdminAccountAsync (research R5)
├── Services/AgentService.cs            # assinaturas com ownerUserId (contracts/ownership.md)
├── Services/PowerBIService.cs          # idem
├── Services/TelegramService.cs         # idem (3 métodos admin)
├── Services/WhatsappService.cs         # idem (4 métodos admin)
├── Profiles/AgentProfile.cs            # ignora OwnerUserId/Owner no mapa de insert
├── Profiles/UserProfile.cs             # NOVO: User → UserInfo
└── DependencyInjection.cs              # registra IUserRepository, IPasswordHasher, UserService, UserBootstrapService

AvaBot.API/
├── Auth/JwtTokenIssuer.cs              # NOVO: emite o JWT (sub, email, name, exp) (research R2)
├── Auth/ClaimsPrincipalExtensions.cs   # NOVO: GetUserId()
├── Controllers/AuthController.cs       # register, login, me, me (PUT), me/password
├── Controllers/AgentController.cs      # ownerUserId em todas as rotas autenticadas
├── Controllers/SessionController.cs    # valida dono em GetSessions e GetMessages
├── Controllers/FileController.cs       # valida dono do agente
├── Controllers/PowerBIController.cs    # passa ownerUserId
├── Controllers/TelegramController.cs   # passa ownerUserId nas rotas admin
├── Controllers/WhatsappController.cs   # passa ownerUserId nas rotas admin
├── Validators/UserRegisterInfoValidator.cs, UserLoginInfoValidator.cs, UserUpdateInfoValidator.cs, UserPasswordChangeInfoValidator.cs  # NOVOS
├── Program.cs                          # registra JwtTokenIssuer; mapeia claim sub; chama UserBootstrapService após build
└── appsettings.json                    # Auth:TokenExpirationMinutes = 43200

AvaBot.Calibration/Local/LocalRepositories.cs   # implementa os métodos novos de IAgentRepository

AvaBot.Tests/
├── Application/Services/UserServiceTest.cs         # NOVO: cadastro, e-mail duplicado/normalizado, login, bloqueio, troca de senha
├── Application/Services/UserBootstrapServiceTest.cs # NOVO: cria conta e atribui órfãos; falha com órfãos sem credenciais; idempotente
├── Application/Services/AgentServiceTest.cs         # + isolamento por dono
├── Application/Services/PowerBIServiceTest.cs       # + agente de outro dono → KeyNotFoundException
├── Infra/Pbkdf2PasswordHasherTest.cs                # NOVO
└── API/Controllers/AuthControllerTest.cs            # reescrito para UserService mockado + JwtTokenIssuer

AvaBot.Tests.API/
├── Support/TestBase.cs, Support/ApiSettings.cs     # login por email, token em dados.token
└── appsettings.Example.json                         # NOVO (o appsettings.json local sai do git)

frontend/src/
├── types/auth.ts                       # AuthCredentials{email,password}, RegisterInfo, UserInfo, AuthResultInfo, PasswordChangeInfo
├── Services/AuthService.ts             # register, login, me, updateMe, changePassword, exp do JWT, user em storage, redirect ?expired=1
├── stores/useAuthStore.ts              # user, register, refreshUser, updateName, changePassword
├── pages/auth/LoginPage.tsx            # e-mail, link "Criar conta", aviso "Esqueceu a senha? Fale com o suporte", mensagem de sessão expirada
├── pages/auth/RegisterPage.tsx         # NOVO
├── pages/admin/AccountPage.tsx         # NOVO: nome/e-mail, alterar nome, alterar senha
├── components/admin/AdminNavbar.tsx    # nome do usuário + link para a conta
├── components/admin/AdminSidebar.tsx   # item "Minha conta"
├── pages/LandingPage.tsx               # links "Entrar" e "Criar conta"
└── App.tsx                             # rotas /register e /admin/account

bruno/Auth/                             # Login.bru atualizado; Register, Me, Update Me, Change Password novos; collection.bru lê dados.token
docs/                                   # README/AUTH: seção de contas, bootstrap e variáveis
.gitignore                              # corrige Avachat.Tests.API → AvaBot.Tests.API (research R8)
```

**Structure Decision**: web application com backend .NET em Clean Architecture (raiz) e frontend React em `frontend/`, exatamente como as features anteriores. Nada de projeto novo.

## Design notes (Fase 1)

- **Fluxo de cadastro**: `AuthController.Register` → validator → `UserService.RegisterAsync(info)` normaliza o e-mail, checa `EmailExistsAsync` (→ `InvalidOperationException` "Este e-mail ja esta em uso" → `409`), hasheia e grava → controller emite o token com `JwtTokenIssuer` e devolve `201 Result<AuthResultInfo>`.
- **Fluxo de login**: `UserService.AuthenticateAsync(email, password)` consulta o bloqueio no cache, busca por e-mail normalizado, verifica hash e `IsActive`; em falha incrementa o contador e devolve `null`; o controller responde `401` genérico. Sucesso zera o contador.
- **Filtro de dono**: contratos em `contracts/ownership.md`. A ordem de implementação é repositório → `AgentService` → demais services → controllers, com os testes de `AgentServiceTest` ajustados para as novas assinaturas.
- **Bootstrap**: `UserBootstrapService.EnsureAdminAccountAsync()` roda uma vez no `Program.cs` dentro de um escopo de DI, antes de `app.Run()`. Loga o resultado com `ILogger` e lança exceção se restar órfão (a API não sobe).
- **Frontend**: `AuthService.isAuthenticated()` decodifica `exp`; `useAuthStore` inicializa `user` do storage e expõe `register`; `ProtectedRoute` continua lendo `isAuthenticated`. A `RegisterPage` chama `register` e navega para `/admin` (FR-005). O cabeçalho mostra `user.name`.
- **Compatibilidade**: o corpo e a resposta de `POST /auth/login` mudam (research R10); todos os consumidores conhecidos estão no repositório e são atualizados na feature.

## Complexity Tracking

Sem violações da constituição a justificar.
