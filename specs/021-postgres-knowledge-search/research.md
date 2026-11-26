# Research: 021-postgres-knowledge-search

## R1. Motor de busca textual

- **Decision**: full-text search nativo do PostgreSQL, com estas peças:
  - `tsvector` gerado e armazenado (`STORED`) com a configuração `portuguese`;
  - índice GIN;
  - consulta com `to_tsquery('portuguese', 'termo1 | termo2 | ...')`;
  - ordenação por `ts_rank` DESC e depois por `knowledge_file_id` e `chunk_index`, para ser determinística;
  - `LIMIT topK`.
- **Rationale**:
  - A busca atual é só textual: um `match` do Elasticsearch com operador OR sobre o analyzer `standard`, filtrado por `agent_id`.
  - O equivalente nativo usa OR entre os termos. `plainto_tsquery` e `websearch_to_tsquery` usam AND, que zeraria resultados de perguntas em linguagem natural.
  - O stemming `portuguese` melhora a cobertura de flexões ("horário"/"horários"), e as stopwords evitam o casamento por palavras como "o", "de" e "que".
  - O GIN atende a SC-003 com folga.
- **Alternatives considered**:
  - pgvector (busca semântica): fora do escopo; a busca atual não usa vetores.
  - `pg_trgm`: mede similaridade de grafia, não relevância.
  - Configuração `simple`: sem stemming nem stopwords; perguntas com "o que é" casariam com quase tudo.

## R2. Acentos e maiúsculas (FR-004)

- **Decision**: normalizar na aplicação. O texto vai para minúsculas (`ToLowerInvariant`), passa por `NormalizationForm.FormD` e perde as marcas `NonSpacingMark`. O resultado é gravado em `search_content`, que gera o `search_vector`. A pergunta passa pela mesma normalização antes de virar tsquery.
- **Rationale**: não depende da extensão `unaccent` (o usuário do banco gerenciado pode não ter `CREATE EXTENSION`) e o comportamento é idêntico nos dois lados. O stemmer `portuguese` funciona sobre o texto sem acento de forma consistente, porque a mesma forma é usada na indexação e na consulta.
- **Alternatives considered**: `unaccent` com função `IMMUTABLE` própria; exige extensão e uma migração com DDL privilegiado.

## R3. Sanitização da consulta

- **Decision**: os tokens são extraídos com `[\p{L}\p{N}]+`, sem duplicatas, limitados a 64, e unidos com ` | `. O valor vai como parâmetro SQL. Sem tokens, a busca retorna uma lista vazia sem consultar o banco.
- **Rationale**: elimina os operadores de tsquery (`&`, `!`, `:`, `(`) vindos do usuário, que causariam erro de sintaxe ou mudariam a semântica. Uma pergunta só com stopwords gera um tsquery vazio, o PostgreSQL emite um NOTICE e nada casa, como pede FR-003.

## R4. Acesso a dados

- **Decision**: o repositório `KnowledgeChunkRepository` (Scoped) usa o `AvaBotContext`.
  - Inserção: `AddRange` + `SaveChanges`, com `search_content` como shadow property.
  - Exclusão: `ExecuteDeleteAsync`.
  - Busca: `Database.SqlQuery<string>` interpolado (parametrizado).
- **Rationale**: o SQL de FTS fica explícito e previsível, sem depender da tradução LINQ de `Matches`/`Rank` sobre shadow property. O DbContext é Scoped e todos os consumidores também (a ingestão em background já cria o seu próprio scope).

## R5. Embeddings

- **Decision**: coluna `embedding real[]`, com mapeamento nativo de `float[]` no Npgsql.
- **Rationale**: FR-007 (manter os vetores) sem a dependência do pgvector. Uma migração futura pode converter para `vector(1536)`.

## R6. Migração dos dados existentes (FR-008)

- **Decision**: o `KnowledgeChunkBackfillService` (`BackgroundService`), na subida da API:
  - lista os arquivos `Ready` sem nenhum trecho;
  - reprocessa cada um, em sequência, com `IngestionService.ProcessFileAsync`, em um scope próprio por arquivo;
  - registra no log o total, quantos foram migrados e quantos falharam;
  - pode ser desligado por `KnowledgeBackfill:Enabled=false`.
- **Rationale**:
  - O conteúdo original já está em `avabot_knowledge_files.file_content`, então não é preciso acesso ao Elasticsearch antigo.
  - É idempotente: só pega arquivos sem trechos.
  - É automático no primeiro deploy e cobre arquivos enviados pelo servidor antigo durante a transição, que entram no próximo restart.
  - Falhas viram `ProcessingStatus.Error`, visível no painel, com reprocessamento manual.
- **Alternatives considered**:
  - `_reindex`/scroll do Elasticsearch antigo: exige rede até o servidor antigo e um script extra.
  - Endpoint manual: exige uma ação humana e autenticação.
  - Fazer o backfill dentro da migração EF: chamadas à OpenAI dentro da migração seriam frágeis.

## R7. Convivência com o servidor antigo (banco compartilhado)

- A migração só cria uma tabela; a API antiga ignora migrações que não conhece.
- Exclusões feitas no servidor antigo apagam o arquivo, e o `CASCADE` apaga os trechos no banco.
- Reprocessamentos feitos no servidor antigo durante a transição só atualizam o ES antigo. O efeito é aceitável, porque a troca na Cloudflare é feita logo após a validação.

## R8. Limpeza do cluster

`kubectl apply -k` não remove recursos que saíram do Kustomize. Depois do merge, o admin apaga:
- o StatefulSet, o Service e o PVC `data-avabot-elasticsearch-0`;
- o PV, cuja StorageClass é `retain`, e com ele o volume da DigitalOcean.

Isso fica documentado no `deploy/k8s/README.md`.
