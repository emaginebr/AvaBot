# Data Model: Busca Textual no Elasticsearch

## Persisted knowledge chunks

Esta feature não altera o documento já indexado nem o mapeamento Elasticsearch. Os campos existentes permanecem:

| Campo | Uso nesta feature |
|---|---|
| `agent_id` | Filtro obrigatório para limitar resultados à base do agente consultado. |
| `content` | Campo textual analisado usado para localizar trechos por correspondência lexical. |
| `embedding` | Vetor existente que permanece armazenado; a recuperação textual desta feature não o consulta. |
| Metadados do arquivo e índice do trecho | Permanecem inalterados e fora da projeção de resposta atual. |

## Search request

Consulta transitória que contém o identificador do agente, texto não vazio e limite `topK`. O limite padrão permanece 5. Nenhum embedding de consulta é armazenado ou calculado nesta etapa.

## Search result

Lista ordenada de conteúdos textuais dos trechos correspondentes, limitada por `topK`. Sem correspondência textual para o agente selecionado, o resultado é uma lista vazia.
