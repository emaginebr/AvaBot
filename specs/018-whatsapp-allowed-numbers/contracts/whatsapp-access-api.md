# Contract: API REST de controle de acesso do WhatsApp

**Controller**: `AvaBot.API/Controllers/WhatsappAccessController.cs` (novo)
**Service**: `AvaBot.Application/Services/WhatsappAccessService.cs` (novo)
**Autenticação**: `[Authorize]` (JWT do painel) em todas as rotas.
**Agente**: resolvido por `AgentService.GetOwnedBySlugAsync(slug, User.GetUserId())`. Se não existir ou for de outra conta → `404 "Agente nao encontrado"`. **Não** exige sessão de WhatsApp iniciada.

Envelope padrão `Result<T>` (`sucesso`, `mensagem`, `erros`, `dados`). Mapeamento de erros, igual ao `WhatsappController`:

| Exceção | HTTP | Mensagem |
|---|---|---|
| `KeyNotFoundException` | 404 | Mensagem da exceção ("Agente nao encontrado", "Contato nao encontrado", "Solicitacao nao encontrada") |
| `ValidationException` (FluentValidation) | 400 | `erros` com as mensagens por campo |
| `InvalidOperationException` | 400 | Mensagem da exceção |
| `Exception` | 500 | `"Erro ao ...: {ex.Message}"` |

---

## DTOs (`AvaBot.DTO/WhatsappAccessDTOs.cs`, todas as propriedades com `[JsonPropertyName]`)

```csharp
public class WhatsappAccessConfigInfo
{
    [JsonPropertyName("mode")] public int Mode { get; set; }                       // 1 = Todos, 2 = Somente contatos autorizados
    [JsonPropertyName("offerMessage")] public WhatsappAccessMessageInfo OfferMessage { get; set; } = new();
    [JsonPropertyName("confirmationMessage")] public WhatsappAccessMessageInfo ConfirmationMessage { get; set; } = new();
    [JsonPropertyName("waitingMessage")] public WhatsappAccessMessageInfo WaitingMessage { get; set; } = new();
    [JsonPropertyName("declinedMessage")] public WhatsappAccessMessageInfo DeclinedMessage { get; set; } = new();
    [JsonPropertyName("contactCount")] public int ContactCount { get; set; }
    [JsonPropertyName("pendingRequestCount")] public int PendingRequestCount { get; set; }
}

public class WhatsappAccessMessageInfo
{
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;   // texto efetivo (personalizado ou padrão)
    [JsonPropertyName("isDefault")] public bool IsDefault { get; set; }
    [JsonPropertyName("defaultText")] public string DefaultText { get; set; } = string.Empty;
}

public class WhatsappAccessConfigUpdateInfo
{
    [JsonPropertyName("mode")] public int Mode { get; set; }
    // null = restaurar o texto padrão; string vazia/só espaços = erro de validação
    [JsonPropertyName("offerMessage")] public string? OfferMessage { get; set; }
    [JsonPropertyName("confirmationMessage")] public string? ConfirmationMessage { get; set; }
    [JsonPropertyName("waitingMessage")] public string? WaitingMessage { get; set; }
    [JsonPropertyName("declinedMessage")] public string? DeclinedMessage { get; set; }
}

public class WhatsappContactInfo
{
    [JsonPropertyName("whatsappContactId")] public long WhatsappContactId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("phone")] public string? Phone { get; set; }               // como digitado; null se veio de aprovação só com LID
    [JsonPropertyName("phoneNormalized")] public string? PhoneNormalized { get; set; }
    [JsonPropertyName("whatsappId")] public string? WhatsappId { get; set; }
    [JsonPropertyName("source")] public int Source { get; set; }                 // 1 = manual, 2 = aprovação
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public DateTime UpdatedAt { get; set; }
}

public class WhatsappContactInsertInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("phone")] public string Phone { get; set; } = string.Empty;
}

public class WhatsappAccessRequestInfo
{
    [JsonPropertyName("whatsappAccessRequestId")] public long WhatsappAccessRequestId { get; set; }
    [JsonPropertyName("whatsappId")] public string WhatsappId { get; set; } = string.Empty;
    [JsonPropertyName("phone")] public string? Phone { get; set; }               // PhoneNormalized
    [JsonPropertyName("profileName")] public string? ProfileName { get; set; }
    [JsonPropertyName("status")] public int Status { get; set; }                 // 1 pendente, 2 aprovada, 3 rejeitada
    [JsonPropertyName("requestedAt")] public DateTime RequestedAt { get; set; }
    [JsonPropertyName("decidedAt")] public DateTime? DecidedAt { get; set; }
    [JsonPropertyName("whatsappContactId")] public long? WhatsappContactId { get; set; }
}

public class WhatsappAccessRequestApproveInfo
{
    [JsonPropertyName("name")] public string? Name { get; set; }                 // opcional; default: profileName → phone → whatsappId
}
```

