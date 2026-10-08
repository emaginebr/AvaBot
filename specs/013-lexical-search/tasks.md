# Tasks: Busca Textual no Elasticsearch

**Input**: Documentos de design em `/specs/013-lexical-search/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/search-api.md`, `quickstart.md`

**Organização**: Tarefas agrupadas pela história P1. A especificação não solicita a criação de testes automatizados; não foram incluídas tarefas de testes. Cada história tem critérios de validação independente para orientar a revisão manual.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparação do repositório.

O projeto backend, o cliente Elasticsearch e o índice existente já estão configurados. Nenhuma tarefa de setup ou nova dependência é necessária.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Preparar componentes compartilhados antes das histórias.

Não há alterações fundacionais compartilhadas nem dados persistidos para esta feature. As mudanças são locais ao fluxo da história US1.

---

## Phase 3: User Story 1 - Buscar conhecimento sem chamada à OpenAI (Priority: P1) 🎯 MVP

**Goal**: Recuperar trechos por correspondência textual no Elasticsearch sem gerar embedding da consulta, preservando o filtro por agente, o limite de resultados e o chat separado.

**Independent Test**: Consultar termos existentes em documentos indexados e receber trechos do agente correto; consultar um termo sem correspondência e receber lista vazia. A busca deve funcionar com OpenAI indisponível, sem alterar separadamente a geração de resposta do chat ou a ingestão.

### Implementation for User Story 1

- [X] T001 [US1] Alterar o contrato interno do Elasticsearch para receber `agentId`, texto da consulta e `topK`, removendo o vetor de consulta e renomeando `HybridSearchAsync` para uma operação textual em `AvaBot.Infra.Interfaces/AppServices/IElasticsearchService.cs`
- [X] T002 [P] [US1] Remover a dependência de `IOpenAIService` e a geração de embedding da consulta, chamando o contrato textual com agente, texto e `topK` em `AvaBot.Application/Services/SearchService.cs`
- [X] T003 [P] [US1] Remover a cláusula kNN, exigir correspondência de `MatchQuery` no conteúdo, preservar o filtro `agent_id`, o limite `topK`, a análise textual atual e a projeção dos trechos em `AvaBot.Infra/AppServices/ElasticsearchService.cs`

**Checkpoint**: Busca direta e recuperação usada pelo chat completam sem embedding de consulta; nenhuma correspondência retorna lista vazia e resultados permanecem limitados ao agente e ao `topK` solicitados.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Não há mudanças cross-cutting adicionais. A interface HTTP, a ingestão de documentos, o índice existente, o modelo de chat e a geração de resposta permanecem inalterados.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Estrutura e dependências já existem; sem tarefas.
- **Foundational (Phase 2)**: Não há trabalho fundacional separado.
- **User Story 1 (Phase 3)**: Implementar T001 antes de T002 e T003, pois os dois consumidores dependem do novo contrato do serviço Elasticsearch.
- **Polish (Phase 4)**: Nenhuma tarefa adicional.

### User Story Dependencies

- **US1 (P1)**: Sem dependência de outra história; é o MVP desta feature.

### Parallel Opportunities

- Depois de T001, T002 e T003 podem ser implementadas em paralelo: uma altera a orquestração da aplicação e a outra implementa a consulta Elasticsearch.

## Parallel Example: User Story 1

```text
Primeiro, concluir T001 para definir a interface textual.
Depois, em paralelo:
Trilha A: T002 — SearchService sem OpenAI
Trilha B: T003 — Elasticsearch com correspondência textual obrigatória
```

## Implementation Strategy

### MVP First (User Story 1)

1. Alterar a interface interna de consulta em T001.
2. Implementar T002 e T003 em paralelo.
3. Conferir o critério independente da US1 e os cenários do [quickstart.md](quickstart.md).

### Incremental Delivery

1. Entregar US1 como incremento único: busca textual isolada de OpenAI, com limite e filtro por agente preservados.
2. Manter chat, geração de respostas e ingestão de documentos nos fluxos existentes.

## Status de execução (2026-10-08)

T001–T003 concluídas.

- `IElasticsearchService.HybridSearchAsync(agentId, float[], queryText, topK)` → `TextSearchAsync(agentId, queryText, topK)`.
- `SearchService` perdeu a dependência de `IOpenAIService` (construtor de 2 → 1 parâmetro); a recuperação não chama mais a OpenAI.
- `ElasticsearchService.TextSearchAsync` removeu o bloco `.Knn(...)` e moveu o `MatchQuery("content")` de `should` para `must`, por D2 — com `filter` + `should`, o `minimum_should_match` padrão é zero e a busca devolveria todos os chunks do agente.
- Contrato `search-api.md` ("erros do Elasticsearch são apresentados como falhas da busca"): `IsValidResponse == false` deixava de retornar **lista vazia**, idêntico a "não achou"; agora lança com mensagem própria. Um vazio válido continua vazio.

Verificação: `dotnet build AvaBot.sln` 0 erros; `dotnet test AvaBot.Tests` **141 aprovados, 0 falhas** (os 140 anteriores adaptados ao contrato novo, +1 teste cobrindo falha ≠ lista vazia). `README.md` atualizado: dizia "hybrid search (kNN + BM25)" no Overview e na lista de Features.

Não executado: a query DSL resultante não foi validada contra um Elasticsearch real (sem servidor neste ambiente). A construção `Must(new Query[] { ... })` é a mesma API já usada para `Filter` no mesmo método.

Fora do escopo, mas afetado pela mudança: `agent_input/ava/docs/emagine-avachat.md` (4 trechos, versionado) ainda descreve "busca híbrida kNN + BM25" e o fluxo RAG com embedding da usuário — é conteúdo de página de produto, não corrigi sem você pedir. O mesmo vale para o empty-state da tela "Busca na Base", que ainda mostra "Nenhum resultado encontrado" quando a busca falha (a falha agora chega como 500 com mensagem, mas a página não tem estado de erro).
