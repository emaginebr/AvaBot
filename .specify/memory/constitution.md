<!--
  SYNC IMPACT REPORT
  ==================
  Version change: 0.0.0 (template) -> 1.0.0
  Modified principles: N/A (initial ratification)
  Added sections:
    - Principle I: Skills Obrigatorias
    - Principle II: Stack Tecnologica
    - Principle III: Case Sensitivity de Diretorios
    - Principle IV: Convencoes de Codigo
    - Principle V: Convencoes de Banco de Dados
    - Principle VI: Autenticacao e Seguranca
    - Principle VII: Variaveis de Ambiente
    - Principle VIII: Padroes de Tratamento de Erros
    - Section: Checklist para Novos Contribuidores
    - Section: Governance
  Removed sections: N/A (initial)
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ no update needed
      (Constitution Check section uses dynamic gates)
    - .specify/templates/spec-template.md ✅ no update needed
      (Generic placeholders, compatible with all principles)
    - .specify/templates/tasks-template.md ✅ no update needed
      (Phase structure is compatible; no principle-specific task types)
    - .specify/templates/commands/ ✅ directory does not exist
  Follow-up TODOs: none
-->

# Avachat Constitution

## Core Principles

### I. Skills Obrigatorias

Para implementacao de novas entidades e funcionalidades, as seguintes
skills **DEVEM** ser utilizadas:

| Skill | Quando usar | Invocacao |
|---|---|---|
| **dotnet-architecture** | Criar/modificar entidades, services, repositories, DTOs, migrations, DI no backend | `/dotnet-architecture` |
| **react-architecture** | Criar Types, Service, Context, Hook e registrar Provider no frontend | `/react-architecture` |

Estas skills cobrem em detalhe:

- Estrutura de projetos e fluxo de dependencia (Clean Architecture backend)
- Regras de repositorios genericos, mapeamento manual, DI centralizado
- Configuracao de DbContext, Fluent API e migracoes via `dotnet ef`
- Padroes de arquivos frontend (Types, Services, Contexts, Hooks)
- Provider chain e registro de novos providers
- Padroes de tratamento de erros no frontend (handleError, clearError,
  loading state)
- Convencoes de nomeacao de DTOs (`Info`, `InsertInfo`, `Result`) e
  chaves portuguesas (`sucesso`, `mensagem`, `erros`)

**NAO** reimplemente esses padroes manualmente — siga as skills.

### II. Stack Tecnologica

#### Backend

| Tecnologia | Versao | Finalidade |
|---|---|---|
| .NET | 8.0 | Runtime e framework principal |
| Entity Framework Core | 9.x | ORM e migracoes |
| PostgreSQL | Latest | Banco de dados relacional |
| NAuth | Latest | Autenticacao (Basic token) |
| zTools | Latest | Upload S3, e-mail (MailerSend), slugs |
| Swashbuckle | 8.x | Swagger / OpenAPI |

#### Frontend

| Tecnologia | Versao | Finalidade |
|---|---|---|
| React | 18.x | Framework UI |
| TypeScript | 5.x | Tipagem estatica |
| React Router | 6.x | Roteamento SPA |
| Vite | 6.x | Build toolchain |
| Bootstrap | 5.x | Sistema de grid e componentes base |
| i18next | 25.x | Internacionalizacao |
| Axios | 1.x | HTTP client (legado) |
| Fetch API | Nativo | HTTP client (novos servicos) |

#### Regras de Stack

- Vite e o bundler obrigatorio — NAO usar CRA, Webpack manual, ou
  outros bundlers.
- NAO introduzir ORMs alternativos (Dapper, etc.) — EF Core e o unico
  ORM permitido.
- NAO adicionar bibliotecas de state management (Redux, Zustand, MobX)
  — Context API e o padrao.
- NAO executar comandos `docker` ou `docker compose` no ambiente local
  — Docker nao esta acessivel.
- Variaveis de ambiente frontend usam prefixo `VITE_` (padrao Vite).
  NAO usar `REACT_APP_`.

### III. Case Sensitivity de Diretorios (Inviolavel)

| Diretorio | Casing | Motivo |
|---|---|---|
| `Contexts/` | Uppercase C | Compatibilidade Docker/Linux |
| `Services/` | Uppercase S | Compatibilidade Docker/Linux |
| `hooks/` | Lowercase h | Convencao React |
| `types/` | Lowercase t | Convencao TypeScript |

Todos os imports DEVEM corresponder exatamente ao casing no disco.

### IV. Convencoes de Codigo

#### Backend (.NET)

| Elemento | Convencao | Exemplo |
|---|---|---|
| Namespaces | PascalCase | `Avachat.Domain.Services` |
| Classes / Interfaces | PascalCase | `CampaignService`, `ICampaignRepository` |
| Metodos | PascalCase | `GetById()`, `MapToDto()` |
| Propriedades | PascalCase | `CampaignId`, `CreatedAt` |
| Campos privados | _camelCase | `_repository`, `_context` |
| Constantes | UPPER_CASE | `BUCKET_NAME` |
| Namespaces | File-scoped | `namespace Avachat.API;` |

