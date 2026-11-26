# Implementation Plan: Base de conhecimento no banco principal (sem Elasticsearch)

**Branch**: `021-postgres-knowledge-search` | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/021-postgres-knowledge-search/spec.md`

## Summary

Os trechos dos arquivos de conhecimento passam do índice `knowledge_chunks` do Elasticsearch para a tabela nova `avabot_knowledge_chunks`, no PostgreSQL principal. A busca vira full-text search nativo (`tsvector` gerado + índice GIN, configuração `portuguese`), com semântica OR e ranking `ts_rank`. Acentos e maiúsculas são ignorados porque o texto é normalizado na aplicação, sem depender de extensões do banco.

A abstração `IElasticsearchService` vira `IKnowledgeChunkRepository`, com os mesmos métodos, exceto `CreateIndexAsync`, que é removido. Os consumidores (Ingestion, KnowledgeFile, Agent, Search, Calibration) só trocam o tipo.

Um `HostedService` de backfill reprocessa, na subida, os arquivos `Ready` que ainda não têm trechos, a partir de `file_content`. O Elasticsearch sai do código, da configuração, do `docker-compose.yml`, dos manifestos k8s e da pipeline.

## Technical Context

**Language/Version**: C# / .NET 9.0
**Primary Dependencies**: ASP.NET Core 9, EF Core 9 + Npgsql (já na Infra). **Remove** `Elastic.Clients.Elasticsearch` 8.17. **Nenhuma dependência nova.**
**Storage**: PostgreSQL, com a tabela nova `avabot_knowledge_chunks`:
- `search_vector tsvector GENERATED ALWAYS AS (to_tsvector('portuguese'::regconfig, search_content)) STORED`;
- índice GIN;
- `embedding real[]`, sem pgvector.

Uma migração: `AddKnowledgeChunks`.
**Testing**: xUnit + Moq (`AvaBot.Tests`); validação real do SQL contra `postgres:17-alpine` no Docker do WSL.
**Target Platform**: Linux container (cluster DOKS `do-nyc2-emagine-k8s`)
**Project Type**: web-service (API) + ferramenta de calibração (console)
**Performance Goals**: busca abaixo de 1 s para agentes com até 5.000 trechos (GIN + filtro por `agent_id`)
**Constraints**:
- sem `CREATE EXTENSION`, porque o usuário do banco gerenciado pode não ter permissão;
- a API precisa subir sem nenhuma configuração de Elasticsearch;
- o banco de produção é compartilhado com o servidor antigo até a troca na Cloudflare, por isso a migração só adiciona coisas.

**Scale/Scope**: dezenas de agentes, centenas de arquivos e milhares de trechos

## Constitution Check

| Princípio | Status | Observação |
|---|---|---|
| I. Skills obrigatórias (dotnet-architecture) | ✅ | Entidade em `Domain/Models`, configuração Fluent no `AvaBotContext`, repositório em `Infra/Repository` com interface em `Infra.Interfaces/Repository`, DI em `Application/DependencyInjection.cs`, migração via `dotnet ef`. |
| II. Stack | ✅ | Só EF Core e Npgsql; remove uma dependência. |
| IV. Convenções de código | ✅ | file-scoped namespaces, `_camelCase` |
| V. Banco | ⚠️ justificado | snake_case, PK `knowledge_chunk_id` identity, `avabot_knowledge_chunks_pkey`. As FKs usam **Cascade**, seguindo o precedente `CascadeDeleteAgentRelations` (os arquivos já cascateiam do agente); veja Complexity Tracking. |
| VI. Segurança | ✅ | Remove um serviço exposto sem autenticação; a consulta é parametrizada e os termos são sanitizados (só letras e números). |
| VII. Variáveis de ambiente | ✅ | Remove `Elasticsearch__Url` e `ELASTICSEARCH_URL`; adiciona `KnowledgeBackfill:Enabled` (opcional, padrão `true`). |

Reavaliado após o design: sem novas violações.

## Project Structure

### Documentation (this feature)

```text
specs/021-postgres-knowledge-search/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/knowledge-chunk-repository.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
AvaBot.Domain/Models/KnowledgeChunk.cs                      # novo
AvaBot.Infra.Interfaces/Repository/IKnowledgeChunkRepository.cs  # substitui AppServices/IElasticsearchService.cs (+ ChunkData)
AvaBot.Infra/Repository/KnowledgeChunkRepository.cs         # novo (substitui AppServices/ElasticsearchService.cs)
AvaBot.Infra/Repository/KnowledgeSearchText.cs              # normalização e montagem do tsquery (puro, testável)
AvaBot.Infra/Context/AvaBotContext.cs                       # DbSet + Fluent
AvaBot.Infra/Migrations/*_AddKnowledgeChunks.cs             # nova
AvaBot.Infra/AvaBot.Infra.csproj                            # remove Elastic.Clients.Elasticsearch
AvaBot.Application/DependencyInjection.cs                   # Scoped repo + HostedService
AvaBot.Application/Services/KnowledgeChunkBackfillService.cs # novo
AvaBot.Application/Services/{Ingestion,KnowledgeFile,Agent,Search}Service.cs
AvaBot.API/Program.cs                                       # remove CreateIndexAsync
AvaBot.API/appsettings*.json, docker-compose.yml, .env.example
AvaBot.Calibration/{Program.cs,Local/LocalKnowledgeBase.cs}
AvaBot.Tests/**                                             # renomeia mocks; testes novos
deploy/k8s/avabot/{kustomization.yaml,api-configmap.yaml}   # remove elasticsearch.yaml
deploy/k8s/bootstrap/deployer-rbac.yaml, deploy/k8s/README.md
.github/workflows/deploy-prod.yml                           # remove rollout/diagnóstico do ES
README.md, docs/CALIBRATION.md
```

**Structure Decision**: Clean Architecture existente. O armazenamento dos trechos é acesso a dados, então sai de `AppServices` (serviço externo) e vai para `Repository`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| FK com `ON DELETE CASCADE` (constituição pede `ClientSetNull`) | Trecho órfão não tem sentido e quebraria FR-005. Os arquivos já cascateiam do agente (migração `CascadeDeleteAgentRelations`). A remoção explícita continua na aplicação. | Com `ClientSetNull`, os trechos ficariam com `knowledge_file_id` nulo e apareceriam na busca de arquivos já excluídos, inclusive quando a exclusão é feita pelo servidor antigo durante a transição. |
| Coluna `search_content` duplicando o texto normalizado | Ignorar acentos sem a extensão `unaccent`, cuja instalação pode ser negada no banco gerenciado. | `unaccent` dentro de coluna gerada exige uma função `IMMUTABLE` própria e `CREATE EXTENSION`. |
