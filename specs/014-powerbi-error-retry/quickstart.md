# Quickstart: validar diagnóstico e retentativas do Power BI

## Configuração local

Em `AvaBot.API/appsettings.json`, seção `PowerBI`, configure:

```json
{
  "PowerBI": {
    "MaxQueryAttempts": 5
  }
}
```

O número conta execuções reais da consulta por mensagem, incluindo a primeira. Configuração ausente, inválida ou menor que um usa o padrão 5.

## Validação funcional

1. Ative Power BI para um agente com um dataset utilizável e envie uma pergunta que execute uma DAX inválida.
2. Confirme que o registro de falha no Histórico de consultas do Power BI contém pergunta, dataset, DAX, status e mensagem/detalhes completos.
3. Confirme que o modelo recebe código, mensagem e detalhes e, quando consegue corrigir a DAX, uma nova consulta é executada e registrada em outra entrada.
4. Provoque resposta transitória 429 com `Retry-After` e confirme que o cliente aguarda o período indicado e repete exatamente a mesma DAX; cada requisição que falhou deve ter sua própria entrada no histórico.
5. Provoque 5xx ou falha de rede e confirme repetição da mesma DAX com backoff exponencial e jitter.
6. Provoque 401/403 e confirme que não há repetição automática ou reenvio ao modelo como solicitação para corrigir DAX.
7. Configure limite 2, provoque falhas repetidas e confirme que há no máximo duas execuções da consulta, contando a inicial; leitura de schema não reduz esse orçamento.
8. Gere uma mensagem diagnóstica acima de 2.000 caracteres e confirme que modelo, banco, endpoint de histórico e tela administrativa recebem/mostram o conteúdo completo, exceto segredos redigidos.
9. Verifique que uma resposta HTTP 200 contendo erro estruturado nos resultados/tabelas é registrada e devolvida como falha, não como resultado vazio bem-sucedido.
10. Cancele a mensagem durante a espera/requisição e confirme que cancelamento do usuário não gera nova tentativa.

## Cobertura automatizada prevista

- Parser de `PowerBIClient`: envelopes no nível raiz, `results[].error`, `tables[].error`, detalhes aninhados, corpo não JSON, status/código/header `Retry-After` e ausência de truncamento.
- `PowerBIToolProvider`: transientes repetem a mesma DAX e gravam cada falha; erro de DAX volta completo ao modelo; autenticação/permissão não retentam; limite inclui inicial e retries; schema fora do contador; segredo continua redigido.
- Loop OpenAI streaming e não streaming: permitem até o orçamento BI mais as chamadas auxiliares necessárias e finalizam ao esgotar o limite.
- Persistência/API/histórico: `ErrorMessage` superior a 2.000 caracteres sobrevive à migração, paginação, DTO e renderização.
- Configuração: default 5 para ausente/inválida, valor explícito positivo respeitado.

Os cenários podem ser exercitados com respostas HTTP controladas nos testes, sem depender de uma credencial ou dataset real.