#### Frontend (TypeScript/React)

| Elemento | Convencao | Exemplo |
|---|---|---|
| Componentes | PascalCase | `LoginPage`, `CampaignCard` |
| Interfaces | PascalCase | `CampaignContextType` |
| Variaveis / Funcoes | camelCase | `getHeaders`, `loadCampaigns` |
| Constantes | UPPER_CASE | `AUTH_STORAGE_KEY` |
| Tipos | `interface` (nao `type`) | `interface CampaignInfo {}` |
| Funcoes | Arrow functions | `const fn = () => {}` |
| Variaveis | `const` por padrao | `const campaigns = []` |

#### JSON Property Names

- Backend: `[JsonPropertyName("camelCase")]` em todas as propriedades
  de DTOs.
- Frontend: Acesso direto via camelCase nos tipos TypeScript.

### V. Convencoes de Banco de Dados (PostgreSQL)

| Elemento | Convencao | Exemplo |
|---|---|---|
| Tabelas | snake_case plural | `campaigns`, `campaign_entries` |
| Colunas | snake_case | `campaign_id`, `created_at` |
| Primary Keys | `{entidade}_id`, bigint identity | `campaign_id bigint PK` |
| Constraint PK | `{tabela}_pkey` | `campaigns_pkey` |
| Foreign Keys | `fk_{pai}_{filho}` | `fk_campaign_entry` |
| Delete behavior | `ClientSetNull` | Nunca Cascade |
| Timestamps | `timestamp without time zone` | Sem timezone |
| Strings | `varchar` com MaxLength | `varchar(260)` |
| Booleans | `boolean` com default | `DEFAULT true` |
| Status/Enums | `integer` | `DEFAULT 1` |

Configuracao de DbContext, Fluent API e comandos de migracao sao
detalhados na skill `dotnet-architecture`.

### VI. Autenticacao e Seguranca

| Aspecto | Padrao |
|---|---|
| Esquema | Basic Authentication via NAuth |
| Header | `Authorization: Basic {token}` |
| Storage (frontend) | localStorage key `"login-with-metamask:auth"` |
| Handler | `NAuthHandler` registrado no DI |
| Protecao de rotas | Atributo `[Authorize]` nos controllers |

#### Regras de Seguranca

- NUNCA armazenar tokens em cookies — usar localStorage.
- NUNCA expor connection strings ou secrets no frontend.
- Controllers com dados sensiveis DEVEM ter `[Authorize]`.
- CORS configurado como `AllowAnyOrigin` apenas em Development.

### VII. Variaveis de Ambiente

#### Backend

| Variavel | Obrigatoria | Descricao |
|---|---|---|
| `ConnectionStrings__AvachatContext` | Sim | Connection string PostgreSQL |
| `ASPNETCORE_ENVIRONMENT` | Sim | Development, Docker, Production |

#### Frontend

| Variavel | Obrigatoria | Descricao |
|---|---|---|
| `VITE_API_URL` | Sim | URL base da API backend |
| `VITE_SITE_BASENAME` | Nao | Base path do React Router |

Prefixo obrigatorio `VITE_` — padrao Vite. Acessar via
`import.meta.env.VITE_*`.

### VIII. Padroes de Tratamento de Erros

#### Backend

```csharp
try { /* logica */ }
catch (Exception ex) { return StatusCode(500, ex.Message); }
```

#### Frontend

Padroes de tratamento de erros no frontend (handleError, clearError,
loading state) sao cobertos pela skill `react-architecture`.

## Checklist para Novos Contribuidores

Antes de submeter qualquer codigo, verifique:

- [ ] Utilizou a skill `dotnet-architecture` para novas entidades backend
- [ ] Utilizou a skill `react-architecture` para novas entidades frontend
- [ ] Tabelas e colunas seguem snake_case no PostgreSQL
- [ ] Imports respeitam o casing exato dos diretorios
- [ ] Variaveis de ambiente frontend usam prefixo `VITE_`
- [ ] Controllers com dados sensiveis possuem `[Authorize]`

## Governance

### Procedimento de Emenda

1. Qualquer alteracao a esta constituicao DEVE ser documentada com
   justificativa explicita.
2. Emendas DEVEM incluir um plano de migracao para codigo existente
   que viole os novos principios.
3. A versao DEVE ser incrementada seguindo semantic versioning:
   - **MAJOR**: Remocao ou redefinicao incompativel de principios.
   - **MINOR**: Novo principio/secao adicionado ou expansao material.
   - **PATCH**: Correcoes de escrita, clarificacoes sem impacto semantico.

### Conformidade

- Todos os PRs e reviews DEVEM verificar conformidade com esta
  constituicao.
- Complexidade adicional DEVE ser justificada na secao Complexity
  Tracking do plano de implementacao.
- Skills `dotnet-architecture` e `react-architecture` sao a fonte
  autoritativa para padroes de implementacao detalhados.

**Version**: 1.0.0 | **Ratified**: 2026-04-02 | **Last Amended**: 2026-04-08
