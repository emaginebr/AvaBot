# AvaBot Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-11-25 (unified 2026-11-25)

## Active Technologies
- C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9, EF Core 9 + Npgsql, OpenAI SDK 2.10 (já instalado; usa `ChatTool`/`ToolChatMessage`), `HttpClient` nativo (Entra ID + Power BI REST), `IMemoryCache` (cache de token), `System.Security.Cryptography.AesGcm`, AutoMapper; frontend com React Router 7, Zustand 5, Tailwind 4 e sonner 2. **Nenhuma dependência nova.** (011-powerbi-agent-tools)
- PostgreSQL. Uma coluna nova em `avabot_agents` e três tabelas novas, com o schema em `jsonb`. O Elasticsearch não é afetado. (011-powerbi-agent-tools)
- C# / .NET 9.0 (backend e testes); PowerShell 7 (script de execução) + ASP.NET Core 9, OpenAI SDK 2.10 (`ChatCompletion.Usage`), xUnit 2.9 + Flurl.Http 4 (já no `AvaBot.Tests.API`). **Nenhuma dependência nova.** (015-question-calibration-report)
- N/A. Sem tabelas nem migração; o rastreamento vive só durante a requisição. As consultas BI continuam indo para `avabot_powerbi_query_logs`, como no teste de agente atual. (015-question-calibration-report)
- C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9 + `Microsoft.AspNetCore.Authentication.JwtBearer` (já na API), EF Core 9 + Npgsql, FluentValidation (já na API), `System.Security.Cryptography` (PBKDF2, BCL), `IMemoryCache` (já registrado), AutoMapper; frontend com React Router 7, Zustand 5, Tailwind 4, sonner 2. **Nenhuma dependência nova.** (016-user-accounts-auth)
- PostgreSQL. Uma tabela nova (`avabot_users`) e uma coluna nova em `avabot_agents` (`owner_user_id`). Elasticsearch não é afetado. (016-user-accounts-auth)
- C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9, EF Core 9 + Npgsql, FluentValidation (já na API), AutoMapper, `System.Security.Cryptography` (SHA-256, `RandomNumberGenerator`, BCL), `Microsoft.AspNetCore.Authentication` (handler próprio, já na API via JwtBearer). **Dependência nova**: `ModelContextProtocol.AspNetCore` **2.2.0** na API (transitivamente `ModelContextProtocol`, `ModelContextProtocol.Core`, `Microsoft.Extensions.AI.Abstractions` 10.8.3) e `ModelContextProtocol` 2.2.0 só em `AvaBot.Tests.API` (cliente para os testes). Todos já no cache NuGet local. Frontend: React Router 7, Zustand 5, Tailwind 4, sonner 2; nada novo. (017-mcp-agent-management)
- PostgreSQL. Uma tabela nova (`avabot_mcp_access_keys`). Nenhuma coluna nova em tabelas existentes. Elasticsearch não é afetado (os documentos adicionados pelo MCP seguem a ingestão atual). (017-mcp-agent-management)
- C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9, EF Core 9 + Npgsql, FluentValidation (já na API), AutoMapper, `IMemoryCache` (já registrado), `HttpClient` do `WppConnectService` existente, `ModelContextProtocol.AspNetCore` 2.2.0 (já na API, feature 017); frontend com React Router 7, Zustand 5, Tailwind 4 e sonner 2. **Nenhuma dependência nova.** (018-whatsapp-allowed-numbers)
- PostgreSQL. 5 colunas novas em `avabot_agents` (`whatsapp_access_mode` + 4 frases) e 3 tabelas novas. Uma migração (`AddWhatsappAccessControl`). Elasticsearch não é afetado. (018-whatsapp-allowed-numbers)
- C# / .NET 9.0 + ASP.NET Core 9, EF Core 9 + Npgsql (já na Infra). **Remove** `Elastic.Clients.Elasticsearch` 8.17. **Nenhuma dependência nova.** (021-postgres-knowledge-search)
- PostgreSQL. Uma tabela nova (`avabot_knowledge_chunks`, com `tsvector` gerado + GIN) substitui o índice do Elasticsearch, que sai do projeto. Uma migração (`AddKnowledgeChunks`). (021-postgres-knowledge-search)

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

## Deploy do frontend, do WPP Connect e da infra

**Sempre** que o usuário pedir para subir/publicar/fazer deploy do frontend, do wppconnect ou da infra (RabbitMQ, Elasticsearch, WPP Connect), use a skill `avabot-deploy`. O deploy é feito pelo repositório irmão `C:\repos\emagine-deploy`, não por este:

- Frontend: `scripts/build-avabot.ps1` → commit `chore: atualiza build do avabot` em `builds/avabot` na `main` → workflow `Deploy Production`.
- WPP Connect / infra: `gh workflow run deploy-infra.yml -R emaginebr/emagine-deploy` (recria RabbitMQ, Elasticsearch e WPP Connect; confirmar antes).

## Commands

Backend (raiz do repositório): # Add commands for C# / .NET 9.0

Frontend (dentro de `frontend/`): `npm test; npm run lint`

## Code Style

C# / .NET 9.0: Follow standard conventions
TypeScript 6.0.2 (frontend/): Follow standard conventions

## Recent Changes
- 021-postgres-knowledge-search: Added C# / .NET 9.0 + ASP.NET Core 9, EF Core 9 + Npgsql (já na Infra). **Remove** `Elastic.Clients.Elasticsearch` 8.17. **Nenhuma dependência nova.**
- 018-whatsapp-allowed-numbers: Added C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9, EF Core 9 + Npgsql, FluentValidation (já na API), AutoMapper, `IMemoryCache` (já registrado), `HttpClient` do `WppConnectService` existente, `ModelContextProtocol.AspNetCore` 2.2.0 (já na API, feature 017); frontend com React Router 7, Zustand 5, Tailwind 4 e sonner 2. **Nenhuma dependência nova.**
- 017-mcp-agent-management: Added C# / .NET 9.0 (backend); TypeScript 6.x + React 19 (frontend) + ASP.NET Core 9, EF Core 9 + Npgsql, FluentValidation (já na API), AutoMapper, `System.Security.Cryptography` (SHA-256, `RandomNumberGenerator`, BCL), `Microsoft.AspNetCore.Authentication` (handler próprio, já na API via JwtBearer). **Dependência nova**: `ModelContextProtocol.AspNetCore` **2.2.0** na API (transitivamente `ModelContextProtocol`, `ModelContextProtocol.Core`, `Microsoft.Extensions.AI.Abstractions` 10.8.3) e `ModelContextProtocol` 2.2.0 só em `AvaBot.Tests.API` (cliente para os testes). Todos já no cache NuGet local. Frontend: React Router 7, Zustand 5, Tailwind 4, sonner 2; nada novo.


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
