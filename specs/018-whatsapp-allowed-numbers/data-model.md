# Data Model: Lista de Números Autorizados e Solicitação de Acesso no WhatsApp

**Feature**: `018-whatsapp-allowed-numbers` | **Date**: 2026-11-23

Migração única: `AddWhatsappAccessControl`, gerada com `dotnet ef migrations add` no `AvaBot.Infra`. São 5 colunas novas em `avabot_agents` e 3 tabelas novas. Nenhum dado existente é alterado: o default `1` em `whatsapp_access_mode` mantém todos os agentes em "Todos" (FR-002).

---

## 1. Agent (existente) — colunas novas

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `WhatsappAccessMode` | `whatsapp_access_mode` | `integer NOT NULL DEFAULT 1` | Enum `WhatsappAccessMode`: `All = 1`, `Restricted = 2` |
| `WhatsappOfferMessage` | `whatsapp_offer_message` | `varchar(500) NULL` | NULL = texto padrão |
| `WhatsappConfirmationMessage` | `whatsapp_confirmation_message` | `varchar(500) NULL` | NULL = texto padrão |
| `WhatsappWaitingMessage` | `whatsapp_waiting_message` | `varchar(500) NULL` | NULL = texto padrão |
| `WhatsappDeclinedMessage` | `whatsapp_declined_message` | `varchar(500) NULL` | NULL = texto padrão |

Navegações novas: `ICollection<WhatsappContact> WhatsappContacts` e `ICollection<WhatsappAccessRequest> WhatsappAccessRequests`.

**Textos padrão**: `AvaBot.Domain/Models/WhatsappAccessDefaults.cs` (constantes):

| Frase | Padrão |
|---|---|
| Oferta | `Me desculpe, você não possui acesso. Deseja solicitar agora? Responda apenas "sim" ou "não".` |
| Confirmação | `Seu acesso foi solicitado. Assim que for aprovado, você poderá fazer mais perguntas.` |
| Aguardando | `Ainda estamos aguardando a aprovação do seu acesso.` |
| Recusa | `Tudo bem. Se mudar de ideia, é só enviar uma nova mensagem.` |

O método `Agent.GetWhatsappMessage(WhatsappAccessMessage kind)` devolve a coluna, ou o padrão quando ela é NULL.

---

## 2. WhatsappContact (novo) — `avabot_whatsapp_contacts`

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `WhatsappContactId` | `whatsapp_contact_id` | `bigint identity` | PK `avabot_whatsapp_contacts_pkey` |
| `AgentId` | `agent_id` | `bigint NOT NULL` | FK `avabot_fk_agents_whatsapp_contacts`, `ClientSetNull` |
| `Name` | `name` | `varchar(120) NOT NULL` | Obrigatório, com trim, 1–120 |
| `Phone` | `phone` | `varchar(30) NULL` | Como digitado (exibição); obrigatório no cadastro manual |
| `PhoneNormalized` | `phone_normalized` | `varchar(15) NULL` | `WhatsappPhone.Normalize`, 10–15 dígitos |
| `PhoneMatchKey` | `phone_match_key` | `varchar(15) NULL` | `WhatsappPhone.MatchKey(PhoneNormalized)` |
| `WhatsappId` | `whatsapp_id` | `varchar(80) NULL` | JID do WhatsApp (`...@c.us` / `...@lid`) |
| `Source` | `source` | `integer NOT NULL DEFAULT 1` | Enum `WhatsappContactSource`: `Manual = 1`, `Approval = 2` |
| `CreatedAt` | `created_at` | `timestamp without time zone` | UTC |
| `UpdatedAt` | `updated_at` | `timestamp without time zone` | UTC |

**Índices**
- `ix_avabot_whatsapp_contacts_agent_id` (`agent_id`)
- `avabot_whatsapp_contacts_agent_phone_key` UNIQUE (`agent_id`, `phone_match_key`) WHERE `phone_match_key IS NOT NULL` (FR-008)
- `avabot_whatsapp_contacts_agent_wid_key` UNIQUE (`agent_id`, `whatsapp_id`) WHERE `whatsapp_id IS NOT NULL`

