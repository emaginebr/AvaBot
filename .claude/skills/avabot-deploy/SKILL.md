---
name: avabot-deploy
description: Sobe para produção o AvaBot (API e frontend de avabot.net) no cluster Kubernetes da emagine (do-nyc2-emagine-k8s, namespace avabot) pela pipeline Deploy Production deste repositório. Use sempre que o usuário pedir para subir, publicar, fazer deploy, rollback, reiniciar ou ver logs/status do AvaBot em produção.
---

# Deploy do AvaBot (cluster Kubernetes)

Desde 2026-11-26 a API e o frontend de `avabot.net` rodam no cluster DOKS `do-nyc2-emagine-k8s`,
namespace `avabot`, atrás do Cloudflare Tunnel `emagine-k8s` e do ingress-nginx compartilhado.
O `emagine-deploy` e o servidor Docker antigo **não** publicam mais o AvaBot. Detalhes de
arquitetura, bootstrap e troca na Cloudflare: `deploy/k8s/README.md`. Padrões do cluster: skill
`k8s-deploy`.

Toda mensagem ao usuário em português, com acentos.

| Item | Valor |
|---|---|
| Pipeline | `.github/workflows/deploy-prod.yml` (**Deploy Production**), environment `production` |
| Disparo | automático em push/merge na `main`; manual com `ref` e `skip_tests` |
| Imagens | `ghcr.io/emaginebr/avabot-api` e `ghcr.io/emaginebr/avabot-web`, tag `sha-<commit7>` |
| Manifestos | `deploy/k8s/avabot` (Kustomize) |
| Banco | PostgreSQL gerenciado (fora do cluster); migrações aplicadas pela API na subida |
| Base de conhecimento | tabela `avabot_knowledge_chunks` no PostgreSQL (sem Elasticsearch) |
| WhatsApp | **desligado**: o WPP Connect não roda no cluster (ver `deploy/k8s/README.md`, seção WhatsApp) |

## 1. Pré-checagens

```bash
git -C /c/repos/AvaBot fetch -q && git -C /c/repos/AvaBot log --oneline -1 origin/main
gh run list --workflow deploy-prod.yml -L 3
```

O que vai para produção é o que está na `main` (ou na `ref` informada), não o disco local.
Código ainda não mergeado: peça o merge ou confirme o disparo a partir do branch.

## 2. Deploy

- Normal: merge na `main` (dispara sozinho).
- Manual (confirmar antes, é produção):

  ```bash
  gh workflow run deploy-prod.yml --ref main -f ref=main
  gh workflow run deploy-prod.yml --ref <branch> -f ref=<branch>   # branch não mergeado
  ```

Acompanhar:

```bash
gh run list --workflow deploy-prod.yml -L 1
gh run watch <id> --exit-status
gh run view <id> --log-failed      # se falhar
```

Jobs: `test` → `manifests` → `images` → `deploy` (Secrets do cluster, `kubectl apply -k`,
rollout da API e do frontend). O rollout não derruba o site (`maxUnavailable: 0`).

## 3. Verificação

```bash
curl -s -o /dev/null -w "%{http_code}\n" https://avabot.net/
curl -s https://avabot.net/api/health
```

Para ver se a requisição chegou ao cluster, confira o log do ingress (abaixo). A resposta
`server: cloudflare` sozinha não prova nada.

## 4. Operação (kubeconfig de admin, só local)

```bash
export KUBECONFIG="$HOME/.kube/do-nyc2-emagine-k8s.kubeconfig"
export PATH="$PATH:$LOCALAPPDATA/Microsoft/WinGet/Links"   # Git Bash no Windows
```

| Ação | Comando |
|---|---|
| Status | `kubectl -n avabot get deploy,pods,ingress -o wide` |
| Logs da API | `kubectl -n avabot logs deploy/avabot-api --tail=200` |
| Backfill da base | `kubectl -n avabot logs deploy/avabot-api \| grep '\[Backfill\]'` |
| Tráfego no ingress | `kubectl -n ingress-nginx logs deploy/ingress-nginx-controller --since=5m \| grep '\[avabot-'` |
| Reiniciar a API | `kubectl -n avabot rollout restart deployment/avabot-api` (confirmar antes) |
| Rollback rápido | Deploy Production com a tag/ref anterior e `skip_tests=true` |
| Rollback de emergência | `kubectl -n avabot rollout undo deployment/avabot-api` (ou `avabot-web`) |

Regras:
- O kubeconfig de admin **nunca** vai para o repositório, secrets, logs ou respostas (nada de
  `cat` ou `kubectl config view --raw`). A pipeline usa só o kubeconfig da SA `avabot-deployer`.
- Leituras (`get`, `logs`, `describe`) podem ser feitas direto. Qualquer ação que altere o cluster
  precisa do ok do usuário.
- Secrets da API (`avabot-api-secrets`) são recriados pela pipeline a partir dos secrets do
  environment `production`; não edite à mão. Configuração não secreta: `deploy/k8s/avabot/api-configmap.yaml`.

## Relatório final

Diga o que foi publicado (commit/ref), o resultado do workflow (com o link do run) e a verificação
feita. Se algo falhou, mostre o job e o trecho do log.
