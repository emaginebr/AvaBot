# Contract: Tools enviadas ao modelo (function calling)

**Feature**: 011-powerbi-agent-tools

As tools só são enviadas quando `agent.PowerBIEnabled == true` e existe ao menos um dataset utilizável (com `SchemaJson`). O enum `dataset` contém as `ToolKey` dos datasets utilizáveis do agente; o modelo não consegue referenciar datasets de outro agente (FR-018), e o executor valida a chave novamente.

## `listar_schema`

```json
{
  "type": "function",
  "function": {
    "name": "listar_schema",
    "description": "Retorna tabelas, colunas (com tipo) e medidas de um dataset do Power BI. Chame antes da primeira consulta a um dataset. Datasets disponíveis:\n- comercio_internacional: Comércio Internacional — Exportações e importações de pescado por país, produto e mês\n- estatisticas_brasileiras: ...",
    "parameters": {
      "type": "object",
      "properties": {
        "dataset": { "type": "string", "enum": ["comercio_internacional", "estatisticas_brasileiras"] }
      },
      "required": ["dataset"],
      "additionalProperties": false
    }
  }
}
```

**Resultado** (texto compacto, lido do `SchemaJson` salvo, sem chamar o Power BI):
```
Dataset: Comércio Internacional
Nomes já no formato de referência DAX: copie-os exatamente, com aspas e colchetes.
Tabela 'Exportacoes' — <userDescription>
  Colunas: [Data] (DateTime) — use para filtros por período; [Pais] (String); [Produto] (String); [Kg] (Double)
  Medidas: [Valor FOB (US$)] — valor em dólares; [Peso (t)]
Tabela ...
```

## `consultar_bi`

```json
{
  "type": "function",
  "function": {
    "name": "consultar_bi",
    "description": "Executa uma consulta somente leitura escrita exclusivamente em DAX válido (deve começar com EVALUATE ou DEFINE; não use sintaxe SQL como LIMIT) em um dataset do Power BI e retorna as linhas. Use TOPN dentro da expressão DAX para limitar linhas. Prefira agregações (SUMMARIZECOLUMNS) e TOPN; o resultado é limitado a 100 linhas.",
    "parameters": {
      "type": "object",
      "properties": {
        "dataset": { "type": "string", "enum": ["comercio_internacional", "estatisticas_brasileiras"] },
        "dax": { "type": "string", "description": "Consulta DAX completa" }
      },
      "required": ["dataset", "dax"],
      "additionalProperties": false
    }
  }
}
```

**Resultado (sucesso)**:
```json
{ "columns": ["Produto[Nome]", "[Valor FOB (US$)]"],
  "rows": [["Tilápia", 12345.67], ["Pescada", 9876.5]],
  "rowCount": 2, "truncated": false }
```

**Resultado (erro)**: o erro é devolvido ao modelo para que ele corrija a consulta ou avise o usuário, sem lançar exceção no chat.
```json
{ "error": "Mensagem de erro do Power BI (sanitizada)" }
```

## Regras do executor

1. Valida que `dataset` pertence ao agente. Se não pertencer, retorna `{error}`.
2. `consultar_bi` aceita apenas texto iniciado (após trim e comentários) por `EVALUATE` ou `DEFINE`. A API já é somente leitura; esta validação é uma defesa adicional.
3. Aplica timeout de `QueryTimeoutSeconds`. Em caso de estouro, `Status = Timeout`.
4. Trunca em `MaxRows` e marca `truncated: true`.
5. Grava um `PowerBIQueryLog` por chamada, inclusive `listar_schema`, com `DurationMs` ≈ 0.
6. Ao atingir `MaxToolCallsPerMessage`, a próxima chamada ao modelo é feita sem tools (`ToolChoice = none`), forçando a resposta final.

## Bloco adicionado ao system prompt (somente com tools)

```
DADOS DO POWER BI: Você tem acesso a ferramentas que consultam dados reais no Power BI.
- Use as ferramentas quando a pergunta exigir números/dados; para outras perguntas use a base de conhecimento.
- Para perguntas que exigem dados do BI, consulte o dataset apropriado; não responda usando apenas conhecimento geral ou a base de conhecimento.
- Se faltar informação necessária para a consulta (ex.: período, produto, indicador, unidade ou escopo geográfico), PERGUNTE ao usuário antes de consultar. Não invente nem assuma valores padrão, inclusive o ano mais recente.
- Antes da primeira consulta a um dataset, chame listar_schema.
- Escreva consultas exclusivamente em DAX válido para Power BI. Não use sintaxe SQL, como LIMIT, OFFSET, FETCH, SELECT ou FROM.
- A consulta deve começar com EVALUATE ou DEFINE. Para limitar linhas, use TOPN dentro da expressão DAX; nunca acrescente LIMIT ao final.
- Use apenas tabelas, colunas e medidas existentes no schema retornado por listar_schema.
- Use SOMENTE valores retornados pelas ferramentas. NUNCA invente ou estime números.
- Se a consulta falhar, informe que não foi possível obter os dados no momento.
- Responda em texto; pode usar listas destacando os principais valores. NÃO use tabelas. Se houver muitas linhas, resuma (totais, maiores e menores valores).
```
