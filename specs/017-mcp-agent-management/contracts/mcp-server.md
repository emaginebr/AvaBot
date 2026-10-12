# Contrato: servidor MCP (`/mcp`)

**Feature**: 017-mcp-agent-management | Base: `ModelContextProtocol.AspNetCore` 2.2.0, Streamable HTTP sem estado (research R1, R2)

## Endereço e transporte

| Item | Valor |
|---|---|
| Endpoint | `POST {API}/mcp` (em produção `https://avabot.net/api/mcp`; em desenvolvimento `http://localhost:5000/mcp`, conforme `VITE_API_URL`) |
| Transporte | Streamable HTTP (revisões 2025-11-25 e 2026-07-28 do protocolo), `Stateless = true`: sem `Mcp-Session-Id`, qualquer instância atende qualquer requisição |
| `GET /mcp` | `405` (sem canal de notificações em modo sem estado) |
| SSE legado (`/mcp/sse`, `/mcp/message`) | não mapeado |
| Cabeçalhos | `Content-Type: application/json`, `Accept: application/json, text/event-stream` (o cliente do SDK já envia); `MCP-Protocol-Version` conforme negociado no `initialize` |
| Autenticação | `Authorization: Bearer avabot_mcp_<43 caracteres>` em **toda** requisição, inclusive `initialize` e `tools/list` (modo sem estado não guarda a autenticação entre chamadas) |

## Autenticação (esquema `McpAccessKey`)

- Política do endpoint: `McpAccessKeyPolicy` = usuário autenticado **pelo esquema `McpAccessKey`**. O JWT do painel em `/mcp` responde `401`; a chave MCP em qualquer rota do painel responde `401` (os dois esquemas não se misturam).
- Sucesso: a requisição segue com `ClaimsPrincipal` (`sub`, `email`, `name`, `mcp_key_id`, `mcp_key_name`); `last_used_at` da chave é atualizado (≤ 1 vez por minuto).
- Falha (cabeçalho ausente, formato errado, chave inexistente, revogada ou dono inativo), sempre a mesma resposta:
  ```http
  HTTP/1.1 401 Unauthorized
  WWW-Authenticate: Bearer
  Content-Type: application/json

  { "sucesso": false, "mensagem": "Chave de acesso invalida ou revogada" }
  ```
  Nenhuma distinção entre os casos (US1 cenário 5). Nenhum dado no corpo.

## `initialize`

Resposta (campos relevantes):
```json
{
  "protocolVersion": "<negociada>",
  "capabilities": { "tools": { "listChanged": false } },
  "serverInfo": { "name": "avabot", "title": "AvaBot", "version": "<versão da API>" },
  "instructions": "Voce esta operando a conta de um usuario do AvaBot. Use quem_sou_eu para saber de quem. Regras: (1) nao existe operacao para excluir agente; a exclusao e feita pelo usuario no painel. (2) Voce nunca le nem grava credenciais: chave de API do modelo, Directory ID, App ID e segredo do Power BI, tokens do Telegram e do WhatsApp. As respostas trazem apenas 'configurado' ou 'nao configurado'; se precisar de uma credencial, peca ao usuario para informa-la no painel, em Agentes > (agente) > a aba correspondente. (3) Todos os ids e listas sao da conta do dono da chave; um agente de outra conta responde 'nao encontrado'. (4) Listas sao paginadas; peca as proximas paginas. (5) testar_agente nao cria conversa publica, mas as consultas Power BI do teste entram no log de consultas."
}
```

Sem `prompts` nem `resources` nesta versão. Só `tools`.

## `tools/list`

Devolve exatamente as 28 ferramentas de [mcp-tools.md](./mcp-tools.md), com `title`, `description` e `inputSchema` em português, e as anotações `readOnlyHint`, `idempotentHint` e `destructiveHint`. Nenhuma ferramenta de exclusão de agente; nenhum campo de segredo em nenhum schema (teste automatizado percorre todos os schemas procurando os nomes proibidos do research R5).

## `tools/call`

Fluxo por chamada (filtros na ordem):

1. **Guarda de segredos** (`McpSecretGuardFilter`): se `arguments` tiver, no primeiro nível, alguma chave da lista proibida (sem diferenciar caixa) — `openAIApiKey`, `removeOpenAIApiKey`, `apiKey`, `telegramBotToken`, `telegramWebhookSecret`, `whatsappToken`, `tenantId`, `directoryId`, `clientId`, `appId`, `clientSecret`, `secret`, `token`, `password`, `senha` —, devolve sem executar a ferramenta:
   ```json
   { "isError": true, "content": [{ "type": "text", "text": "Credenciais (chave de API do modelo, Directory ID, App ID e segredo do Power BI, tokens do Telegram e do WhatsApp) sao informadas apenas pelo usuario no painel, em Agentes > (agente) > Configuracoes, Power BI ou Telegram. Nenhum campo desta chamada foi gravado. Remova o campo '<nome>' e repita a operacao." }] }
   ```
