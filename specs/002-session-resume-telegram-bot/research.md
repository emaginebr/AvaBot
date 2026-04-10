# Research: Retomada de Sessao e Bot Telegram

**Date**: 2026-04-10  
**Feature**: 002-session-resume-telegram-bot

---

## R1: Geracao de Resume Token Seguro

**Decision**: Usar `Guid.NewGuid().ToString("N")` (32 caracteres hexadecimais) para gerar o resume token.

**Rationale**: GUIDs v4 sao criptograficamente aleatorios no .NET (usam `RandomNumberGenerator`), produzem tokens de 128 bits com 2^122 combinacoes possiveis, e sao nativos do .NET sem dependencias externas. O formato "N" (sem hifens) facilita uso em URLs.

**Alternatives Considered**:
- `RandomNumberGenerator.GetBytes(32)` + Base64: Mais entropia (256 bits) mas desnecessario para este caso. GUIDs ja excedem o necessario.
- JWT como token: Overhead excessivo para um token de lookup simples. Nao precisa de payload decodificavel.
- Hash incremental: Previsivel, inseguro.

---

## R2: Telegram Bot API - Webhook vs Polling

**Decision**: Usar webhook (nao polling) para receber updates do Telegram.

**Rationale**: Webhook e mais eficiente para producao — o Telegram envia atualizacoes imediatamente via HTTPS POST ao nosso endpoint. Nao requer processo de polling em loop. A spec ja define webhook como abordagem.

**Alternatives Considered**:
- Long polling (`getUpdates`): Requer processo dedicado em loop. Mais simples para desenvolvimento local, mas inadequado para producao com Docker/Nginx.

---

## R3: Biblioteca C# para Telegram Bot API

**Decision**: Usar `Telegram.Bot` NuGet package (pacote oficial da comunidade, amplamente usado).

**Rationale**: E o pacote mais maduro para .NET com suporte completo a Bot API, incluindo deserializacao de updates, envio de mensagens e configuracao de webhook. Ativamente mantido.

**Alternatives Considered**:
- HTTP direto com HttpClient: Funcional mas requer deserializacao manual de todos os tipos do Telegram, propenso a erros.
- `TelegramBotApiClientLib`: Menos popular e menos documentado.

---

## R4: Armazenamento do Resume Token

**Decision**: Adicionar coluna `resume_token varchar(32)` na tabela `avachat_chat_sessions` com indice unico.

**Rationale**: O token e um atributo direto da sessao. Coluna com indice unico garante busca rapida (O(log n)) e unicidade. 32 caracteres e suficiente para GUID sem hifens.

**Alternatives Considered**:
- Tabela separada de tokens: Overhead desnecessario, uma coluna na sessao e mais simples.
- Redis/cache: Nao persistente, inadequado para tokens que nao expiram.

---

## R5: Mapeamento Telegram Chat -> Sessao Avachat

**Decision**: Criar tabela `avachat_telegram_chats` com `telegram_chat_id` (bigint, PK), `agent_id` (FK), `chat_session_id` (FK). Quando /start e enviado, criar nova sessao e atualizar o registro.

**Rationale**: O `chat_id` do Telegram e unico por conversa privada e e do tipo long/bigint. Manter uma tabela separada permite rastrear a sessao ativa de cada usuario do Telegram e trocar quando /start e enviado.

**Alternatives Considered**:
- Usar campo `UserPhone` ou `UserEmail` para identificar: Telegram nao fornece esses dados sem permissao explicita. `chat_id` e sempre disponivel.
- Coluna `telegram_chat_id` na tabela sessions: Poluiria o modelo de sessao com dados especificos de plataforma.

---

## R6: Processamento de Mensagens Telegram (Sincrono vs Assincrono)

**Decision**: Processar mensagens sincronamente no endpoint do webhook — receber update, processar via ChatService, enviar resposta via Telegram Bot API, retornar 200 OK.

**Rationale**: O Telegram espera resposta 200 OK em ate 60 segundos. O pipeline RAG + OpenAI geralmente responde em 5-15 segundos. Processamento sincrono e mais simples e suficiente para o volume esperado.

**Alternatives Considered**:
- Fila RabbitMQ + Worker: Overhead desnecessario para o volume atual. Pode ser adicionado futuramente se necessario.
- Retornar 200 imediatamente + processar em background task: Mais complexo, necessario apenas se o processamento exceder 60s regularmente.

---

## R7: Webhook Secret Token

**Decision**: Gerar um secret token aleatorio via `Guid.NewGuid().ToString("N")` e configura-lo via variavel de ambiente `TELEGRAM_WEBHOOK_SECRET`. Enviar ao Telegram durante registro do webhook.

**Rationale**: O Telegram envia o secret no header `X-Telegram-Bot-Api-Secret-Token` em cada requisicao ao webhook. O endpoint valida comparando com o valor configurado. Impede que terceiros enviem updates falsos.

**Alternatives Considered**:
- Sem verificacao: Inseguro, qualquer um poderia enviar requisicoes ao endpoint.
- IP whitelisting do Telegram: Fragil, IPs podem mudar.

---

## R8: Resposta do Bot - Texto vs Markdown

**Decision**: Enviar respostas usando `ParseMode.Markdown` do Telegram para formatacao basica (negrito, italico, codigo).

**Rationale**: As respostas do ChatGPT frequentemente incluem formatacao Markdown. O Telegram suporta Markdown nativo, melhorando a legibilidade das respostas.

**Alternatives Considered**:
- Texto puro: Perde formatacao util.
- HTML: Requer conversao de Markdown para HTML, overhead desnecessario.
