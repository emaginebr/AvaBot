# Research: Lista de Números Autorizados e Solicitação de Acesso no WhatsApp

**Feature**: `018-whatsapp-allowed-numbers` | **Date**: 2026-11-23

Nenhum item do Technical Context ficou como NEEDS CLARIFICATION. As decisões abaixo resolvem as dúvidas de projeto levantadas pela spec.

---

## R1. Onde o controle de acesso entra no webhook

**Decision**: em `WhatsappService.ProcessWebhookAsync`, o controle entra **depois** do filtro de grupo/menção e **antes** da rejeição de tipo não suportado, da criação de sessão e de qualquer chamada ao `ChatService`. A carga do agente (`GetBySlugAsync`) sobe para antes da checagem de tipo, porque o modo de acesso está no agente. A decisão fica num service novo, `WhatsappAccessService.EvaluateIncomingAsync(agent, sender, text)`, que devolve `Allow`, `Reply(texto)` ou `Ignore`. O `WhatsappService` só executa a decisão.

**Rationale**: atende ao FR-010 e ao SC-001 (zero IA para não autorizados) e ao edge case "tipo não suportado de não autorizado segue o fluxo de solicitação". Com a regra num service separado, ela pode ser testada com mocks sem montar payload do WPP Connect.

**Alternatives considered**: um middleware/filtro no controller, que teria de duplicar o parsing do payload e a resolução do agente; e checar só dentro do `ChatService`, o que deixaria a sessão ser criada e misturaria canais (o FR-030 pede que só o WhatsApp seja afetado).

## R2. Identificação do remetente (privado, grupo, LID)

**Decision**: um parser estático `WhatsappSender.FromPayload(JsonElement)` extrai:
- `WhatsappId`: o JID do remetente. Em conversa privada é `from` (`5511...@c.us` ou `...@lid`). Em grupo é `author`, o participante, e não `from`, que é o id do grupo.
- `Phone`: os dígitos do JID quando ele termina em `@c.us`. Quando termina em `@lid`, o valor vem da resolução da R3.
- `ProfileName`: `sender.pushname` → `notifyName` → `sender.formattedName` (o primeiro não vazio, cortado em 120 caracteres).

**Rationale**: o WPP Connect repassa o objeto da mensagem do WA-JS (`Object.assign({event, session}, message)`), em que `author` identifica o participante em grupos e `sender` traz o nome de perfil. O código atual usa `from` em grupos para endereçar a resposta, o que continua igual. A verificação de acesso usa `author` (FR-012).

**Alternatives considered**: usar `sender.id` como fonte única, descartado porque em algumas versões ele vem como objeto `Wid` e não como string; `from`/`author` são strings estáveis.

## R3. Resolução de LID para telefone

**Decision**: um método novo `IWppConnectService.GetPhoneByLidAsync(session, lid)` chama `GET /api/{session}/contact/pn-lid/{lid}` do WPP Connect Server, que devolve `{ lid, phoneNumber }`. O sucesso fica em `IMemoryCache` por 24 h, chave `wpp:pnlid:{session}:{lid}`. Falha ou ausência é cacheada por 10 min como "não resolvido". Qualquer erro devolve `null`, nunca uma exceção. Em seguida o sistema tenta o contato por telefone e depois por `WhatsappId`, e se não achar o remetente segue o fluxo de solicitação (FR-011).

**Rationale**: a imagem `wppconnect/server-cli:latest` com `@wppconnect-team/wppconnect` 2.3.1 expõe a rota `pn-lid`, que usa `WPP.contact.getPnLidEntry`. O cache evita uma chamada HTTP por mensagem. A spec já aceita a falha: a aprovação autoriza o próprio identificador (`whatsapp_id`), então a resolução é só uma otimização para contatos cadastrados à mão.

