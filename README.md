# Avachat - Knowledge Agent Chatbot Platform

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791)
![Elasticsearch](https://img.shields.io/badge/Elasticsearch-8.17-005571)
![OpenAI](https://img.shields.io/badge/OpenAI-gpt--4o-412991)
![License](https://img.shields.io/badge/License-MIT-green)

## Overview

**Avachat** is a platform for creating AI-powered chatbot agents with custom knowledge bases. Each agent has its own personality (system prompt), knowledge documents, and can engage in real-time conversations via WebSocket using RAG (Retrieval-Augmented Generation) with hybrid search (kNN + BM25).

Built with **ASP.NET Core 9**, **Entity Framework Core**, **Elasticsearch** for vector/text search, and **OpenAI** for embeddings and chat completion. Follows **Clean Architecture** with separated Domain, Application, Infrastructure, and API layers.

---

## 🚀 Features

- 🤖 **Multi-Agent Support** - Create unlimited agents with custom system prompts and knowledge bases
- 📚 **RAG Pipeline** - Upload `.md` documents, auto-chunked and indexed with embeddings for retrieval
- 🔍 **Hybrid Search** - kNN vector search + BM25 text search via Elasticsearch
- 💬 **Real-Time Chat** - WebSocket streaming with token-by-token response delivery
- 🔄 **Auto Slug Generation** - Agent slugs generated from name with accent handling and uniqueness
- 📋 **Session Management** - REST endpoint to start sessions with user data collection
- 🛠️ **CLI Agent Loader** - Console app to create/sync agents from local files
- 📊 **Chat History** - Paginated sessions and messages with user context in AI prompts
- 🐳 **Docker Ready** - Development and production compose files with health checks

---

## 🛠️ Technologies Used

### Core Framework
- **ASP.NET Core 9.0** - Web API with Controllers and WebSocket middleware
- **Entity Framework Core 9.x** - ORM with Npgsql provider for PostgreSQL

### Database & Search
- **PostgreSQL 17** - Primary relational database
- **Elasticsearch 8.17** - Vector and full-text search engine for knowledge chunks

### AI
- **OpenAI API** - `text-embedding-3-small` for embeddings, `gpt-4o` for chat completion

### Libraries
- **AutoMapper 16.x** - Object mapping between layers
- **FluentValidation** - Request validation
- **MediatR** - Mediator pattern
- **Flurl.Http** - HTTP client for Console app and integration tests
- **Swashbuckle** - Swagger/OpenAPI documentation

### Testing
- **xUnit** - Test framework
- **Moq** - Mocking library
- **FluentAssertions** - Fluent assertion library

---

## 📁 Project Structure

```
Avachat/
├── Avachat.API/                 # REST API + WebSocket handler
│   ├── Controllers/             # AgentController, ChatSessionController, KnowledgeFileController
│   ├── Validators/              # FluentValidation validators
│   └── WebSocket/               # ChatWebSocketHandler
├── Avachat.Application/         # Business logic layer
│   ├── Profiles/                # AutoMapper profiles
│   └── Services/                # AgentService, ChatService, IngestionService, SearchService
├── Avachat.Domain/              # Domain models and enums
│   ├── Models/                  # Agent, ChatSession, ChatMessage, KnowledgeFile
│   └── Enums/                   # ProcessingStatus, SenderType
├── Avachat.DTO/                 # Data Transfer Objects
├── Avachat.Infra/               # Infrastructure implementation
│   ├── AppServices/             # ElasticsearchService, OpenAIService
│   ├── Context/                 # AvachatContext (EF Core DbContext)
│   └── Repository/              # Generic repository implementations
├── Avachat.Infra.Interfaces/    # Generic repository and service interfaces
├── Avachat.Console/             # CLI for creating/syncing agents from files
├── Avachat.Tests/               # Unit tests (xUnit + Moq)
├── Avachat.Tests.API/           # Integration tests (Flurl + FluentAssertions + WebSocket)
├── agent_input/                 # Agent configuration input directory
│   ├── system_prompt.md         # Agent system prompt
│   ├── description.md           # Agent description
│   └── docs/                    # Knowledge base documents (.md)
├── bruno/                       # Bruno API collection
├── docker-compose.yml           # Development environment
├── docker-compose-prod.yml      # Production environment
├── Dockerfile                   # Multi-stage .NET build
├── avachat.sql                  # Database schema
└── .github/workflows/           # CI/CD pipelines
```

---

## 🏗️ System Design

![System Design](docs/system-design.png)

The platform follows a layered architecture where the API layer handles HTTP/WebSocket requests, delegates to Application services for business logic, which in turn use Infrastructure services for data access, search, and AI integration.

**RAG Flow:** User message → Generate embedding (OpenAI) → Hybrid search in Elasticsearch → Build prompt with context → Stream response from GPT-4o → Save to database.

> 📄 **Source:** The editable Mermaid source is available at [`docs/system-design.mmd`](docs/system-design.mmd).

---

## ⚙️ Environment Configuration

### 1. Copy the environment template

```bash
cp .env.example .env
```

### 2. Edit the `.env` file

```bash
# Database
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password_here
POSTGRES_DB=avachat
CONNECTION_STRING=Host=db;Database=avachat;Username=postgres;Password=your_password_here

# Elasticsearch
ELASTICSEARCH_URL=http://elasticsearch:9200

# OpenAI
OPENAI_API_KEY=your_openai_api_key_here

# App
APP_PORT=5000
```

⚠️ **IMPORTANT**:
- Never commit the `.env` file with real credentials
- Only `.env.example` and `.env.prod.example` are version controlled
- You **must** provide a valid OpenAI API key for the chat to work

---

## 🐳 Docker Setup

### Quick Start

```bash
# 1. Configure environment
cp .env.example .env
# Edit .env with your OpenAI API key

# 2. Build and start
docker compose up -d --build

# 3. Verify
docker compose ps
```

### Accessing Services

| Service | URL |
|---------|-----|
| **API** | http://localhost:5000 |
| **Swagger** | http://localhost:5000/swagger |
| **WebSocket Chat** | ws://localhost:5000/ws/chat/{slug} |
| **Elasticsearch** | http://localhost:9200 |
| **Kibana** | http://localhost:5601 |
| **PostgreSQL** | localhost:5432 |

### Docker Commands

| Action | Command |
|--------|---------|
| Start services | `docker compose up -d` |
| Start with rebuild | `docker compose up -d --build` |
| Stop services | `docker compose stop` |
| View logs | `docker compose logs -f api` |
| Remove all | `docker compose down` |
| Remove with data (⚠️) | `docker compose down -v` |

---

## 🔧 Manual Setup (Without Docker)

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL 17
- Elasticsearch 8.17
- OpenAI API key

### Setup Steps

#### 1. Create the database

```bash
psql -U postgres -c "CREATE DATABASE avachat;"
psql -U postgres -d avachat -f avachat.sql
```

#### 2. Configure appsettings

Edit `Avachat.API/appsettings.Development.json` with your local connection strings and OpenAI key.

#### 3. Run the API

```bash
dotnet run --project Avachat.API
```

The API will be available at `http://localhost:5030`.

---

## 🧪 Testing

### Unit Tests

```bash
dotnet test Avachat.Tests
```

### Integration Tests (requires API running)

```bash
# Terminal 1: start the API
docker compose up -d

# Terminal 2: run integration tests
dotnet test Avachat.Tests.API
```

### Test Structure

```
Avachat.Tests/                    # 71 unit tests
├── Application/Services/         # AgentService, ChatService, IngestionService, SearchService
└── API/
    ├── Controllers/              # AgentController, ChatSessionController, KnowledgeFileController
    └── Validators/               # AgentInsertInfoValidator

Avachat.Tests.API/                # Integration tests (HTTP + WebSocket)
└── Controllers/                  # Agent, ChatSession, KnowledgeFile, ChatWebSocket
```

---

## 📚 API Documentation

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/agents` | List all agents |
| GET | `/api/agents/{slug}` | Get agent by slug |
| GET | `/api/agents/{slug}/chat-config` | Get chat configuration |
| POST | `/api/agents` | Create agent (slug auto-generated) |
| PUT | `/api/agents/{id}` | Update agent |
| DELETE | `/api/agents/{id}` | Delete agent |
| PATCH | `/api/agents/{id}/status` | Toggle agent status |
| POST | `/api/agents/{slug}/sessions` | Start chat session |
| GET | `/api/agents/{agentId}/sessions` | List sessions (paginated) |
| GET | `/api/sessions/{sessionId}/messages` | List messages (paginated) |
| GET | `/api/agents/{agentId}/files` | List knowledge files |
| POST | `/api/agents/{agentId}/files` | Upload `.md` file (multipart, max 10MB) |
| DELETE | `/api/agents/{agentId}/files/{fileId}` | Delete knowledge file |
| POST | `/api/agents/{agentId}/files/{fileId}/reprocess` | Reprocess file |
| WS | `/ws/chat/{slug}?sessionId={id}` | WebSocket chat connection |

### WebSocket Protocol

```
1. Connect: ws://localhost:5000/ws/chat/{slug}?sessionId={id}
2. Receive: {"type":"ready"}
3. Send:    {"type":"message","content":"Hello"}
4. Receive: {"type":"chunk","content":"H"}
            {"type":"chunk","content":"ello"}
            {"type":"done"}
```

### Bruno Collection

Import the collection from `bruno/Avachat API/` in [Bruno](https://www.usebruno.com/) for interactive API testing.

---

## 🤖 CLI Agent Loader

Create and sync agents from local files:

```bash
# Structure
agent_input/
├── system_prompt.md    # Agent personality (required)
├── description.md      # Agent description (optional)
└── docs/               # Knowledge base (.md files)
    ├── doc1.md
    └── doc2.md

# Run (from project root)
dotnet run --project Avachat.Console -- "Agent Name"
```

The CLI will:
- Create the agent if it doesn't exist, or update it if it does
- Sync all `.md` files from `docs/` (upload new, replace changed, remove orphans)

---

## 🚀 Deployment

### Production

```bash
# 1. Configure production secrets
cp .env.prod.example .env.prod

# 2. Create external network
docker network create avachat-external

# 3. Deploy
docker compose --env-file .env.prod -f docker-compose-prod.yml up -d --build
```

### GitHub Actions

Production deploy via SSH is configured in `.github/workflows/deploy-prod.yml` (manual trigger via `workflow_dispatch`).

**Required GitHub Secrets:** `PROD_SSH_HOST`, `PROD_SSH_USER`, `PROD_SSH_PASSWORD`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `CONNECTION_STRING`, `ELASTICSEARCH_URL`, `OPENAI_API_KEY`

---

## 💾 Database

### Schema

The database uses `avachat_` prefix on all tables:

| Table | Description |
|-------|-------------|
| `avachat_agents` | Agent configurations |
| `avachat_knowledge_files` | Uploaded knowledge documents |
| `avachat_chat_sessions` | Chat sessions with user data |
| `avachat_chat_messages` | Individual chat messages |

### Initialize

```bash
psql -U postgres -d avachat -f avachat.sql
```

### Backup

```bash
docker compose exec postgres pg_dump -U postgres avachat > backup.sql
```

### Restore

```bash
docker compose exec -T postgres psql -U postgres avachat < backup.sql
```

---

## 👨‍💻 Author

Developed by **[Rodrigo Landim](https://github.com/emaginebr)**

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

**⭐ If you find this project useful, please consider giving it a star!**
