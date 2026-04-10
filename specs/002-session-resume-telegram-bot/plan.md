# Implementation Plan: Retomada de Sessao e Bot Telegram

**Branch**: `002-session-resume-telegram-bot` | **Date**: 2026-04-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-session-resume-telegram-bot/spec.md`

## Summary

Adicionar funcionalidade de retomada de sessao (resume token) ao Avachat e integracao com Telegram Bot. O resume token e um GUID unico gerado na criacao de cada sessao, permitindo que usuarios retomem sua ultima conversa. O bot Telegram recebe mensagens via webhook, processa com o pipeline RAG existente e responde no Telegram.

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: ASP.NET Core 9.0, Entity Framework Core 9.x, Telegram.Bot (NuGet), OpenAI SDK 2.10, Elasticsearch.Net 8.17  
**Storage**: PostgreSQL 17 (relacional), Elasticsearch 8.17 (busca vetorial)  
**Testing**: xUnit, Moq, FluentAssertions  
**Target Platform**: Linux server (Docker)  
**Project Type**: Web service (REST API + WebSocket)  
**Performance Goals**: Resume endpoint < 2s, Telegram response < 10s  
**Constraints**: Telegram webhook timeout 60s, HTTPS obrigatorio para webhook  
**Scale/Scope**: Volume moderado, um agente por bot Telegram

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Skills Obrigatorias | PASS | `dotnet-architecture` sera usada para novas entidades (TelegramChat) e modificacoes (ChatSession) |
| II. Stack Tecnologica | PASS | .NET 9.0, EF Core 9.x, PostgreSQL. Telegram.Bot e nova dependencia justificada (integracao externa) |
| III. Case Sensitivity | N/A | Feature e backend-only |
| IV. Convencoes de Codigo | PASS | PascalCase para classes/metodos, _camelCase para campos privados |
| V. Convencoes de BD | PASS | snake_case para tabelas/colunas, bigint PKs, ClientSetNull para FKs |
| VI. Autenticacao | PASS | Endpoints publicos (resume, webhook) sao AllowAnonymous. Setup webhook e Authorize |
| VII. Variaveis de Ambiente | PASS | Novas variaveis: TELEGRAM_BOT_TOKEN, TELEGRAM_WEBHOOK_SECRET, TELEGRAM_WEBHOOK_URL, TELEGRAM_AGENT_SLUG |
| VIII. Tratamento de Erros | PASS | try/catch com StatusCode(500, ex.Message) no padrao existente |

**Post-Phase 1 Re-check**: Todas as gates mantidas. Nenhuma violacao.

## Project Structure

### Documentation (this feature)

```text
specs/002-session-resume-telegram-bot/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 - Research decisions
├── data-model.md        # Phase 1 - Data model changes
├── quickstart.md        # Phase 1 - Quick start guide
├── contracts/           # Phase 1 - API contracts
│   ├── session-resume-api.md
│   └── telegram-webhook-api.md
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
Avachat.Domain/
├── Models/
│   ├── ChatSession.cs          # MODIFICAR: adicionar ResumeToken
│   └── TelegramChat.cs         # NOVO: modelo do chat Telegram

Avachat.DTO/
├── ChatDTOs.cs                 # MODIFICAR: adicionar ResumeToken ao ChatSessionInfo
│                               # NOVO: ChatSessionResumeInfo (sessao + mensagens)
└── TelegramDTOs.cs             # NOVO: DTOs do Telegram (se necessario)

Avachat.Infra.Interfaces/
└── Repository/
    ├── IChatSessionRepository.cs   # MODIFICAR: adicionar GetByResumeTokenAsync
    └── ITelegramChatRepository.cs  # NOVO: interface do repositorio Telegram

Avachat.Infra/
├── Context/
│   └── AvachatContext.cs        # MODIFICAR: adicionar DbSet<TelegramChat>, Fluent API
├── Repository/
│   ├── ChatSessionRepository.cs # MODIFICAR: implementar GetByResumeTokenAsync
│   └── TelegramChatRepository.cs # NOVO: repositorio Telegram
└── Migrations/
    ├── *_AddResumeToken.cs      # NOVO: migracao resume token
    └── *_AddTelegramChat.cs     # NOVO: migracao tabela Telegram

Avachat.Application/
└── Services/
    ├── ChatService.cs           # MODIFICAR: gerar ResumeToken na criacao
    └── TelegramService.cs       # NOVO: logica do bot Telegram

Avachat.API/
├── Controllers/
│   ├── SessionController.cs     # MODIFICAR: endpoint de retomada
│   └── TelegramController.cs    # NOVO: webhook + setup endpoints
└── WebSocket/
    └── ChatWebSocketHandler.cs  # MODIFICAR: incluir resumeToken no evento ready

Avachat.API/
└── Program.cs                   # MODIFICAR: registrar servicos Telegram no DI
```

**Structure Decision**: Segue a estrutura Clean Architecture existente do projeto. Novos arquivos seguem o padrao de nomeacao e organizacao ja estabelecido. Nenhum projeto novo necessario.

## Complexity Tracking

Nenhuma violacao de constituicao a justificar.