**Validação** (Application, antes de gravar; violação → `InvalidOperationException` / `ValidationException`)
- `Name` não vazio após trim e com até 120 caracteres.
- Pelo menos um entre `PhoneNormalized` e `WhatsappId`. No cadastro manual, `Phone` é obrigatório e `Normalize` não pode ser `null` ("Telefone invalido").
- Duplicidade de `PhoneMatchKey` ou `WhatsappId` no mesmo agente → "Este telefone ja esta cadastrado neste agente".

**Backfill de identificador**: quando o webhook encontra o contato pelo telefone e `WhatsappId` está NULL, ou é diferente e chegou como `@lid`, o service grava o JID recebido. Assim, uma mensagem futura que chegue só com o LID é reconhecida mesmo sem resolver o telefone.

---

## 3. WhatsappAccessRequest (novo) — `avabot_whatsapp_access_requests`

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `WhatsappAccessRequestId` | `whatsapp_access_request_id` | `bigint identity` | PK `avabot_whatsapp_access_requests_pkey` |
| `AgentId` | `agent_id` | `bigint NOT NULL` | FK `avabot_fk_agents_whatsapp_access_requests`, `ClientSetNull` |
| `WhatsappId` | `whatsapp_id` | `varchar(80) NOT NULL` | JID do remetente |
| `PhoneNormalized` | `phone_normalized` | `varchar(15) NULL` | Se conhecido (JID `@c.us` ou LID resolvido) |
| `PhoneMatchKey` | `phone_match_key` | `varchar(15) NULL` | Para o FR-026 |
| `ProfileName` | `profile_name` | `varchar(120) NULL` | Nome de perfil do WhatsApp |
| `Status` | `status` | `integer NOT NULL DEFAULT 1` | Enum `WhatsappAccessRequestStatus`: `Pending = 1`, `Approved = 2`, `Rejected = 3` |
| `RequestedAt` | `requested_at` | `timestamp without time zone NOT NULL` | UTC |
| `DecidedAt` | `decided_at` | `timestamp without time zone NULL` | Preenchido ao aprovar ou rejeitar |
| `DecidedByUserId` | `decided_by_user_id` | `bigint NULL` | FK `avabot_fk_users_whatsapp_access_requests`, `ClientSetNull` |
| `WhatsappContactId` | `whatsapp_contact_id` | `bigint NULL` | FK `avabot_fk_whatsapp_contacts_access_requests`, `ClientSetNull`; contato gerado ou vinculado |

**Índices**
- `ix_avabot_whatsapp_access_requests_agent_status` (`agent_id`, `status`)
- `avabot_whatsapp_access_requests_pending_key` UNIQUE (`agent_id`, `whatsapp_id`) WHERE `status = 1` (FR-022)

**Transições de estado**

```text
             "sim" no WhatsApp
 (nenhum) ───────────────────────▶ Pending
                                    │  │
             aprovar (painel/MCP)   │  │  rejeitar (painel/MCP)
                       ┌────────────┘  └────────────┐
                       ▼                            ▼
                   Approved ◀──── aprovar ────── Rejected
                       ▲
 cadastro manual com o mesmo telefone (FR-026) ── Pending
```

- Aprovar `Pending` ou `Rejected`: cria o `WhatsappContact` (`Source = Approval`, com `Name` informado, ou `ProfileName`, ou o telefone, ou o JID, nessa ordem; mais `WhatsappId`, `PhoneNormalized` e `PhoneMatchKey`). Se já existir um contato com o mesmo `PhoneMatchKey` ou `WhatsappId`, vincula esse contato em vez de criar outro. Grava `DecidedAt`, `DecidedByUserId` e `WhatsappContactId`.
- Rejeitar só vale para `Pending`. Rejeitar outro status → `InvalidOperationException` "Solicitacao nao esta pendente".
- `Approved` é final.
- Remover o contato não altera o pedido aprovado. O remetente volta a ser "sem pedido ativo" e recebe a oferta (US4 cenário 3).

**Pedido vigente de um remetente**: o mais recente por `RequestedAt` entre os pedidos do agente com o mesmo `WhatsappId`, ou com o mesmo `PhoneMatchKey` quando ele é conhecido. Só `Pending` e `Rejected` afetam o fluxo; `Approved` sem contato equivale a "nenhum".

