# Implementation Plan: Lista de Números Autorizados e Solicitação de Acesso no WhatsApp

**Branch**: `018-whatsapp-allowed-numbers` | **Date**: 2026-11-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/018-whatsapp-allowed-numbers/spec.md`

## Summary

Cada agente ganha um **modo de acesso ao WhatsApp**: "Todos", que é o padrão e mantém o comportamento atual, ou "Somente contatos autorizados". No modo restrito, só os contatos da lista do agente (nome + telefone e/ou identificador do WhatsApp) chegam à IA. Os demais remetentes passam por um **fluxo de solicitação de acesso** feito só com frases prontas personalizáveis por agente: oferta com "sim"/"não", confirmação, aguardando aprovação e recusa. Esse fluxo nunca consome tokens e envia no máximo 1 frase automática por hora por remetente. O dono aprova ou rejeita os pedidos no painel ou pelo MCP, e a aprovação cria o contato.

Abordagem técnica:
- **Banco**: 5 colunas em `avabot_agents` e 3 tabelas novas (`avabot_whatsapp_contacts`, `avabot_whatsapp_access_requests`, `avabot_whatsapp_sender_states`).
- **Regra**: um `WhatsappAccessService` com a regra de decisão, chamado pelo `WhatsappService.ProcessWebhookAsync` antes de qualquer sessão ou IA.
- **Telefone e resposta**: normalização de telefone com chave de comparação que ignora o nono dígito brasileiro, e parser determinístico de "sim"/"não".
- **LID**: resolução de LID para telefone pelo WPP Connect (`contact/pn-lid`), com cache. Se não der, a aprovação autoriza o próprio identificador.
- **Superfícies**: controller REST novo, 9 ferramentas MCP e três seções novas na página de WhatsApp do painel.

## Technical Context

**Language/Version**: C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend)  
**Primary Dependencies**: ASP.NET Core 9, EF Core 9 + Npgsql, FluentValidation (já na API), AutoMapper, `IMemoryCache` (já registrado), `HttpClient` do `WppConnectService` existente, `ModelContextProtocol.AspNetCore` 2.2.0 (já na API, feature 017); frontend com React Router 7, Zustand 5, Tailwind 4 e sonner 2. **Nenhuma dependência nova.**  
**Storage**: PostgreSQL. 5 colunas novas em `avabot_agents` (`whatsapp_access_mode` + 4 frases) e 3 tabelas novas. Uma migração (`AddWhatsappAccessControl`). Elasticsearch não é afetado.  
**Testing**: xUnit 2.9 + Moq em `AvaBot.Tests` (Domain: telefone, parser; Application: `WhatsappAccessService` com a tabela de verdade de 16 casos, `WhatsappService` com a ordem do webhook; API: validators, controller, ferramentas MCP, `McpOutputDtoTest`); `AvaBot.Tests.API` com Flurl para as rotas REST (rodam só no ambiente com banco); frontend `npm run lint` + `npm run build`.  
**Target Platform**: API Linux/Docker existente + WPP Connect Server (`wppconnect/server-cli` com `@wppconnect-team/wppconnect` 2.3.1); painel SPA.  
**Project Type**: Web service (backend .NET, Clean Architecture) + frontend React.  
**Performance Goals**: no modo restrito, o webhook acrescenta de 1 a 4 consultas indexadas por mensagem (contato, pedido, estado e gravação) e, no máximo, 1 chamada HTTP de resolução de LID por remetente a cada 24 h (cache). A resposta da frase pronta sai em menos de 1 s, sem IA. O modo "Todos" tem custo zero além da leitura de uma coluna do agente já carregado.  
**Constraints**: zero tokens de IA e zero sessões de chat para quem não está autorizado (SC-001); no máximo 1 frase limitada por hora por (agente, remetente), persistida para sobreviver a restart (SC-005); comportamento idêntico no modo "Todos" (SC-007); nenhuma mudança no contrato do webhook nem nas rotas atuais do WhatsApp; nenhum `WhatsappToken` no MCP; mensagens da API em português sem acento, como as atuais.  
**Scale/Scope**: dezenas de agentes; centenas de contatos e pedidos por agente. Entregas: 3 entidades + 5 colunas, 1 migração, 3 repositórios, 1 service novo + mudanças em `WhatsappService`, `WppConnectService` e `AgentRepository.DeleteAsync`, 1 controller com 9 rotas, 3 validators, 9 ferramentas MCP, 3 componentes + 1 store + 1 service no frontend, 9 requisições Bruno.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I. Skills obrigatórias | Backend segue a `dotnet-architecture`: entidades e enums em `AvaBot.Domain`, DTOs com `[JsonPropertyName]` em `AvaBot.DTO`, interfaces genéricas `IXxxRepository<T>` em `Infra.Interfaces`, repositórios e Fluent API em `Infra`, service em `Application`, DI em `DependencyInjection.cs`, migração via `dotnet ef`. Frontend: `types/`, `Services/` e `stores/` (Zustand) no padrão vigente das features 007–017; desvio preexistente, não ampliado. ✅ |
| II. Stack | Nenhuma dependência nova. .NET 9, Zustand e Tailwind são desvios preexistentes já registrados nas features anteriores. ✅ |
| III. Case sensitivity | `frontend/src/Services/`, `stores/`, `types/`, `components/admin/whatsapp/`; backend em PascalCase. ✅ |
| IV. Convenções de código | PascalCase, `_camelCase`, namespaces file-scoped, JSON camelCase; ferramentas MCP em `snake_case` como as da feature 017; frontend com `interface`, arrow functions e `const`. ✅ |
| V. Banco | Tabelas `avabot_*` em snake_case plural, PKs `{entidade}_id bigint identity` com `{tabela}_pkey`, FKs `avabot_fk_{pai}_{filho}` com **`ClientSetNull`** (sem Cascade; a remoção explícita no `AgentRepository.DeleteAsync` segue o padrão do Power BI), `timestamp without time zone`, `varchar` com tamanho, status/enums `integer` com default. ✅ |
| VI. Autenticação | Rotas novas com `[Authorize]` e verificação de dono; webhook continua `[AllowAnonymous]` sem mudança; MCP pelo esquema `McpAccessKey` existente; nenhum segredo no frontend nem no MCP. ✅ |
| VII. Variáveis de ambiente | Nenhuma variável nova. ✅ |
| VIII. Tratamento de erros | Controller com `try/catch`: `KeyNotFoundException` → 404, `ValidationException` / `InvalidOperationException` → 400, `Exception` → 500 com `Result.Failure`. Webhook continua engolindo erros e devolvendo 200; falha no envio da frase só é logada. ✅ |

**Resultado**: aprovado sem violações. **Reavaliação após a Fase 1**: os contratos não criam rota pública, variável nem dependência. O FK `agent_id NOT NULL` com `ClientSetNull` repete o que `avabot_powerbi_datasets` já faz, e as linhas filhas são removidas em código antes do agente. ✅

## Project Structure

### Documentation (this feature)

```text
specs/018-whatsapp-allowed-numbers/
├── plan.md
├── research.md                       # R1–R11
├── data-model.md                     # colunas, 3 tabelas, estados, repositórios
├── quickstart.md                     # validação ponta a ponta com 2 celulares
├── contracts/
│   ├── whatsapp-access-api.md        # REST, DTOs, validators, service, frontend, Bruno
│   ├── whatsapp-webhook-flow.md      # nova ordem do webhook, algoritmo de decisão, tabela de verdade
│   └── mcp-tools.md                  # 9 ferramentas *_whatsapp
├── checklists/requirements.md
└── tasks.md                          # /speckit.tasks
```

### Source Code (repository root)

```text
AvaBot.Domain/
├── Enums/WhatsappAccessMode.cs                     # NOVO: All=1, Restricted=2
├── Enums/WhatsappContactSource.cs                  # NOVO: Manual=1, Approval=2
├── Enums/WhatsappAccessRequestStatus.cs            # NOVO: Pending=1, Approved=2, Rejected=3
├── Enums/WhatsappAccessMessage.cs                  # NOVO: Offer, Confirmation, Waiting, Declined
├── Models/Agent.cs                                 # + WhatsappAccessMode, 4 frases, navegações, GetWhatsappMessage()
├── Models/WhatsappContact.cs                       # NOVO
├── Models/WhatsappAccessRequest.cs                 # NOVO
├── Models/WhatsappSenderState.cs                   # NOVO
├── Models/WhatsappAccessDefaults.cs                # NOVO: textos padrão
├── Models/WhatsappPhone.cs                         # NOVO: Normalize, MatchKey, FromJid (R4)
└── Models/WhatsappReplyParser.cs                   # NOVO: sim/não (R5)

