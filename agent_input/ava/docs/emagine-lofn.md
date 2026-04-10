# Lofn

> Microservico multi-tenant de e-commerce com catalogo de produtos, vendas e pagamentos.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/Lofn) | [Website](https://emagine.com.br/lofn)

---

## O que e o Lofn?

O Lofn e um microservico backend completo para e-commerce e vendas online. Ele gerencia lojas, produtos, categorias, carrinho de compras e membros de lojas, com integracao nativa de pagamentos via Stripe e API dual (GraphQL para leitura, REST para escrita).

Com arquitetura multi-tenant, o Lofn permite que multiplas lojas e redes de vendedores operem de forma isolada em uma unica instalacao. E ideal para marketplaces, lojas online e plataformas B2C que precisam de um backend robusto e escalavel.

---

## Principais Funcionalidades

- **Gestao de lojas** — CRUD completo de lojas com logo, status e busca por slug
- **Catalogo de produtos** — Gerenciamento de produtos com slug automatico, multiplas imagens, descontos e destaque
- **Categorias inteligentes** — Categorias vinculadas a lojas com contagem automatica de produtos
- **Carrinho de compras** — Entidade de carrinho com sincronizacao backend
- **API dual GraphQL + REST** — HotChocolate GraphQL para consultas otimizadas e REST para operacoes de escrita
- **Pagamentos Stripe** — IDs de produto/preco do Stripe associados para checkout direto
- **Multi-tenant** — Isolamento de dados por tenant com resolucao automatica
- **Upload de imagens** — Multiplas imagens por produto com ordenacao via AWS S3
- **Membros de loja** — Gestao de vendedores e membros por loja

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | .NET 8, ASP.NET Core, C# |
| GraphQL | HotChocolate 14.3 |
| Banco de Dados | PostgreSQL 17, Entity Framework Core 9 |
| Pagamentos | Stripe |
| Armazenamento | AWS S3 |
| Autenticacao | NAuth (JWT multi-tenant) |
| Imagens | SixLabors.ImageSharp |
| Documentacao | Swagger/Swashbuckle |
| Testes | xUnit 2.9, Moq 4.20 |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Clean Architecture com 8 projetos: API, GraphQL (schemas publico + admin), Application, Domain, DTOs/ACL compartilhados, Infra.Interfaces, Infra e Tests.

O GraphQL possui dois schemas: publico (lojas, produtos, categorias, destaque) e admin (minhas lojas, meus produtos, minhas categorias). REST para CRUD de produtos, categorias, lojas, imagens, carrinho e membros.

Fluxo de autenticacao: Bearer token enviado ao NAuth via RemoteAuthHandler, TenantMiddleware resolve o tenant.

### Produtos Emagine Relacionados

- **lofn-react** — SDK e componentes React para frontend de e-commerce
- **NAuth** — Autenticacao e validacao JWT
- **zTools** — ChatGPT, upload S3, email e utilitarios
- **Brahma** — Produto complementar para gestao de pedidos
- **ProxyPay** — Gateway de pagamentos alternativo

---

## Casos de Uso

1. **Loja online completa** — Backend para loja virtual com catalogo, carrinho e checkout Stripe
2. **Marketplace multi-vendor** — Multiplas lojas operando com produtos e vendedores independentes
3. **Catalogo B2C com busca avancada** — GraphQL otimizado para frontends que precisam de filtragem, ordenacao e paginacao
4. **Plataforma de vendas com rede de vendedores** — Gestao de membros e vendedores por loja

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Qual a diferenca entre o Lofn e o Brahma?**
R: O Lofn foca em catalogo de produtos, lojas e vendas B2C. O Brahma foca em gestao de pedidos de compra e redes de vendas.

**P: O GraphQL e obrigatorio?**
R: Nao, voce pode usar apenas a API REST. O GraphQL e opcional e otimizado para consultas de leitura no frontend.

**P: Como funciona o upload de imagens?**
R: Imagens sao enviadas via endpoint REST, processadas com ImageSharp e armazenadas no AWS S3 com URLs assinadas.

**P: Posso usar outro gateway alem do Stripe?**
R: O Lofn integra nativamente com Stripe. Para outros gateways, considere usar o ProxyPay.

**P: Existe SDK frontend pronto?**
R: Sim, o lofn-react fornece componentes React, hooks e servicos prontos para consumir a API.

### Comerciais

**P: Posso usar o Lofn para criar meu marketplace?**
R: Sim, a arquitetura multi-tenant e multi-loja e ideal para marketplaces.

**P: Qual o modelo de licenciamento?**
R: Entre em contato com a equipe comercial da Emagine.

---

## Como Comecar

1. Clone e configure: `cp .env.example .env`
2. Crie a rede: `docker network create emagine-network`
3. Suba: `docker-compose up -d --build`
4. Acesse Swagger em `/swagger/ui` e GraphQL em `/graphql`
5. Use o script seed (`scripts/info-store.py`) para dados de demonstracao

---

## Diferenciais Competitivos

- **API dual** — GraphQL para leitura otimizada + REST para escrita, maximizando performance e flexibilidade
- **Multi-tenant nativo** — Isolamento real por banco de dados para operacao de marketplace
- **SDK frontend pronto** — lofn-react com componentes, hooks e servicos para desenvolvimento rapido
- **Ecossistema completo** — Autenticacao, pagamentos, armazenamento e IA integrados nativamente

---

## Contato e Suporte

- **Website**: https://emagine.com.br/lofn
- **Repositorio**: https://github.com/emaginebr/Lofn
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
