# Integracao do Agente com Power BI

> Guia para habilitar o AvaBot a responder perguntas com dados reais do Power BI, por agente.

**Created:** 2026-10-07
**Last Updated:** 2026-10-07

---

## O que a feature faz

Cada agente guarda as proprias credenciais de *service principal* do Entra ID e uma lista de datasets. Para cada dataset o schema (tabelas, colunas com tipo e medidas) e gerado automaticamente por consultas DAX `INFO.VIEW.*` e guardado no banco.

Com a flag **Power BI ativo** ligada, o agente recebe duas ferramentas ao conversar — `listar_schema` e `consultar_bi` — e o proprio modelo decide quando consultar os dados. Com a flag desligada, o comportamento do chat e exatamente o anterior (so base de conhecimento), sem custo extra de latencia.

A flag nasce desligada para todos os agentes existentes.

---

## Pre-requisitos no Entra ID

1. **Aplicativo registrado** com um *client secret*. Anote tres valores (pagina *App registrations* > seu app):
   - **Directory (tenant) ID**
   - **Application (client) ID**
   - **Value** do client secret (o *Secret ID* nao e usado)

   Se o secret expirar, a autenticacao comeca a falhar com `authentication_failed`. A data de expiracao so aparece no portal (Certificates & secrets) nem na API do Power BI.

## Pre-requisitos no Power BI

2. **Admin portal** (> Tenants > Tenant settings), liberado para o aplicativo (ou para o grupo de seguranca dele):
   - *Service principals can use Fabric/Power BI APIs*
   - *Dataset Execute Queries REST API*

3. **No workspace**, de ao aplicativo o papel **Contributor**, ou conceda permissao **Build** em cada dataset.

   > **Armadilha comum:** com papel *Viewer*, o aplicativo autentica, lista workspaces e lista datasets, mas toda consulta DAX falha com **404 `PowerBIEntityNotFound`**, nao com 403. Isso nao e credencial quebrada. A tela de credenciais mostra esse cenario no passo `dataset:<nome>` do teste de conexao.

4. **Modelo PBIR/Direct Lake**: se o dataset veio de um `.pbix` publicado como PBIR, o endpoint legado `executeQueries` pode continuar devolvendo `PowerBIEntityNotFound` mesmo com Contributor. Nesse caso a leitura por essa API nao esta disponivel — vale testar o *Testar conexao* antes de prometer a integracao para o cliente.

---

## Configurar o backend

### Chave de criptografia dos segredos

Os client secrets sao gravados criptografados (AES-256-GCM) no banco. A chave e uma variavel de ambiente obrigatoria para usar a feature:

```env
POWERBI_SECRET_ENCRYPTION_KEY=<32 bytes em base64>
```

Para gerar:

```bash
openssl rand -base64 32
```

```powershell
[Convert]::ToBase64String((1..32 | % { Get-Random -Max 256 }) -as [byte[]])
```

O `docker-compose.yml` e o `docker-compose-prod.yml` ja repassam a variavel como `PowerBI__SecretEncryptionKey`.

> **Nao troque a chave depois de salvar credenciais:** os segredos ja gravados deixam de ser decodificaveis e cada consulta passa a falhar. Se precisar rotacionar, cadastre os segredos de novo.
>
> Sem a chave configurada, a API sobe normalmente, mas salvar ou usar credenciais falha com `PowerBI:SecretEncryptionKey não configurada`.

### Aplicar a migração

```bash
dotnet ef database update --project AvaBot.Infra --startup-project AvaBot.API
```

A migração `AddPowerBIIntegration` cria `avabot_agent_powerbi_configs`, `avabot_powerbi_datasets` e `avabot_powerbi_query_logs` e a coluna `powerbi_enabled` em `avabot_agents` (default `false`).

### Opções (`appsettings.json` > secao `PowerBI`)

| Chave | Padrao | Uso |
|---|---|---|
| `SecretEncryptionKey` | vazio | chave AES-256 em base64 (via env) |
| `MaxRows` | 100 | linhas maximas enviadas ao modelo por consulta |
| `MaxToolCallsPerMessage` | 5 | chamadas de ferramenta por mensagem |
| `QueryTimeoutSeconds` | 30 | tempo limite de cada consulta DAX |
| `QueryLogRetentionDays` | 30 | retencao do historico de consultas |
| `ApiBaseUrl` | `https://api.powerbi.com/v1.0/myorg` | endpoint REST |
| `AuthorityBaseUrl` | `https://login.microsoftonline.com` | endpoint do Entra ID |

---

## Usar a area Power BI no admin

Menu **Power BI** (com um agente selecionado na navbar). Tres abas:

### 1. Credenciais

Informe Tenant ID, Client ID e Client Secret e salve. O segredo nunca volta para a tela: aparece mascarado como `••••xxxx` (4 ultimos caracteres). Para trocar, digite o novo; para manter, deixe o campo em branco.

**Testar conexao** roda as etapas em ordem e para na primeira falha de autenticacao:

| Etapa | O que verifica |
|---|---|
| `auth` | token via client credentials no escopo `https://analysis.windows.net/powerbi/api/.default` |
| `workspaces` | `GET /groups` e os datasets visiveis |
| `dataset:<nome>` | `EVALUATE ROW("ok", 1)` em cada dataset ja cadastrado |