**Verificação obrigatória**: o quickstart (passo 6) confirma a rota com uma sessão real. Se ela não existir na imagem em produção, `GetPhoneByLidAsync` devolve `null` (404 tratado) e a funcionalidade continua correta, só que dependendo da aprovação por identificador.

**Alternatives considered**: `GET /api/{session}/contact/{id}`, que nem sempre traz o telefone de um LID, e não resolver o LID, o que faria contatos manuais nunca serem reconhecidos quando chegam por LID.

## R4. Normalização de telefone e equivalência do nono dígito

**Decision**: duas funções puras em `AvaBot.Domain/Models/WhatsappPhone.cs`:
- `Normalize(input)`: mantém só os dígitos e remove o prefixo `00`. Se o texto original não começava com `+`, tem 10 ou 11 dígitos e não começa com `55`, prefixa `55`. É válido com 10 a 15 dígitos; caso contrário, `null` (FR-007).
- `MatchKey(normalized)`: se começa com `55`, tem 13 dígitos e o quinto dígito é `9` (celular com nono dígito), remove esse `9`. Senão, devolve o próprio valor.

A unicidade (FR-008) e a busca no webhook usam a coluna `phone_match_key`.

**Rationale**: o WhatsApp entrega JIDs brasileiros antigos sem o 9 (`551187654321@c.us`), enquanto o usuário cadastra com o 9. Uma chave canônica resolve a comparação nos dois sentidos com um índice simples, sem tentar duas buscas.

**Alternatives considered**: `libphonenumber-csharp`, que seria uma dependência nova só para isto e não trata a regra do 9 do WhatsApp; e guardar as duas variantes, que dobraria as linhas e complicaria a unicidade.

## R5. Reconhecimento de "sim"/"não" sem IA

**Decision**: `WhatsappReplyParser.Parse(text)` faz trim e lowercase, remove os acentos (decomposição Unicode FormD + remoção de `NonSpacingMark`) e remove tudo que não é letra. Retorna `Yes` para `sim` ou `s`, `No` para `nao` ou `n` e `None` para o resto.

**Rationale**: atende ao FR-016 de forma determinística, sem custo de IA (SC-001). "Sim!", "SIM", " sim. " e "Não" são reconhecidos, e "sim, quero" não é, para evitar falso positivo.

**Alternatives considered**: aceitar frases ("quero", "pode ser"), que é ambíguo e o pedido do usuário foi "responda apenas sim ou não".

## R6. Estado do fluxo e limite de 1 frase por hora

**Decision**: tabela `avabot_whatsapp_sender_states` com uma linha por (agente, `whatsapp_id`), contendo `last_auto_reply_at` e `offer_sent_at`. É lida e gravada pelo `WhatsappAccessService`. Regras:
- Uma frase limitada (oferta ou aguardando) só é enviada se `last_auto_reply_at` for nulo ou tiver mais de 1 h. Ao enviar, grava `last_auto_reply_at = agora` e, no caso da oferta, `offer_sent_at = agora`.
- Confirmação e recusa respondem a "sim"/"não" válidos, isto é, com `offer_sent_at` nas últimas 24 h e sem pedido pendente ou rejeitado. São enviadas sem checar o limite, gravam `last_auto_reply_at = agora` e limpam `offer_sent_at`.

**Rationale**: o estado sobrevive a restart e deploy, o que mantém o SC-005 ("nunca mais de 1 por hora"), e funciona mesmo com mais de uma réplica da API. O volume é pequeno: uma linha por remetente não autorizado por agente.

**Alternatives considered**: `IMemoryCache`, mais simples, mas um restart dentro da janela reenviaria a oferta e perderia o "sim" pendente; e colunas no pedido de acesso, que não cobrem quem ainda não pediu nem quem disse "não".

## R7. Persistência das frases prontas e do modo

