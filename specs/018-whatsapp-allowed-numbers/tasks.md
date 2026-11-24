---
description: "Task list for 018-whatsapp-allowed-numbers"
---

# Tasks: Lista de Números Autorizados e Solicitação de Acesso no WhatsApp

**Input**: Design documents from `specs/018-whatsapp-allowed-numbers/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (whatsapp-access-api.md, whatsapp-webhook-flow.md, mcp-tools.md), quickstart.md

**Tests**: incluídos, porque o plan.md define a suíte (`AvaBot.Tests` com xUnit + Moq e `AvaBot.Tests.API` com Flurl). O ambiente local não tem banco nem docker: localmente rodam `dotnet build`, `dotnet test AvaBot.Tests` e `npm run lint`/`npm run build`. Os testes de `AvaBot.Tests.API` só rodam no ambiente com banco.

**Organization**: tarefas agrupadas por user story. Ordem de execução, por dependência: US4 (contatos) → US1 (restrição) → US2 (solicitação) → US3 (aprovação) → US5 (frases/modo no painel). US1–US4 são P1 e US5 é P2.

**Skills obrigatórias (constituição I)**: `dotnet-architecture` para entidades, repositórios, DTOs, DI e migração; o padrão de frontend do repositório (`types/` + `Services/` + `stores/` Zustand); `react-modal` para modais; `react-alert` (sonner) para toasts.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1–US5 conforme o spec.md

---

## Phase 1: Setup

**Purpose**: confirmar a base antes de mexer no código.

- [X] T001 Rodar `dotnet build AvaBot.sln` e `dotnet test AvaBot.Tests` na raiz e anotar falhas preexistentes. Há alterações não commitadas em `AvaBot.Application/Services/PowerBIToolProvider.cs` e `AvaBot.Tests/Application/Services/PowerBIToolProviderTest.cs`; não tocar nelas.
- [X] T002 [P] Rodar `npm run lint` e `npm run build` em `frontend/` e anotar o estado inicial.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: domínio, persistência, DTOs e o esqueleto do service que todas as histórias usam.

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase terminar.

### Domínio puro

- [X] T003 [P] Criar enums em `AvaBot.Domain/Enums/`: `WhatsappAccessMode.cs` (`All = 1`, `Restricted = 2`), `WhatsappContactSource.cs` (`Manual = 1`, `Approval = 2`), `WhatsappAccessRequestStatus.cs` (`Pending = 1`, `Approved = 2`, `Rejected = 3`) e `WhatsappAccessMessage.cs` (`Offer`, `Confirmation`, `Waiting`, `Declined`). Seguir o estilo dos enums existentes na pasta.
- [X] T004 [P] Criar `AvaBot.Domain/Models/WhatsappAccessDefaults.cs` com as 4 constantes de texto padrão, exatamente como na tabela do data-model.md §1, mais `MaxMessageLength = 500`.
- [X] T005 [P] Criar `AvaBot.Domain/Models/WhatsappPhone.cs` (classe estática), conforme research R4:
  - `string? Normalize(string? input)`: só dígitos; remove o prefixo `00`; se o texto original não começa com `+`, tem 10 ou 11 dígitos e não começa com `55`, prefixa `55`; devolve `null` fora de 10–15 dígitos.
  - `string? MatchKey(string? normalized)`: se começa com `55`, tem 13 dígitos e o caractere de índice 4 é `'9'`, remove esse caractere.
  - `string? FromJid(string? jid)`: dígitos antes de `@c.us`; `null` para `@lid`, `@g.us` ou vazio.
- [X] T006 [P] Criar `AvaBot.Domain/Models/WhatsappReplyParser.cs`: enum aninhado ou separado `WhatsappReply { None, Yes, No }` e `Parse(string? text)`. A normalização faz trim, lowercase, remoção de acentos via `NormalizationForm.FormD` + remoção de `UnicodeCategory.NonSpacingMark` e mantém só letras. `sim`/`s` → Yes, `nao`/`n` → No, o resto → None (research R5).
- [X] T007 [P] Criar `AvaBot.Tests/Domain/WhatsappPhoneTest.cs` cobrindo:
  - `"+55 (11) 98765-4321"`, `"11 98765-4321"` e `"5511987654321"` normalizam igual.
  - `"(11) 3333-4444"` vira `551133334444`.
  - `"+1 415 555 0100"` não ganha 55.
  - `"123"` → null.
  - `MatchKey("5511987654321") == MatchKey("551187654321")`.
  - `FromJid("5511987654321@c.us")`, `FromJid("123@lid") == null` e `FromJid("123-456@g.us") == null`.
- [X] T008 [P] Criar `AvaBot.Tests/Domain/WhatsappReplyParserTest.cs` com `[Theory]`:
  - Yes: `"sim"`, `"Sim!"`, `" SIM. "`, `"s"`
  - No: `"não"`, `"Nao"`, `"N"`
  - None: `"sim, quero"`, `"talvez"`, `""`, `null`

### Entidades e persistência

- [X] T009 Alterar `AvaBot.Domain/Models/Agent.cs`:
  - Propriedades `WhatsappAccessMode WhatsappAccessMode { get; set; } = WhatsappAccessMode.All` e `string?` para `WhatsappOfferMessage`, `WhatsappConfirmationMessage`, `WhatsappWaitingMessage` e `WhatsappDeclinedMessage`.
  - Navegações `ICollection<WhatsappContact> WhatsappContacts` e `ICollection<WhatsappAccessRequest> WhatsappAccessRequests`.
  - Método `string GetWhatsappMessage(WhatsappAccessMessage kind)`, que devolve a coluna, ou o padrão de `WhatsappAccessDefaults` quando ela é null.
  Depende de T003 e T004.
- [X] T010 [P] Criar `AvaBot.Domain/Models/WhatsappContact.cs`, `AvaBot.Domain/Models/WhatsappAccessRequest.cs` e `AvaBot.Domain/Models/WhatsappSenderState.cs` com as propriedades das tabelas do data-model.md §2–§4. Navegações: `Agent` nas três; `DecidedByUser` (`User?`) e `WhatsappContact?` no pedido. Depende de T003.
- [X] T011 Mapear em `AvaBot.Infra/Context/AvaBotContext.cs`:
  - `DbSet<WhatsappContact> WhatsappContacts`, `DbSet<WhatsappAccessRequest> WhatsappAccessRequests` e `DbSet<WhatsappSenderState> WhatsappSenderStates`.
  - Tabelas, colunas, PKs, FKs (`ClientSetNull`) e índices únicos parciais com `HasFilter`, exatamente como no data-model.md.
  - No bloco de `Agent`: as 5 colunas novas (`whatsapp_access_mode` com `.HasConversion<int>().HasDefaultValue(WhatsappAccessMode.All)` e as frases como `varchar(500)`).
  - Usar como modelo o bloco `McpAccessKey`. Depende de T009 e T010.
- [X] T012 Gerar a migração com `dotnet ef migrations add AddWhatsappAccessControl --project AvaBot.Infra --startup-project AvaBot.API`. Revisar o `.cs` gerado em `AvaBot.Infra/Migrations/`: só 5 `AddColumn` em `avabot_agents`, 3 `CreateTable` e os índices; nenhum `DropColumn`. Depende de T011.
- [X] T013 [P] Criar as interfaces `AvaBot.Infra.Interfaces/Repository/IWhatsappContactRepository.cs`, `IWhatsappAccessRequestRepository.cs` e `IWhatsappSenderStateRepository.cs` com as assinaturas do data-model.md §6.
- [X] T014 [P] Implementar `AvaBot.Infra/Repository/WhatsappContactRepository.cs`:
  - `GetAllByAgentAsync` ordena por `Name`.
  - `FindBySenderAsync` faz `AgentId == x && ((key != null && PhoneMatchKey == key) || WhatsappId == wid)`.
  - `CreateAsync` e `UpdateAsync` preenchem `CreatedAt`/`UpdatedAt` em UTC.
  Depende de T011 e T013.
- [X] T015 [P] Implementar `AvaBot.Infra/Repository/WhatsappAccessRequestRepository.cs`:
  - `GetByAgentAsync(agentId, status)` ordena por `RequestedAt` desc.
  - `GetCurrentForSenderAsync` devolve o mais recente com `WhatsappId == wid || (key != null && PhoneMatchKey == key)`.
  - `GetPendingByPhoneAsync` e `CountPendingAsync`.
  Depende de T011 e T013.
- [X] T016 [P] Implementar `AvaBot.Infra/Repository/WhatsappSenderStateRepository.cs`: `GetAsync` e `UpsertAsync`. O upsert insere ou atualiza por `(AgentId, WhatsappId)`; se o insert violar o índice único, captura a `DbUpdateException`, desanexa, relê e atualiza. Depende de T011 e T013.
- [X] T017 Alterar `AvaBot.Infra/Repository/AgentRepository.cs` `DeleteAsync`: antes de `_context.Agents.Remove(agent)`, remover nesta ordem `WhatsappSenderStates`, `WhatsappAccessRequests` e `WhatsappContacts` com `AgentId == id` (FR-029). Depende de T011.
- [X] T018 [P] Criar `AvaBot.DTO/WhatsappAccessDTOs.cs` com os 7 DTOs do contracts/whatsapp-access-api.md (`WhatsappAccessConfigInfo`, `WhatsappAccessMessageInfo`, `WhatsappAccessConfigUpdateInfo`, `WhatsappContactInfo`, `WhatsappContactInsertInfo`, `WhatsappAccessRequestInfo` e `WhatsappAccessRequestApproveInfo`), todos com `[JsonPropertyName]`.
- [X] T019 [P] Criar `AvaBot.Application/Profiles/WhatsappAccessProfile.cs` (AutoMapper): `WhatsappContact → WhatsappContactInfo` (`Source` como int) e `WhatsappAccessRequest → WhatsappAccessRequestInfo` (`Phone ← PhoneNormalized`, `Status` como int). Seguir o estilo de `McpAccessKeyProfile.cs`. Depende de T010 e T018.
- [X] T020 Criar o esqueleto de `AvaBot.Application/Services/WhatsappAccessService.cs`: construtor com os 3 repositórios, `IMapper` e `ILogger<WhatsappAccessService>`; métodos ainda vazios, que as histórias preenchem. Registrar em `AvaBot.Application/DependencyInjection.cs` os 3 repositórios (`services.AddScoped<IWhatsappContactRepository<WhatsappContact>, WhatsappContactRepository>()` etc.) e `services.AddScoped<WhatsappAccessService>()`. Depende de T014–T016 e T019.
- [X] T021 Rodar `dotnet build AvaBot.sln` e `dotnet test AvaBot.Tests --filter "FullyQualifiedName~Whatsapp"` (T007 e T008 verdes). Se `AvaBot.Calibration/Local/LocalRepositories.cs` quebrar por implementar uma interface alterada, ajustar lá.

**Checkpoint**: domínio e persistência prontos, migração gerada, build verde.

---

## Phase 3: User Story 4 - Gerenciar a lista de contatos autorizados (Priority: P1)

**Goal**: o dono lista, adiciona, edita e remove contatos (nome + telefone) por agente, com normalização e bloqueio de duplicidade.

**Independent Test**: pelas rotas `/whatsapp/{slug}/contacts` (Bruno ou painel), adicionar `(11) 98765-4321`, tentar `+55 11 8765-4321` (duplicado, 400), editar o nome, remover; o outro agente não vê o contato.

### Tests for User Story 4

- [X] T022 [P] [US4] Criar `AvaBot.Tests/Application/Services/WhatsappAccessServiceContactsTest.cs` (Moq nos repositórios) com estes cenários:
  - `AddContactAsync` normaliza e grava `Phone`, `PhoneNormalized` e `PhoneMatchKey` com `Source = Manual`.
  - Duplicado pela chave do nono dígito → `InvalidOperationException` "Este telefone ja esta cadastrado neste agente".
  - Telefone inválido → `InvalidOperationException` "Telefone invalido".
  - `UpdateContactAsync` com id de outro agente → `KeyNotFoundException` "Contato nao encontrado".
  - Update que troca o telefone limpa um `WhatsappId` `@c.us` de outro número e mantém `@lid`.
  - `RemoveContactAsync` remove.
  - Adicionar contato com telefone igual ao de um pedido `Pending` marca o pedido `Approved` com `WhatsappContactId`, `DecidedAt` e `DecidedByUserId = null` (FR-026).
- [X] T023 [P] [US4] Criar `AvaBot.Tests/API/Validators/WhatsappContactInsertInfoValidatorTest.cs`: nome vazio, nome com 121 caracteres, telefone vazio e telefone inválido falham; `"11 98765-4321"` passa.

### Implementation for User Story 4

- [X] T024 [US4] Implementar em `AvaBot.Application/Services/WhatsappAccessService.cs`: `ListContactsAsync`, `AddContactAsync`, `UpdateContactAsync` e `RemoveContactAsync`, conforme o contrato REST e a validação do data-model.md §2, incluindo a regra do FR-026 via `GetPendingByPhoneAsync`. As exceções e mensagens são as da tabela de erros do contrato. Depende de T020.
- [X] T025 [P] [US4] Criar `AvaBot.API/Validators/WhatsappContactInsertInfoValidator.cs` (FluentValidation, mensagens do contrato) e registrar como os validators existentes, conforme `Program.cs`/DI atual.
- [X] T026 [US4] Criar `AvaBot.API/Controllers/WhatsappAccessController.cs` com `[ApiController]`, `[Authorize]` e um helper privado `ResolveAgentAsync(slug)` (`AgentService.GetOwnedBySlugAsync(slug, User.GetUserId()) ?? throw new KeyNotFoundException("Agente nao encontrado")`). Rotas: `GET/POST whatsapp/{slug}/contacts`, `PUT/DELETE whatsapp/{slug}/contacts/{contactId}`. O mapeamento de erros segue o contrato (404/400/500 com `Result<object>.Failure`), validando com o validator antes de chamar o service, como no `McpAccessKeyController`. Depende de T024 e T025.
- [X] T027 [P] [US4] Criar `AvaBot.Tests/API/Controllers/WhatsappAccessControllerTest.cs` com testes das rotas de contatos: agente alheio → 404; `InvalidOperationException` → 400 com a mensagem; sucesso → 200 com `Result<WhatsappContactInfo>`. Seguir o padrão de `AvaBot.Tests/API/Controllers/` existente.
- [X] T028 [P] [US4] Criar `frontend/src/types/whatsappAccess.ts` com todas as interfaces do contrato e as constantes `WHATSAPP_ACCESS_MODE` e `WHATSAPP_REQUEST_STATUS`.
- [X] T029 [US4] Criar `frontend/src/Services/WhatsappAccessService.ts` com todos os métodos do contrato (`getConfig`, `updateConfig`, `listContacts`, `addContact`, `updateContact`, `removeContact`, `listRequests`, `approveRequest`, `rejectRequest`), reutilizando `getApiUrl`, os cabeçalhos de auth e `handleResponse` como em `frontend/src/Services/AgentService.ts` (extrair para módulo compartilhado só se já não for exportado). Depende de T028.
- [X] T030 [US4] Criar `frontend/src/stores/useWhatsappAccessStore.ts` (Zustand, no estilo de `useMcpAccessKeyStore.ts`):
  - Estado: `config`, `contacts`, `requests`, `requestFilter`, `loading` e `error`.
  - Ações: `loadContacts(slug)`, `addContact`, `updateContact`, `removeContact` e `reset()`. As ações de config e pedidos entram em US1, US3 e US5.
  Depende de T029.
- [X] T031 [US4] Criar `frontend/src/components/admin/whatsapp/WhatsappContactList.tsx`:
  - Tabela com nome, telefone (ou "Não identificado") e origem (Manual/Aprovação).
  - Botão "Adicionar contato" e editar em modal (skill `react-modal`) com os campos nome e telefone; erros da API viram toast (sonner).
  - Remover com confirmação.
  Depende de T030.
- [X] T032 [US4] Alterar `frontend/src/pages/admin/WhatsappPage.tsx`: abaixo do bloco de conexão, renderizar a seção "Contatos autorizados" (`WhatsappContactList`). O `useEffect` chama `loadContacts(selectedAgent.slug)` e `reset()` ao trocar de agente. Depende de T031.
- [X] T033 [P] [US4] Criar as requisições Bruno `bruno/WhatsApp/Contacts List.bru`, `Contact Add.bru`, `Contact Update.bru` e `Contact Delete.bru`, copiando o formato e as variáveis de `bruno/WhatsApp/Status.bru`.

**Checkpoint**: contatos gerenciáveis pela API e pelo painel; nenhum efeito no atendimento ainda.

---

## Phase 4: User Story 1 - Restringir o atendimento a contatos autorizados (Priority: P1) 🎯 MVP

**Goal**: com o modo "Somente contatos autorizados", só contatos da lista chegam à IA. Os demais recebem a frase de oferta (limitada a 1 por hora) sem nenhum token gasto, e grupos com não autorizado são ignorados. O modo "Todos" fica idêntico ao atual.

**Independent Test**: quickstart passos 4 e 5. Em modo "Todos", **B** é atendido. Ativado o modo restrito, **A** (cadastrado) é atendido, **B** recebe só a oferta e uma segunda mensagem de **B** logo em seguida não recebe nada.

### Tests for User Story 1

- [X] T034 [P] [US1] Criar `AvaBot.Tests/Application/Services/WhatsappSenderTest.cs` com payloads JSON de exemplo:
  - Privado `@c.us`: `WhatsappId = from` e `Phone` = dígitos.
  - Privado `@lid`: `Phone = null`.
  - Grupo: `WhatsappId = author`, não `from`.
  - `ProfileName` cai de `sender.pushname` para `notifyName` e depois para `sender.formattedName`, com corte em 120.
  - Payload sem `author` em grupo: `WhatsappId` vazio.
- [X] T035 [P] [US1] Criar `AvaBot.Tests/Application/Services/WhatsappAccessServiceEvaluateTest.cs` com os casos 1–6, 15 e 16 da tabela de verdade de `contracts/whatsapp-webhook-flow.md` §3 (Allow por telefone, por `@lid` e por chave sem 9; Ignore em grupo; Reply(Oferta); Ignore por limite de 1 h; oferta para tipo não suportado; frase personalizada). Verificar também o backfill de `WhatsappId` no contato e que o estado gravado tem `LastAutoReplyAt` e `OfferSentAt`.
- [X] T036 [P] [US1] Criar `AvaBot.Tests/Application/Services/WhatsappServiceTest.cs` (mocks de `IWppConnectService`, `IAgentRepository<Agent>`, `AgentService` e `ChatService` conforme as dependências reais, e `WhatsappAccessService`):
  - Modo `All` nunca chama `EvaluateIncomingAsync` e chama o `ChatService` como hoje.
  - Modo `Restricted` com `Reply` envia o texto via `SendMessageAsync` e **não** chama `CreateSessionAsync` nem `ProcessMessageAsync`.
  - `Ignore` não envia nada.
  - `Allow` segue para o `ChatService`.
  - Tipo `ptt` de não autorizado recebe a oferta e não a rejeição de tipo.
  - Remetente `@lid` chama `GetPhoneByLidAsync` antes de avaliar.
  Se `AgentService`/`ChatService` não forem mockáveis (classes concretas sem métodos virtuais), extrair o mínimo necessário ou testar via `WhatsappAccessService` mockado e repositórios reais em memória, documentando a escolha no topo do arquivo.

### Implementation for User Story 1

- [X] T037 [P] [US1] Criar `AvaBot.Application/Services/WhatsappSender.cs` (record/classe com `WhatsappId`, `Phone`, `ProfileName` e `IsGroup`, e `static WhatsappSender FromPayload(JsonElement payload)`), conforme research R2.
- [X] T038 [P] [US1] Criar `AvaBot.Application/Services/WhatsappAccessDecision.cs` com `Kind { Allow, Reply, Ignore }`, `Text` e `Reason`, e fábricas `Allow()`, `Reply(string text, string stage)` e `Ignore(string reason)`.
- [X] T039 [US1] Adicionar `Task<string?> GetPhoneByLidAsync(string session, string lid)` em `AvaBot.Infra.Interfaces/AppServices/IWppConnectService.cs` e implementar em `AvaBot.Infra/AppServices/WppConnectService.cs` (research R3):
  - Chama `GET /api/{session}/contact/pn-lid/{Uri.EscapeDataString(lid)}` com o cliente autenticado existente e lê `phoneNumber` (aceitando também `response.phoneNumber`; tirar `@c.us` se vier).
  - Cache em `IMemoryCache` (injetar no construtor): sucesso por 24 h; null por 10 min.
  - Qualquer exceção ou status diferente de 2xx → log `Warning` e `null`.
  Atualizar qualquer implementação fake ou local de `IWppConnectService`, se existir.
- [X] T040 [US1] Implementar `EvaluateIncomingAsync(Agent agent, WhatsappSender sender, string? text)` em `AvaBot.Application/Services/WhatsappAccessService.cs`, passos A–C e E da seção 2 do `contracts/whatsapp-webhook-flow.md`, sem o ramo de "sim"/"não" e sem o passo D, que entram em US2. O helper privado `LimitedReplyAsync(agent, sender, kind, now)` aplica a janela de 1 h e grava o estado. Inclui o log FR-013 no formato do contrato. Depende de T024, T037 e T038.
- [X] T041 [US1] Alterar `ProcessWebhookAsync` em `AvaBot.Application/Services/WhatsappService.cs` para a nova ordem da seção 1 do `contracts/whatsapp-webhook-flow.md`:
  1. Criar o `WhatsappSender` após extrair os campos.
  2. Carregar o agente (`GetBySlugAsync` + validações atuais) antes da checagem de `msgType`.
  3. Se `Restricted`: resolver o LID quando `Phone == null` e o JID termina em `@lid`, chamar `EvaluateIncomingAsync` (`text` = `body` só quando `type == "chat"`) e executar `Ignore`/`Reply` (envio no destino `from` sem `@c.us`, erros só logados) ou seguir no `Allow`.
  Injetar `WhatsappAccessService` no construtor. Manter intacto o restante (rejeição de tipo, sessão, `ChatService`). Depende de T039 e T040.
- [X] T042 [US1] Implementar em `WhatsappAccessService`:
  - `GetConfigAsync(agent)`: modo, as 4 frases com `Text`/`IsDefault`/`DefaultText`, `ContactCount` e `PendingRequestCount`. Adicionar `CountByAgentAsync` em `IWhatsappContactRepository`/`WhatsappContactRepository` se necessário.
  - `UpdateConfigAsync(agent, info)`: grava o modo e as frases, com null = padrão e trim; atualiza o agente pelo `IAgentRepository<Agent>.UpdateAsync`.
- [X] T043 [P] [US1] Criar `AvaBot.API/Validators/WhatsappAccessConfigUpdateInfoValidator.cs`: `Mode` em {1, 2}; cada frase null ou com 1–500 caracteres após trim ("A frase nao pode ficar vazia" / "A frase deve ter no maximo 500 caracteres"). Adicionar o teste `AvaBot.Tests/API/Validators/WhatsappAccessConfigUpdateInfoValidatorTest.cs`.
- [X] T044 [US1] Adicionar ao `AvaBot.API/Controllers/WhatsappAccessController.cs` as rotas `GET whatsapp/{slug}/access` e `PUT whatsapp/{slug}/access`, com as mensagens do contrato. Depende de T042 e T043.
- [X] T045 [US1] Adicionar ao `frontend/src/stores/useWhatsappAccessStore.ts` as ações `loadConfig(slug)` e `saveConfig(slug, info)` e recarregar o config após mutações de contato. Criar `frontend/src/components/admin/whatsapp/WhatsappAccessSettings.tsx` só com o seletor de modo ("Todos" / "Somente contatos autorizados") e o botão Salvar; as frases entram em US5. Renderizar no topo das seções novas em `frontend/src/pages/admin/WhatsappPage.tsx`, carregando o config no `useEffect`.
- [X] T046 [P] [US1] Criar `bruno/WhatsApp/Access Get.bru` e `bruno/WhatsApp/Access Update.bru`.

**Checkpoint**: MVP. A restrição funciona de ponta a ponta com contatos manuais, e não autorizados recebem só a oferta, sem IA.

---

## Phase 5: User Story 2 - Usuário não cadastrado solicita acesso pelo WhatsApp (Priority: P1)

**Goal**: "sim" à oferta cria o pedido pendente e envia a confirmação; "não" envia a recusa; com pedido pendente, novas mensagens recebem "aguardando"; com pedido rejeitado, tudo é ignorado. Nada usa IA.

**Independent Test**: quickstart passos 6.1 e 7.2. **B** responde "Sim" → confirmação + pedido pendente no banco. **B** responde "não" numa nova oferta → recusa, sem pedido.

### Tests for User Story 2

- [X] T047 [P] [US2] Estender `AvaBot.Tests/Application/Services/WhatsappAccessServiceEvaluateTest.cs` com os casos 7–14 da tabela de verdade:
  - "Sim!" cria o pedido com `WhatsappId`, `PhoneNormalized`, `PhoneMatchKey` e `ProfileName`, e envia a confirmação ignorando o limite.
  - "não" envia a recusa sem pedido.
  - "talvez" é ignorado pelo limite.
  - Oferta de 25 h atrás → nova oferta.
  - Pendente → aguardando (respeitando 1 h).
  - "sim" repetido com pendente não cria um segundo pedido.
  - Rejeitado → Ignore.
  - Aprovado com contato removido → oferta.
  Incluir o caso de `DbUpdateException` de unicidade na criação → `Ignore("pedido-duplicado")`.

### Implementation for User Story 2

- [X] T048 [US2] Completar `EvaluateIncomingAsync` em `AvaBot.Application/Services/WhatsappAccessService.cs` com:
  - O passo D (`GetCurrentForSenderAsync`: `Rejected` → Ignore; `Pending` → limitado `Waiting`; `Approved` → seguir).
  - O ramo de resposta do passo E (`offerOpen` em 24 h + `WhatsappReplyParser.Parse`): Yes cria o pedido, limpa `OfferSentAt`, grava `LastAutoReplyAt` e responde `Confirmation`; No limpa a oferta, grava `LastAutoReplyAt` e responde `Declined`.
  - Captura da violação de unicidade na criação do pedido.
  Tudo conforme o `contracts/whatsapp-webhook-flow.md`. Depende de T040.

**Checkpoint**: o fluxo completo de solicitação funciona pelo WhatsApp; os pedidos ficam no banco.

---

## Phase 6: User Story 3 - Aprovar ou rejeitar solicitações (Priority: P1)

**Goal**: o dono vê os pedidos (com o contador de pendentes), aprova (com o nome editável, criando ou vinculando o contato) ou rejeita, e pode aprovar depois de rejeitar.

**Independent Test**: quickstart passos 6.3–6.4 e 7.3. Aprovar **B** → contato com origem "Aprovação" e a próxima mensagem de **B** é respondida pela IA. Rejeitar → mensagens seguintes ignoradas.

### Tests for User Story 3

- [X] T049 [P] [US3] Criar `AvaBot.Tests/Application/Services/WhatsappAccessServiceRequestsTest.cs` com estes cenários:
  - `ListRequestsAsync` com e sem filtro.
  - Aprovar `Pending` cria o contato `Source = Approval` com o nome informado, ou na falta dele `ProfileName`, telefone ou JID; grava `DecidedAt`, `DecidedByUserId` e `WhatsappContactId`.
  - Aprovar quando já existe contato com a mesma chave ou JID vincula o existente sem duplicar.
  - Aprovar `Rejected` funciona.
  - Aprovar `Approved` → `InvalidOperationException` "Solicitacao ja aprovada".
  - Rejeitar não pendente → "Solicitacao nao esta pendente".
  - Id de outro agente → `KeyNotFoundException` "Solicitacao nao encontrada".
  - Pedido aprovado só com `@lid` (sem telefone) gera um contato com `Phone = null` e `WhatsappId` preenchido.
- [X] T050 [P] [US3] Criar `AvaBot.Tests/API/Validators/WhatsappAccessRequestApproveInfoValidatorTest.cs`: nome null passa; nome com 121 caracteres falha.

### Implementation for User Story 3

- [X] T051 [US3] Implementar `ListRequestsAsync`, `ApproveRequestAsync(agent, requestId, name, decidedByUserId)` e `RejectRequestAsync(agent, requestId, decidedByUserId)` em `AvaBot.Application/Services/WhatsappAccessService.cs`, conforme as transições do data-model.md §3. Depende de T024.
- [X] T052 [P] [US3] Criar `AvaBot.API/Validators/WhatsappAccessRequestApproveInfoValidator.cs`.
- [X] T053 [US3] Adicionar ao `AvaBot.API/Controllers/WhatsappAccessController.cs` as rotas `GET whatsapp/{slug}/access-requests?status=`, `POST whatsapp/{slug}/access-requests/{requestId}/approve` (corpo `WhatsappAccessRequestApproveInfo`, `User.GetUserId()` como decisor) e `POST whatsapp/{slug}/access-requests/{requestId}/reject`. Estender os testes em `AvaBot.Tests/API/Controllers/WhatsappAccessControllerTest.cs`. Depende de T051 e T052.
- [X] T054 [US3] Adicionar ao `frontend/src/stores/useWhatsappAccessStore.ts` as ações `loadRequests(slug, status?)`, `setRequestFilter`, `approveRequest(slug, id, name?)` (recarrega pedidos, contatos e config) e `rejectRequest(slug, id)`.
- [X] T055 [US3] Criar `frontend/src/components/admin/whatsapp/WhatsappAccessRequestList.tsx`:
  - Abas Pendentes (com contador `config.pendingRequestCount`), Aprovadas e Rejeitadas.
  - Colunas nome de perfil, telefone (ou "Não identificado") e data.
  - "Aprovar" abre um modal (`react-modal`) com o campo nome preenchido (`profileName` → telefone); "Rejeitar" pede confirmação. Toasts sonner.
  Renderizar a seção "Solicitações de acesso" em `frontend/src/pages/admin/WhatsappPage.tsx` entre as configurações e os contatos, carregando os pendentes no `useEffect`. Depende de T054.
- [X] T056 [P] [US3] Criar `bruno/WhatsApp/Access Requests List.bru`, `Access Request Approve.bru` e `Access Request Reject.bru`.

**Checkpoint**: o ciclo completo (pedir → aprovar → atendido pela IA) funciona pelo painel.

---

## Phase 7: User Story 5 - Configurar modo e frases prontas (Priority: P2)

**Goal**: o dono personaliza as 4 frases (restaurar padrão, sem vazio, máximo de 500) e recebe o aviso ao restringir com a lista vazia; a lista é preservada ao voltar para "Todos".

**Independent Test**: alterar a frase de oferta e dispará-la para **B** (chega o texto novo); "Restaurar padrão" volta ao original; com 0 contatos, escolher "Somente contatos autorizados" mostra o aviso.

### Tests for User Story 5

- [X] T057 [P] [US5] Criar `AvaBot.Tests/Application/Services/WhatsappAccessServiceConfigTest.cs` com estes cenários:
  - `GetConfigAsync` de um agente novo → modo 1, `IsDefault = true` nas 4 frases e `Text == DefaultText`.
  - `UpdateConfigAsync` grava texto com trim e `IsDefault = false`.
  - null restaura o padrão.
  - Mudar para `All` não apaga contatos nem pedidos (repositórios não recebem delete).

### Implementation for User Story 5

- [X] T058 [US5] Completar `frontend/src/components/admin/whatsapp/WhatsappAccessSettings.tsx`:
  - 4 `textarea` (Oferta, Confirmação, Aguardando aprovação, Recusa) com contador `n/500`, "Restaurar padrão" por frase (envia `null`) e validação local de vazio.
  - Aviso fixo quando o modo restrito está selecionado e `config.contactCount === 0`: "Nenhum contato está autorizado; todos receberão apenas as frases de solicitação".
  - Salvar envia modo + 4 frases num único `saveConfig`, com toast de sucesso ou erro.

**Checkpoint**: todas as histórias funcionam de forma independente.

---

## Phase 8: MCP (FR-031)

**Purpose**: expor o controle de acesso aos assistentes de IA pelo servidor MCP da feature 017.

- [X] T059 Criar `AvaBot.API/Mcp/Tools/WhatsappAccessTools.cs` (`[McpServerToolType]`) com as 9 ferramentas do `contracts/mcp-tools.md`, no padrão de `AvaBot.API/Mcp/Tools/ChannelTools.cs`: método público com `RequestContext` delegando a `XxxAsync(McpCaller caller, ...)` dentro de `McpToolResults.RunAsync`, e agente via `GetOwnedByIdAsync`. Detalhes:
  - Anotações `ReadOnly`/`Idempotent`/`Destructive` conforme a tabela do contrato.
  - `atualizar_acesso_whatsapp` é parcial, com `restoreDefaults`.
  - `listar_solicitacoes_whatsapp` converte o status textual ("pendente"/"aprovada"/"rejeitada").
  - Validação com os mesmos validators FluentValidation, do mesmo jeito que `AgentTools` faz.
  Registrar a classe onde as demais são registradas (`Program.cs` / `WithTools<>`).
- [X] T060 Acrescentar o parágrafo do controle de acesso em `AvaBot.API/Mcp/McpServerInstructions.cs` (texto no contrato).
- [X] T061 [P] Criar `AvaBot.Tests/API/Mcp/WhatsappAccessToolsTest.cs` (usar `McpTestContext.cs`) com estes cenários:
  - Agente alheio → `isError` "Agente nao encontrado".
  - Atualização parcial mantém as frases omitidas e `restoreDefaults: ["oferta"]` zera só a oferta.
  - Status textual inválido → `isError`.
  - `caller.UserId` repassado como decisor na aprovação e na rejeição.
- [X] T062 [P] Atualizar `AvaBot.Tests/API/Mcp/McpOutputDtoTest.cs` para incluir os DTOs de `AvaBot.DTO/WhatsappAccessDTOs.cs` na checagem de campos de segredo. Atualizar `AvaBot.Tests/API/Mcp/McpToolRegistrationTest.cs` com as 9 ferramentas novas.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [X] T063 [P] Criar `AvaBot.Tests.API/Controllers/WhatsappAccessControllerTest.cs` (Flurl, no padrão de `McpAccessKeyControllerTest.cs`) cobrindo:
  - O CRUD de contatos, o GET/PUT de config e a listagem de pedidos.
  - 404 para agente alheio e 401 sem token.
  Roda só no ambiente com banco.
- [X] T064 [P] Atualizar `docs/MCP_SETUP.md` com as ferramentas `*_whatsapp` na lista. Se existir um doc ou README de WhatsApp, acrescentar uma seção curta "Controle de acesso" descrevendo os modos, o fluxo de solicitação e o limite de 1 frase por hora.
- [X] T065 Rodar `dotnet build AvaBot.sln`, `dotnet test AvaBot.Tests` e, em `frontend/`, `npm run lint` e `npm run build`; corrigir o que quebrar.
- [ ] T066 Executar o `specs/018-whatsapp-allowed-numbers/quickstart.md` no ambiente com WPP Connect pareado e registrar o resultado do passo 6.2 (rota `contact/pn-lid` disponível ou não) em `specs/018-whatsapp-allowed-numbers/research.md`, na R3.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup e bloqueia todas as histórias.
- **US4 (Phase 3)**: depende da Fase 2. É a primeira porque o US1 precisa de contatos para ser testado.
- **US1 (Phase 4)**: depende da Fase 2 e do `AddContactAsync` (T024) para o teste independente; o código do gate depende só da Fase 2 + T024.
- **US2 (Phase 5)**: depende de US1 (T040, `EvaluateIncomingAsync` base).
- **US3 (Phase 6)**: o backend (T051–T053) depende só da Fase 2 + T024 e pode correr em paralelo a US1/US2; o teste ponta a ponta precisa de pedidos criados por US2.
- **US5 (Phase 7)**: depende de US1 (T042–T045).
- **MCP (Phase 8)**: depende dos métodos de service de US1, US3 e US4.
- **Polish (Phase 9)**: depende de tudo.

### Within Each User Story

- Testes do service escritos junto ou antes do service; validators antes do controller; service antes do controller; tipos → service TS → store → componente → página.
- `WhatsappAccessService.cs`, `WhatsappAccessController.cs`, `useWhatsappAccessStore.ts` e `WhatsappPage.tsx` são arquivos compartilhados entre histórias: as tarefas que os tocam são sequenciais (sem [P]).

### Parallel Opportunities

- Fase 2: T003–T008 em paralelo; depois T013–T016 e T018–T019 em paralelo.
- US4: T022, T023, T025, T027, T028 e T033 em paralelo.
- US1: T034–T038, T043 e T046 em paralelo.
- US3: T049, T050, T052 e T056 em paralelo; o backend de US3 pode andar junto com US1/US2 por outra pessoa.
- Fase 8: T061 e T062 em paralelo após T059.

---

## Parallel Example: Phase 2

```text
Task: "T005 WhatsappPhone em AvaBot.Domain/Models/WhatsappPhone.cs"
Task: "T006 WhatsappReplyParser em AvaBot.Domain/Models/WhatsappReplyParser.cs"
Task: "T007 WhatsappPhoneTest em AvaBot.Tests/Domain/WhatsappPhoneTest.cs"
Task: "T008 WhatsappReplyParserTest em AvaBot.Tests/Domain/WhatsappReplyParserTest.cs"
```

## Parallel Example: User Story 1

```text
Task: "T034 WhatsappSenderTest"
Task: "T035 WhatsappAccessServiceEvaluateTest (casos 1–6, 15, 16)"
Task: "T037 WhatsappSender.FromPayload"
Task: "T038 WhatsappAccessDecision"
Task: "T043 WhatsappAccessConfigUpdateInfoValidator + teste"
```

---

## Implementation Strategy

### MVP First

1. Fases 1 e 2.
2. Fase 3 (US4: contatos), necessária para testar a restrição.
3. Fase 4 (US1: restrição). **Parar e validar** com o quickstart passos 4–5: o agente já pode ser fechado para uma lista conhecida, sem gasto de IA com estranhos.
4. Deploy opcional do MVP: não autorizados recebem a oferta, mas o "sim" ainda não cria pedido.

### Incremental Delivery

1. MVP (US4 + US1).
2. US2: pedidos pelo WhatsApp. Recomenda-se publicar junto com US3, para que os pedidos não fiquem sem tela de aprovação.
3. US3: aprovação no painel.
4. US5: personalização das frases.
5. MCP e Polish.

---

## Notes

- Mensagens da API em português sem acento, como as existentes ("Agente nao encontrado"); textos do frontend e frases padrão com acentuação correta.
- Não alterar o contrato do webhook nem as rotas atuais do `WhatsappController`.
- Commit ao fim de cada fase. Não incluir no commit as alterações preexistentes não relacionadas (`PowerBIToolProvider*`, `appsettings.Development.json`, `.specify/*`, `bruno/WhatsApp/QR Code.bru`, `Status.bru`).
