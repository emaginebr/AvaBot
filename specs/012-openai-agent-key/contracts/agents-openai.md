# Contrato: Credencial OpenAI de Agentes

Prefixo dos endpoints: API base do AvaBot. Todos os endpoints abaixo exigem autenticação administrativa (`[Authorize]`). As respostas seguem o envelope existente `Result<T>` com `sucesso`, `mensagem`, `erros` e `dados`.

## Criar ou atualizar agente

### `POST /agents` e `PUT /agents/{id}`

Adicionar ao corpo atual do agente:

```json
{
  "openAIApiKey": "sk-...",
  "removeOpenAIApiKey": false
}
```

Regras:

- Criação com `openAIApiKey` ausente, nulo ou vazio produz agente sem chave própria.
- Atualização com `openAIApiKey` ausente, nulo ou vazio e `removeOpenAIApiKey` falso mantém a credencial já salva.
- Atualização com `openAIApiKey` não vazio substitui a credencial depois de aparar espaços externos e protegê-la antes da persistência.
- `removeOpenAIApiKey: true` remove a credencial. Enviar simultaneamente remoção e nova chave é inválido e deve retornar `400`.
- Corpos e valores de chave não podem ser incluídos em logs.

O retorno `AgentInfo` pode incluir `hasOpenAIApiKey: boolean`. Não incluir chave, máscara parcial, dica, texto cifrado nem atributo que permita recuperar o valor. A mesma regra vale para `GET /agents`, `GET /agents/{slug}`, `GET /agents/{slug}/chat-config` e respostas de criação/atualização.

## Diagnosticar conexão

### `POST /agents/{id}/openai/diagnose`

Requisição:

```json
{
  "apiKey": "sk-..."
}
```

`apiKey` é opcional. Quando não informado ou vazio, usar a chave salva daquele agente; se não houver chave salva, retornar validação com orientação para informar uma chave. Quando informado, testar o valor atual do formulário sem persistir.

Resposta de diagnóstico concluído:

```json
{
  "sucesso": true,
  "mensagem": "Diagnóstico concluído",
  "erros": [],
  "dados": {
    "success": true,
    "message": "A chave autenticou com sucesso."
  }
}
```

- `dados.success` diferencia autenticação aprovada de falha de autenticação, conectividade, limitação do provedor ou outro erro diagnosticável.
- Uma chave inválida ou falha do provedor deve produzir mensagem segura em `dados.message`, sem retornar a chave ou o corpo bruto da resposta externa.
- O teste usa uma operação de autenticação sem geração de conteúdo e não grava resultado nem credencial.
- Agente inexistente retorna `404`; ausência de chave salva e ausência de chave na requisição retorna `400`.
- A credencial é aceita somente no corpo HTTPS autenticado e nunca deve ser registrada em logs.

## Consumo interno

- Chat síncrono, streaming e teste de agente recebem o identificador do agente para escolher sua chave.
- Busca semântica e ingestão de arquivos também resolvem a chave do agente antes de criar embeddings.
- Agente sem chave retorna erro de domínio compreensível; não há fallback para `OpenAI:ApiKey` global.
