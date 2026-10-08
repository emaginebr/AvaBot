# Contrato: Busca Textual da Base de Conhecimento

## Endpoint existente

### `GET /agents/{id}/search?query={texto}&topK={limite}`

- Mantém a autenticação e o envelope de resposta existentes do AvaBot.
- `query` é obrigatório e não pode ser vazio nem conter somente espaços; entrada inválida retorna `400`.
- `topK` mantém valor padrão 5 e limita a quantidade de trechos retornados.
- `200` retorna `Result<List<string>>`; nenhuma correspondência retorna lista vazia.
- A consulta exige correspondência textual no conteúdo e filtro pelo `agent_id` informado.
- A chamada de busca não solicita embedding nem invoca a API da OpenAI.
- Erros do Elasticsearch são apresentados como falhas da busca.

## Uso no chat

O chat continua solicitando os trechos ao serviço de busca e, após receber os resultados, pode invocar separadamente o modelo configurado para redigir a resposta. A retirada da OpenAI da busca não altera a chamada de geração conversacional.

## Indexação

O contrato de ingestão de arquivos e de indexação permanece inalterado nesta feature. Embeddings previamente indexados não são apagados nem consultados pela busca textual.
