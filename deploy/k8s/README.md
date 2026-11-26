# AvaBot no Kubernetes (DigitalOcean)

Cluster DOKS `do-nyc2-emagine-k8s`, namespace `avabot`. Tráfego: **Cloudflare** (TLS público,
proxy) → **Cloudflare Tunnel compartilhado** `emagine-k8s` (pods `cloudflared` no namespace
`cloudflared`, conexão de saída, sem IP público nem Load Balancer) → **ingress-nginx compartilhado**
(interno, Service ClusterIP, HTTP) → Services `avabot-api` (8080) e `avabot-web` (80). O
PostgreSQL é externo ao cluster e guarda também a base de conhecimento (tabela
`avabot_knowledge_chunks`, busca textual nativa; feature 021). Não há Elasticsearch.

**WhatsApp fora do ar**: o WPP Connect não roda no cluster (foi retirado da infra do
`emagine-deploy`). A API sobe normalmente; as telas de WhatsApp mostram erro de conexão até o WPP
Connect voltar (ver [WhatsApp](#whatsapp)). Chat web, widget, Telegram, Power BI e MCP funcionam.

O deploy antigo (SSH + `docker compose` no servidor Docker, mais o build do frontend no
`emagine-deploy`) foi substituído por esta pipeline: cada merge na `main` publica a API e o
frontend no cluster.

| Pasta | Conteúdo | Quem aplica |
|---|---|---|
| [`bootstrap/`](bootstrap/) | namespace `avabot` e a credencial restrita da pipeline (ServiceAccount `avabot-deployer`) | admin do cluster, uma vez |
| [`avabot/`](avabot/) | API, frontend, Ingress e ConfigMap via Kustomize | pipeline **Deploy Production** (`kubectl apply -k`) |

A entrada compartilhada (ingress-nginx e cloudflared) é infra do **cluster** e já existe; não é
mantida neste repositório.

Nenhum arquivo daqui contém credenciais. Os Secrets `avabot-api-secrets` e `ghcr-pull` são
criados pela pipeline a partir dos secrets do GitHub; o job `manifests` confere a cada execução que
nenhum Secret com dados foi versionado. O token do túnel (Secret `cloudflared-token`, namespace
`cloudflared`) é credencial do cluster e não vai para o GitHub do projeto.

## Arquitetura

```
cliente → Cloudflare (HTTPS, Always Use HTTPS, Redirect Rule do www)
        → túnel emagine-k8s → pods cloudflared
        → http://ingress-nginx-controller.ingress-nginx.svc.cluster.local:80
        → Ingress avabot (/api → avabot-api:8080, / → avabot-web:80)
        → Ingress avabot-stream (/api/ws WebSocket do chat, /api/mcp; sem buffer, timeout 1 h)
avabot-api → PostgreSQL externo (dados e base de conhecimento)
```

- Sem TLS no Ingress: o HTTPS termina na Cloudflare.
- `/api` chega à API **sem rewrite**: a API usa `PathBase=/api` (ConfigMap) e continua aceitando
  os caminhos sem prefixo nas chamadas internas.
- A API confia em `X-Forwarded-For`/`X-Forwarded-Proto` (o Service é ClusterIP: só o ingress fala
  com ela), então enxerga o IP real do cliente.
- CORS aberto (`*`) na API pelas anotações do Ingress, como no nginx antigo: o widget
  (`https://avabot.net/widget.js`) é embutido em sites de terceiros.
- Upload de arquivos até 100 MB (`proxy-body-size`).
- Health: API `GET /api/health` (inclui o banco); frontend `/healthz`.
- A Cloudflare corta requisições HTTP sem resposta por mais de ~100 s (erro 524).

## Secrets

| Nome no GitHub | Onde | Uso |
|---|---|---|
| `KUBECONFIG` | environment `production` | kubeconfig da ServiceAccount `avabot-deployer` (gerado no bootstrap) |
| `GHCR_PULL_USER` (variable) / `GHCR_PULL_TOKEN` | organização ou environment `production` | Secret `ghcr-pull` (PAT classic só com `read:packages`) |
| `AVABOT_CONNECTION_STRING` | repositório ou `production` | `ConnectionStrings__AvaBotContext` |
| `AVABOT_JWT_SECRET` (≥ 32 bytes) | repositório ou `production` | `Auth__JwtSecret` |
| `POWERBI_SECRET_ENCRYPTION_KEY` | repositório ou `production` | `PowerBI__SecretEncryptionKey` (a mesma de hoje, senão os segredos salvos ficam ilegíveis) |
| `AVABOT_USERNAME` / `AVABOT_PASSWORD` (opcionais) | repositório ou `production` | `Auth__Username` / `Auth__Password` |
| `WPP_SECRET_KEY` (opcional) | repositório ou `production` | `WppConnect__SecretKey` |

Os secrets `AVABOT_*`, `POWERBI_*` e `WPP_*` já existem no repositório (eram do deploy antigo) e
são reaproveitados. Cada um vira uma chave do Secret `avabot-api-secrets`.

## Bootstrap (admin, uma vez)

Requer `kubectl` na máquina do admin, com o kubeconfig **de admin** do cluster. Esse kubeconfig
nunca vai para o GitHub nem para o repositório.

1. **Pré-requisitos**:
   - liberar o cluster no firewall do PostgreSQL (Managed DB: *Trusted sources*);
   - conferir a folga de memória: a API pede 256Mi e o frontend 32Mi
     (`kubectl describe nodes | grep -A6 'Allocated resources'`). Sem folga, o pod fica
     `Pending`;
   - **anotar o IP do servidor atual** no registro A/AAAA de `avabot.net` (para a reversão);
   - conferir a infra compartilhada: `kubectl -n ingress-nginx get deploy` e
     `kubectl -n cloudflared get pods` (2/2 Running).
2. **Namespace e credencial da pipeline** (gere o kubeconfig num diretório temporário fora do
   repositório):

   ```bash
   kubectl apply -f deploy/k8s/bootstrap/namespace.yaml
   kubectl apply -f deploy/k8s/bootstrap/deployer-rbac.yaml
   umask 077
   deploy/k8s/bootstrap/make-kubeconfig.sh > "$TMP/avabot-deployer.kubeconfig"
   KUBECONFIG="$TMP/avabot-deployer.kubeconfig" kubectl auth can-i create deployments -n avabot    # yes
   KUBECONFIG="$TMP/avabot-deployer.kubeconfig" kubectl auth can-i get pods -n kube-system          # no
   gh secret set KUBECONFIG --env production < "$TMP/avabot-deployer.kubeconfig"
   rm -f "$TMP/avabot-deployer.kubeconfig"
   ```

   Cadastrar também `GHCR_PULL_USER`/`GHCR_PULL_TOKEN`, se ainda não existirem na organização.

## Deploy

Automático a cada push/merge na `main`. À mão: Actions → **Deploy Production** → `ref` = `main`
(ou uma tag). A pipeline roda os testes unitários e o build do frontend, publica as imagens
`ghcr.io/emaginebr/avabot-api` e `avabot-web` com a tag `sha-<commit>`, cria os Secrets do
cluster, aplica `deploy/k8s/avabot` e espera o rollout da API e do frontend. As migrações do
banco são aplicadas pela própria API na subida.

Pela linha de comando (inclusive de um branch ainda não mergeado):

```bash
gh workflow run deploy-prod.yml --ref <branch> -f ref=<branch>
```

## Base de conhecimento (migração do Elasticsearch)

Desde a feature 021, os trechos dos arquivos ficam na tabela `avabot_knowledge_chunks` do
PostgreSQL. Na subida, a API reprocessa sozinha, a partir de `file_content`, os arquivos prontos que
ainda não têm trechos (`KnowledgeChunkBackfillService`, um arquivo por vez; o custo dos embeddings
na OpenAI é pequeno). Isso faz a migração do índice antigo sem acesso ao Elasticsearch e também
cobre arquivos enviados pelo servidor antigo até a troca (entram no próximo restart). Para desligar:
`KnowledgeBackfill__Enabled=false` no ConfigMap.

Conferir **antes da troca na Cloudflare**:

```bash
kubectl -n avabot logs deploy/avabot-api | grep '\[Backfill\]'
# esperado: "[Backfill] concluído: total=N, migrados=N, falhas=0, ..."
```

Arquivos com falha ficam com status **Erro** no painel (Arquivos) e podem ser reprocessados por lá.

**Limpeza do Elasticsearch que chegou a ser criado no cluster** (admin, uma vez; `kubectl apply -k`
não apaga o que saiu do Kustomize; o volume usa a StorageClass `retain`):

```bash
kubectl -n avabot delete statefulset avabot-elasticsearch --ignore-not-found
kubectl -n avabot delete service avabot-elasticsearch --ignore-not-found
PV=$(kubectl -n avabot get pvc data-avabot-elasticsearch-0 -o jsonpath='{.spec.volumeName}')
kubectl -n avabot delete pvc data-avabot-elasticsearch-0 --ignore-not-found
kubectl delete pv "$PV"   # depois apague o volume correspondente no painel da DigitalOcean
kubectl apply -f deploy/k8s/bootstrap/deployer-rbac.yaml   # Role sem statefulsets/pvc
```

O Elasticsearch do servidor antigo (porta 9200 publicada sem autenticação) é desligado junto com
o ambiente antigo.

## WhatsApp

O WPP Connect não roda no cluster. Para religar o WhatsApp, suba um WPP Connect acessível pela API
(no namespace `avabot` ou fora dele) e ajuste em `avabot/api-configmap.yaml`:

- `WppConnect__BaseUrl`: URL do WPP Connect;
- `WppConnect__WebhookBaseUrl`: URL pela qual o WPP Connect alcança a API, **com** `/api` no fim
  (ex.: `http://avabot-api:8080/api` dentro do cluster);
- no WPP Connect, `WEBHOOK_ALLOWED_URLS` com o webhook de cada agente
  (`<WebhookBaseUrl>/whatsapp/<slug>/webhook`).

## Troca da origem na Cloudflare

1. **Validar por dentro** antes da troca, com um pod temporário (sem `--rm`, que perde linhas da
   saída):

   ```bash
   kubectl -n avabot run routecheck --restart=Never --image=curlimages/curl:8.17.0 --command -- sh -c \
     "curl -s -H Host:avabot.net http://ingress-nginx-controller.ingress-nginx.svc.cluster.local/api/health; echo; curl -s -o /dev/null -w '%{http_code}' -H Host:avabot.net http://ingress-nginx-controller.ingress-nginx.svc.cluster.local/"
   kubectl -n avabot wait --for=jsonpath='{.status.phase}'=Succeeded pod/routecheck --timeout=90s
   kubectl -n avabot logs routecheck
   kubectl -n avabot delete pod routecheck
   ```

2. **www**: hoje o redirect `www.avabot.net` → `avabot.net` é feito pelo nginx do servidor antigo,
   que deixa de receber tráfego na troca. Antes dela, criar na Cloudflare: Rules → Redirect Rules,
   Request URL `https://www.avabot.net/*` → `https://avabot.net/${1}`, 301, preservando a query
   string (o registro do `www` precisa estar proxied); e SSL/TLS → Edge Certificates → **Always
   Use HTTPS** ligado. O `www` não entra no túnel nem no Ingress.
3. **Troca** (alguns segundos fora do ar):
   - Cloudflare → DNS da zona: apagar o registro A/AAAA de `avabot.net` (IP anotado no bootstrap);
   - Zero Trust → Networks → Tunnels → `emagine-k8s` → **Add route → Published application**:
     hostname `avabot.net`, Service `HTTP` `ingress-nginx-controller.ingress-nginx.svc.cluster.local:80`;
   - conferir o CNAME `avabot.net` → `<id-do-túnel>.cfargotunnel.com` (proxied); se o painel não o
     criar, criar à mão.
4. **Confirmar que o tráfego chega ao cluster**: o log do ingress deve mostrar o IP público real
   dos clientes (`kubectl -n ingress-nginx logs deploy/ingress-nginx-controller --since=2m | grep avabot.net`).
   Resposta com `server: cloudflare` não prova nada: compare com uma rota que só existe no cluster
   (`/api/health`).
5. **Validação funcional**: login no painel, chat em `/chat/biia`, widget num site externo (CORS e
   WebSocket), busca na base de conhecimento (backfill concluído), Telegram (o webhook já é
   `https://avabot.net/api/telegram/...`, nada a mudar) e MCP (`https://avabot.net/api/mcp`).

**Reverter a troca**: remover a rota de `avabot.net` no túnel (e o CNAME, se ficar) e recriar o
registro A/AAAA (proxied) com o IP anotado. O efeito é quase imediato. O ambiente antigo continua
de pé até ser desligado.

## Desligar o ambiente antigo

Só depois da troca validada:

- no `emagine-deploy`: remover os blocos `server` de `avabot.net`/`www.avabot.net` do `nginx.conf`,
  o `scripts/build-avabot.ps1` e `builds/avabot/` (o frontend agora é a imagem `avabot-web`);
- no servidor Docker: `docker rm -f avabot-api` e apagar `/opt/avabot`;
- no `emagine-deploy`: tirar o Elasticsearch da infra compartilhada se nenhum outro projeto o usar
  (o AvaBot não usa mais);
- remover do GitHub os secrets do deploy antigo (`PROD_SSH_*`), se nenhum outro workflow usar;
- atualizar a skill `.claude/skills/avabot-deploy` e o `CLAUDE.md` (o frontend deixa de ser
  publicado pelo `emagine-deploy`).

## Operação

| Ação | Como |
|---|---|
| Deploy | merge na `main`, ou Actions → Deploy Production (`ref`) |
| Rollback rápido | Deploy Production com a tag anterior e `skip_tests=true` (a imagem já existe, o build é pulado) |
| Rollback de emergência | `kubectl -n avabot rollout undo deployment/avabot-api` (e `avabot-web`) |
| Logs | `kubectl -n avabot logs deploy/avabot-api --tail=200 -f` |
| Backfill da base | `kubectl -n avabot logs deploy/avabot-api \| grep '\[Backfill\]'` |
| Status | `kubectl -n avabot get deploy,pods,ingress -o wide` |
| Túnel | `kubectl -n cloudflared logs deploy/cloudflared --tail=50` (erro 1033 na Cloudflare = túnel sem conector) |
| Rotacionar a credencial da pipeline | apagar o Secret `avabot-deployer-token`, reaplicar `bootstrap/deployer-rbac.yaml`, gerar outro kubeconfig e atualizar o secret `KUBECONFIG` |
