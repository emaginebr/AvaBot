# Quickstart: MCP de gestão de agentes

**Feature**: 017-mcp-agent-management

## Configuração

Nenhuma variável de ambiente nova. A migração `AddMcpAccessKeys` é aplicada na subida da API pelo `StartupMigrator` (como desde a feature de migração automática). Para aplicar à mão:

```bash
dotnet ef database update --project AvaBot.Infra --startup-project AvaBot.API
```

Restore: o pacote `ModelContextProtocol.AspNetCore` 2.2.0 (e `ModelContextProtocol` 2.2.0 em `AvaBot.Tests.API`) já está no cache NuGet local; em CI o restore baixa do nuget.org.

**Deploy (produção)**: o proxy que publica `https://avabot.net/api` precisa repassar `/api/mcp` sem buffer de resposta (`proxy_buffering off` no nginx, ou equivalente) e com timeout de leitura de pelo menos 120 s, porque `tools/call` pode responder em `text/event-stream` e uma pergunta de teste pode demorar o mesmo que no painel. Sem isso o assistente vê a chamada "pendurar".

## Validação manual (cenários da spec)

1. **US1 (conectar)**: entrar no painel, abrir "Minha conta" → "Assistentes de IA (MCP)", criar a chave "Claude do notebook". O modal mostra a chave uma vez, o endereço (`http://localhost:5000/mcp` em dev) e os trechos de configuração. Fechar o modal: a chave não aparece mais; a lista mostra "nunca usada". Configurar um assistente (o caminho mais curto é o Claude Code):
   ```bash
   claude mcp add --transport http avabot http://localhost:5000/mcp --header "Authorization: Bearer avabot_mcp_..."
   ```
   Perguntar "quem sou eu no AvaBot" → nome e e-mail da conta. Voltar à lista: "último uso" preenchido. Revogar a chave; repetir a pergunta → o assistente relata que a chave não é mais válida. Criar uma chave, editar um caractere dela no cliente → mesma recusa, mesma mensagem.
2. **US2 (criar e editar)**: pedir "crie um agente chamado Suporte Loja que responda só sobre trocas e devoluções, coletando nome e e-mail". Conferir em `/admin/agents` que existe, com esses dados e sem chave de API. Pedir "deixe o tom mais formal" e "troque o modelo para gpt-4o-mini" → `atualizar_agente`; conferir no painel. Pedir para desativar e reativar. Pedir "exclua o agente Suporte Loja" → o assistente diz que não tem como e orienta usar o painel. Pedir "configure a chave de API sk-teste no agente" → recusa do guarda de segredos; no painel, o agente continua sem chave e com o prompt intacto.
3. **US2 cenário 7 / SC-004 (isolamento)**: criar uma segunda conta, uma chave para ela, e pedir ao assistente dessa conta `obter_agente` com o id de um agente da primeira → "Agente nao encontrado". Repetir com `listar_sessoes`, `listar_documentos`, `obter_powerbi`.
4. **US3 (logs e teste)**: num agente com conversas, pedir "o que perguntaram ontem para o agente X e onde ele respondeu mal" → o assistente usa `listar_sessoes` e `listar_mensagens`. Num agente com Power BI, pedir "liste as consultas com erro" → `listar_consultas_bi` com `status = error`. Pedir "teste a pergunta 'qual foi o faturamento de setembro'" → resposta com rastro; em `/admin/sessions` **não** aparece sessão nova; em `/admin/powerbi` → logs, a consulta do teste aparece (clarificação 2).
5. **US4 (Power BI e canais)**: num agente cujas credenciais BI foram salvas no painel, pedir "ligue o Power BI, liste os workspaces, vincule o dataset Faturamento, gere o schema e descreva as colunas de data em português" → sequência de ferramentas; conferir no painel. Pedir "me mostre a configuração do Power BI" → só ligado/credencial configurada/último teste, sem Directory ID, App ID ou segredo (nem mascarado). Pedir "grave o Directory ID 1234" → recusa. Num agente sem credencial, pedir para ligar o Power BI → orientação para informar no painel. Telegram: pedir a situação → nome do bot e webhook, sem token; pedir para registrar o webhook → ok. WhatsApp: pedir a situação → conectado ou não.
6. **US5 (base)**: pedir "adicione na base do agente Suporte Loja a política de trocas: ..." → `adicionar_documento`; em `/admin/knowledge/files` o documento aparece e passa a "pronto"; perguntar em `testar_agente` algo coberto pelo documento → a resposta usa o texto. Pedir para remover → some da lista. Colar um texto acima de 10 MB → recusa com o limite.
7. **FR-019 (auditoria)**: no log da API, cada escrita feita pelo assistente aparece como `MCP tool=atualizar_agente userId=... mcpKeyId=... agentId=... sucesso=true durationMs=...`, sem o conteúdo dos argumentos.
8. **SC-001**: uma pessoa que nunca viu a feature segue só o `docs/MCP_SETUP.md` e chega ao "quem sou eu" em menos de 5 minutos.

## Testes automatizados

```bash
dotnet test AvaBot.Tests
cd frontend && npm run lint && npm run build
```

Testes de API (precisam da API rodando e do `AvaBot.Tests.API/appsettings.json` local com `ApiSettings.Email`/`Password`):
```bash
dotnet test AvaBot.Tests.API
```

`McpServerTest` cria a própria chave via `POST /auth/me/mcp-keys`, conecta com o cliente do SDK em `{BaseUrl}/mcp`, roda o ciclo completo e revoga a chave no final.

## Depuração sem assistente

Com a coleção Bruno (`bruno/MCP/`): `Create Key` → `Tools List` → `Call Tool`. Ou com `curl`:

```bash
curl -s http://localhost:5000/mcp \
  -H "Authorization: Bearer avabot_mcp_..." \
  -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"quem_sou_eu","arguments":{}}}'
```

Também dá para usar o MCP Inspector (`npx @modelcontextprotocol/inspector`) apontando para o endpoint com o cabeçalho `Authorization`.
