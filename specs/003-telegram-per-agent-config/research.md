# Research: Configuracao Telegram por Agente

**Date**: 2026-04-11

## R1: Instanciacao Dinamica do TelegramBotClient

**Decision**: Criar instancias de `TelegramBotClient` sob demanda por agente, sem cache singleton.

**Rationale**: O `TelegramBotClient` do pacote Telegram.Bot e leve e stateless (apenas encapsula o HttpClient + token). Criar uma nova instancia por request e seguro e simples. A frequencia de chamadas por agente e baixa (mensagens de chat), nao justificando um cache/pool.

**Alternatives considered**:
- Cache em `ConcurrentDictionary<long, ITelegramBotClient>`: Complexidade desnecessaria; requer invalidacao ao trocar token. Descartado.
- Factory pattern com DI: Over-engineering para o volume esperado. Descartado.

## R2: Geracao de Webhook Secret

**Decision**: Usar `RandomNumberGenerator.GetHexString(32)` do .NET 9 para gerar secrets de 64 caracteres hexadecimais.

**Rationale**: Criptograficamente seguro, disponivel nativamente no .NET 8+, sem dependencias externas. O Telegram aceita secrets de ate 256 caracteres ASCII.

**Alternatives considered**:
- `Guid.NewGuid().ToString("N")`: 32 hex chars, entropia menor (122 bits vs 256 bits). Descartado por seguranca.
- `Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`: Funciona mas inclui caracteres especiais (+, /, =) que podem causar problemas em headers. Descartado.

## R3: Validacao de Unicidade do TelegramBotToken

**Decision**: Unique index no banco de dados na coluna `telegram_bot_token` (filtrado para nao-nulos) + validacao no `AgentService` antes de salvar.

**Rationale**: Dupla garantia - o indice filtrado previne race conditions, e a validacao no service da feedback amigavel ao usuario. Indice filtrado permite multiplos agentes sem token (NULL).

**Alternatives considered**:
- Apenas validacao no service: Vulneravel a race conditions. Descartado.
- Apenas unique index: Erro de banco generico, sem mensagem amigavel. Descartado.

## R4: Estrategia de Remocao da Config Global

**Decision**: Remover completamente a secao `Telegram` do appsettings e o registro singleton de `ITelegramBotClient` no DI. O `TelegramService` passa a receber o agent por parametro ou resolve via slug.

**Rationale**: Clarificacao da spec confirma remocao total. Manter config global criaria ambiguidade sobre qual config tem precedencia.

**Changes required**:
- `appsettings.Docker.json`: Remover secao `Telegram`
- `appsettings.Development.json`: Remover secao `Telegram` (se existir)
- `appsettings.Production.json`: Remover secao `Telegram` (se existir)
- `.env.example`: Remover variaveis `TELEGRAM_*`
- `docker-compose.yml`: Remover variaveis de ambiente `Telegram__*`
- `DependencyInjection.cs`: Remover bloco de registro do `ITelegramBotClient`

## R5: Rota do Webhook com Slug

**Decision**: Alterar a rota do webhook de `POST /telegram/webhook` para `POST /api/telegram/{slug}/webhook`. O controller resolve o agente pelo slug e valida o secret correspondente.

**Rationale**: Padrao RESTful claro. O slug ja e unico por agente (indice unico no banco). A URL `/api/telegram/{slug}/webhook` segue a convencao solicitada na spec.

**Alternatives considered**:
- Query parameter `?agent=slug`: Menos limpo na URL, mais dificil de configurar no BotFather. Descartado.
- Header customizado com agent ID: Telegram nao suporta headers customizados no webhook. Descartado.
