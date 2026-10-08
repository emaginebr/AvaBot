# Research: diagnóstico completo e retentativas Power BI

**Feature**: `014-powerbi-error-retry`  
**Date**: 2026-10-08

## Decisão 1: preservar o diagnóstico completo da resposta

**Decision**: O cliente Power BI manterá o status HTTP, código de erro e o corpo integral da resposta quando houver falha. O diagnóstico estruturado exposto ao modelo e persistido no campo histórico conterá a mensagem principal e todos os detalhes disponíveis, além do corpo original integral para preservar o conteúdo não interpretado. Se a resposta não puder ser reconhecida, o corpo original será mantido como diagnóstico, em vez de substituído por uma mensagem genérica. Credenciais conhecidas serão redigidas antes de persistir ou encaminhar o diagnóstico.

**Rationale**: A extração atual só examina `error.code`, `error.message` e uma forma restrita de `error.details`; ela descarta o corpo não reconhecido e pode sobrescrever a mensagem principal com um detalhe. A API JSON documenta mensagens de erro no objeto raiz e erros em resultados/tabelas. A resposta deve ser interpretada tolerantemente para não perder estruturas aninhadas ou mensagens alternativas. O cliente atual usa `POST .../groups/{workspaceId}/datasets/{datasetId}/executeQueries`, que é a API JSON, não o endpoint Arrow.

**Alternatives considered**:
- Guardar apenas `error.message`: rejeitado porque pode não existir e não inclui os detalhes que explicam o erro.
- Guardar apenas o código HTTP: rejeitado porque não permite ao administrador nem ao modelo corrigir DAX.
- Guardar só o primeiro campo de detalhe reconhecido: rejeitado porque descarta outras mensagens e pode substituir a causa principal.

**Source**: [Power BI Execute Queries REST API](https://learn.microsoft.com/en-us/rest/api/power-bi/datasets/execute-queries)

## Decisão 2: classificar falhas antes de repetir

**Decision**: Erros de consulta/DAX são registrados e devolvidos ao modelo para que ele possa enviar DAX corrigida. Respostas temporárias do serviço — HTTP 429, HTTP 5xx, timeout da consulta e falha de transporte/rede — repetem automaticamente a mesma DAX, sem chamada de correção do modelo. Erros HTTP 401/403 ou falhas identificadas como autenticação/permissão terminam sem retentativa.

**Rationale**: A correção de DAX pode resolver erro 400, enquanto regenerar a consulta não corrige throttling, indisponibilidade ou conectividade. Para 429, respeitar `Retry-After` quando fornecido. Para 5xx e falhas de rede, usar backoff exponencial com jitter. O limite do produto é cinco execuções totais por mensagem, conforme especificação, e prevalece sobre recomendações externas de três ou quatro tentativas. Cada requisição Power BI que falhar fica como entrada própria no histórico.

**Alternatives considered**:
- Reenviar todas as falhas ao modelo para regenerar DAX: rejeitado para falhas temporárias, pois a consulta pode estar correta e a mudança da DAX não resolve a indisponibilidade.
- Repetir erros de autorização: rejeitado porque nova consulta não altera credenciais nem permissões.
- Repetir indefinidamente até sucesso: rejeitado por custo, latência e risco de loop.

**Sources**: [Power BI Execute DAX Queries best practices — erros e retries](https://learn.microsoft.com/en-us/power-bi/developer/execute-dax-queries-arrow/best-practices); [Power BI Execute Queries REST API](https://learn.microsoft.com/en-us/rest/api/power-bi/datasets/execute-queries)

**Scope note**: A página de best practices se refere ao endpoint Arrow. Deste material, o plano aproveita somente a orientação HTTP genérica sobre 429/5xx/backoff. O AvaBot continua usando o endpoint JSON; seus envelopes e detalhes de erro seguem o contrato específico de Execute Queries JSON.

## Decisão 3: separar limite de consultas do limite geral de ferramentas

**Decision**: Adicionar a configuração `PowerBI:MaxQueryAttempts`, padrão 5, e manter um contador de execuções efetivas de `consultar_bi` por toolset/mensagem. O contador inclui a execução inicial, retries automáticos com a mesma DAX e chamadas posteriores do modelo com DAX corrigida; não inclui `listar_schema`. O executor deve impedir qualquer requisição Power BI após esgotar o orçamento. O limite geral `MaxToolCallsPerMessage` continua como proteção independente, com capacidade efetiva suficiente para a consulta ao schema mais o orçamento de consultas BI.

**Rationale**: O limite atual `MaxToolCallsPerMessage` conta todas as ferramentas, inclusive `listar_schema`. Reutilizá-lo como limite de consulta faria a leitura de schema reduzir o número de tentativas BI disponíveis. Um único contador no toolset mantém o limite consistente entre o loop OpenAI e as repetições HTTP automáticas.

**Alternatives considered**:
- Reutilizar `MaxToolCallsPerMessage`: rejeitado porque mistura schema e consulta e pode permitir menos que cinco tentativas BI.
- Contar apenas chamadas de ferramenta do modelo: rejeitado porque várias repetições HTTP transitórias podem ocorrer dentro de uma chamada de ferramenta.

## Decisão 4: armazenar sem limite de 2.000 caracteres

**Decision**: Manter o campo histórico existente e seu contrato JSON, mas remover o truncamento no código e o limite de tamanho `varchar(2000)` no banco, convertendo a coluna para `text`. O valor persistido conterá o diagnóstico completo, incluindo o corpo original integral; ele fica associado ao registro da tentativa, que já guarda pergunta e DAX. A tela de histórico existente já exibe `errorMessage` sem truncamento próprio.

**Rationale**: O PostgreSQL já armazena a mensagem como texto semanticamente, mas o mapeamento atual impõe comprimento máximo 2.000 e o código trunca no executor. A mudança requer migração EF para atualizar o tipo/limite preservando linhas existentes, sem criar entidade ou endpoint novo.

**Alternatives considered**:
- Aumentar o limite para outro número fixo: rejeitado porque continua podendo cortar a resposta.
- Criar uma tabela de tentativas: rejeitado porque cada falha já tem um registro separado em `PowerBIQueryLog` e pergunta/DAX/status/duração são campos existentes.

## Decisão 5: tratar erros também em respostas HTTP 200

**Decision**: Além de status HTTP não bem-sucedidos, inspecionar os campos de erro documentados nas respostas JSON (resposta, resultado e tabela) antes de considerar uma consulta bem-sucedida. Erros encontrados em `results[].error` ou `tables[].error` serão convertidos para o mesmo diagnóstico completo e fluxo de falha.

**Rationale**: O contrato JSON expõe objetos de erro no nível do resultado e tabela. Um status 200 não deve ser suficiente para marcar sucesso se o conteúdo da resposta sinaliza um erro.

**Alternatives considered**:
- Considerar todo status 200 um sucesso: rejeitado porque o corpo pode conter um erro de consulta estruturado.

**Source**: [Power BI Execute Queries REST API — response schema](https://learn.microsoft.com/en-us/rest/api/power-bi/datasets/execute-queries)
