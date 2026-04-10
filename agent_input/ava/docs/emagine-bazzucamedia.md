# BazzucaMedia

> Plataforma multi-tenant de gerenciamento e publicacao automatizada em redes sociais.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/BazzucaMedia)

---

## O que e o BazzucaMedia?

O BazzucaMedia e uma plataforma completa de gerenciamento de redes sociais que permite agendar, publicar e gerenciar conteudo em multiplas plataformas simultaneamente. Suporta X/Twitter, Instagram, Facebook, LinkedIn, TikTok e YouTube.

O sistema permite gerenciar multiplos clientes, agendar publicacoes em calendario, fazer upload de midias e publicar automaticamente nas redes sociais configuradas. A publicacao no LinkedIn utiliza automacao via Playwright com filas RabbitMQ para processamento assincrono confiavel.

---

## Principais Funcionalidades

- **Publicacao multi-plataforma** — Publique em X/Twitter, Instagram, Facebook, LinkedIn, TikTok e YouTube
- **Agendamento em calendario** — Agende posts com resolucao de conflitos em incrementos de 30 minutos
- **Gestao de clientes** — CRUD completo de clientes com soft-delete
- **Credenciais por rede social** — Gerenciamento OAuth de credenciais por plataforma
- **Publicacao no X/Twitter** — Sincrona via OAuth 1.0a com suporte a upload de video chunked
- **Publicacao no LinkedIn** — Assincrona via Playwright + RabbitMQ com retry automatico e dead-letter queue
- **Armazenamento de midias** — Upload para AWS S3 via zTools
- **Multi-tenant** — Banco de dados por tenant com isolamento total
- **Retry e DLQ** — Retry automatico com TTL e dead-letter queue para falhas no LinkedIn
- **Console de debug** — CLI para debug visual do Playwright (navegador visivel)

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | .NET 8, ASP.NET Core, C# |
| Worker | Background Service (.NET) |
| Banco de Dados | PostgreSQL 17, Entity Framework Core 9 |
| Mensageria | RabbitMQ (filas + DLQ) |
| Automacao | Microsoft Playwright (Chromium) |
| Autenticacao | NAuth (JWT multi-tenant) |
| Armazenamento | zTools (AWS S3) |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Clean Architecture com 6 camadas + Worker + Console CLI.

Dois fluxos de publicacao:
- **Sincrono (X/Twitter):** API -> PostService -> XService -> X/Twitter API
- **Assincrono (LinkedIn):** API -> RabbitMQ (fila `bazzuca.linkedin.msg`) -> Worker consome -> Playwright baixa midia do S3 -> publica via navegador -> atualiza status. Falhas vao para fila de retry com TTL ou DLQ apos max retries.

### Produtos Emagine Relacionados

- **bazzuca-react** — Frontend React com componentes de gestao de clientes, posts e calendario
- **NAuth** — Autenticacao JWT multi-tenant
- **zTools** — Upload S3, ChatGPT e email
- **emagine-deploy** — Hospedagem com RabbitMQ centralizado

---

## Casos de Uso

1. **Agencia de marketing digital** — Gerencie redes sociais de multiplos clientes com agendamento e publicacao automatizada
2. **Equipe de social media** — Planeje conteudo em calendario e publique em multiplas plataformas com um clique
3. **Automacao de LinkedIn** — Publicacao automatizada no LinkedIn com retry e gestao de falhas
4. **Gestao de conteudo multi-marca** — Cada tenant gerencia seus proprios clientes e redes sociais de forma isolada

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Quais redes sociais sao suportadas?**
R: X/Twitter, Instagram, Facebook, LinkedIn, TikTok e YouTube.

**P: Como funciona a publicacao no LinkedIn?**
R: O LinkedIn utiliza automacao via Playwright (navegador headless) com filas RabbitMQ para processamento assincrono. Isso garante confiabilidade com retry automatico e dead-letter queue.

**P: Como funciona a publicacao no X/Twitter?**
R: Sincrona via OAuth 1.0a com suporte a upload de video chunked.

**P: O que acontece se uma publicacao falhar?**
R: O sistema faz retry automatico com TTL configuravel. Apos o maximo de tentativas, a mensagem vai para a dead-letter queue para analise manual.

**P: Posso agendar posts para horarios especificos?**
R: Sim, o calendario suporta agendamento com resolucao de 30 minutos e deteccao automatica de conflitos.

**P: Existe frontend pronto?**
R: Sim, o bazzuca-react fornece componentes React para gestao de clientes, posts e calendario.

### Comerciais

**P: A plataforma pode ser auto-hospedada?**
R: Sim, basta ter Docker com suporte a RabbitMQ e Playwright.

**P: Qual o custo?**
R: Entre em contato com a equipe comercial da Emagine.

---

## Como Comecar

1. Configure `.env` com credenciais de banco, NAuth, zTools e RabbitMQ
2. Suba: `docker-compose up -d --build` (API + Worker + RabbitMQ + PostgreSQL)
3. Acesse RabbitMQ Management em `http://localhost:15672`
4. Use a API ou instale bazzuca-react para o frontend

---

## Diferenciais Competitivos

- **Publicacao multi-plataforma** — 6 redes sociais suportadas em uma unica plataforma
- **LinkedIn automatizado** — Playwright + RabbitMQ para publicacao confiavel com retry
- **Filas com DLQ** — Processamento assincrono robusto com gestao de falhas
- **Multi-tenant** — Isolamento total por banco de dados para operacao multi-cliente

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/BazzucaMedia
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
