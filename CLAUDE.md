# AvaBot Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-10-10 (unified 2026-10-10)

## Active Technologies
- C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9, EF Core 9 + Npgsql, OpenAI SDK 2.10 (já instalado; usa `ChatTool`/`ToolChatMessage`), `HttpClient` nativo (Entra ID + Power BI REST), `IMemoryCache` (cache de token), `System.Security.Cryptography.AesGcm`, AutoMapper; frontend com React Router 7, Zustand 5, Tailwind 4 e sonner 2. **Nenhuma dependência nova.** (011-powerbi-agent-tools)
- PostgreSQL. Uma coluna nova em `avabot_agents` e três tabelas novas, com o schema em `jsonb`. O Elasticsearch não é afetado. (011-powerbi-agent-tools)
- C# / .NET 9.0 (backend e testes); PowerShell 7 (script de execução) + ASP.NET Core 9, OpenAI SDK 2.10 (`ChatCompletion.Usage`), xUnit 2.9 + Flurl.Http 4 (já no `AvaBot.Tests.API`). **Nenhuma dependência nova.** (015-question-calibration-report)
- N/A. Sem tabelas nem migração; o rastreamento vive só durante a requisição. As consultas BI continuam indo para `avabot_powerbi_query_logs`, como no teste de agente atual. (015-question-calibration-report)
- C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9 + `Microsoft.AspNetCore.Authentication.JwtBearer` (já na API), EF Core 9 + Npgsql, FluentValidation (já na API), `System.Security.Cryptography` (PBKDF2, BCL), `IMemoryCache` (já registrado), AutoMapper; frontend com React Router 7, Zustand 5, Tailwind 4, sonner 2. **Nenhuma dependência nova.** (016-user-accounts-auth)
- PostgreSQL. Uma tabela nova (`avabot_users`) e uma coluna nova em `avabot_agents` (`owner_user_id`). Elasticsearch não é afetado. (016-user-accounts-auth)

### Backend (repo root)
- C# / .NET 9.0 + ASP.NET Core, Entity Framework Core 9.x (001-knowledge-agent-chatbot)
- C# / .NET 9.0 + ASP.NET Core 9.0, Entity Framework Core 9.x, Telegram.Bot (NuGet), OpenAI SDK 2.10, Elasticsearch.Net 8.17 (002-session-resume-telegram-bot)
- PostgreSQL 17 (relacional), Elasticsearch 8.17 (busca vetorial) (002-session-resume-telegram-bot)
- C# / .NET 9.0 + ASP.NET Core 9.0 + Entity Framework Core 9.x, Telegram.Bot 22.x, AutoMapper, NAuth (003-telegram-per-agent-config)
- PostgreSQL (via EF Core), Elasticsearch 8.17 (busca vetorial - nao impactado) (003-telegram-per-agent-config)
- C# / .NET 9.0 + ASP.NET Core 9.0 + Entity Framework Core 9.x, HttpClient (nativo), AutoMapper, NAuth (004-whatsapp-wpp-integration)

### Frontend (frontend/)
- TypeScript 6.0.2 + React 19.x, React Router 7.x, Tailwind CSS 4.x (005-landing-chat-widget)
- N/A (estado em memoria) (006-chat-start-session)
- TypeScript 6.0.2 + React 19.x + React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, react-markdown 10.x, react-dropzone 15.x (007-admin-auth-panel)
- localStorage (JWT token) (007-admin-auth-panel)
- TypeScript 6.0.2 + React 19.x + React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, react-markdown 10.x, Vite 8.x (008-session-resume-cookies)
- Cookies (dados de sessão por agente), localStorage (auth token admin) (008-session-resume-cookies)
- TypeScript 6.x + React 19.x + React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, sonner 2.x (009-telegram-bot-admin)
- N/A (dados persistidos via API backend existente) (009-telegram-bot-admin)
- TypeScript 6.x + React 19.x + React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, sonner 2.x (010-whatsapp-admin)

## Project Structure

```text
AvaBot.API/                  # Backend .NET (REST API + WebSocket)
AvaBot.Application/
AvaBot.Domain/
AvaBot.DTO/
AvaBot.Infra/
AvaBot.Infra.Interfaces/
AvaBot.Console/
AvaBot.Tests/
AvaBot.Tests.API/
frontend/                    # Frontend React/Vite
├── src/
└── ...
specs/                       # Specs compartilhadas (backend 001-004, frontend 005-010)
```

## Commands

Backend (raiz do repositório): # Add commands for C# / .NET 9.0

Frontend (dentro de `frontend/`): `npm test; npm run lint`

## Code Style

C# / .NET 9.0: Follow standard conventions
TypeScript 6.0.2 (frontend/): Follow standard conventions

## Recent Changes
- 016-user-accounts-auth: Added C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9 + `Microsoft.AspNetCore.Authentication.JwtBearer` (já na API), EF Core 9 + Npgsql, FluentValidation (já na API), `System.Security.Cryptography` (PBKDF2, BCL), `IMemoryCache` (já registrado), AutoMapper; frontend com React Router 7, Zustand 5, Tailwind 4, sonner 2. **Nenhuma dependência nova.**
- 015-question-calibration-report: Added C# / .NET 9.0 (backend e testes); PowerShell 7 (script de execução) + ASP.NET Core 9, OpenAI SDK 2.10 (`ChatCompletion.Usage`), xUnit 2.9 + Flurl.Http 4 (já no `AvaBot.Tests.API`). **Nenhuma dependência nova.**
- 011-powerbi-agent-tools: Added C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9, EF Core 9 + Npgsql, OpenAI SDK 2.10 (já instalado; usa `ChatTool`/`ToolChatMessage`), `HttpClient` nativo (Entra ID + Power BI REST), `IMemoryCache` (cache de token), `System.Security.Cryptography.AesGcm`, AutoMapper; frontend com React Router 7, Zustand 5, Tailwind 4 e sonner 2. **Nenhuma dependência nova.**


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
