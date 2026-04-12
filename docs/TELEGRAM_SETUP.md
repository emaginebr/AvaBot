# Configuracao do Telegram Bot

> Guia completo para configurar e integrar o bot do Telegram com o AvaBot, por agente.

**Created:** 2026-04-11
**Last Updated:** 2026-04-11

---

## Pre-requisitos

1. **Conta no Telegram** com acesso ao [BotFather](https://t.me/BotFather)
2. **AvaBot API** rodando e acessivel publicamente em `https://avabot.net`
3. **Agente cadastrado** no banco de dados do AvaBot (voce precisara do `id` e `slug` do agente)

---

## 1. Criar o Bot no Telegram

1. Abra o Telegram e inicie uma conversa com o [@BotFather](https://t.me/BotFather)
2. Envie o comando `/newbot`
3. Siga as instrucoes:
   - Escolha um **nome** para o bot (ex: `AvaBot Assistente`)
   - Escolha um **username** unico terminando em `bot` (ex: `avabot_assistente_bot`)
4. O BotFather retornara o **Bot Token** no formato:
   ```
   123456789:ABCdefGHIjklMNOpqrsTUVwxyz
   ```
5. **Guarde este token** — ele sera configurado no agente

---

## 2. Configurar o Agente

A configuracao do Telegram e feita **por agente no banco de dados**. Nao ha configuracao global no appsettings.

### Campos do Agente

| Campo | Descricao | Obrigatorio |
|---|---|---|
| `telegramBotName` | Nome do bot para exibicao | Nao |
| `telegramBotToken` | Token obtido do BotFather | Sim (para ativar) |
| `telegramWebhookSecret` | Secret para validar requests (gerado automaticamente) | Auto |

### Via API

Ao criar ou atualizar um agente, inclua os campos Telegram:

```http
PUT /api/agents/{id}
Authorization: Bearer <seu-token-jwt>
Content-Type: application/json

{
  "name": "Assistente Vendas",
  "description": "Bot de vendas",
  "systemPrompt": "Voce e um assistente de vendas...",
  "chatModel": "gpt-4o",
  "telegramBotName": "AvaBot Assistente",
  "telegramBotToken": "123456789:ABCdefGHIjklMNOpqrsTUVwxyz"
}
```

> O `telegramWebhookSecret` e gerado automaticamente quando o token e configurado pela primeira vez. Nao e necessario envia-lo.

---

## 3. Registrar o Webhook

Apos configurar o agente com o token, registre o webhook no Telegram:

```http
POST /api/telegram/{agentId}/setup-webhook
Authorization: Bearer <seu-token-jwt>
```

Resposta:
```json
{
  "sucesso": true,
  "mensagem": "Webhook registrado com sucesso",
  "dados": {
    "agentId": 1,
    "agentSlug": "assistente-vendas",
    "webhookUrl": "https://avabot.net/api/telegram/assistente-vendas/webhook",
    "isConfigured": true
  }
}
```

### Verificar o Webhook

```http
GET /api/telegram/{agentId}/webhook-info
Authorization: Bearer <seu-token-jwt>
```

### Regenerar o Secret

Em caso de comprometimento do secret:

```http
POST /api/telegram/{agentId}/regenerate-secret
Authorization: Bearer <seu-token-jwt>
```

Isso gera um novo secret e atualiza automaticamente o webhook no Telegram.

---

## 4. Como Funciona

### URL do Webhook

Cada agente recebe um webhook unico baseado no slug:

```
https://avabot.net/api/telegram/{slug}/webhook
```

Exemplos:
- `https://avabot.net/api/telegram/assistente-vendas/webhook`
- `https://avabot.net/api/telegram/suporte-tecnico/webhook`

### Fluxo de Conversa

```
Usuario Telegram  -->  Telegram API  -->  POST /api/telegram/{slug}/webhook
                                                      |
                                               Resolve agente pelo slug
                                               Valida webhook secret
                                                      |
                                               TelegramService
                                                      |
                                                ChatService
                                                      |
                                               Resposta via Telegram API
```

### Comando `/start`

1. Usuario envia `/start` no chat com o bot
2. O sistema cria uma nova sessao de chat e registra o TelegramChat no banco
3. Envia mensagem de boas-vindas com o nome do agente

### Mensagens de Texto

1. Usuario envia uma mensagem de texto
2. O sistema valida que o usuario iniciou o chat com `/start`
3. Processa a mensagem via ChatService e envia a resposta em Markdown

> Apenas mensagens de texto sao aceitas. Imagens, audios, stickers e outros tipos sao rejeitados.

---

## 5. Banco de Dados

### Campos Telegram na tabela `avabot_agents`

| Coluna | Tipo | Descricao |
|---|---|---|
| `telegram_bot_name` | `varchar(260)` | Nome do bot (opcional) |
| `telegram_bot_token` | `varchar(260)` | Token do bot (unico, opcional) |
| `telegram_webhook_secret` | `varchar(260)` | Secret do webhook (gerado automaticamente) |

### Tabela `avabot_telegram_chats`

| Coluna | Tipo | Descricao |
|---|---|---|
| `telegram_chat_id` | `bigint` (PK) | ID do chat no Telegram |
| `agent_id` | `bigint` (FK) | Referencia ao agente |
| `chat_session_id` | `bigint` (FK) | Referencia a sessao |
| `telegram_username` | `varchar(260)` | Username do usuario (opcional) |
| `telegram_first_name` | `varchar(260)` | Primeiro nome (opcional) |

---

## 6. Multiplos Bots

O sistema suporta multiplos bots Telegram simultaneamente. Cada agente pode ter seu proprio bot:

1. Crie um bot no BotFather para cada agente
2. Configure o token em cada agente via API
3. Registre o webhook de cada agente: `POST /api/telegram/{agentId}/setup-webhook`
4. Cada bot recebe mensagens na URL `https://avabot.net/api/telegram/{slug}/webhook`

> Cada token deve ser unico — o sistema impede que dois agentes usem o mesmo token.

---

## 7. Testando

### Com Bruno (API Client)

O projeto inclui uma collection do Bruno em `bruno/Telegram/`:

- **Setup Webhook**: Registra o webhook de um agente
- **Webhook (Simulate)**: Simula um update do Telegram
- **Webhook Info**: Consulta a configuracao do webhook
- **Regenerate Secret**: Regenera o webhook secret

### Simulacao Manual de Webhook

```bash
curl -X POST https://avabot.net/api/telegram/assistente-vendas/webhook \
  -H "Content-Type: application/json" \
  -H "X-Telegram-Bot-Api-Secret-Token: seu-webhook-secret" \
  -d '{
    "update_id": 123456,
    "message": {
      "message_id": 1,
      "from": {
        "id": 987654321,
        "is_bot": false,
        "first_name": "Teste",
        "username": "usuario_teste"
      },
      "chat": {
        "id": 987654321,
        "first_name": "Teste",
        "username": "usuario_teste",
        "type": "private"
      },
      "date": 1700000000,
      "text": "/start"
    }
  }'
```

### Teste direto no Telegram

1. Abra o Telegram e busque pelo username do seu bot
2. Envie `/start` para iniciar a conversa
3. Envie uma mensagem de texto e aguarde a resposta do agente

---

## Troubleshooting

| Problema | Causa Provavel | Solucao |
|---|---|---|
| Bot nao responde | Webhook nao registrado | Execute `POST /api/telegram/{id}/setup-webhook` |
| Erro 401 no webhook | Secret incorreto ou agente inativo | Verifique status do agente e regenere o secret |
| "Envie /start primeiro" | Sessao nao iniciada | Envie `/start` antes de outras mensagens |
| Webhook retorna erro | URL inacessivel | Verifique HTTPS e acesso publico ao dominio |
| Token duplicado | Outro agente usa o mesmo token | Use um token unico por agente |
| Markdown quebrado | Caracteres especiais | O bot faz fallback para texto puro automaticamente |
