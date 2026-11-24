# Configuracao do WhatsApp via WPP Connect

> Guia completo para configurar e integrar o WhatsApp com o AvaBot, por agente.

**Created:** 2026-04-12
**Last Updated:** 2026-04-12

---

## Pre-requisitos

1. **Docker e Docker Compose** instalados
2. **AvaBot API** rodando
3. **Agente cadastrado** no AvaBot com `WhatsappToken` configurado
4. **Celular com WhatsApp** para escanear o QR code

---

## 1. Subir o WPP Connect

O WPP Connect roda como container Docker junto com o AvaBot.

### Configurar o Secret Key

No arquivo `.env`, configure a chave secreta do WPP Connect:

```env
WPP_SECRET_KEY=sua_chave_secreta_aqui
```

### Subir os containers

```bash
docker compose up -d
```

O WPP Connect estara disponivel na porta 21465. O Swagger pode ser acessado em `http://localhost:21465/api-docs`.

---

## 2. Configurar o Agente

Ao criar ou atualizar um agente, inclua o campo `whatsappToken`:

```http
PUT /api/agents/{id}
Authorization: Bearer <seu-token-jwt>
Content-Type: application/json

{
  "name": "Assistente Vendas",
  "systemPrompt": "Voce e um assistente de vendas...",
  "chatModel": "gpt-4o",
  "whatsappToken": "assistente-vendas-wpp"
}
```

> O `whatsappToken` e o identificador da sessao no WPP Connect. Deve ser unico por agente.

---

## 3. Conectar ao WhatsApp

### Passo 1: Iniciar a sessao

```http
POST /whatsapp/{slug}/start-session
Authorization: Bearer <seu-token-jwt>
```

### Passo 2: Obter o QR Code

```http
GET /whatsapp/{slug}/qrcode
Authorization: Bearer <seu-token-jwt>
```

Resposta:
```json
{
  "sucesso": true,
  "mensagem": "QR code obtido com sucesso",
  "dados": {
    "agentSlug": "assistente-vendas",
    "qrCode": "data:image/png;base64,iVBORw0KGgo..."
  }
}
```

### Passo 3: Escanear o QR Code

1. Abra o WhatsApp no celular
2. Va em **Dispositivos conectados** > **Conectar dispositivo**
3. Escaneie o QR code retornado pela API

### Passo 4: Verificar a conexao

```http
GET /whatsapp/{slug}/status
Authorization: Bearer <seu-token-jwt>
```

---

## 4. Como Funciona

### Fluxo de Mensagens

```
Usuario WhatsApp  -->  WPP Connect  -->  POST /whatsapp/{slug}/webhook  -->  WhatsappService
                                                                                   |
                                                                             ChatService
                                                                                   |
                                                                             WPP Connect
                                                                             send-message
                                                                                   |
                                                                          Usuario WhatsApp
```

### Processamento

1. Usuario envia mensagem de texto no WhatsApp
2. WPP Connect recebe e encaminha para o webhook do AvaBot
3. WhatsappService resolve o agente pelo slug
4. Se o agente estiver no modo "Somente contatos autorizados", o controle de acesso decide antes da IA (ver "Controle de acesso")
5. ChatService processa a mensagem com o modelo de IA do agente
6. Resposta e enviada de volta pelo WPP Connect

> Apenas mensagens de texto em conversas privadas sao processadas. Mensagens de grupo, imagens, audios e outros tipos sao ignorados ou rejeitados com mensagem amigavel.

---

## 5. Endpoints

