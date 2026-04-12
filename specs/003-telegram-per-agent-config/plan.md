# Implementation Plan: Configuracao Telegram por Agente

**Branch**: `003-telegram-per-agent-config` | **Date**: 2026-04-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-telegram-per-agent-config/spec.md`

## Summary

Mover a configuracao do Telegram do appsettings para o banco de dados, armazenando TelegramBotName, TelegramBotToken e TelegramWebhookSecret na entidade Agent. Refatorar o webhook para usar rotas baseadas em slug (`/api/telegram/{slug}/webhook`), permitindo multiplos bots simultaneos. Adicionar endpoints para regenerar o webhook secret e verificar a configuracao do webhook no Telegram.

## Technical Context

**Language/Version**: C# / .NET 9.0 + ASP.NET Core 9.0
**Primary Dependencies**: Entity Framework Core 9.x, Telegram.Bot 22.x, AutoMapper, NAuth
**Storage**: PostgreSQL (via EF Core), Elasticsearch 8.17 (busca vetorial - nao impactado)
**Testing**: xUnit (AvaBot.Tests, AvaBot.Tests.API)
**Target Platform**: Linux server (Docker) / Windows (Development)
**Project Type**: web-service (REST API)
**Performance Goals**: N/A - feature administrativa, baixa frequencia de chamadas
**Constraints**: Dominio fixo https://avabot.net, webhook URL padrao `https://avabot.net/api/telegram/{slug}/webhook`
**Scale/Scope**: Dezenas de agentes, cada um com no maximo 1 bot Telegram

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Status | Notas |
|-----------|--------|-------|
| I. Skills Obrigatorias | PASS | Usar `dotnet-architecture` para alteracoes em entidade/repository/service/DI |
| II. Stack Tecnologica | PASS | Nenhuma tecnologia nova introduzida |
| III. Case Sensitivity | N/A | Sem alteracoes em frontend |
| IV. Convencoes de Codigo | PASS | PascalCase para propriedades C#, _camelCase para campos privados |
| V. Convencoes de Banco | PASS | Colunas snake_case: `telegram_bot_name`, `telegram_bot_token`, `telegram_webhook_secret` |
| VI. Autenticacao | PASS | Endpoints admin protegidos com `[Authorize]`, webhook `[AllowAnonymous]` |
| VII. Variaveis de Ambiente | PASS | Remover variaveis TELEGRAM_* do .env; config agora via banco |
| VIII. Tratamento de Erros | PASS | Padrao try/catch com StatusCode(500, ex.Message) |

## Project Structure

### Documentation (this feature)

```text
specs/003-telegram-per-agent-config/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── telegram-endpoints.md
└── tasks.md
```

### Source Code (repository root)

```text
AvaBot.Domain/
└── Models/
    └── Agent.cs                    # +3 campos Telegram

AvaBot.DTO/
└── AgentDTOs.cs                   # +3 campos em AgentInfo, +2 em AgentInsertInfo
                                    # +TelegramWebhookInfo DTO

AvaBot.Infra/
├── Context/
│   └── AvaBotContext.cs            # +3 colunas, +unique index TelegramBotToken
└── Repository/
    └── AgentRepository.cs          # +GetByTelegramBotTokenAsync

AvaBot.Infra.Interfaces/
└── Repository/
    └── IAgentRepository.cs         # +GetByTelegramBotTokenAsync

AvaBot.Application/
├── DependencyInjection.cs          # Remover singleton ITelegramBotClient
├── Profiles/
│   └── AgentProfile.cs             # Atualizar mapeamento
└── Services/
    ├── AgentService.cs             # +ConfigureTelegramAsync, +RegenerateWebhookSecretAsync
    └── TelegramService.cs          # Refatorar: resolver agent por slug, criar BotClient dinamico

AvaBot.API/
├── Controllers/
│   └── TelegramController.cs       # Refatorar rotas: {slug}/webhook, setup-webhook/{id}, webhook-info/{id}
└── appsettings.Docker.json         # Remover secao Telegram
```

**Structure Decision**: Projeto Clean Architecture existente mantido. Nenhum projeto novo necessario. Alteracoes distribuidas entre Domain (modelo), Infra (contexto/repositorio), Application (services/DI) e API (controller).

## Complexity Tracking

Nenhuma violacao de constituicao. Tabela nao aplicavel.
