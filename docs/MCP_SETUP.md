# MCP para assistentes de IA

> Como conectar o Claude, o Cursor, o VS Code ou outro assistente compatível com MCP (Model Context Protocol) à sua conta do AvaBot, para que ele crie, configure e inspecione os seus agentes por conversa.

**Created:** 2026-10-12
**Last Updated:** 2026-10-12

---

## O que é

O AvaBot expõe um **servidor MCP remoto** no mesmo endereço da API. Um assistente conectado a ele opera a sua conta: lista e cria agentes, ajusta o prompt, lê as conversas e os logs de consulta Power BI, faz perguntas de teste, configura datasets e schema do Power BI, registra o webhook do Telegram e mantém a base de conhecimento.

Duas regras valem sempre, por construção:

- **Nenhuma ferramenta exclui um agente.** A exclusão continua existindo só no painel.
- **Nenhuma ferramenta lê ou grava credenciais**: chave de API do modelo, Directory ID, App ID e segredo do Power BI, token do bot do Telegram, segredo do webhook e token do WhatsApp. As respostas trazem apenas "configurado / não configurado". Uma chamada que inclua um desses campos é recusada por inteiro e nada é gravado.

Não é preciso instalar nada na sua máquina: só o endereço do servidor e uma chave de acesso.

---

## 1. Criar a chave de acesso

1. Entre no painel e abra **Minha conta**.
2. Na seção **Assistentes de IA (MCP)**, dê um nome à chave (ex.: `Claude do notebook`) e clique em **Criar chave**.
3. Copie a chave exibida. **Ela aparece uma única vez**; depois de fechar o modal só é possível revogá-la e criar outra.

Cada chave tem o formato `avabot_mcp_` seguido de 43 caracteres. Crie uma chave por assistente ou por máquina: assim dá para revogar uma sem afetar as outras. O limite é de 20 chaves ativas por conta.

### Endereço do servidor

| Ambiente | Endereço |
|---|---|
| Produção | `https://avabot.net/api/mcp` |
| Desenvolvimento local | `http://localhost:5000/mcp` |

Toda requisição leva o cabeçalho `Authorization: Bearer <chave>`. O token do painel (JWT) **não** funciona no MCP, e a chave MCP **não** funciona nas rotas do painel.

---

## 2. Conectar o assistente

O modal de criação já mostra os trechos abaixo preenchidos com a sua chave.

### Claude Code

```bash
claude mcp add --transport http avabot https://avabot.net/api/mcp --header "Authorization: Bearer avabot_mcp_..."
```

Use `--scope user` para valer em todos os projetos. Confira com `claude mcp list`.

### Claude Desktop e claude.ai

Em **Configurações > Conectores > Adicionar conector personalizado**:

- Nome: `AvaBot`
- URL do servidor: `https://avabot.net/api/mcp`
- Cabeçalho: `Authorization: Bearer avabot_mcp_...`

O assistente pede confirmação na primeira vez que usa cada ferramenta.

### Cursor

Arquivo `.cursor/mcp.json` (projeto) ou `~/.cursor/mcp.json` (global):

```json
{
  "mcpServers": {
    "avabot": {
      "url": "https://avabot.net/api/mcp",
      "headers": { "Authorization": "Bearer avabot_mcp_..." }
    }
  }
}
```

### VS Code

Arquivo `.vscode/mcp.json` do workspace:

```json
{
  "servers": {
    "avabot": {
      "type": "http",
      "url": "https://avabot.net/api/mcp",
      "headers": { "Authorization": "Bearer avabot_mcp_..." }
    }
  }
}
```

### ChatGPT

Em **Configurações > Conectores > Criar**, informe o nome, a URL `https://avabot.net/api/mcp` e, em autenticação, o cabeçalho `Authorization: Bearer avabot_mcp_...` (modo desenvolvedor habilitado na conta).

### Primeira pergunta

Pergunte ao assistente: **"quem sou eu no AvaBot?"**. Ele deve responder com o nome e o e-mail da sua conta e o nome da chave usada. Se responder que a chave é inválida, confira se copiou a chave inteira e se o endereço termina em `/mcp`.

---

## 3. Ferramentas disponíveis

Todos os ids e listas são da conta dona da chave. Um agente de outra conta responde "Agente nao encontrado", igual a um inexistente.

