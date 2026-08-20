# AvaBot Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-12 (unified 2026-08-20)

## Active Technologies

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
- 010-whatsapp-admin: Added TypeScript 6.x + React 19.x + React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, sonner 2.x
- 009-telegram-bot-admin: Added TypeScript 6.x + React 19.x + React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, sonner 2.x
- 008-session-resume-cookies: Added TypeScript 6.0.2 + React 19.x + React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, react-markdown 10.x, Vite 8.x
- 004-whatsapp-wpp-integration: Added C# / .NET 9.0 + ASP.NET Core 9.0 + Entity Framework Core 9.x, HttpClient (nativo), AutoMapper, NAuth
- 003-telegram-per-agent-config: Added C# / .NET 9.0 + ASP.NET Core 9.0 + Entity Framework Core 9.x, Telegram.Bot 22.x, AutoMapper, NAuth
- 002-session-resume-telegram-bot: Added C# / .NET 9.0 + ASP.NET Core 9.0, Entity Framework Core 9.x, Telegram.Bot (NuGet), OpenAI SDK 2.10, Elasticsearch.Net 8.17


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
