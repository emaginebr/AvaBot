# Contract: fluxo do webhook com controle de acesso

**Endpoint** (sem mudança de contrato): `POST /whatsapp/{slug}/webhook`, `[AllowAnonymous]`, sempre `200 OK`.
**Mudança**: comportamento interno de `WhatsappService.ProcessWebhookAsync` e novo `WhatsappAccessService.EvaluateIncomingAsync`.

---

## 1. Nova ordem do `ProcessWebhookAsync`

```text
1. event != "onmessage"                         → return                       (igual)
2. timestamp < início da app                    → return                       (igual)
3. extrai isGroup, type, from, body             (igual)
   + sender = WhatsappSender.FromPayload(payload)   (novo: whatsappId = isGroup ? author : from; profileName)
4. grupo sem menção ao bot                      → return                       (igual)
5. agent = GetBySlugAsync(slug); inválido/inativo/sem token → return          (MOVIDO para antes da checagem de tipo)
6. se agent.WhatsappAccessMode == Restricted:                                  (NOVO)
     se sender.Phone == null e sender.WhatsappId termina em "@lid":
         sender.Phone = await wpp.GetPhoneByLidAsync(slug, sender.WhatsappId)  (cache, R3)
     decision = await access.EvaluateIncomingAsync(agent, sender, type == "chat" ? body : null)
     Ignore         → log + return
     Reply(texto)   → SendMessageAsync(slug, replyTarget, texto) (erro de envio só logado) + return
     Allow          → segue
7. type != "chat"                               → mensagem de rejeição de tipo + return   (igual)
8. from/body vazios                             → return                       (igual)
9. status da sessão, CreateSessionAsync, ChatService, SendMessageAsync         (igual)
```

`replyTarget` é o mesmo destino que o código atual usa para responder: `from` sem `@c.us` em conversa privada e, para `@lid`, o próprio JID. Em grupo nunca há `Reply` (só `Ignore`), conforme o FR-012.

No modo `All`, os passos 1–5 e 7–9 se comportam exatamente como hoje (SC-007). A única diferença é a ordem 5 ↔ 7, que não muda o resultado: um agente inválido já descartava a mensagem.

---

## 2. `EvaluateIncomingAsync(agent, sender, text)`

Entrada: agente em modo `Restricted`, `sender { WhatsappId, Phone?, ProfileName?, IsGroup }` e `text` (null para tipos não suportados). `now = DateTime.UtcNow`.

```text
A. key = sender.Phone != null ? WhatsappPhone.MatchKey(sender.Phone) : null
B. contact = contacts.FindBySenderAsync(agent, key, sender.WhatsappId)
   contato encontrado:
       backfill de WhatsappId se necessário (data-model §2)
       → Allow
C. sender.IsGroup → Ignore("grupo-nao-autorizado")
D. request = requests.GetCurrentForSenderAsync(agent, sender.WhatsappId, key)
   request.Status == Rejected → Ignore("rejeitado")
   request.Status == Pending  → Limited(Waiting)
E. state = states.GetAsync(agent, sender.WhatsappId) ?? novo
   offerOpen = state.OfferSentAt != null && now - state.OfferSentAt <= 24h
   reply = offerOpen ? WhatsappReplyParser.Parse(text) : None
   reply == Yes:
       requests.CreateAsync(Pending, WhatsappId, Phone, MatchKey, ProfileName, RequestedAt = now)
       state.OfferSentAt = null; state.LastAutoReplyAt = now; upsert
       → Reply(agent.GetWhatsappMessage(Confirmation))
   reply == No:
       state.OfferSentAt = null; state.LastAutoReplyAt = now; upsert
       → Reply(agent.GetWhatsappMessage(Declined))
   senão → Limited(Offer)

Limited(kind):
   state.LastAutoReplyAt != null && now - state.LastAutoReplyAt < 1h → Ignore("limite-1h")
   state.LastAutoReplyAt = now
   kind == Offer → state.OfferSentAt = now
   upsert → Reply(agent.GetWhatsappMessage(kind))
```

**Concorrência**: dois "sim" simultâneos podem tentar criar dois pedidos. O índice único parcial `(agent_id, whatsapp_id) WHERE status = 1` barra o segundo, e o service captura a `DbUpdateException` de unicidade e devolve `Ignore("pedido-duplicado")`. O upsert do estado também captura a violação de unicidade e relê.

**Log** (FR-013), uma linha `Information` por decisão diferente de `Allow`:
`[WhatsApp Acesso] agente={Slug} remetente={WhatsappId} telefone={Phone} etapa={Offer|Confirmation|Waiting|Declined|Ignore:<motivo>}`

---

## 3. Tabela de verdade (casos de teste do `WhatsappAccessServiceTest`)

| # | Contato | Pedido | Oferta aberta | Último aviso | Texto | Resultado |
|---|---|---|---|---|---|---|
| 1 | sim (por telefone) | — | — | — | qualquer | Allow |
| 2 | sim (por `@lid`) | — | — | — | qualquer | Allow |
| 3 | sim (sem 9 vs com 9) | — | — | — | qualquer | Allow |
| 4 | não, grupo | — | — | — | qualquer | Ignore |
| 5 | não | — | não | nunca | "oi" | Reply(Oferta) |
| 6 | não | — | não | há 10 min | "oi" | Ignore (limite) |
| 7 | não | — | sim (há 5 min) | há 5 min | "Sim!" | Reply(Confirmação) + pedido criado |
| 8 | não | — | sim (há 5 min) | há 5 min | "não" | Reply(Recusa), sem pedido |
| 9 | não | — | sim (há 5 min) | há 5 min | "talvez" | Ignore (limite) |
| 10 | não | — | sim (há 25 h) | há 25 h | "sim" | Reply(Oferta) (janela expirada) |
| 11 | não | Pending | — | há 2 h | "oi" | Reply(Aguardando) |
| 12 | não | Pending | — | há 10 min | "sim" | Ignore (limite), sem pedido novo |
| 13 | não | Rejected | — | — | "oi" | Ignore |
| 14 | não | Approved (contato removido) | não | há 2 h | "oi" | Reply(Oferta) |
| 15 | não | — | não | nunca | null (áudio) | Reply(Oferta) |
| 16 | frases personalizadas | — | não | nunca | "oi" | Reply(texto personalizado) |