**Validators** (`AvaBot.API/Validators/`, FluentValidation):
- `WhatsappAccessConfigUpdateInfoValidator`: `Mode` em {1, 2}. Cada mensagem pode ser `null` ou ter de 1 a 500 caracteres após trim ("A frase nao pode ficar vazia").
- `WhatsappContactInsertInfoValidator`: `Name` obrigatório, até 120 caracteres. `Phone` obrigatório, e `WhatsappPhone.Normalize(Phone) != null` ("Telefone invalido. Informe DDD e numero, com ou sem codigo do pais").
- `WhatsappAccessRequestApproveInfoValidator`: `Name` até 120 caracteres quando informado.

---

## Rotas

### Configuração

| Método | Rota | Corpo | Resposta 200 (`dados`) | Mensagem |
|---|---|---|---|---|
| GET | `/whatsapp/{slug}/access` | — | `WhatsappAccessConfigInfo` | "Configuracao obtida com sucesso" |
| PUT | `/whatsapp/{slug}/access` | `WhatsappAccessConfigUpdateInfo` | `WhatsappAccessConfigInfo` | "Configuracao salva com sucesso" |

O PUT grava o modo e as 4 frases de uma vez. `null` restaura o padrão.

### Contatos autorizados

| Método | Rota | Corpo | Resposta 200 (`dados`) |
|---|---|---|---|
| GET | `/whatsapp/{slug}/contacts` | — | `List<WhatsappContactInfo>` ordenada por `Name` |
| POST | `/whatsapp/{slug}/contacts` | `WhatsappContactInsertInfo` | `WhatsappContactInfo` |
| PUT | `/whatsapp/{slug}/contacts/{contactId}` | `WhatsappContactInsertInfo` | `WhatsappContactInfo` |
| DELETE | `/whatsapp/{slug}/contacts/{contactId}` | — | `true` |

- Telefone duplicado no agente (comparação pela chave do nono dígito) → 400 "Este telefone ja esta cadastrado neste agente".
- POST e PUT com telefone igual ao de um pedido pendente → o pedido fica aprovado e vinculado ao contato (FR-026).
- PUT que troca o telefone limpa o `whatsappId` quando ele era `@c.us` de outro número (`FromJid` diferente da nova chave). Um `whatsappId` `@lid` é mantido.
- Contato de outro agente ou inexistente → 404 "Contato nao encontrado".

### Solicitações de acesso

| Método | Rota | Corpo | Resposta 200 (`dados`) |
|---|---|---|---|
| GET | `/whatsapp/{slug}/access-requests?status={1\|2\|3}` | — | `List<WhatsappAccessRequestInfo>`, mais recentes primeiro; sem `status` = todas |
| POST | `/whatsapp/{slug}/access-requests/{requestId}/approve` | `WhatsappAccessRequestApproveInfo` | `WhatsappContactInfo` (contato criado ou vinculado) |
| POST | `/whatsapp/{slug}/access-requests/{requestId}/reject` | — | `WhatsappAccessRequestInfo` |

- Aprovar um pedido `Approved` → 400 "Solicitacao ja aprovada".
- Rejeitar um pedido que não está `Pending` → 400 "Solicitacao nao esta pendente".

---

## Service (assinaturas)

