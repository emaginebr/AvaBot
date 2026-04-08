# Quickstart — Chatbot com Agente de Conhecimento

## Pre-requisitos

- .NET 9 SDK
- Node.js 20+
- PostgreSQL 16
- Elasticsearch 8.x
- Chave de API OpenAI (com acesso a `text-embedding-3-small` e `gpt-4o`)

## 1. Clonar e configurar

```bash
git clone <repo-url>
cd Avachat
git checkout 001-knowledge-agent-chatbot
```

## 2. Configurar variaveis de ambiente

### Backend

Criar `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "AvachatContext": "Host=localhost;Database=avachat;Username=postgres;Password=postgres"
  },
  "Elasticsearch": {
    "Url": "http://localhost:9200"
  },
  "OpenAI": {
    "ApiKey": "<sua-chave-openai>",
    "EmbeddingModel": "text-embedding-3-small",
    "ChatModel": "gpt-4o"
  },
  "Chat": {
    "MaxHistoryMessages": 20
  }
}
```

### Frontend

Criar `.env.development`:

```env
VITE_API_URL=http://localhost:5000
VITE_WS_URL=ws://localhost:5000
```

## 3. Banco de dados

```bash
cd Backend
dotnet ef database update
```

## 4. Elasticsearch

Verificar que o Elasticsearch esta rodando:

```bash
curl http://localhost:9200/_cluster/health
```

## 5. Iniciar o backend

```bash
cd Backend
dotnet run
```

O backend estara disponivel em `http://localhost:5000`.

## 6. Iniciar o frontend

```bash
cd Frontend
npm install
npm run dev
```

O frontend estara disponivel em `http://localhost:5173`.

## 7. Teste rapido

1. Acesse o painel admin e crie um agente com slug `teste`
2. Configure os campos de coleta (nome, e-mail) e o prompt de sistema
3. Faca upload de um arquivo `.md` com conteudo de teste
4. Aguarde o status mudar para "pronto"
5. Acesse `http://localhost:5173/chat/teste`
6. Forneca seus dados e envie uma pergunta sobre o conteudo do arquivo

## Docker Compose (ambiente completo)

```bash
docker compose up -d
```

Servicos:
- `api` — Backend .NET 9 (porta 5000)
- `frontend` — React + Nginx (porta 80)
- `postgres` — PostgreSQL 16 (porta 5432)
- `elasticsearch` — Elasticsearch 8.x (porta 9200)
- `kibana` — Kibana (porta 5601, opcional)
