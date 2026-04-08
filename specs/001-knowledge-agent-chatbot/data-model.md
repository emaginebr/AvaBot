# Modelo de Dados — Knowledge Agent Chatbot

## Convenções do Projeto

| Aspecto | Convenção |
|---|---|
| Nomes de tabelas | snake_case plural (ex.: `agents`, `knowledge_files`) |
| Nomes de colunas | snake_case (ex.: `agent_id`, `created_at`) |
| Chave primária | `{entidade}_id`, tipo `bigint identity` |
| Constraint PK | `{tabela}_pkey` |
| Chave estrangeira | `fk_{pai}_{filho}` |
| Comportamento de exclusão | `ClientSetNull` (nunca Cascade) |
| Timestamps | `timestamp without time zone` |
| Strings | `varchar` com `MaxLength` definido |
| Booleanos | `boolean` com valor padrão |
| Status / Enums | `integer` |

---

## Entidades e Tabelas PostgreSQL

### 1. agents

Representa um agente de chatbot configurado. Cada agente possui um prompt de sistema próprio, controle de dados a coletar do usuário e um identificador único via slug.

| Coluna | Tipo | Restrições | Descrição |
|---|---|---|---|
| `agent_id` | `bigint` | PK identity, NOT NULL | Identificador único do agente |
| `name` | `varchar(260)` | NOT NULL | Nome de exibição do agente |
| `slug` | `varchar(260)` | NOT NULL, UNIQUE | Identificador amigável para URL |
| `description` | `text` | NULL | Descrição opcional do agente |
| `system_prompt` | `text` | NOT NULL | Prompt de sistema usado nas conversas |
| `status` | `integer` | NOT NULL DEFAULT 1 | Estado do agente (ver tabela de valores) |
| `collect_name` | `boolean` | NOT NULL DEFAULT false | Coleta nome do usuário antes de iniciar |
| `collect_email` | `boolean` | NOT NULL DEFAULT false | Coleta e-mail do usuário antes de iniciar |
| `collect_phone` | `boolean` | NOT NULL DEFAULT false | Coleta telefone do usuário antes de iniciar |
| `created_at` | `timestamp without time zone` | NOT NULL | Data e hora de criação |
| `updated_at` | `timestamp without time zone` | NOT NULL | Data e hora da última atualização |

**Constraint:** `agents_pkey` (PK em `agent_id`)
**Constraint:** `agents_slug_key` (UNIQUE em `slug`)

**Valores de `status`:**

| Valor | Significado |
|---|---|
| `1` | Ativo — o agente está disponível para atender sessões |
| `0` | Inativo — o agente está desabilitado e não aceita novas sessões |

---

### 2. knowledge_files

Armazena os arquivos de base de conhecimento vinculados a um agente. O conteúdo de cada arquivo é processado e indexado no Elasticsearch em chunks vetorizados.

| Coluna | Tipo | Restrições | Descrição |
|---|---|---|---|
| `knowledge_file_id` | `bigint` | PK identity, NOT NULL | Identificador único do arquivo |
| `agent_id` | `bigint` | FK → `agents`, NULL | Agente proprietário do arquivo |
| `file_name` | `varchar(500)` | NOT NULL | Nome original do arquivo enviado |
| `file_content` | `text` | NOT NULL | Conteúdo textual extraído do arquivo |
| `file_size` | `bigint` | NOT NULL | Tamanho do arquivo em bytes |
| `processing_status` | `integer` | NOT NULL DEFAULT 0 | Estado do processamento (ver tabela de valores) |
| `error_message` | `text` | NULL | Mensagem de erro, quando aplicável |
| `created_at` | `timestamp without time zone` | NOT NULL | Data e hora de criação |
| `updated_at` | `timestamp without time zone` | NOT NULL | Data e hora da última atualização |

**Constraint:** `knowledge_files_pkey` (PK em `knowledge_file_id`)
**Constraint:** `fk_agents_knowledge_files` (FK em `agent_id` → `agents.agent_id`, `ClientSetNull`)

**Valores de `processing_status` e transições de estado:**

| Valor | Estado | Descrição |
|---|---|---|
| `0` | `processing` | Arquivo recém-enviado, aguardando chunking e vetorização |
| `1` | `ready` | Processamento concluído; chunks disponíveis no Elasticsearch |
| `2` | `error` | Falha no processamento; detalhes em `error_message` |

**Diagrama de transições:**

```
[0: processing] ──sucesso──► [1: ready]
[0: processing] ──falha───► [2: error]
[2: error]      ──reprocessamento──► [0: processing]
```

> Nota: a coluna `error_message` deve ser preenchida somente quando `processing_status = 2` e limpa (NULL) ao reiniciar o processamento.

---

### 3. chat_sessions

Representa uma sessão de conversa entre um usuário e um agente. Dados de identificação do usuário (`user_name`, `user_email`, `user_phone`) são coletados somente quando o agente correspondente possui as respectivas flags habilitadas.

| Coluna | Tipo | Restrições | Descrição |
|---|---|---|---|
| `chat_session_id` | `bigint` | PK identity, NOT NULL | Identificador único da sessão |
| `agent_id` | `bigint` | FK → `agents`, NULL | Agente que atende a sessão |
| `user_name` | `varchar(260)` | NULL | Nome do usuário (se coletado) |
| `user_email` | `varchar(260)` | NULL | E-mail do usuário (se coletado) |
| `user_phone` | `varchar(50)` | NULL | Telefone do usuário (se coletado) |
| `started_at` | `timestamp without time zone` | NOT NULL | Início da sessão |
| `ended_at` | `timestamp without time zone` | NULL | Encerramento da sessão (NULL = sessão em aberto) |