```csharp
public class WhatsappAccessService
{
    // Painel e MCP: recebem o Agent ja resolvido e validado como do usuario pelo chamador
    Task<WhatsappAccessConfigInfo> GetConfigAsync(Agent agent);
    Task<WhatsappAccessConfigInfo> UpdateConfigAsync(Agent agent, WhatsappAccessConfigUpdateInfo info);
    Task<List<WhatsappContactInfo>> ListContactsAsync(Agent agent);
    Task<WhatsappContactInfo> AddContactAsync(Agent agent, WhatsappContactInsertInfo info);
    Task<WhatsappContactInfo> UpdateContactAsync(Agent agent, long contactId, WhatsappContactInsertInfo info);
    Task RemoveContactAsync(Agent agent, long contactId);
    Task<List<WhatsappAccessRequestInfo>> ListRequestsAsync(Agent agent, int? status);
    Task<WhatsappContactInfo> ApproveRequestAsync(Agent agent, long requestId, string? name, long decidedByUserId);
    Task<WhatsappAccessRequestInfo> RejectRequestAsync(Agent agent, long requestId, long decidedByUserId);

    // Webhook (ver whatsapp-webhook-flow.md)
    Task<WhatsappAccessDecision> EvaluateIncomingAsync(Agent agent, WhatsappSender sender, string? text);
}
```

O controller resolve o `Agent` dono pelo slug, e a ferramenta MCP resolve pelo id (`GetOwnedByIdAsync`). Ambos chamam os mesmos métodos.

---

## Frontend

**`frontend/src/types/whatsappAccess.ts`**: interfaces espelhando os DTOs (`WhatsappAccessConfigInfo`, `WhatsappAccessMessageInfo`, `WhatsappAccessConfigUpdateInfo`, `WhatsappContactInfo`, `WhatsappContactInsertInfo`, `WhatsappAccessRequestInfo`) e as constantes `WHATSAPP_ACCESS_MODE = { ALL: 1, RESTRICTED: 2 }` e `WHATSAPP_REQUEST_STATUS = { PENDING: 1, APPROVED: 2, REJECTED: 3 }`.

**`frontend/src/Services/WhatsappAccessService.ts`**: `getConfig(slug)`, `updateConfig(slug, info)`, `listContacts(slug)`, `addContact(slug, info)`, `updateContact(slug, id, info)`, `removeContact(slug, id)`, `listRequests(slug, status?)`, `approveRequest(slug, id, name?)`, `rejectRequest(slug, id)`. Usa os mesmos `getApiUrl`, cabeçalhos de autenticação e `handleResponse` do `AgentService.ts`.

**`frontend/src/stores/useWhatsappAccessStore.ts`** (Zustand): estado `config`, `contacts`, `requests`, `requestFilter`, `loading` e `error`. As ações chamam o service e recarregam `config` (contadores) após mutações. `reset()` é chamado ao trocar de agente.

**Componentes** (`frontend/src/components/admin/whatsapp/`):
- `WhatsappAccessSettings.tsx`: rádio "Todos" / "Somente contatos autorizados", 4 `textarea` (máx. 500, contador) com "Restaurar padrão" e botão Salvar. Se o modo for restrito e `contactCount === 0`, mostra o aviso "Nenhum contato está autorizado; todos receberão apenas as frases de solicitação" (FR-012).
- `WhatsappAccessRequestList.tsx`: abas Pendentes (com contador), Aprovadas e Rejeitadas. Colunas: nome de perfil, telefone (ou "Não identificado"), data, ações. "Aprovar" abre um modal com o nome preenchido (`profileName` → telefone) e "Rejeitar" pede confirmação.
- `WhatsappContactList.tsx`: tabela (nome, telefone, origem), "Adicionar contato", editar em modal e remover com confirmação.
- `WhatsappPage.tsx`: as três seções abaixo do bloco de conexão, com `useEffect` que carrega config, pedidos pendentes e contatos ao mudar o `selectedAgent`.

## Bruno

`bruno/WhatsApp/`: `Access Get.bru`, `Access Update.bru`, `Contacts List.bru`, `Contact Add.bru`, `Contact Update.bru`, `Contact Delete.bru`, `Access Requests List.bru`, `Access Request Approve.bru`, `Access Request Reject.bru`.
