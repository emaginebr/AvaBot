# Contrato: isolamento por dono nas rotas do painel

**Feature**: 016-user-accounts-auth | FR-009, FR-010, FR-011, FR-012

Regra única: toda rota `[Authorize]` que recebe um agente (por `id` ou `slug`) só enxerga agentes cujo `owner_user_id` é o `sub` do token. Agente de outro usuário responde **exatamente** como agente inexistente: `404` com `{ "sucesso": false, "mensagem": "Agente nao encontrado" }` (ou `"Arquivo nao encontrado"` / `"Sessao nao encontrada"` quando a rota já usa essas mensagens). Nenhum corpo, header ou tempo de resposta diferente.

Rotas públicas (`[AllowAnonymous]`) não mudam: continuam resolvendo o agente por slug sem dono.

## Rotas autenticadas afetadas

| Rota | Hoje | Depois |
|---|---|---|
| `GET /agents` | lista todos | lista só os do usuário (`GetAllByOwnerAsync`) |
| `POST /agents` | cria sem dono | grava `OwnerUserId` = usuário do token |
| `PUT /agents/{id}` | qualquer agente | só do usuário; senão `404` |
| `DELETE /agents/{id}` | idem | idem |
| `PATCH /agents/{id}/status` | idem | idem |
| `GET /agents/{id}/search` | idem | idem (valida antes de chamar `SearchService`) |
| `POST /agents/{id}/test` | já valida existência | valida dono |
| `POST /agents/{id}/openai/diagnose` | idem | idem |
| `GET /sessions/agents/{agentId}` | lista por agentId | valida dono do agente antes; senão `404 "Agente nao encontrado"` |
| `GET /sessions/{sessionId}/messages` | lista por sessão | carrega a sessão, confere o dono do agente dela; senão `404 "Sessao nao encontrada"` |
| `GET/POST /files/{agentId}` | por agentId | valida dono do agente antes; senão `404 "Agente nao encontrado"` |
| `DELETE /files/{agentId}/{fileId}`, `POST .../reprocess` | confere `file.AgentId == agentId` | além disso valida dono do agente |
| `powerbi/{slug}/*` (todas as 13 rotas) | `GetAgentBySlugOrThrowAsync(slug)` | `GetBySlugAsync(slug, ownerUserId)`; `KeyNotFoundException` → `404` já tratado em `Failure()` |
| `POST /telegram/{id}/setup-webhook`, `GET .../webhook-info`, `POST .../regenerate-secret` | `GetByIdAsync(id)` | `GetByIdAsync(id, ownerUserId)`; `KeyNotFoundException` → `404` já tratado |
| `POST /whatsapp/{slug}/start-session`, `GET .../qrcode`, `GET .../status`, `POST .../disconnect` | `GetBySlugAsync(slug)` | `GetOwnedBySlugAsync(slug, ownerUserId)`; `KeyNotFoundException` → `404` já tratado |

## Rotas públicas inalteradas (FR-012)

`GET /agents/{slug}`, `GET /agents/{slug}/chat-config`, `POST /sessions/agents/{slug}`, `GET /sessions/resume/{slug}`, `POST /telegram/{slug}/webhook`, `POST /whatsapp/{slug}/webhook`, `GET /ws/chat/{slug}` (WebSocket), página inicial e widget.

## Assinaturas na camada Application

```csharp
// IAgentRepository<T>
Task<List<T>> GetAllByOwnerAsync(long ownerUserId);
Task<T?> GetByIdAsync(long id, long ownerUserId);
Task<T?> GetBySlugAsync(string slug, long ownerUserId);

// AgentService
Task<List<Agent>> GetAllAsync(long ownerUserId);
Task<Agent?> GetOwnedByIdAsync(long id, long ownerUserId);
Task<Agent?> GetOwnedBySlugAsync(string slug, long ownerUserId);
Task<Agent> CreateAsync(AgentInsertInfo info, long ownerUserId);
Task<Agent?> UpdateAsync(long id, AgentInsertInfo info, long ownerUserId);
Task<bool> DeleteAsync(long id, long ownerUserId);
Task<Agent?> ToggleStatusAsync(long id, long ownerUserId);
Task<string> GetOpenAIApiKeyAsync(long agentId, long ownerUserId);
// GetBySlugAsync(slug) e GetByIdAsync(id) continuam para uso público/interno (chat, webhooks, ChatService).

// PowerBIService: todo método público ganha `long ownerUserId` como primeiro parâmetro depois do slug.
// TelegramService: SetupWebhookAsync(id, ownerUserId), GetWebhookInfoAsync(id, ownerUserId), RegenerateWebhookSecretAsync(id, ownerUserId).
// WhatsappService: StartSessionAsync(slug, ownerUserId), GetQrCodeAsync(slug, ownerUserId), GetStatusAsync(slug, ownerUserId), DisconnectAsync(slug, ownerUserId).
// ProcessWebhookAsync e ProcessUpdateAsync (públicos) não mudam.
```

## Leitura do usuário no controller

```csharp
// AvaBot.API/Auth/ClaimsPrincipalExtensions.cs
public static long GetUserId(this ClaimsPrincipal user)
    => long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("Token sem identificador de usuario"));
```

Um token antigo (sem `sub`, emitido antes desta versão) cai nesse `throw` e o controller devolve `401` pelo `catch` padrão. Na prática, tokens antigos expiram em 8 h.