**Constraint:** `chat_sessions_pkey` (PK em `chat_session_id`)
**Constraint:** `fk_agents_chat_sessions` (FK em `agent_id` → `agents.agent_id`, `ClientSetNull`)

---

### 4. chat_messages

Armazena cada mensagem trocada dentro de uma sessão de chat, tanto do usuário quanto do assistente.

| Coluna | Tipo | Restrições | Descrição |
|---|---|---|---|
| `chat_message_id` | `bigint` | PK identity, NOT NULL | Identificador único da mensagem |
| `chat_session_id` | `bigint` | FK → `chat_sessions`, NULL | Sessão à qual a mensagem pertence |
| `sender_type` | `integer` | NOT NULL | Remetente da mensagem (ver tabela de valores) |
| `content` | `text` | NOT NULL | Conteúdo textual da mensagem |
| `created_at` | `timestamp without time zone` | NOT NULL | Data e hora do envio |

**Constraint:** `chat_messages_pkey` (PK em `chat_message_id`)
**Constraint:** `fk_chat_sessions_chat_messages` (FK em `chat_session_id` → `chat_sessions.chat_session_id`, `ClientSetNull`)

**Valores de `sender_type`:**

| Valor | Significado |
|---|---|
| `0` | `user` — mensagem enviada pelo usuário |
| `1` | `assistant` — mensagem gerada pelo agente |

---

## Relacionamentos entre Entidades

```
agents (1) ──────────────────── (0..N) knowledge_files
   |                                    fk_agents_knowledge_files
   |
   └──── (1) ──────────────────── (0..N) chat_sessions
                                          fk_agents_chat_sessions
                                               |
                                               └──── (1) ────── (0..N) chat_messages
                                                                         fk_chat_sessions_chat_messages
```

- Um **agent** pode ter zero ou mais **knowledge_files** (sua base de conhecimento).
- Um **agent** pode ter zero ou mais **chat_sessions** (histórico de atendimentos).
- Uma **chat_session** pertence a exatamente um **agent** e contém zero ou mais **chat_messages**.
- Quando um **agent** é excluído, as chaves estrangeiras das tabelas filhas são definidas como `NULL` (`ClientSetNull`); os registros filhos não são removidos automaticamente.

---

## Índice Elasticsearch — Armazenamento Vetorial

O Elasticsearch é utilizado para armazenar e pesquisar os chunks vetorizados dos arquivos de conhecimento. Esta camada não é modelada no PostgreSQL.

### Estratégia de indexação

Os chunks podem ser armazenados em um único índice global com filtro por `agent_id`, ou em índices separados por agente no padrão `knowledge_chunks_{agent_id}`. A abordagem de índice único com filtro é recomendada para simplificar a operação.

**Nome sugerido do índice:** `knowledge_chunks`
*(ou `knowledge_chunks_{agent_id}` para isolamento por agente)*

### Mapeamento do documento

| Campo | Tipo Elasticsearch | Configuração | Descrição |
|---|---|---|---|
| `chunk_id` | `keyword` | NOT NULL | Identificador único do chunk (ex.: UUID) |
| `agent_id` | `keyword` | NOT NULL | Referência ao `agent_id` do PostgreSQL; usado como filtro de busca |
| `knowledge_file_id` | `keyword` | NOT NULL | Referência ao `knowledge_file_id` do PostgreSQL |
| `content` | `text` | analyzer: `standard` | Texto do chunk, indexado para busca BM25 (lexical) |
| `embedding` | `dense_vector` | `dims: 1536`, `similarity: cosine` | Vetor semântico gerado pelo modelo de embeddings |
| `chunk_index` | `integer` | NOT NULL | Posição sequencial do chunk dentro do arquivo de origem |

### Exemplo de mapeamento JSON

```json
{
  "mappings": {
    "properties": {
      "chunk_id": { "type": "keyword" },
      "agent_id": { "type": "keyword" },
      "knowledge_file_id": { "type": "keyword" },
      "content": { "type": "text", "analyzer": "standard" },
      "embedding": {
        "type": "dense_vector",
        "dims": 1536,
        "index": true,
        "similarity": "cosine"
      },
      "chunk_index": { "type": "integer" }
    }
  }
}
```

### Fluxo de indexação

1. Um arquivo é enviado e salvo em `knowledge_files` com `processing_status = 0` (processing).
2. Um job assíncrono divide o `file_content` em chunks de texto.
3. Cada chunk é vetorizado via modelo de embeddings (dimensão 1536).
4. Os documentos são indexados no Elasticsearch com os campos acima.
5. Após indexação bem-sucedida, `processing_status` é atualizado para `1` (ready).
6. Em caso de falha, `processing_status` é atualizado para `2` (error) e `error_message` é preenchido.

### Busca híbrida

A recuperação de contexto para o agente combina duas estratégias:

- **BM25 (lexical):** busca por palavras-chave no campo `content`.
- **kNN (semântica):** busca por similaridade de cosseno no campo `embedding`.

Ambas as buscas são sempre filtradas pelo `agent_id` correspondente à sessão ativa, garantindo isolamento de contexto entre agentes.
