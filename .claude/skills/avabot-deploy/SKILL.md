---
name: avabot-deploy
description: Sobe para produção o frontend do AvaBot (avabot.net) ou o WPP Connect / infraestrutura compartilhada (RabbitMQ, Elasticsearch, WPP Connect) usando o repositório C:\repos\emagine-deploy. Use sempre que o usuário pedir para subir, publicar ou fazer deploy do frontend, do wppconnect ou da infra do AvaBot.
---

# Deploy do frontend e da infra do AvaBot (via emagine-deploy)

O frontend do AvaBot e o WPP Connect **não** são publicados por este repositório. Quem publica é o repo irmão `C:\repos\emagine-deploy` (GitHub `emaginebr/emagine-deploy`), que tem dois caminhos independentes:

| Alvo | Como sobe | Workflow |
|---|---|---|
| Frontend (`avabot.net`) | Script `scripts/build-avabot.ps1` gera o build em `builds/avabot/`, commit + push na `main` | `Version and Tag` → `Deploy Production` (automático no push) |
| WPP Connect / infra | Disparo manual | `Deploy Infrastructure` (`deploy-infra.yml`, só `workflow_dispatch`) |

A API (.NET) **não** faz parte deste fluxo.

Toda mensagem ao usuário em português, com acentos.

---

## A. Frontend

### 1. Pré-checagens (AvaBot)

O `build-avabot.ps1` roda `git pull` em `C:\repos\AvaBot\frontend` **no branch atual** e compila o que estiver no disco. Então:

```bash
git -C /c/repos/AvaBot branch --show-current      # tem que ser main
git -C /c/repos/AvaBot status --short frontend/   # alterações locais entram no build
```

- Se não estiver na `main`, pergunte antes de trocar (pode haver trabalho em andamento). O código a publicar precisa estar mergeado na `main`.
- Se houver alterações não commitadas em `frontend/`, avise: elas iriam para produção sem estar no git. Pergunte se segue.
- O build usa `frontend/.env.production` (`VITE_API_URL=https://avabot.net/api`, `VITE_WS_URL=wss://avabot.net/api`).

### 2. Pré-checagens (emagine-deploy)

```bash
cd /c/repos/emagine-deploy
git checkout main && git pull --ff-only
git status --short
```

Arquivos já modificados ou não rastreados (ex.: `.qwen/`) **não** entram no commit: use `git add builds/avabot` apenas.

### 3. Build

```bash
cd /c/repos/emagine-deploy && pwsh -NoProfile -File ./scripts/build-avabot.ps1
```

Faz clean de `builds/avabot`, `git pull`, `npm install`, `npm run build` e copia `dist/` para `builds/avabot/`. Se falhar, pare e mostre o erro.

### 4. Commit e push (dispara o deploy)

Padrão de mensagem do repositório: `chore: atualiza build do avabot`. Commit direto na `main` (sem PR, como os demais builds).

```bash
cd /c/repos/emagine-deploy
git add builds/avabot
git diff --cached --stat
git commit -m "chore: atualiza build do avabot"
git push origin main
```

Se `git diff --cached` vier vazio, o build não mudou: avise e não faça commit.

### 5. Acompanhar e verificar

```bash
gh run list -R emaginebr/emagine-deploy -L 4
gh run watch <id-do-Deploy-Production> -R emaginebr/emagine-deploy --exit-status
curl -s -o /dev/null -w "%{http_code}\n" https://avabot.net/
```

O `Deploy Production` só começa depois do `Version and Tag` terminar. Ele faz `docker compose down` + `up --build` do Nginx que serve **todos** os sites da Emagine: há alguns segundos de indisponibilidade geral.

---

## B. WPP Connect / infraestrutura

O workflow `Deploy Infrastructure` sincroniza `/opt/emagine-deploy` com `origin/main`, regrava o `.env` com os secrets (`RABBITMQ_*`, `WPP_SECRET_KEY`) e recria, em sequência: **RabbitMQ → Elasticsearch → WPP Connect** (`--force-recreate`), depois verifica os containers.

Arquivos envolvidos em `emagine-deploy`: `docker-compose-infra.yml`, `wppconnect.Dockerfile` (base `wppconnect/server-cli:latest` + `@wppconnect-team/wppconnect` fixado), `wppconnect.config.js`.

### 1. Se houver mudança de configuração

Qualquer alteração nesses arquivos precisa estar na `main` do `emagine-deploy` **antes** do disparo (o servidor faz `git reset --hard origin/main`). Commit e push; lembre que o push na `main` também dispara o `Deploy Production` (reinício do Nginx).

### 2. Confirmar com o usuário antes de disparar

Avise o impacto e peça confirmação:
- RabbitMQ e Elasticsearch também são recriados (não há job só do WPP Connect). Todos os projetos que usam essa infra ficam indisponíveis por até ~2 min; a base de conhecimento do AvaBot (Elasticsearch) fica fora nesse intervalo.
- Os volumes persistem (`wpp_tokens`, `es_data`, `rabbitmq_data`), então a sessão pareada do WhatsApp normalmente sobrevive. Se não sobreviver, é preciso parear de novo pelo painel (start-session + QR code).

### 3. Disparar e acompanhar

```bash
gh workflow run deploy-infra.yml -R emaginebr/emagine-deploy --ref main
gh run list -R emaginebr/emagine-deploy --workflow deploy-infra.yml -L 1
gh run watch <id> -R emaginebr/emagine-deploy --exit-status
```

Depois, confira no painel do AvaBot (WhatsApp > status) se a sessão continua conectada.

---

## Relatório final

Diga o que foi publicado (frontend, infra ou ambos), o commit no `emagine-deploy`, o resultado dos workflows (com link do run) e a verificação feita. Se algo falhou, mostre a etapa e o log (`gh run view <id> --log-failed -R emaginebr/emagine-deploy`).