| Metodo | Rota | Auth | Descricao |
|--------|------|------|-----------|
| POST | `/whatsapp/{slug}/start-session` | Authorize | Inicia sessao no WPP Connect |
| GET | `/whatsapp/{slug}/qrcode` | Authorize | Obtem QR code em base64 |
| GET | `/whatsapp/{slug}/status` | Authorize | Verifica status da sessao |
| POST | `/whatsapp/{slug}/disconnect` | Authorize | Encerra a sessao |
| POST | `/whatsapp/{slug}/webhook` | AllowAnonymous | Recebe mensagens do WPP Connect |
| GET / PUT | `/whatsapp/{slug}/access` | Authorize | Modo de acesso e frases prontas |
| GET / POST | `/whatsapp/{slug}/contacts` | Authorize | Lista / adiciona contatos autorizados |
| PUT / DELETE | `/whatsapp/{slug}/contacts/{id}` | Authorize | Edita / remove contato |
| GET | `/whatsapp/{slug}/access-requests?status=` | Authorize | Pedidos de acesso (1 pendente, 2 aprovada, 3 rejeitada) |
| POST | `/whatsapp/{slug}/access-requests/{id}/approve` | Authorize | Aprova o pedido e cria o contato |
| POST | `/whatsapp/{slug}/access-requests/{id}/reject` | Authorize | Rejeita o pedido |

---

## Controle de acesso

Cada agente tem um **modo de acesso ao WhatsApp**, configurado no painel em WhatsApp > Acesso ao agente:

- **Todos** (padrão): qualquer pessoa que escrever é atendida pela IA.
- **Somente contatos autorizados**: só os contatos da lista do agente (nome e telefone) chegam à IA.

No modo restrito, quem não está na lista passa por um fluxo de **solicitação de acesso** feito só com frases prontas, personalizáveis por agente. Esse fluxo nunca consome tokens de IA nem cria sessão de conversa:

1. A primeira mensagem recebe a frase de **oferta** ("deseja solicitar agora? responda sim ou não").
2. "sim" (ou "s") registra um pedido pendente e envia a **confirmação**; "não" (ou "n") envia a **recusa**. A resposta vale por até 24 h após a oferta.
3. Enquanto o pedido está pendente, novas mensagens recebem o aviso de **aguardando aprovação**.
4. O dono aprova ou rejeita o pedido no painel (ou pelo MCP). Aprovado, a pessoa vira contato autorizado e é atendida na próxima mensagem; rejeitada, as próximas mensagens são ignoradas em silêncio.

Regras:

- No máximo **1 frase automática por hora** por pessoa (a resposta direta ao "sim"/"não" não conta), para evitar spam e loops com outros bots.
- Telefones são comparados só pelos dígitos, com código do país (sem código, assume Brasil); o nono dígito de celulares brasileiros é ignorado na comparação.
- Em **grupos**, vale quem enviou a mensagem; menções de não autorizados são ignoradas, sem fluxo de solicitação no grupo.
- Quando o WhatsApp identifica o remetente só por um identificador interno (LID), o AvaBot tenta descobrir o telefone pelo WPP Connect (`GET /api/{session}/contact/pn-lid/{lid}`). Se não conseguir, a aprovação do pedido autoriza o próprio identificador.

---

## 6. Desconectar

Para encerrar a sessao WhatsApp de um agente:

```http
POST /whatsapp/{slug}/disconnect
Authorization: Bearer <seu-token-jwt>
```

---

## 7. Testando

### Com Bruno (API Client)

O projeto inclui uma collection do Bruno em `bruno/WhatsApp/`:

- **Start Session**: Inicia a sessao
- **QR Code**: Obtem o QR code
- **Status**: Verifica o status
- **Disconnect**: Encerra a sessao
- **Webhook (Simulate)**: Simula uma mensagem recebida

---

## Troubleshooting

| Problema | Causa Provavel | Solucao |
|---|---|---|
| QR code nao aparece | Sessao nao iniciada | Chame `POST /whatsapp/{slug}/start-session` primeiro |
| Erro de conexao com WPP | Container nao esta rodando | Verifique com `docker compose ps` |
| Mensagens nao chegam | Webhook nao configurado | Reinicie a sessao com start-session |
| Token duplicado | Outro agente usa o mesmo token | Use um token unico por agente |
| Sessao desconectada | WhatsApp desconectou no celular | Reconecte via start-session + qrcode |
