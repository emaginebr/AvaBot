# Data Model: 021-postgres-knowledge-search

## KnowledgeChunk → `avabot_knowledge_chunks` (nova)

| Propriedade | Coluna | Tipo | Regras |
|---|---|---|---|
| KnowledgeChunkId | `knowledge_chunk_id` | `bigint` identity always | PK `avabot_knowledge_chunks_pkey` |
| AgentId | `agent_id` | `bigint` not null | FK `avabot_fk_agents_knowledge_chunks` → `avabot_agents`, Cascade |
| KnowledgeFileId | `knowledge_file_id` | `bigint` not null | FK `avabot_fk_knowledge_files_knowledge_chunks` → `avabot_knowledge_files`, Cascade |
| ChunkIndex | `chunk_index` | `integer` not null | ordem no arquivo (0..n-1); único por arquivo (`ix_avabot_knowledge_chunks_file_chunk`) |
| Content | `content` | `text` not null | texto original devolvido à busca |
| Embedding | `embedding` | `real[]` not null | vetor da ingestão (1536 dims hoje); não usado na busca |
| CreatedAt | `created_at` | `timestamp without time zone` not null | UTC |
| *(shadow)* SearchContent | `search_content` | `text` not null | `content` normalizado (minúsculas, sem acentos) |
| *(shadow)* SearchVector | `search_vector` | `tsvector` | `GENERATED ALWAYS AS (to_tsvector('portuguese'::regconfig, search_content)) STORED` |

Índices:
- `ix_avabot_knowledge_chunks_agent_id` (btree `agent_id`): filtro da busca e exclusão por agente;
- `ix_avabot_knowledge_chunks_file_chunk` (único `knowledge_file_id, chunk_index`): exclusão por arquivo, sem duplicação;
- `ix_avabot_knowledge_chunks_search_vector` (GIN `search_vector`).

Relações: `Agent 1─N KnowledgeChunk` e `KnowledgeFile 1─N KnowledgeChunk`. Não há navegações de coleção novas no domínio, para não carregar trechos com o arquivo.

Ciclo de vida:
1. a ingestão apaga os trechos do arquivo e depois insere os novos (reprocessamento sem duplicação);
2. ao excluir um arquivo, a aplicação apaga os trechos explicitamente, e o Cascade garante o resto;
3. ao excluir um agente, a aplicação apaga os trechos por agente, e o Cascade também cobre.

## KnowledgeFile (existente, sem mudança de schema)

Fonte do backfill: `file_content`, com `processing_status = Ready (1)` e nenhum trecho.
