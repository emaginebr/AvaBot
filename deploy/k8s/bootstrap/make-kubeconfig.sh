#!/usr/bin/env bash
# Gera o kubeconfig da ServiceAccount avabot-deployer (permissões só no namespace avabot) e o
# imprime em stdout. Não grava arquivo nenhum.
#
# Uso (admin do cluster, depois de `kubectl apply -f deploy/k8s/bootstrap/`):
#   umask 077
#   deploy/k8s/bootstrap/make-kubeconfig.sh > "$TMPDIR_FORA_DO_REPO/avabot-deployer.kubeconfig"
#   # cadastrar o conteúdo do arquivo como secret KUBECONFIG do environment `production` no GitHub
#   #   gh secret set KUBECONFIG --env production < "$TMPDIR_FORA_DO_REPO/avabot-deployer.kubeconfig"
#   rm "$TMPDIR_FORA_DO_REPO/avabot-deployer.kubeconfig"
#
# O arquivo gerado contém um token: não faça commit (o .gitignore cobre *.kubeconfig e
# kubeconfig*) e apague-o assim que cadastrar o secret.
set -euo pipefail

NAMESPACE="avabot"
SECRET="avabot-deployer-token"
CLUSTER_NAME="do-nyc2-emagine-k8s"
USER_NAME="avabot-deployer"
CONTEXT_NAME="avabot"

if ! command -v kubectl >/dev/null 2>&1; then
  echo "Erro: 'kubectl' não encontrado no PATH." >&2
  exit 1
fi

# Endereço do API server do contexto atual (admin)
server="$(kubectl config view --minify -o jsonpath='{.clusters[0].cluster.server}')"
if [[ -z "$server" ]]; then
  echo "Erro: não foi possível ler o endereço do API server do contexto atual." >&2
  exit 1
fi

# O token é preenchido pelo cluster logo após o apply; espera alguns segundos se ainda estiver vazio
token_b64=""
for _ in $(seq 1 15); do
  token_b64="$(kubectl -n "$NAMESPACE" get secret "$SECRET" -o jsonpath='{.data.token}' 2>/dev/null || true)"
  [[ -n "$token_b64" ]] && break
  sleep 2
done
if [[ -z "$token_b64" ]]; then
  echo "Erro: o Secret $NAMESPACE/$SECRET não tem token. Aplicou deploy/k8s/bootstrap/?" >&2
  exit 1
fi

# CA do cluster: o do Secret da ServiceAccount; se faltar, o do contexto atual
ca_b64="$(kubectl -n "$NAMESPACE" get secret "$SECRET" -o jsonpath='{.data.ca\.crt}')"
if [[ -z "$ca_b64" ]]; then
  ca_b64="$(kubectl config view --minify --raw -o jsonpath='{.clusters[0].cluster.certificate-authority-data}')"
fi
if [[ -z "$ca_b64" ]]; then
  echo "Erro: não foi possível ler o CA do cluster." >&2
  exit 1
fi

token="$(printf '%s' "$token_b64" | base64 -d)"

cat <<EOF
apiVersion: v1
kind: Config
clusters:
  - name: ${CLUSTER_NAME}
    cluster:
      server: ${server}
      certificate-authority-data: ${ca_b64}
users:
  - name: ${USER_NAME}
    user:
      token: ${token}
contexts:
  - name: ${CONTEXT_NAME}
    context:
      cluster: ${CLUSTER_NAME}
      user: ${USER_NAME}
      namespace: ${NAMESPACE}
current-context: ${CONTEXT_NAME}
EOF
