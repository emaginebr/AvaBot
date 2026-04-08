# Implementation Plan: Chatbot com Agente de Conhecimento

**Branch**: `001-knowledge-agent-chatbot` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-knowledge-agent-chatbot/spec.md`

## Summary

Sistema de chatbot RAG (Retrieval-Augmented Generation) com agentes de
conhecimento. Cada agente possui base de conhecimento propria alimentada
por arquivos Markdown, indexados no Elasticsearch para busca hibrida
(kNN + BM25). Comunicacao em tempo real via WebSocket com streaming de
respostas do gpt-4o. Coleta configuravel de dados do usuario (nome,
e-mail, telefone) por agente, com historico completo persistido em
PostgreSQL.

## Technical Context

**Language/Version**: C# / .NET 9.0
**Primary Dependencies**: ASP.NET Core, Entity Framework Core 9.x,
Elastic.Clients.Elasticsearch, OpenAI .NET SDK, MediatR, FluentValidation
**Frontend**: React 19, TypeScript 5.x, Vite 6.x, Zustand, TailwindCSS,
React Markdown, React Dropzone
**Storage**: PostgreSQL 16 (agentes, arquivos, sessoes, mensagens) +
Elasticsearch 8.x (chunks vetoriais)
**Real-time**: ASP.NET Core native WebSocket middleware
**Testing**: xUnit (backend), Vitest (frontend)
**Target Platform**: Docker containers (Linux)
**Performance Goals**: Primeiro token em <3s, 50 sessoes simultaneas
**Constraints**: Arquivos ate 10MB, historico em banco, sem autenticacao
de usuarios finais
**Scale/Scope**: Instancia unica, sem multi-tenancy

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Status | Nota |
|-----------|--------|------|
| I. Skills Obrigatorias | PASS | Usar `/dotnet-architecture` e `/react-architecture` na implementacao |
| II. Stack Tecnologica | DEVIATION | Ver Complexity Tracking abaixo |
| III. Case Sensitivity | PASS | Manter `Contexts/`, `Services/`, `hooks/`, `types/` |
| IV. Convencoes de Codigo | PASS | PascalCase backend, camelCase frontend, arrow functions |
| V. Convencoes de BD | PASS | snake_case, bigint PK, ClientSetNull, varchar c/ MaxLength |
| VI. Autenticacao | PASS | Controllers admin com `[Authorize]` via NAuth |
| VII. Variaveis de Ambiente | PASS | `VITE_` prefix, `ConnectionStrings__AvachatContext` |
| VIII. Tratamento de Erros | PASS | try/catch com StatusCode(500), handleError no frontend |

## Project Structure

### Documentation (this feature)

```text
specs/001-knowledge-agent-chatbot/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── agents-api.md
│   ├── knowledge-files-api.md
│   ├── chat-api.md
│   └── chat-history-api.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Backend/
├── Avachat.API/
│   ├── Controllers/
│   │   ├── AgentController.cs
│   │   ├── KnowledgeFileController.cs
│   │   └── ChatSessionController.cs
│   ├── WebSocket/
│   │   └── ChatWebSocketHandler.cs
│   └── Program.cs
├── Avachat.Domain/
│   ├── Models/
│   │   ├── Agent.cs
│   │   ├── KnowledgeFile.cs
│   │   ├── ChatSession.cs
│   │   └── ChatMessage.cs
│   ├── Services/
│   │   ├── AgentService.cs
│   │   ├── IngestionService.cs
│   │   ├── EmbeddingService.cs
│   │   ├── SearchService.cs
│   │   └── ChatService.cs
│   └── Enums/
│       ├── ProcessingStatus.cs
│       └── SenderType.cs
├── Avachat.Infra/
│   ├── Context/
│   │   └── AvachatContext.cs
│   ├── Repository/
│   │   ├── AgentRepository.cs
│   │   ├── KnowledgeFileRepository.cs
│   │   ├── ChatSessionRepository.cs
│   │   └── ChatMessageRepository.cs
│   └── AppServices/
│       ├── ElasticsearchService.cs
│       └── OpenAIService.cs
├── Avachat.Infra.Interfaces/
│   ├── Repository/
│   │   ├── IAgentRepository.cs
│   │   ├── IKnowledgeFileRepository.cs
│   │   ├── IChatSessionRepository.cs
│   │   └── IChatMessageRepository.cs
│   └── AppServices/
│       ├── IElasticsearchService.cs
│       └── IOpenAIService.cs
└── Avachat.Application/
    └── DependencyInjection.cs

