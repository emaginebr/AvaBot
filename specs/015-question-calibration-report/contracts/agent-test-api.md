# Contrato: `POST /agents/{id}/test` (estendido)

Autenticação: `[Authorize]` (inalterado). O caminho de execução é o mesmo do teste de agente do painel.

## Request

```json
{
  "query": "Considere todos os produtos de tilápia",
  "history": [
    { "role": "user", "content": "Qual foi o volume de exportação da tilápia em 2024?" },
    { "role": "assistant", "content": "Você quer considerar ..." }
  ]
}
```

- `query`: obrigatório. Vazio → `400` "O parametro 'query' e obrigatorio" (inalterado).
- `history`: opcional. Um item com `role` fora de `user` ou `assistant`, ou com `content` vazio → `400` "Historico invalido: ...".
- O histórico é cortado nas `Chat:MaxHistoryMessages` mais recentes; o número de itens removidos sai em `historyOmittedCount`.

## Response 200

```json
{
  "sucesso": true,
  "mensagem": "Operacao realizada com sucesso",
  "dados": {
    "searchQuery": "...",
    "searchResults": ["trecho 1", "trecho 2"],
    "systemPrompt": "...",
    "messages": [{ "role": "system", "content": "..." }, { "role": "user", "content": "..." }],
    "assistantResponse": "...",
    "powerBIQueries": [ /* AgentTestPowerBIQueryInfo, inalterado */ ],

    "chatModel": "gpt-4.1-mini",
    "powerBIAvailable": true,
    "powerBIDatasets": ["ABIPESCA - Comércio Internacional"],
    "maxQueryAttempts": 5,
    "historyOmittedCount": 0,
    "trace": {
      "error": null,
      "rounds": [
        {
          "number": 1,
          "messages": [
            { "role": "system", "content": "...", "toolCalls": null, "toolCallId": null },
            { "role": "user", "content": "Qual foi ...?", "toolCalls": null, "toolCallId": null }
          ],
          "toolsOffered": true,
          "toolChoiceNone": false,
          "finishReason": "tool_calls",
          "responseText": null,
          "toolCalls": [
            {
              "id": "call_abc",
              "name": "listar_schema",
              "argumentsJson": "{\"dataset\":\"abipesca\"}",
              "result": "Dataset: ...",
              "durationMs": 3
            }
          ],
          "inputTokens": 4120,
          "outputTokens": 18,
          "durationMs": 912
        }
      ]
    }
  }
}
```

Regras:
- `trace.rounds[i].messages` traz o conjunto **completo** enviado ao modelo naquela rodada, sem truncamento.
- `toolCalls[].result` é o texto exato devolvido ao modelo como `ToolChatMessage`.
- `inputTokens`/`outputTokens` vêm como `null` quando o provedor não informa.
- Agente sem ferramentas: uma única rodada, com `toolsOffered: false`.
- Os campos atuais ficam com a mesma forma e semântica; o painel não muda.

## Response 500 com resultado parcial

Quando a falha ocorre depois da primeira chamada ao modelo:

```json
{
  "sucesso": false,
  "mensagem": "<mensagem do erro>",
  "erros": [],
  "dados": { "...": "mesmo formato acima", "assistantResponse": "", "trace": { "error": "<mensagem>", "rounds": [ /* rodadas concluídas */ ] } }
}
```

Falhas antes da primeira chamada (agente sem chave OpenAI, erro na busca) continuam `500` sem `dados`. Agente inexistente continua `404`.