AvaBot.DTO/WhatsappAccessDTOs.cs                    # NOVO: 7 DTOs (contrato REST)

AvaBot.Infra.Interfaces/
├── Repository/IWhatsappContactRepository.cs        # NOVO
├── Repository/IWhatsappAccessRequestRepository.cs  # NOVO
├── Repository/IWhatsappSenderStateRepository.cs    # NOVO
└── AppServices/IWppConnectService.cs               # + GetPhoneByLidAsync

AvaBot.Infra/
├── Context/AvaBotContext.cs                        # 3 DbSets, mapeamentos, 5 colunas do agente
├── Repository/WhatsappContactRepository.cs         # NOVO
├── Repository/WhatsappAccessRequestRepository.cs   # NOVO
├── Repository/WhatsappSenderStateRepository.cs     # NOVO
├── Repository/AgentRepository.cs                   # DeleteAsync remove os filhos novos (R10)
├── AppServices/WppConnectService.cs                # GetPhoneByLidAsync: GET contact/pn-lid + IMemoryCache (R3)
└── Migrations/<ts>_AddWhatsappAccessControl.cs     # gerada por dotnet ef

AvaBot.Application/
├── Services/WhatsappAccessService.cs               # NOVO: config, contatos, pedidos, EvaluateIncomingAsync
├── Services/WhatsappAccessDecision.cs              # NOVO
├── Services/WhatsappSender.cs                      # NOVO: FromPayload (R2)
├── Services/WhatsappService.cs                     # ProcessWebhookAsync: nova ordem + gate de acesso
├── Profiles/WhatsappAccessProfile.cs               # NOVO: entidades → DTOs
└── DependencyInjection.cs                          # 3 repositórios + WhatsappAccessService