Frontend/
├── src/
│   ├── types/
│   │   ├── agent.ts
│   │   ├── knowledgeFile.ts
│   │   ├── chatSession.ts
│   │   └── chatMessage.ts
│   ├── Services/
│   │   ├── AgentService.ts
│   │   ├── KnowledgeFileService.ts
│   │   └── ChatHistoryService.ts
│   ├── stores/
│   │   ├── useAgentStore.ts
│   │   ├── useChatStore.ts
│   │   └── useKnowledgeFileStore.ts
│   ├── hooks/
│   │   ├── useWebSocket.ts
│   │   └── useChat.ts
│   ├── pages/
│   │   ├── admin/
│   │   │   ├── AgentListPage.tsx
│   │   │   ├── AgentFormPage.tsx
│   │   │   ├── KnowledgeFilesPage.tsx
│   │   │   └── ChatHistoryPage.tsx
│   │   └── chat/
│   │       └── ChatPage.tsx
│   ├── components/
│   │   ├── chat/
│   │   │   ├── ChatWindow.tsx
│   │   │   ├── MessageBubble.tsx
│   │   │   ├── TypingIndicator.tsx
│   │   │   └── UserDataForm.tsx
│   │   ├── admin/
│   │   │   ├── AgentForm.tsx
│   │   │   ├── FileUpload.tsx
│   │   │   └── FileStatusBadge.tsx
│   │   └── common/
│   │       ├── NotFoundPage.tsx
│   │       └── UnavailablePage.tsx
│   ├── App.tsx
│   └── main.tsx
└── vite.config.ts
```

**Structure Decision**: Web application com backend .NET (Clean Architecture
em 5 projetos) e frontend React separado. Elasticsearch como servico
externo para busca vetorial.

## Complexity Tracking

> **Constitution deviations that must be justified**

| Deviation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| .NET 9 (constitution: 8.0) | Projeto novo; .NET 9 traz melhorias em WebSocket e performance | .NET 8 funcionaria mas perde otimizacoes relevantes para streaming |
| React 19 (constitution: 18.x) | Projeto novo; React 19 e a versao corrente | React 18 funcionaria mas sem melhorias de Suspense e concurrent features |
| Zustand (constitution: Context API) | Estado complexo de WebSocket + streaming + multiplos stores precisam de state management robusto | Context API geraria re-renders excessivos no chat em tempo real |
| TailwindCSS (constitution: Bootstrap) | Interface de chat customizada requer controle fino de estilos; utility-first e mais produtivo | Bootstrap forçaria overrides extensivos para o layout do chat |
| MediatR / CQRS | Separacao clara entre comandos (ingestao, chat) e queries; orquestracao de pipelines de ingestao | Chamadas diretas a services funcionariam mas sem pipeline validation |
| FluentValidation | Validacao estruturada de DTOs com regras complexas (slug unico, tamanho arquivo) | DataAnnotations sao limitadas para validacoes compostas |
| Elasticsearch | Busca vetorial hibrida (kNN + BM25 com RRF) e requisito core; PostgreSQL pgvector nao suporta RRF nativamente | pgvector suporta kNN mas nao busca hibrida com RRF |
| stores/ directory | Zustand usa stores em vez de Contexts; diretorio dedicado para organizacao | Misturar com Contexts/ confundiria convencao |