| Grupo | Ferramenta | O que faz |
|---|---|---|
| Conta | `quem_sou_eu` | Nome, e-mail e chave em uso |
| Agentes | `listar_agentes` | Lista os agentes (sem prompt, com indicadores de credencial) |
| | `obter_agente` | Detalhes de um agente, incluindo o prompt |
| | `criar_agente` | Cria um agente (nome, prompt, descrição, modelo, coleta de dados, nome do bot) |
| | `atualizar_agente` | Atualização parcial; renomear regenera o slug |
| | `definir_agente_ativo` | Ativa ou desativa o agente |
| Conversas | `listar_sessoes` | Sessões paginadas, mais recentes primeiro, com canal e contagem de mensagens |
| | `listar_mensagens` | Mensagens de uma sessão em ordem, com papel visitante/agente |
| | `listar_consultas_bi` | Logs de consulta Power BI, com filtro `status` = success, error ou timeout |
| | `testar_agente` | Pergunta de teste com rastro completo; não cria sessão |
| Power BI | `obter_powerbi` | Ligado, credencial configurada, último teste, datasets |
| | `definir_powerbi_ativo` | Liga ou desliga (ligar exige credencial e schema) |
| | `testar_conexao_powerbi` | Teste de conexão por etapa |
| | `listar_workspaces_powerbi` | Workspaces e datasets disponíveis |
| | `listar_datasets_powerbi` | Datasets vinculados |
| | `vincular_dataset_powerbi` | Vincula um dataset |
| | `atualizar_dataset_powerbi` | Nome e descrição do dataset |
| | `desvincular_dataset_powerbi` | Remove o vínculo (destrutiva) |
| | `gerar_schema_powerbi` | Lê tabelas, colunas e medidas do Power BI |
| | `obter_schema_powerbi` | Devolve o schema gravado |
| | `descrever_schema_powerbi` | Grava descrições de tabelas, colunas e medidas |
| Canais | `obter_telegram` | Nome do bot, bot configurado, webhook registrado |
| | `registrar_webhook_telegram` | Registra o webhook com o token já informado no painel |
| | `obter_whatsapp` | Status da conexão do WhatsApp |
| Base | `listar_documentos` | Documentos da base com situação de processamento |
| | `adicionar_documento` | Adiciona um documento de texto (Markdown, até 10 MB) |
| | `reprocessar_documento` | Reprocessa um documento |
| | `remover_documento` | Remove um documento (destrutiva) |

Listas são paginadas (`page`, `pageSize`); o assistente pede as próximas páginas. As consultas Power BI feitas por perguntas de teste entram no log de consultas do agente, como no teste do painel.

---

## 4. O que o assistente não pode fazer

| Operação | Onde fazer |
|---|---|
| Excluir um agente | Painel, Agentes |
| Informar, trocar ou remover a chave de API do modelo | Painel, Agentes > (agente) > Configurações |
| Informar Directory ID, App ID ou segredo do Power BI | Painel, Agentes > (agente) > Power BI |
| Informar o token do bot do Telegram ou regenerar o segredo do webhook | Painel, Agentes > (agente) > Telegram |
| Parear o WhatsApp (QR code) ou desconectar | Painel, Agentes > (agente) > WhatsApp |
| Enviar arquivos binários para a base | Painel, Base de conhecimento (só `.md`) |
| Gerenciar contas, senhas e chaves de outras pessoas | Não existe |

Quando o assistente precisa de uma credencial, ele responde com a orientação de onde informá-la no painel.

---

## 5. Revogar uma chave

Em **Minha conta > Assistentes de IA (MCP)**, clique em **Revogar** na chave. A próxima requisição com ela recebe `401` imediatamente; não há cache. A chave continua na lista como "revogada", com a data. Trocar a senha do painel não afeta as chaves.

A lista mostra o **último uso** de cada chave (atualizado no máximo uma vez por minuto), útil para descobrir chaves esquecidas.

---

## 6. Auditoria

Cada ferramenta de escrita gera uma linha no log técnico da API:

```
MCP tool=atualizar_agente userId=7 mcpKeyId=3 agentId=12 sucesso=True durationMs=48
```

Ferramentas de leitura vão para o nível `Debug`. O conteúdo dos argumentos (prompt, documento) nunca é logado, nem o valor da chave.

---

## 7. Deploy

O servidor MCP usa Streamable HTTP sem estado: qualquer instância da API atende qualquer requisição, sem afinidade de sessão. As respostas de `tools/call` podem vir em `text/event-stream`, então o proxy que publica `https://avabot.net/api` precisa, para `/api/mcp`:

- repassar a resposta **sem buffer** (`proxy_buffering off` no nginx, ou equivalente);
- timeout de leitura de pelo menos **120 s**, porque uma pergunta de teste pode demorar o mesmo que no painel.

Sem isso o assistente vê a chamada "pendurar". Nenhuma variável de ambiente nova é necessária; a tabela `avabot_mcp_access_keys` é criada pela migração `AddMcpAccessKeys` na subida da API.

---

## 8. Depuração sem assistente

Com a coleção Bruno (`bruno/MCP/`): `Create Key` guarda a chave em `mcpKey`; `Tools List` e `Call Tool` mandam JSON-RPC bruto para `/mcp`.

Com `curl`:

```bash
curl -s http://localhost:5000/mcp \
  -H "Authorization: Bearer avabot_mcp_..." \
  -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"quem_sou_eu","arguments":{}}}'
```

Com o MCP Inspector: `npx @modelcontextprotocol/inspector`, transporte "Streamable HTTP", URL do servidor e o cabeçalho `Authorization`.

Respostas de erro de negócio vêm com `isError: true` e a mensagem em português no `content`. `401` com `{"sucesso":false,"mensagem":"Chave de acesso invalida ou revogada"}` significa chave ausente, errada, revogada ou conta inativa, sem distinção.
