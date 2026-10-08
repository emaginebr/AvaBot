# Quickstart: Busca Textual no Elasticsearch

## Conferir uma busca direta

1. Use `GET /agents/{id}/search?query={texto}&topK=5` com um agente que já tenha documentos indexados.
2. Consulte um termo que exista no conteúdo e confirme que os trechos correspondentes são retornados.
3. Consulte um termo inexistente e confirme que a lista retornada está vazia.
4. Consulte um agente diferente e confirme que nenhum trecho do primeiro agente é retornado.
5. Repita a busca sem uma chave OpenAI disponível; a busca direta deve continuar funcionando sobre os documentos já indexados.

## Conferir o fluxo de chat

1. Envie uma mensagem por um fluxo de chat que consulta a base de conhecimento.
2. Confirme que a recuperação textual pode ser feita sem embedding da pergunta.
3. Confirme separadamente que a geração da resposta continua usando o modelo de chat configurado, quando esse modelo estiver disponível.

## Limites desta mudança

- Documentos já indexados podem manter o campo `embedding`; a busca textual não usa esse campo.
- Ingestão e reprocessamento de arquivos permanecem inalterados e podem continuar exigindo OpenAI para gerar embeddings de trechos.
- Consultas com vocabulário diferente do conteúdo podem não encontrar documentos relacionados por significado.
- Nenhuma migração ou reindexação é necessária para aplicar a alteração da consulta.