AvaBot.API/
├── Controllers/WhatsappAccessController.cs         # NOVO: 9 rotas (contrato REST)
├── Validators/WhatsappAccessConfigUpdateInfoValidator.cs   # NOVO
├── Validators/WhatsappContactInsertInfoValidator.cs        # NOVO
├── Validators/WhatsappAccessRequestApproveInfoValidator.cs # NOVO
├── Mcp/Tools/WhatsappAccessTools.cs                # NOVO: 9 ferramentas
└── Mcp/McpServerInstructions.cs                    # + parágrafo do controle de acesso

AvaBot.Calibration/Local/LocalRepositories.cs       # só se implementar IWppConnectService/IAgentRepository (verificar no build)

AvaBot.Tests/
├── Domain/WhatsappPhoneTest.cs                     # NOVO
├── Domain/WhatsappReplyParserTest.cs               # NOVO
├── Application/Services/WhatsappSenderTest.cs      # NOVO: privado, grupo (author), LID, nome de perfil
├── Application/Services/WhatsappAccessServiceTest.cs  # NOVO: tabela de verdade + CRUD + aprovação/rejeição + FR-026
├── Application/Services/WhatsappServiceTest.cs     # NOVO: modo Todos inalterado, gate antes do ChatService, tipo não suportado
├── API/Validators/WhatsappAccess*ValidatorTest.cs  # NOVOS
├── API/Controllers/WhatsappAccessControllerTest.cs # NOVO: mapeamento de erros e dono
├── API/Mcp/WhatsappAccessToolsTest.cs              # NOVO
├── API/Mcp/McpOutputDtoTest.cs                     # + DTOs novos
└── API/Mcp/McpToolRegistrationTest.cs              # + 9 ferramentas

AvaBot.Tests.API/Controllers/WhatsappAccessControllerTest.cs  # NOVO: rotas REST contra a API no ar

frontend/src/
├── types/whatsappAccess.ts                         # NOVO
├── Services/WhatsappAccessService.ts               # NOVO
├── stores/useWhatsappAccessStore.ts                # NOVO
├── components/admin/whatsapp/WhatsappAccessSettings.tsx     # NOVO
├── components/admin/whatsapp/WhatsappAccessRequestList.tsx  # NOVO
├── components/admin/whatsapp/WhatsappContactList.tsx        # NOVO
└── pages/admin/WhatsappPage.tsx                    # + 3 seções

bruno/WhatsApp/                                     # + 9 requisições
docs/MCP_SETUP.md                                   # + ferramentas *_whatsapp na lista
```

**Structure Decision**: web application com backend .NET em Clean Architecture (raiz) e frontend React em `frontend/`, como as features anteriores. Nenhum projeto novo.

## Design notes (Fase 1)

- **Ordem de implementação**:
  1. Domain: enums, `WhatsappPhone`, `WhatsappReplyParser` e defaults, com testes. É código puro e roda localmente.
  2. Entidades, contexto, repositórios e migração.
  3. `WhatsappAccessService` (config, contatos, pedidos) com testes; controller, validators e Bruno.
  4. `WhatsappSender`, `GetPhoneByLidAsync`, `EvaluateIncomingAsync` e o gate no `WhatsappService`, com a tabela de verdade. Com isso o MVP de backend (US1 + US2 + US3 via API) fica completo.
  5. Ferramentas MCP.
  6. Frontend.
  7. `AgentRepository.DeleteAsync`, docs e testes de API.
- **Regra central em um só lugar**: painel, MCP e webhook passam pelo `WhatsappAccessService`. O webhook só executa a decisão (`Allow`, `Reply` ou `Ignore`), o que permite testar o algoritmo inteiro com repositórios mockados.
- **Modo "Todos" intocado**: o gate só roda com `WhatsappAccessMode == Restricted`. A única mudança visível no caminho atual é carregar o agente antes da checagem de tipo, sem efeito no resultado. O `WhatsappServiceTest` cobre isso.
- **Grupos**: a verificação usa `author`. Não autorizado em grupo → `Ignore`, para não poluir o grupo com o fluxo de solicitação. O destino da resposta da IA em grupo continua `from`, como hoje.
- **Risco: rota `pn-lid`**. Ela depende da versão do WPP Connect Server (R3). Se faltar, o fluxo continua correto, porque a aprovação autoriza o identificador. Apenas contatos cadastrados à mão, que só chegam por LID, não são reconhecidos até o backfill, que acontece quando a pessoa escreve uma vez por `@c.us`. O quickstart (passo 6) verifica isso.
- **Testes de API**: o ambiente local não tem banco nem docker (memória do projeto). O `AvaBot.Tests.API/WhatsappAccessControllerTest` é escrito, mas só roda no ambiente com banco. Localmente valem `dotnet build`, `dotnet test AvaBot.Tests` e o lint/build do frontend.

## Complexity Tracking

Nenhuma violação da constituição a justificar.