---

## 4. WhatsappSenderState (novo) — `avabot_whatsapp_sender_states`

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| `WhatsappSenderStateId` | `whatsapp_sender_state_id` | `bigint identity` | PK `avabot_whatsapp_sender_states_pkey` |
| `AgentId` | `agent_id` | `bigint NOT NULL` | FK `avabot_fk_agents_whatsapp_sender_states`, `ClientSetNull` |
| `WhatsappId` | `whatsapp_id` | `varchar(80) NOT NULL` | JID do remetente |
| `LastAutoReplyAt` | `last_auto_reply_at` | `timestamp without time zone NULL` | Última frase automática enviada |
| `OfferSentAt` | `offer_sent_at` | `timestamp without time zone NULL` | Oferta aguardando "sim"/"não" (24 h) |
| `UpdatedAt` | `updated_at` | `timestamp without time zone NOT NULL` | UTC |

**Índice**: `avabot_whatsapp_sender_states_agent_wid_key` UNIQUE (`agent_id`, `whatsapp_id`).

Esta tabela não aparece no painel nem no MCP. As linhas só são removidas junto com o agente (o volume é baixo; não há limpeza periódica nesta versão).

---

## 5. Tipos de domínio auxiliares (sem tabela)

| Tipo | Local | Conteúdo |
|---|---|---|
| `WhatsappAccessMode`, `WhatsappContactSource`, `WhatsappAccessRequestStatus`, `WhatsappAccessMessage` | `AvaBot.Domain/Enums/` (ou junto aos models, conforme a skill) | Enums acima; `WhatsappAccessMessage { Offer, Confirmation, Waiting, Declined }` |
| `WhatsappPhone` | `AvaBot.Domain/Models/WhatsappPhone.cs` | `Normalize`, `MatchKey`, `FromJid` (dígitos de `...@c.us`, `null` para `@lid`/`@g.us`) |
| `WhatsappReplyParser` | `AvaBot.Domain/Models/WhatsappReplyParser.cs` | `Parse(text) → Yes / No / None` (research R5) |
| `WhatsappSender` | `AvaBot.Application/Services/WhatsappSender.cs` | `WhatsappId`, `Phone`, `ProfileName`, `IsGroup`; `FromPayload(JsonElement)` (research R2) |
| `WhatsappAccessDecision` | `AvaBot.Application/Services/WhatsappAccessDecision.cs` | `Allow`, `Reply(string text)`, `Ignore(string reason)` |

---

## 6. Repositórios (Infra.Interfaces → Infra)

```csharp
public interface IWhatsappContactRepository<T> where T : class
{
    Task<List<T>> GetAllByAgentAsync(long agentId);
    Task<T?> GetByIdAsync(long id, long agentId);
    Task<T?> FindBySenderAsync(long agentId, string? phoneMatchKey, string whatsappId);
    Task<bool> ExistsAsync(long agentId, string? phoneMatchKey, string? whatsappId, long? excludeId = null);
    Task<T> CreateAsync(T contact);
    Task<T> UpdateAsync(T contact);
    Task DeleteAsync(T contact);
}

public interface IWhatsappAccessRequestRepository<T> where T : class
{
    Task<List<T>> GetByAgentAsync(long agentId, int? status);
    Task<int> CountPendingAsync(long agentId);
    Task<T?> GetByIdAsync(long id, long agentId);
    Task<T?> GetCurrentForSenderAsync(long agentId, string whatsappId, string? phoneMatchKey);
    Task<T?> GetPendingByPhoneAsync(long agentId, string phoneMatchKey);
    Task<T> CreateAsync(T request);
    Task<T> UpdateAsync(T request);
}

public interface IWhatsappSenderStateRepository<T> where T : class
{
    Task<T?> GetAsync(long agentId, string whatsappId);
    Task<T> UpsertAsync(T state);
}
```

O `AgentRepository.DeleteAsync` ganha a remoção de `WhatsappSenderStates`, depois `WhatsappAccessRequests` e por fim `WhatsappContacts` do agente, antes de `_context.Agents.Remove(agent)` (research R10).
