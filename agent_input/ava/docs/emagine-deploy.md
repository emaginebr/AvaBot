# Emagine Deploy

> Sistema de deploy multi-site com Nginx, Docker e monitoramento integrado.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/emagine-deploy) | [Website](https://emagine.com.br)

---

## O que e o Emagine Deploy?

O Emagine Deploy e o sistema centralizado de deploy e hospedagem de todos os produtos da Emagine. Ele utiliza um unico container Nginx Alpine para servir 8+ aplicacoes web com SSL/TLS, virtual hosting por dominio e reverse proxy para APIs backend.

Alem de hospedar os sites, o sistema inclui monitoramento com Prometheus + Grafana + Node Exporter e message broker centralizado com RabbitMQ.

---

## Principais Funcionalidades

- **Hospedagem multi-dominio** — 8+ dominios servidos por um unico container Nginx Alpine
- **SSL/TLS automatico** — Certificados SSL por dominio com redirect HTTP para HTTPS
- **Reverse proxy** — Proxy para APIs backend (NAuth, NNews, Bazzuca)
- **SPA routing** — Configuracao try_files para Single Page Applications
- **Build automatizado** — Scripts PowerShell para build individual ou em lote
- **Monitoramento** — Prometheus + Grafana + Node Exporter para metricas e alertas
- **Message broker** — RabbitMQ centralizado para comunicacao assincrona
- **CI/CD completo** — GitHub Actions com versionamento semantico via GitVersion
- **Deploy automatico** — Deploy em producao via SSH no push para main

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Servidor Web | Nginx Alpine |
| Containers | Docker, Docker Compose |
| Build Scripts | PowerShell |
| Site Portfolio | React 18, TypeScript, Vite, Tailwind CSS |
| i18n | i18next |
| Monitoramento | Prometheus, Grafana, Node Exporter |
| Mensageria | RabbitMQ |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Arquitetura centralizada onde o Nginx atua como ponto de entrada unico. Sites estaticos sao servidos diretamente e APIs backend sao acessadas via reverse proxy.

Dominios hospedados: emagine.com.br, easysla.com, goblinwars.net, monexup.com, slaproyale.com, nochainswap.org, pandoravault.com, bazzuca.media.

### Produtos Emagine Relacionados

- **Todos os produtos** — O emagine-deploy hospeda todos os sites e APIs do ecossistema
- **NAuth, NNews, BazzucaMedia** — APIs acessadas via reverse proxy
- **RabbitMQ** — Utilizado pelo BazzucaMedia para publicacao assincrona

---

## Casos de Uso

1. **Hospedagem centralizada** — Todas as aplicacoes Emagine hospedadas em um unico servidor com gestao simplificada
2. **Deploy automatizado** — Push para main dispara build e deploy automatico via GitHub Actions
3. **Monitoramento proativo** — Grafana dashboards para acompanhar saude e performance de todos os servicos

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como adiciono um novo site?**
R: Adicione a configuracao do dominio no Nginx, o certificado SSL e o script de build PowerShell.

**P: Como funciona o deploy automatico?**
R: O GitHub Actions detecta push para main, executa o build e faz deploy via SSH para o servidor.

**P: O monitoramento e obrigatorio?**
R: Nao, Prometheus/Grafana sao opcionais mas recomendados para producao.

**P: Posso rodar localmente?**
R: Sim, use `docker-compose-local.yml` para desenvolvimento local sem SSL.

### Comerciais

**P: A Emagine oferece servico de hospedagem?**
R: Sim, a infraestrutura do emagine-deploy pode ser utilizada para hospedar produtos. Entre em contato.

---

## Como Comecar

1. Copie: `cp .env.example .env`
2. Configure credenciais do RabbitMQ e Grafana
3. Crie a rede: `docker network create emagine-network`
4. Build: `./scripts/build-all.ps1`
5. Suba: `docker compose up -d --build`

---

## Diferenciais Competitivos

- **Centralizacao** — Todos os sites e APIs em um unico ponto de entrada com SSL
- **Leve** — Nginx Alpine com footprint minimo de recursos
- **Monitoramento integrado** — Prometheus + Grafana prontos para uso
- **CI/CD automatizado** — Deploy em producao sem intervencao manual

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/emagine-deploy
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
