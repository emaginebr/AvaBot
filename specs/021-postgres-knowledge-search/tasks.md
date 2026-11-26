# Tasks: Base de conhecimento no banco principal (sem Elasticsearch)

**Input**: `specs/021-postgres-knowledge-search/` (plan.md, spec.md, research.md, data-model.md, contracts/)
**Tests**: testes de unidade para a normalização e a montagem da consulta, além do ajuste dos mocks existentes; validação do SQL contra um PostgreSQL real no Docker do WSL.

## Phase 1: Setup

- [X] T001 Remover o pacote `Elastic.Clients.Elasticsearch` de `AvaBot.Infra/AvaBot.Infra.csproj`

## Phase 2: Foundational (bloqueia todas as histórias)

- [X] T002 Criar a entidade `KnowledgeChunk` em `AvaBot.Domain/Models/KnowledgeChunk.cs`
- [X] T003 Criar `IKnowledgeChunkRepository` + `ChunkData` em `AvaBot.Infra.Interfaces/Repository/IKnowledgeChunkRepository.cs` e apagar `AvaBot.Infra.Interfaces/AppServices/IElasticsearchService.cs`
- [X] T004 Mapear `KnowledgeChunk` (DbSet, Fluent, shadow `search_content`/`search_vector`, índices, FKs) em `AvaBot.Infra/Context/AvaBotContext.cs`
- [X] T005 Gerar a migração `AddKnowledgeChunks` em `AvaBot.Infra/Migrations/` (`dotnet ef migrations add`)
- [X] T006 [P] Criar `KnowledgeSearchText` (normalização + tsquery OR sanitizado) em `AvaBot.Infra/Repository/KnowledgeSearchText.cs`

## Phase 3: User Story 1 + 2 — busca e gestão de arquivos (P1)

**Goal**: agentes e painel usam os trechos do banco, com o mesmo comportamento.
**Independent Test**: enviar um arquivo, buscar um termo dele (com e sem acento), excluir o arquivo e buscar de novo.

- [X] T007 [US1] Implementar `KnowledgeChunkRepository` em `AvaBot.Infra/Repository/KnowledgeChunkRepository.cs` e apagar `AvaBot.Infra/AppServices/ElasticsearchService.cs`
- [X] T008 [US1] Registrar o repositório como Scoped em `AvaBot.Application/DependencyInjection.cs` (no lugar do singleton do ES)
- [X] T009 [P] [US2] Trocar o tipo em `AvaBot.Application/Services/{IngestionService,KnowledgeFileService,AgentService,SearchService}.cs` e ajustar os logs
- [X] T010 [US1] Remover a criação do índice de `AvaBot.API/Program.cs`
- [X] T011 [P] [US1] Adaptar `AvaBot.Calibration/Local/LocalKnowledgeBase.cs` e `AvaBot.Calibration/Program.cs`
- [X] T012 [P] [US1] Renomear os mocks nos testes de `AvaBot.Tests/**`
- [X] T013 [P] [US1] Testes de `KnowledgeSearchText` em `AvaBot.Tests/Infra/Repository/KnowledgeSearchTextTest.cs`

## Phase 4: User Story 3 — migração dos dados (P1)

**Goal**: os arquivos já processados voltam a ter trechos sem reenvio.
**Independent Test**: um banco com arquivos `Ready` sem trechos → após a subida, todos têm trechos ou ficam com `Error`.

- [X] T014 [US3] Criar `KnowledgeChunkBackfillService` (BackgroundService) em `AvaBot.Application/Services/KnowledgeChunkBackfillService.cs` e registrá-lo em `AvaBot.Application/DependencyInjection.cs` (`KnowledgeBackfill:Enabled`)
- [X] T015 [P] [US3] Testes do backfill em `AvaBot.Tests/Application/Services/KnowledgeChunkBackfillServiceTest.cs`

## Phase 5: User Story 4 — produção sem Elasticsearch (P2)

**Goal**: deploy no cluster sem nenhum componente de ES.
**Independent Test**: `kubectl kustomize` sem ES; pipeline sem passos de ES; API sobe sem configuração.

- [X] T016 [P] [US4] Remover a seção `Elasticsearch` de `AvaBot.API/appsettings*.json`
- [X] T017 [P] [US4] Remover o ES e o Kibana de `docker-compose.yml` e `ELASTICSEARCH_URL` de `.env.example`
- [X] T018 [P] [US4] Apagar `deploy/k8s/avabot/elasticsearch.yaml` e ajustar `deploy/k8s/avabot/{kustomization.yaml,api-configmap.yaml}` e `deploy/k8s/bootstrap/deployer-rbac.yaml`
- [X] T019 [P] [US4] Remover o rollout e o diagnóstico do ES em `.github/workflows/deploy-prod.yml`
- [X] T020 [P] [US4] Atualizar `deploy/k8s/README.md` (sem ES; limpeza dos recursos antigos), `README.md` e `docs/CALIBRATION.md`

## Phase 6: Polish

- [X] T021 `dotnet build` + `dotnet test AvaBot.Tests`; varredura de referências a Elasticsearch (SC-005)
- [X] T022 Validar a migração e a busca contra `postgres:17-alpine` no Docker do WSL (acentos, OR, stopwords, isolamento por agente, uso do GIN)
- [X] T023 `kubectl kustomize deploy/k8s/avabot` e validação YAML do workflow

## Dependencies

- Phase 2 → Phase 3 → Phase 4. Phase 5 é independente do código (pode correr em paralelo), mas só faz sentido publicar junto.
- T004 → T005 (a migração depende do mapeamento). T003 → T007/T009/T011/T012.

## Implementation Strategy

Entrega única (MVP = Phases 1–4, porque sem o backfill a produção perde a base). A Phase 5 vai no mesmo PR para destravar o deploy no cluster.
