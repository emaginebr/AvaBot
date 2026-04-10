# NNews

> Microservico CMS multi-tenant para noticias e blogs com geracao de conteudo por IA.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/NNews)

---

## O que e o NNews?

O NNews e um microservico CMS (Content Management System) multi-tenant para gerenciamento de noticias, artigos e blogs. Ele oferece uma API REST completa para criar, editar e publicar artigos com suporte a categorias hierarquicas, tags e upload de imagens.

O grande diferencial do NNews e a integracao nativa com inteligencia artificial: e possivel gerar artigos completos via ChatGPT e imagens via DALL-E 3 diretamente pela API, acelerando drasticamente a producao de conteudo.

Com arquitetura multi-tenant, cada tenant (blog/portal) opera com banco de dados isolado, segredos JWT proprios e controle de acesso independente.

---

## Principais Funcionalidades

- **Gestao completa de artigos** — CRUD com status (rascunho, publicado, arquivado, agendado)
- **Geracao de conteudo com IA** — Criacao e atualizacao de artigos via ChatGPT
- **Geracao de imagens com IA** — Imagens criadas automaticamente via DALL-E 3
- **Categorias hierarquicas** — Organizacao em arvore com categorias pai-filho
- **Tags com merge** — Sistema de tags com funcionalidade de merge para consolidacao
- **Multi-tenant** — Banco de dados por tenant com isolamento total
- **Autenticacao JWT por tenant** — Segredos JWT dinamicos por tenant via NAuth
- **Upload de imagens** — Ate 100MB via integracao com zTools (S3)
- **Controle de acesso por roles** — Permissoes por artigo baseadas em roles
- **Pacotes NuGet** — ACL e DTO distribuidos para integracao em projetos .NET
- **Logging estruturado** — Serilog com saida para console e arquivo

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | ASP.NET Core 8.0, C# |
| Banco de Dados | PostgreSQL 16, Entity Framework Core 8.0 |
| Autenticacao | NAuth v0.5.5 (JWT multi-tenant) |
| IA | zTools (ChatGPT + DALL-E 3) |
| Armazenamento | zTools (S3 upload) |
| Mapeamento | AutoMapper |
| Logging | Serilog |
| Documentacao | Swagger/Swashbuckle |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Clean Architecture: API -> Application -> Domain -> Infra -> PostgreSQL. Inclui Infra.Interfaces e ACL (NuGet).

Resolucao de tenant: requests nao autenticados usam header `X-Tenant-Id`, autenticados usam claim `tenant_id` do JWT, e consumidores via ACL usam `DefaultTenantId` do appsettings.

24 endpoints cobrindo artigos (inclusive criacao/atualizacao com IA), categorias, tags e upload de imagens.

### Produtos Emagine Relacionados

- **nnews-react** — Biblioteca React para frontend de CMS
- **devblog** — Blog que consome o NNews como backend de conteudo
- **NAuth** — Autenticacao e validacao JWT
- **zTools** — ChatGPT, DALL-E, upload S3 e utilitarios

---

## Casos de Uso

1. **Portal de noticias** — Crie um portal com artigos, categorias e tags, com geracao de conteudo assistida por IA
2. **Blog corporativo multi-tenant** — Multiplos blogs operando em uma unica instalacao com dados isolados
3. **Geracao de conteudo automatizada** — Use ChatGPT para gerar rascunhos e DALL-E para criar imagens de capa
4. **CMS headless** — Use o NNews como backend de conteudo para qualquer frontend via API REST

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como funciona a geracao de artigos com IA?**
R: Envie um prompt para o endpoint de criacao com IA, e o NNews gera o artigo completo via ChatGPT e opcionalmente a imagem via DALL-E 3.

**P: Posso usar o NNews sem IA?**
R: Sim, os endpoints de criacao e edicao tradicionais funcionam normalmente sem IA.

**P: Como funciona o multi-tenant?**
R: Cada tenant tem seu proprio banco PostgreSQL. O tenant e resolvido via header, JWT ou configuracao do ACL.

**P: Qual o limite de upload de imagens?**
R: Ate 100MB por imagem, armazenadas no S3 via zTools.

**P: Existe SDK frontend?**
R: Sim, o nnews-react fornece componentes React, hooks e servicos prontos.

**P: Posso integrar em meu projeto .NET?**
R: Sim, instale o pacote NuGet do NNews com ACL e DTOs prontos.

### Comerciais

**P: O NNews pode ser auto-hospedado?**
R: Sim, basta ter Docker e configurar PostgreSQL, NAuth e zTools.

**P: Qual o custo de uso da IA?**
R: Os custos de IA sao os da API OpenAI (ChatGPT/DALL-E). O NNews em si nao adiciona custo por geracao.

---

## Como Comecar

1. Configure `.env` com credenciais de banco, NAuth e zTools
2. Suba: `docker-compose up -d --build`
3. Acesse Swagger para explorar os 24 endpoints
4. Instale nnews-react no frontend para uma interface completa

---

## Diferenciais Competitivos

- **IA nativa** — Geracao de artigos e imagens com ChatGPT e DALL-E integrados na API
- **Multi-tenant real** — Banco de dados por tenant para isolamento total
- **Headless CMS** — API REST completa para consumo por qualquer frontend
- **Pacote NuGet** — Integracao em projetos .NET em minutos
- **24 endpoints** — API abrangente cobrindo artigos, categorias, tags e imagens

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/NNews
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