O resultado fica gravado como ultimo teste.

### 2. Datasets

**Adicionar dataset** lista workspaces e datasets nas credenciais atuais (se a listagem falhar, digite os GUIDs manualmente). O nome vira a *chave da ferramenta* (`comercio_internacional`), unica por agente.

A **descricao de negocio** e enviada ao modelo na descricao da ferramenta e e o que ele usa para escolher entre varios datasets — vale preencher.

**Gerar schema** executa `INFO.VIEW.TABLES()`, `INFO.VIEW.COLUMNS()` e `INFO.VIEW.MEASURES()` (ate 60 s, sincrono). Itens ocultos do modelo sao ignorados. Se as funcoes `INFO.VIEW.*` nao estiverem disponiveis, o sistema cai para `COLUMNSTATISTICS()` e o schema fica marcado como **Parcial** (sem medidas e sem tipos).

Ao regerar, as descricoes escritas por voce sao preservadas por chave (tabela, tabela+coluna, tabela+medida). Se a geracao falhar, o schema anterior continua valendo e o erro aparece no card.

**Ver schema** abre o catalogo com busca por nome e edicao das descricoes de tabelas, colunas e medidas. A descricao do modelo aparece em cinza; a sua aparece abaixo. Salve antes de fechar.

**Remover** o ultimo dataset com schema desliga a flag Power BI automaticamente, e a mensagem informa isso.

### 3. Historico de consultas

Uma linha por chamada de ferramenta (`listar_schema` e `consultar_bi`), com pergunta do usuario, dataset, DAX executado, duracao, linhas, status e erro. Clique na linha para ver o DAX completo. Registros com mais de `QueryLogRetentionDays` sao removidos por um servico em segundo plano (verificacao a cada 24 h).

### Ligar a flag

O switch **Power BI ativo** mostra o aviso de exposicao antes de ligar: os dados dos datasets vinculados ficam acessiveis a qualquer usuario que converse com o agente, em **todos os canais** (web, Telegram, WhatsApp). Nao ha filtragem por usuario na v1 — a permissao e do aplicativo, nao de quem pergunta.

Ligar sem credenciais ou sem schema e bloqueado com a explicacao do que falta.

---

## Como o agente usa as ferramentas

- `listar_schema(dataset)` devolve o schema salvo em texto compacto, sem chamar o Power BI.
- `consultar_bi(dataset, dax)` executa DAX **somente leitura** (precisa comecar com `EVALUATE` ou `DEFINE`, mesmo depois de comentarios) e devolve `{columns, rows, rowCount, truncated}`.
- O `dataset` e um enum com as chaves dos datasets **daquele agente**; uma chave de outro agente devolve erro, e o executor valida de novo.
- Em caso de erro, o texto vai ao modelo como `{error}` para que ele corrija a consulta ou avise o usuario. A conversa nunca quebra por falha de ferramenta.
- Ao atingir `MaxToolCallsPerMessage`, a rodada seguinte roda sem ferramentas, forcando a resposta final.
- O prompt ganha o bloco **DADOS DO POWER BI**: se faltar periodo/produto/indicador, o agente pergunta antes de consultar; numeros so saem das ferramentas; a resposta vem em texto ou lista, sem tabelas.

Cada consulta e gravada com a sessao e a pergunta que a originaram. Nem o client secret nem o access token aparecem em resposta, log ou historico.

---

## Limites e custos

- Limite de linhas por consulta antes de truncar: `MaxRows` (padrao 100).
- Rate limit do Power BI: 120 requisicoes/min por principal, sem retry automatico na v1 (um 429 vira erro de ferramenta).
- Cada mensagem com ferramenta gasta rodadas extras de LLM (ate `MaxToolCallsPerMessage`).
- O schema completo so entra no contexto quando o modelo chama `listar_schema`.

---

## Troubleshooting

| Sintoma | Causa provavel | O que fazer |
|---|---|---|
| `auth` falha com `authentication_failed` | tenant/client/secret errados ou secret expirado | confira os tres valores no portal; veja a data de expiracao em Certificates & secrets |
| `workspaces` vazio | aplicativo nao e membro de nenhum workspace | adicione o app ao workspace |
| `dataset:<nome>` falha com `PowerBIEntityNotFound` | papel Viewer, ou falta *Dataset Execute Queries REST API*, ou modelo PBIR | de Contributor ou Build; libere a tenant setting; teste outro dataset |
| Schema **Parcial** | `INFO.VIEW.*` indisponivel | use **Gerar schema** de novo quando a permissao for liberada; o parcial so tem tabelas e colunas |
| `PowerBI:SecretEncryptionKey não configurada` | env ausente ou chave com outro tamanho | gere 32 bytes em base64 e reinicie o container |
| Segredo nao decodifica apos deploy | chave de criptografia foi trocada | cadastre os segredos de novo |
| Resposta com numero errado | modelo estimou em vez de consultar | reforce a descricao de negocio do dataset e veja o DAX no Historico |

---

## Escopo da v1

Fora da v1: **RLS por usuario final** (quem conversa com o agente ve os mesmos dados) e edicao manual completa do schema. Ambos estao registrados em `specs/011-powerbi-agent-tools/spec.md`.
