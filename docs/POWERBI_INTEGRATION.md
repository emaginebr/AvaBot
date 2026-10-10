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
| `MaxToolCallsPerMessage` | 5 | chamadas de ferramenta por mensagem (protecao geral do loop de tools) |
| `MaxQueryAttempts` | 5 | execucoes de `consultar_bi` por mensagem: primeira, replays automaticos e DAX corrigida |
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

No schema completo, a geracao tambem guarda, para o agente:
- a formula DAX de cada medida (vinda de `INFO.VIEW.MEASURES()`), para ele saber que filtros a medida ja aplica;
- os relacionamentos entre tabelas visiveis (`INFO.VIEW.RELATIONSHIPS()`), para filtrar a tabela de fatos pelas dimensoes;
- os valores distintos das colunas de texto com ate 30 valores (cardinalidade lida em `COLUMNSTATISTICS()`, valores lidos numa unica consulta `UNION`), para filtrar pelo valor exato.

Esses tres itens sao opcionais: se alguma dessas consultas falhar, o schema basico e gerado do mesmo jeito. Schemas gerados antes desta versao nao os tem; use **Gerar schema** de novo.

Ao regerar, as descricoes escritas por voce sao preservadas por chave (tabela, tabela+coluna, tabela+medida). Se a geracao falhar, o schema anterior continua valendo e o erro aparece no card.

**Ver schema** abre o catalogo com busca por nome e edicao das descricoes de tabelas, colunas e medidas. A descricao do modelo aparece em cinza; a sua aparece abaixo. Salve antes de fechar.

**Remover** o ultimo dataset com schema desliga a flag Power BI automaticamente, e a mensagem informa isso.

### 3. Historico de consultas

Uma linha por **requisicao** ao Power BI, inclusive cada tentativa automatica, mais as chamadas de `listar_schema`. Cada registro traz pergunta do usuario, dataset, DAX executado, duracao, linhas, status e o diagnostico integral da falha (categoria, status HTTP, codigo, mensagem principal com todos os detalhes e o corpo original da resposta, com segredos redigidos). A coluna `error_message` e `text`, entao mensagens longas nao sao cortadas. Clique na linha para ver o DAX completo. Registros com mais de `QueryLogRetentionDays` sao removidos por um servico em segundo plano (verificacao a cada 24 h).

### Ligar a flag

O switch **Power BI ativo** mostra o aviso de exposicao antes de ligar: os dados dos datasets vinculados ficam acessiveis a qualquer usuario que converse com o agente, em **todos os canais** (web, Telegram, WhatsApp). Nao ha filtragem por usuario na v1 — a permissao e do aplicativo, nao de quem pergunta.

Ligar sem credenciais ou sem schema e bloqueado com a explicacao do que falta.

---

## Como o agente usa as ferramentas

### Guardas do executor (calibracao 015)

Antes de enviar uma consulta ao Power BI, `consultar_bi` aplica tres verificacoes locais. Nenhuma delas consome o limite de tentativas; todas devolvem um erro com `queryMayBeCorrected: true` para o modelo corrigir.

- **Schema obrigatorio**: `consultar_bi` so roda depois de `listar_schema` do mesmo dataset na mesma mensagem. Sem isso o modelo inventa nomes de tabela.
- **Literais validados**: filtros `'Tabela'[Coluna] = "x"` ou `IN {...}` (inclusive via `VAR`) sao conferidos contra a lista de valores da coluna, quando o schema a traz (colunas de texto com ate 30 valores). Um valor inexistente volta com a lista correta e, se o valor existir em outra coluna, a sugestao dela.
- **Linhas identicas**: um resultado com 2+ linhas em que todas as medidas repetem o mesmo numero recebe um campo `warning`, porque quase sempre e um filtro na coluna agrupada dentro do `CALCULATE`, que anula o agrupamento.

- `listar_schema(dataset)` devolve o schema salvo em texto compacto, sem chamar o Power BI.
- `consultar_bi(dataset, dax)` executa DAX **somente leitura** (precisa comecar com `EVALUATE` ou `DEFINE`, mesmo depois de comentarios) e devolve `{columns, rows, rowCount, truncated}`.
- O `dataset` e um enum com as chaves dos datasets **daquele agente**; uma chave de outro agente devolve erro, e o executor valida de novo.
- Em caso de erro de `consultar_bi`, o modelo recebe um diagnostico estruturado: `{error: {category, statusCode, errorCode, message, responseBody, attemptNumber, maxAttempts, queryMayBeCorrected}}`. A conversa nunca quebra por falha de ferramenta.
- classificacao da falha (`category`):
  - `dax_query` (normalmente HTTP 400, e rejeicoes locais como DAX sem `EVALUATE`/`DEFINE`) — vai ao modelo com `queryMayBeCorrected: true` para que ele corrija a consulta e tente de novo;
  - `transient_service` (HTTP 429, 5xx, timeout da consulta e falha de rede) — o proprio executor **repete a mesma DAX**, sem chamar o modelo no meio; com orcamento sobrando, 429 espera o `Retry-After` indicado e o resto usa backoff exponencial com jitter (comeca em ~1 s, teto de 30 s);
  - `authentication_or_permission` (401/403 e o `PowerBIEntityNotFound` de dataset PBIR) — **nao repete** e nao pede correcao de DAX, porque regerar a consulta nao muda credencial nem permissao;
  - `attempt_limit` — o orcamento da mensagem acabou e nenhuma nova requisicao e enviada.
- O orcamento e por mensagem e conta execucoes reais de `consultar_bi` (primeira + replays + DAX corrigida), com teto `MaxQueryAttempts` (padrao 5). `listar_schema` **nao** consome orcamento, e cancelamento do usuario nao gera retentativa.
- `MaxToolCallsPerMessage` continua valendo como protecao geral do loop de ferramentas; o teto efetivo de chamadas e dimensionado para nao cortar o orcamento BI depois da leitura de schema.
- O prompt ganha o bloco **DADOS DO POWER BI**: se faltar periodo/produto/indicador, o agente pergunta antes de consultar; numeros so saem das ferramentas; a resposta vem em texto ou lista, sem tabelas.

Cada consulta e gravada com a sessao e a pergunta que a originaram. Nem o client secret nem o access token aparecem em resposta, log ou historico.

---

## Limites e custos

- Limite de linhas por consulta antes de truncar: `MaxRows` (padrao 100).
- Rate limit do Power BI: 120 requisicoes/min por principal. O 429 agora e tratado com retentativa automatica da mesma DAX respeitando `Retry-After`, limitado por `MaxQueryAttempts` por mensagem — o que reduz o risco de estourar o limite, mas uma conversa com muitas correcoes de DAX consome mais cotas que antes.
- Cada mensagem com ferramenta gasta rodadas extras de LLM (ate `MaxToolCallsPerMessage`) e ate `MaxQueryAttempts` requisicoes ao Power BI. Cada tentativa que falha tambem grava uma linha no historico.
- O diagnostico integral (com corpo da resposta) e mais texto para o modelo do que a mensagem curta de antes; em erro de DAX isso e proposital, porque e o que permite corrigir a consulta.
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
