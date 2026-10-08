# Research: Busca Textual no Elasticsearch

**Branch**: `013-lexical-search`  
**Date**: 2026-10-08

## Current Project Findings

- `SearchService.SearchAsync` atualmente gera um embedding da consulta usando `IOpenAIService.GenerateEmbeddingAsync(agentId, query)` e depois chama o serviço Elasticsearch com vetor, texto, agente e `topK`.
- `ElasticsearchService.HybridSearchAsync` executa uma consulta híbrida: filtro `agent_id`, `MatchQuery` no texto `content` e kNN no campo vetorial `embedding`.
- O índice guarda `agent_id`, `content`, metadados de arquivo/trecho e vetores. A ingestão gera e indexa esses vetores separadamente, antes das consultas.
- `ChatService` usa `SearchService` antes da conclusão de chat, tanto no teste quanto na conversa; retirar a chamada de embedding de `SearchService` remove a etapa OpenAI de recuperação nesses caminhos, mas o chat ainda pode chamar a OpenAI depois para redigir sua resposta.
- `AgentController` expõe a busca direta em `GET /agents/{id}/search`, exige texto não vazio e passa o `topK` recebido ao serviço.

## Decisions

### D1. Consultar somente o texto na etapa de recuperação

**Decision**: `SearchService` encaminhará `agentId`, texto e `topK` ao serviço Elasticsearch sem depender de `IOpenAIService`. O serviço Elasticsearch usará correspondência textual obrigatória em `content`, filtro obrigatório por `agent_id` e limite `topK`.

**Rationale**: Cumpre a spec: buscas diretas podem funcionar sem chave ou disponibilidade OpenAI, preservando a separação entre recuperar trechos e gerar a resposta conversacional.

**Alternatives considered**:
- Continuar busca híbrida (texto + kNN): rejeitada porque exige vetor da pergunta e dependência OpenAI em tempo de consulta.
- Substituir por embeddings locais ou inference no Elasticsearch: rejeitada porque a feature especifica busca textual e isso introduziria outro modelo/operacionalização.

### D2. Tornar a correspondência textual obrigatória

**Decision**: Remover a chamada kNN e garantir que o `MatchQuery` seja uma cláusula exigida para o documento, usando a forma mais direta da consulta lexical ou `must`/`minimum_should_match` explícito. Preservar o filtro `agent_id` e `Size(topK)`.

**Rationale**: A consulta atual combina filtro e `should`. Em Elasticsearch, quando bool contém `filter` e `should`, o padrão de `minimum_should_match` é zero; ao retirar kNN sem mudar a semântica, o `MatchQuery` poderá ser opcional e documentos do agente sem correspondência ainda poderão ser devolvidos. A spec exige lista vazia sem correspondência.

**Alternatives considered**:
- Deixar o `should` atual sem mínimo explícito: rejeitada pelo risco de retornar todos os documentos filtrados do agente.
- Descartar também o filtro por agente: rejeitada por violar isolamento dos resultados.

### D3. Manter índice e ingestão inalterados

**Decision**: Não alterar o mapeamento Elasticsearch, os vetores já indexados, `ChunkData`, `IngestionService` ou geração de resposta em `ChatService`.

**Rationale**: O escopo elimina OpenAI somente ao vetorizar consultas durante a recuperação. Manter a ingestão é compatível com a spec e evita reprocessamento ou migração do índice. Os vetores deixam de ser utilizados nesta consulta lexical, mas podem permanecer armazenados.

**Alternatives considered**:
- Excluir vetores e remover embeddings da ingestão: rejeitada como ampliação de escopo; não é necessária para retirar a chamada de embedding da consulta.
- Remover também geração OpenAI de resposta: rejeitada porque a spec mantém o chat como etapa separada.

## External References

- [Elastic: Match query](https://www.elastic.co/docs/reference/query-languages/query-dsl/query-dsl-match-query) — consulta full-text analisa o texto de entrada e procura correspondência lexical em campos textuais.
- [Elastic: Boolean query](https://www.elastic.co/guide/en/elasticsearch/reference/8.19/query-dsl-bool-query.html) — `should` tem padrão `minimum_should_match` zero quando bool também possui `must` ou `filter`.
- Código local: `AvaBot.Application/Services/SearchService.cs`, `AvaBot.Infra.Interfaces/AppServices/IElasticsearchService.cs`, `AvaBot.Infra/AppServices/ElasticsearchService.cs`, `AvaBot.Application/Services/ChatService.cs`, `AvaBot.Application/Services/IngestionService.cs` e `AvaBot.API/Controllers/AgentController.cs`.