**Decision**: colunas novas em `avabot_agents`: `whatsapp_access_mode integer NOT NULL DEFAULT 1` (1 = Todos, 2 = Somente contatos autorizados) e `whatsapp_offer_message`, `whatsapp_confirmation_message`, `whatsapp_waiting_message` e `whatsapp_declined_message`, todas `varchar(500)` NULL. NULL significa "usar o texto padrão", que fica em `WhatsappAccessDefaults` no Domain. "Restaurar padrão" grava NULL, e a API rejeita string vazia (FR-027).

**Rationale**: o default da coluna cumpre o FR-002 e o SC-007 sem script de dados. Com NULL como padrão, um texto padrão melhorado no futuro chega a todos os agentes que não personalizaram.

**Alternatives considered**: uma tabela de frases, que é normalização desnecessária para 4 campos fixos 1:1 com o agente; e gravar o texto padrão na migração, que congela o padrão.

## R8. Endpoints REST e escopo de autorização

**Decision**: um controller novo, `WhatsappAccessController`, com rotas `whatsapp/{slug}/access`, `whatsapp/{slug}/contacts` e `whatsapp/{slug}/access-requests` (contrato em `contracts/whatsapp-access-api.md`), todas com `[Authorize]`. O dono é resolvido por `AgentService.GetOwnedBySlugAsync(slug, User.GetUserId())`, e um agente alheio responde como inexistente (404 "Agente nao encontrado"), como nas rotas atuais do WhatsApp.

Essas rotas **não** exigem sessão do WhatsApp iniciada: o `ResolveAgentAsync` do `WhatsappService` exige `WhatsappToken`, e o dono pode querer montar a lista antes de parear.

**Rationale**: segue o padrão das rotas por slug que o frontend já usa (`selectedAgent.slug`) e as regras de dono da feature 016 (FR-028).

**Alternatives considered**: estender o `WhatsappController`, que já tem 5 ações com tratamento repetitivo; um controller separado mantém o arquivo legível.

## R9. Ferramentas MCP

**Decision**: uma classe nova, `AvaBot.API/Mcp/Tools/WhatsappAccessTools.cs`, com 9 ferramentas por `agentId`, que chamam o mesmo `WhatsappAccessService` (contrato em `contracts/mcp-tools.md`). `remover_contato_whatsapp` e `rejeitar_solicitacao_whatsapp` levam `Destructive = true`. Nenhuma ferramenta expõe o `WhatsappToken`.

**Rationale**: atende ao FR-031 e reaproveita `McpCaller`, `McpToolResults.RunAsync` e os filtros da feature 017.

**Alternatives considered**: estender o `ChannelTools`, que ficaria grande e misturaria a conexão (somente leitura) com o controle de acesso (escrita).

## R10. Exclusão do agente

**Decision**: as FKs novas (`agent_id`) usam `ClientSetNull`, como as do Power BI, e o `AgentRepository.DeleteAsync` passa a remover explicitamente estados, pedidos e contatos do agente antes do `Remove(agent)` (FR-029).

**Rationale**: segue a constituição V (nunca `Cascade` em tabelas novas) e o padrão que o repositório já usa para os filhos do Power BI.

## R11. Frontend

**Decision**: a `WhatsappPage` ganha três seções abaixo da conexão, cada uma num componente em `components/admin/whatsapp/`:
1. `WhatsappAccessSettings`: modo e frases, com "Restaurar padrão" por frase e aviso de lista vazia (FR-012 / US5).
2. `WhatsappAccessRequestList`: abas Pendentes, Aprovadas e Rejeitadas, contador de pendentes e modal de aprovação com o nome editável.
3. `WhatsappContactList`: tabela e formulário em modal (`react-modal`), com confirmação na remoção.

Os dados vêm de `Services/WhatsappAccessService.ts` e `stores/useWhatsappAccessStore.ts` (Zustand, padrão vigente desde a feature 007), e os toasts usam sonner.

**Rationale**: o spec manda tudo para a área de WhatsApp já existente. Separar em componentes evita inflar a página, que já tem 277 linhas.
