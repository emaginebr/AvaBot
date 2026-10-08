# Contrato interno: erros e retentativas de consultas Power BI

**Feature**: `014-powerbi-error-retry`

Este contrato descreve o resultado de erro retornado pela ferramenta interna `consultar_bi` ao loop de ferramentas do modelo. O endpoint administrativo de histórico mantém seu formato existente e continua expondo `errorMessage` como texto.

## Resultado de erro para o modelo

```json
{
  "error": {
    "category": "dax_query",
    "statusCode": 400,
    "errorCode": "DatasetExecuteQueriesError",
    "message": "Mensagem principal completa. Detalhe 1. Detalhe aninhado 2.",
    "responseBody": "<corpo HTTP original integral quando necessário para preservar detalhes não interpretados>",
    "attemptNumber": 2,
    "maxAttempts": 5,
    "queryMayBeCorrected": true
  }
}
```

- `category` identifica `dax_query`, `transient_service`, `authentication_or_permission` ou `attempt_limit`.
- `statusCode` e `errorCode` são preenchidos quando fornecidos pela API; falhas de rede não possuem resposta HTTP.
- `message` contém a mensagem principal e todos os textos de detalhe disponíveis, sem truncamento. Se não houver texto interpretável, descreve que não há mensagem legível e preserva o corpo original em `responseBody`.
- `responseBody` contém o corpo de resposta integral quando recebido. Segredos conhecidos são redigidos antes do envio ao modelo ou persistência.
- `attemptNumber` conta execução inicial e todas as repetições automáticas; `maxAttempts` é o limite configurado.
- `queryMayBeCorrected` é verdadeiro somente para erros de consulta que podem ser corrigidos alterando a DAX e quando resta orçamento.

O resultado de sucesso mantém o contrato atual:

```json
{ "columns": ["..."], "rows": [["..."]], "rowCount": 1, "truncated": false }
```

## Classificação e fluxo

| Falha | Ação | Consome tentativa | Mensagem ao modelo |
|---|---|---:|---|
| Erro HTTP de DAX/consulta (normalmente 400) | Registra a falha e retorna diagnóstico completo; modelo pode emitir DAX corrigida | Sim | Antes da possível DAX corrigida |
| HTTP 429 com orçamento disponível | Espera `Retry-After` quando fornecido e repete a mesma DAX | Sim por execução HTTP | Só se o orçamento acabar |
| HTTP 5xx, timeout ou falha de rede com orçamento disponível | Backoff exponencial com jitter e repete a mesma DAX | Sim por execução HTTP | Só se o orçamento acabar |
| HTTP 401/403 ou falha de permissão/autenticação | Não repete | A chamada inicial conta | Diagnóstico final, sem sugerir correção da DAX |
| Limite esgotado | Não chama o Power BI novamente | Não há execução adicional | Devolve diagnóstico final com `queryMayBeCorrected: false` |

Cada tentativa HTTP que falhar cria uma entrada individual no histórico, contendo pergunta, dataset, DAX, status, duração e diagnóstico integral. Cancelamento explícito da mensagem pelo usuário não dispara retry.

## Compatibilidade da API Power BI

O cliente usa a API REST JSON `executeQueries`. O parser deve reconhecer o erro da raiz da resposta e os envelopes de erro presentes nos resultados ou tabelas, além de detalhes aninhados. Um status HTTP 200 não deve ser considerado sucesso quando o corpo indicar erro. Resposta não JSON ou envelope desconhecido deve preservar o corpo original como diagnóstico.
