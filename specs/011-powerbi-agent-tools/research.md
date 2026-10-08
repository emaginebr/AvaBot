# Research: Integração do Agente com Power BI

**Feature**: 011-powerbi-agent-tools | **Date**: 2026-10-07

## R1. Como o agente decide e executa consultas ao Power BI

- **Decision**: Usar *function calling* (tools) da OpenAI via SDK `OpenAI` 2.10 já instalado (`ChatCompletionOptions.Tools`, `ChatTool.CreateFunctionTool`, `ToolChatMessage`). O loop de tools fica dentro de `OpenAIService`, em um método novo. `StreamChatCompletionAsync` e `ChatCompletionAsync` permanecem intocados.
- **Rationale**: O modelo decide sozinho quando precisa de dados, escolhe o dataset e formula a consulta, sem classificador de intenção. Com a flag desligada nenhuma tool é enviada e o código executado é exatamente o atual (FR-014, SC-005).
- **Alternatives considered**:
  - Classificador de intenção (chamada extra ao LLM antes de cada mensagem): rejeitado por aumentar latência e custo de todas as mensagens, inclusive de agentes sem Power BI.
  - Injetar dados no prompt como o RAG: inviável, porque as consultas dependem de parâmetros da pergunta.
  - MCP server do Power BI: adiciona um processo e uma dependência externa sem ganho para este caso. A API REST basta.

## R2. Streaming com tool calls

- **Decision**: Usar `CompleteChatStreamingAsync` em todas as iterações. Os `ToolCallUpdates` (por `Index`: `ToolCallId`, `FunctionName`, `FunctionArgumentsUpdate`) são acumulados. Quando `FinishReason == ToolCalls`, o sistema executa as tools, adiciona `AssistantChatMessage(toolCalls)` e `ToolChatMessage(id, resultado)` e chama de novo. Os tokens de texto são repassados ao chamador assim que chegam (`yield return`). O máximo é de 5 chamadas de tool por mensagem (configurável); ao atingi-lo, a última chamada é feita sem tools (`ToolChoice = None`) para forçar a resposta final.
- **Rationale**: Mantém a experiência de streaming no web chat e é compatível com Telegram e WhatsApp, que já acumulam os tokens.
- **Alternatives considered**: Fazer as iterações de tool sem streaming e só a final com streaming. Rejeitado porque exigiria uma chamada extra quando o modelo responde direto, sem tool.

## R3. API do Power BI para consultas

- **Decision**: `POST https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/executeQueries`, com body `{"queries":[{"query":"<DAX>"}],"serializerSettings":{"includeNulls":true}}`. Resposta em `results[0].tables[0].rows` (objetos com chaves `Tabela[Coluna]` ou `[Medida]`).
- **Rationale**: Endpoint oficial, somente leitura (só aceita `EVALUATE`), funciona com service principal.
- **Limites conhecidos**: 1 query por chamada, máximo de 100k linhas ou 1M valores, 15 MB, 120 requisições/min por usuário. O sistema trunca para 100 linhas antes de enviar ao modelo (FR-020), e o prompt orienta o modelo a usar agregações e `TOPN`.
- **Permissões (verificado em 2026-10-07 com o tenant de teste)**: O aplicativo com papel *Viewer* no workspace autentica e lista workspaces e datasets, mas recebe `PowerBIEntityNotFound` em `executeQueries`, inclusive com `EVALUATE ROW("x",1)`. São necessários papel Contributor ou superior, ou permissão Build no dataset, além da configuração de tenant "Dataset Execute Queries REST API". O "Testar conexão" detecta esse caso e orienta o administrador.

## R4. Autenticação (Entra ID, client credentials)

- **Decision**: `POST https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token` com `grant_type=client_credentials`, `client_id`, `client_secret` e `scope=https://analysis.windows.net/powerbi/api/.default`. O token fica em `IMemoryCache` com chave `(tenantId, clientId)` e expira 5 min antes de `expires_in`. A chamada usa `HttpClient` nativo (named client), sem MSAL.
- **Rationale**: É uma única requisição HTTP e evita uma nova dependência NuGet, seguindo o padrão do `WppConnectService`. Validado manualmente com as credenciais de teste.
- **Alternatives considered**: MSAL (`Microsoft.Identity.Client`), rejeitado por ser uma dependência extra para um único fluxo simples.

## R5. Geração automática do schema

