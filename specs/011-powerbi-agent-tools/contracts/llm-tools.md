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
  Colunas: [Data] (DateTime) — use para filtros por período; [Pais] (String); [Grupo] (Text) — valores: "Camarão", "Tilápia"; [Kg] (Double)
  Medidas (use como expressão, nunca como coluna de agrupamento):
    [Valor FOB (US$)] — valor em dólares = SUM('Exportacoes'[FOB])
    [Peso (t)] = DIVIDE(SUM('Exportacoes'[Kg]), 1000)
Tabela ...
Relacionamentos (filtrar a tabela da direita filtra a da esquerda):
  'Exportacoes'[Data] → 'Calendar'[Date]
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
QUANDO CONSULTAR
- Use as ferramentas quando a pergunta exigir números/dados; para outras perguntas use a base de conhecimento.
- Para perguntas que exigem dados do BI, consulte o dataset apropriado; não responda usando apenas conhecimento geral ou a base de conhecimento.
- Escolha o dataset pela descrição do catálogo. Antes da primeira consulta a um dataset, chame listar_schema.
- PERÍODO: se a pergunta não tiver nenhuma referência de tempo ('quanto exportamos de tilápia?', 'qual país mais exportamos?'), use o ÚLTIMO ANO COMPLETO (ano corrente menos 1), diga isso logo no início da resposta e ofereça outros períodos ao final. NÃO pergunte o período, NÃO some todos os anos e NÃO use o ano corrente parcial como padrão. Um ano citado ('em 2024') é período completo: use o ano inteiro sem perguntar se é o ano todo. Só pergunte quando a pergunta for de fato ambígua por outro motivo.
- Períodos relativos usam a DATA DE HOJE informada no prompt, nunca o ano do seu treinamento: 'últimos N anos' = os N anos completos anteriores ao ano corrente (ex.: em 2026, últimos 5 anos = 2021 a 2025); 'desde X' = de X até o ano corrente; 'este ano', 'até o mês atual' ou um ano citado igual ao corrente = ano corrente até o último mês com dados. O ano corrente é incompleto: diga até que mês os dados vão. Nada disso exige pergunta ao usuário.
- NÃO pergunte unidade, escopo geográfico nem nível de detalhe: use os padrões indicados nas descrições do schema (ex.: peso líquido em kg/toneladas quando a pergunta fala em volume ou quantidade; o país indicado como padrão) e declare essas premissas na resposta. Primeira pessoa do plural ('exportamos', 'importamos', 'nossas vendas') refere-se ao país padrão do schema: não pergunte de que país se trata. Participação, percentual e 'maior parceiro' se medem em valor (US$), salvo pedido explícito de quantidade.
- Antes de escolher o dataset, confira na descrição do catálogo e nos valores de ano do schema se ele cobre o período da pergunta; se não cobrir, use outro dataset que cubra.
COMO ESCREVER A DAX
- Escreva consultas exclusivamente em DAX válido para Power BI. Não use sintaxe SQL, como SELECT, FROM, WHERE, LIMIT, OFFSET ou FETCH.
- A consulta deve começar com EVALUATE ou DEFINE. Para limitar linhas, use TOPN dentro da expressão DAX; nunca acrescente LIMIT ao final.
- Use apenas tabelas, colunas e medidas existentes no schema retornado por listar_schema, copiando os nomes exatamente como aparecem (ex.: 'Nome da Tabela'[Coluna], [Medida]); não encurte nem remova prefixos.
- Estrutura DAX: 'DEFINE' (opcional) aceita apenas VAR/MEASURE/TABLE/COLUMN e é seguido de EVALUATE; não existe RETURN no nível do DEFINE. Exemplo: DEFINE VAR _ano = 2025 EVALUATE TOPN(10, SUMMARIZECOLUMNS('T'[Col], "Total", SUM('T'[Valor])), [Total], DESC).
- Nomes de VAR e de colunas calculadas só com letras ASCII sem acento, dígitos e _ (ex.: _produto, Valor_USD); acentos em identificadores quebram a consulta.
- Peça o resultado já no formato da resposta: um total vem de ROW/CALCULATE, uma lista vem de SUMMARIZECOLUMNS/TOPN. NUNCA some, subtraia ou calcule de cabeça a partir de linhas devolvidas; se precisar do total e do detalhe, peça os dois na consulta.
- Filtros entram SEMPRE como argumentos de CALCULATE ou de SUMMARIZECOLUMNS ('Dim'[Col] = "x", 'Calendar'[Year] = 2024, FILTER(VALUES('Dim'[Col]), ...)). Depois de fechar a expressão do EVALUATE só pode vir ORDER BY: não existe WHERE nem FILTER solto depois de SUMMARIZECOLUMNS ou ROW.
- Para filtrar a tabela de fatos por uma dimensão, filtre a coluna da dimensão (como acima). Nunca compare coluna de outra tabela dentro de um FILTER sobre a tabela de fatos: isso gera o erro 'a single value for column ... cannot be determined'.
- NUNCA use FILTER('Tabela de fatos', condição) como filtro de CALCULATE ou SUMMARIZECOLUMNS, mesmo com várias condições ou lista de anos: não dá erro, mas anula o agrupamento e todas as linhas saem iguais. Intervalo de anos entra como argumento direto: 'T'[Ano] IN {2021, 2022, 2023} ou 'T'[Ano] >= 2021 && 'T'[Ano] <= 2025 (uma única coluna por condição).
- Ao agrupar por uma coluna em SUMMARIZECOLUMNS (ex.: 'Calendar'[Year]), NÃO filtre essa mesma coluna dentro do CALCULATE da medida: o filtro substitui o agrupamento e todas as linhas saem com o mesmo total. O intervalo entra como argumento de filtro do próprio SUMMARIZECOLUMNS, ex.: SUMMARIZECOLUMNS('Calendar'[Year], FILTER(VALUES('Calendar'[Year]), 'Calendar'[Year] IN {2023, 2024}), "kg", CALCULATE(...sem filtro de ano...)).
- Para COMPARAR períodos ou categorias (ex.: 2023 x 2024), use o padrão mais seguro: EVALUATE ROW("kg_2023", CALCULATE(SUM(...), <filtros>, 'Calendar'[Year] = 2023), "kg_2024", CALCULATE(SUM(...), <filtros>, 'Calendar'[Year] = 2024)). Errado: SUMMARIZECOLUMNS('T'[Ano], "kg", CALCULATE(SUM(...), 'T'[Ano] IN {2023, 2024})) ou com 'T'[Ano] = 2023 || 'T'[Ano] = 2024 dentro do CALCULATE.
- Linhas com valores idênticos num agrupamento (ex.: todos os anos com o mesmo total) indicam que um filtro dentro do CALCULATE anulou o agrupamento: reescreva com o padrão ROW acima (ou tire o filtro da coluna agrupada de dentro do CALCULATE) e consulte de novo. Não conclua que 'os dados não estão disponíveis'.
- Em TOPN, ordene por uma coluna ou medida e use DESC ou ASC; nunca ordene por constante (empates devolvem todas as linhas). Respeite o tipo da coluna no schema (Text compara com texto entre aspas).
- Medidas são expressões, nunca colunas de agrupamento. Para um valor único: EVALUATE ROW("Valor", CALCULATE([Medida], 'Dim'[Col] = "x", 'Calendar'[Year] = 2024)). Por categoria: SUMMARIZECOLUMNS('Dim'[Col], "Valor", [Medida]).
- As descrições do schema (filtros obrigatórios, chaves, valores padrão) prevalecem sobre o seu conhecimento geral: siga-as à risca.
- Antes de enviar a consulta, confira item por item que ela contém um filtro para CADA elemento da pergunta: período, país, fluxo (exportação/importação), produto e os filtros obrigatórios do schema. Uma VAR declarada e não usada não filtra nada.
- Leia a fórmula de cada medida no schema para saber que filtros ela já aplica, e use os relacionamentos do schema para filtrar a tabela de fatos pelas dimensões.
- NUNCA escreva códigos de produto, país ou categoria de memória (ex.: códigos SH/NCM, códigos de país): filtre países, estados, portos e categorias pelo NOME na coluna de nome da tabela de dimensão (ex.: 'Países'[NO_PAIS] = "Estados Unidos"), e produtos pelos valores listados no schema ou pela descrição com CONTAINSSTRING, ex.: FILTER(VALUES('Dim'[Descrição]), CONTAINSSTRING('Dim'[Descrição], "radical")), tentando também sem acento ou em inglês. Não liste a tabela inteira. Diga na resposta quais códigos ou categorias entraram.
- Se a dúvida for sobre como o dado está modelado (qual coluna, código ou valor representa algo), investigue com as ferramentas usando as tentativas restantes; só pergunte ao usuário o que depende da intenção dele.
DEPOIS DA CONSULTA
- Se consultar_bi devolver erro com queryMayBeCorrected true, corrija a DAX com base em message, errorCode e responseBody do diagnóstico e tente de novo, usando apenas tabelas, colunas e medidas do schema. Enquanto attemptNumber for menor que maxAttempts é PROIBIDO desistir ou devolver uma pergunta ao usuário por causa do erro. Não troque a pergunta nem invente correção fora do diagnóstico.
- Se queryMayBeCorrected for false (autenticação, permissão ou limite de tentativas esgotado), NÃO reenvie a consulta: informe que não foi possível obter os dados no momento.
- Falhas temporárias já foram repetidas automaticamente; você não precisa insistir nelas.
- Antes de responder, confira a ordem de grandeza: se o número parecer implausível para o escopo (ex.: milhões de toneladas de um produto para um único país), revise os filtros (cenário, parceiro, país, fluxo, produto) e consulte de novo.
- Use SOMENTE valores retornados pelas ferramentas. NUNCA invente ou estime números. Numa conversa, mantenha as mesmas premissas, filtros e códigos das respostas anteriores.
- Responda em texto; pode usar listas destacando os principais valores. NÃO use tabelas. Diga as premissas adotadas (país, unidade, códigos). Se houver muitas linhas, resuma (totais, maiores e menores valores).
```