2. **Execução com mapa de erros e auditoria** (`McpToolExecutionFilter`):
   - sucesso: `{ "isError": false, "content": [{ "type": "text", "text": "<JSON do DTO de saída>" }], "structuredContent": <DTO> }`;
   - erro de negócio: `{ "isError": true, "content": [{ "type": "text", "text": "<mensagem>" }] }` com as mensagens da tabela abaixo;
   - log: `MCP tool={name} userId={id} mcpKeyId={id} agentId={id|-} sucesso={true|false} durationMs={n}` (Information para ferramentas de escrita, Debug para leitura; nunca o conteúdo dos argumentos).
3. Erros de protocolo (ferramenta inexistente, JSON inválido, argumento obrigatório ausente ou de tipo errado) seguem o padrão do SDK: erro JSON-RPC `-32602`/`-32601`. Quando o nome não existe, a mensagem é do próprio SDK; o `instructions` já avisa que exclusão não existe.

### Mensagens de erro (`isError = true`)

| Situação | Texto |
|---|---|
| Agente inexistente ou de outra conta | `Agente nao encontrado` |
| Sessão inexistente ou de outra conta | `Sessao nao encontrada` |
| Documento inexistente ou de outro agente | `Documento nao encontrado` |
| Dataset inexistente ou de outro agente | mensagem atual do `PowerBIService` |
| Validação (nome vazio, prompt vazio, nome > 260, etc.) | mensagens do validator do painel, unidas por `; ` |
| Power BI sem credencial | `O Power BI deste agente ainda nao tem credencial. Peca ao usuario para informar Directory ID, App ID e segredo no painel, em Agentes > <nome> > Power BI, e tente de novo.` |
| Telegram sem token | `Este agente nao tem bot do Telegram configurado. Peca ao usuario para informar o token no painel, em Agentes > <nome> > Telegram.` |
| Documento acima do limite | `Documento excede o limite de 10MB` |
| Falha do modelo na pergunta de teste | mensagem da `AgentTestFailedException` + o resultado parcial (rodadas concluídas) serializado no mesmo `content` |
| Erro inesperado | `Erro interno ao executar <ferramenta>. Tente de novo; se persistir, avise o usuario.` (detalhe só no log do servidor) |

## Paginação

Ferramentas de lista aceitam `page` (≥ 1, padrão 1) e `pageSize` (padrão e máximo por ferramenta em [data-model.md](../data-model.md)); devolvem `{ items, total, page, maxPage }` (`PaginatedResult<T>`, o mesmo do painel) ou, para logs BI, `{ page, pageSize, total, items }` (`PowerBIQueryLogPageInfo`). `pageSize` acima do máximo é reduzido ao máximo, como o painel.

## Serialização

`JsonSerializerOptions` com `PropertyNamingPolicy = CamelCase`, `DefaultIgnoreCondition = WhenWritingNull`, enums como texto onde o DTO já faz isso (`schemaStatus`, `status` dos logs). Datas em UTC ISO-8601, como a API.

## Configuração do servidor (Program.cs)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...)                                                  // inalterado
    .AddScheme<AuthenticationSchemeOptions, McpAccessKeyAuthenticationHandler>("McpAccessKey", null);

builder.Services.AddAuthorization(o => o.AddPolicy("McpAccessKeyPolicy", p =>
    p.AddAuthenticationSchemes("McpAccessKey").RequireAuthenticatedUser()));

builder.Services.AddMcpServer(o =>
    {
        o.ServerInfo = new() { Name = "avabot", Title = "AvaBot", Version = <versão> };
        o.ServerInstructions = McpServerInstructions.Text;
    })
    .WithHttpTransport()                                                // Stateless = true (padrão)
    .WithTools<AccountTools>(McpJson.Options)
    .WithTools<AgentTools>(McpJson.Options)
    .WithTools<ConversationTools>(McpJson.Options)
    .WithTools<PowerBITools>(McpJson.Options)
    .WithTools<ChannelTools>(McpJson.Options)
    .WithTools<KnowledgeTools>(McpJson.Options)
    .WithRequestFilters(f =>
    {
        f.AddCallToolFilter(McpSecretGuardFilter.Create());
        f.AddCallToolFilter(McpToolExecutionFilter.Create());
    })
    .AddAuthorizationFilters();

app.MapMcp("/mcp").RequireAuthorization("McpAccessKeyPolicy");
```

CORS: `/mcp` não precisa de CORS (clientes não são navegadores); a política atual não muda.

## Compatibilidade

Nenhuma rota, resposta ou mensagem existente do painel muda. Mudanças aditivas: `GET /powerbi/{slug}/query-logs` ganha o parâmetro opcional `status` (`success` | `error` | `timeout`).