- **Decision**: Executar via `executeQueries` três consultas: `EVALUATE INFO.VIEW.TABLES()`, `EVALUATE INFO.VIEW.COLUMNS()` e `EVALUATE INFO.VIEW.MEASURES()`. Itens com `IsHidden = true` são filtrados (FR-013). **Fallback**: se as funções `INFO.VIEW.*` falharem por permissão ou versão, usar `EVALUATE COLUMNSTATISTICS()`, que exige apenas leitura e retorna tabela e coluna, mas não medidas nem tipos. Nesse caso o status do schema fica como "parcial", com aviso.
- **Rationale**: Usa a mesma permissão das consultas (sem XMLA/Premium) e o mesmo executor.
- **Pendente**: validar o formato exato das colunas retornadas (`[Name]`, `[Table]`, `[DataType]`, `[IsHidden]`, `[Description]`, `[Expression]`) quando a permissão de consulta do tenant de teste for liberada. O parser deve localizar as colunas pelo sufixo do nome, sem depender da posição.

## R6. Armazenamento do schema e preservação das descrições

- **Decision**: Guardar o schema em uma coluna `jsonb` (`schema_json`) do dataset, com a estrutura `{tables:[{name, description, userDescription, columns:[{name,dataType,description,userDescription}], measures:[{name,description,userDescription}]}]}`. Ao regerar, o sistema faz um merge por chave (`tabela`, `tabela+coluna`, `tabela+medida`) que mantém `userDescription` dos itens existentes (FR-012).
- **Rationale**: O schema é sempre lido e escrito inteiro (para a tool `listar_schema` e para a tela). Normalizar em três tabelas não traz benefício de consulta.
- **Alternatives considered**: Tabelas `powerbi_schema_tables` e `powerbi_schema_columns`, rejeitadas por exigirem mais CRUD e migração sem ganho funcional.

## R7. Proteção do segredo (client secret)

- **Decision**: Criptografar com AES-256-GCM (`System.Security.Cryptography.AesGcm`), com a chave de 32 bytes em base64 vinda de `PowerBI:SecretEncryptionKey` (env `PowerBI__SecretEncryptionKey`). O valor armazenado é `base64(nonce|tag|ciphertext)`. A API nunca devolve o segredo; devolve só `hasClientSecret` e a máscara dos 4 últimos caracteres, guardados em uma coluna separada `client_secret_hint`.
- **Rationale**: Não depende de key ring persistido, como exigiria o ASP.NET Data Protection, que em containers precisa de volume. A configuração por variável de ambiente segue o padrão do projeto.
- **Alternatives considered**: ASP.NET Core Data Protection (exige persistir chaves entre deploys) e Azure Key Vault (infraestrutura nova). Ambos rejeitados.

## R8. Desenho das tools (dinâmicas, sem código por dataset)

- **Decision**: Para um agente com flag ativa, o backend monta duas tools:
  1. `listar_schema` com parâmetro `dataset` (enum das chaves dos datasets do agente). Retorna o schema compacto em texto (tabela, colunas com tipo, medidas e descrições).
  2. `consultar_bi` com parâmetros `dataset` (enum) e `dax` (string). Retorna JSON `{columns, rows, rowCount, truncated}` ou `{error}`.

  A descrição de cada tool traz o catálogo "chave: nome e descrição de negócio" dos datasets. A chave é um slug gerado do nome (`tool_key`), único por agente.
- **Rationale**: Atende FR-016 a FR-018. O schema completo só entra no contexto quando o modelo precisa dele, o que economiza tokens com vários datasets.
- **Otimização opcional**: se a soma dos schemas do agente for menor que cerca de 6k caracteres, incluí-los direto no system prompt, poupando uma ida e volta. A decisão de implementar fica para depois de medir.

## R9. Instruções ao modelo (FR-022, FR-022a, FR-022b)

- **Decision**: Quando há tools, o sistema acrescenta ao system prompt um bloco "DADOS DO POWER BI" com estas regras:
  - usar apenas valores retornados pelas tools e nunca inventar números;
  - se faltar período, produto ou indicador, perguntar ao usuário antes de consultar;
  - consultar o schema antes da primeira consulta a um dataset;
  - preferir agregações e `TOPN`;
  - responder em texto e listas destacando os principais valores, sem tabelas;
  - em caso de erro, informar que não foi possível obter os dados.

  A instrução atual "Responda SOMENTE com base no contexto fornecido" é estendida para "contexto fornecido ou dados retornados pelas ferramentas".

## R10. Retenção dos registros de consulta (30 dias)

- **Decision**: Um `BackgroundService` (`PowerBIQueryLogCleanupService`) roda a cada 24 h e remove registros com `created_at < now - 30d` (dias configuráveis em `PowerBI:QueryLogRetentionDays`).
- **Alternatives considered**: Limpeza oportunista a cada insert (acopla escrita e limpeza) e `pg_cron` (exige extensão no banco). Ambas rejeitadas.

## R11. Constituição desatualizada

- **Observação**: `.specify/memory/constitution.md` lista .NET 8, React 18, Bootstrap e proíbe Zustand. O código real e o `CLAUDE.md` usam .NET 9, React 19, Tailwind 4 e Zustand 5. Seguindo o precedente das specs 009 e 010, os gates são avaliados contra a stack real. Recomenda-se emendar a constituição em uma tarefa separada.
