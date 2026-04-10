# Brahma

> Microservico multi-tenant para gerenciamento de pedidos de compra e e-commerce.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/Brahma) | [Website](https://emagine.com.br/brahma)

---

## O que e o Brahma?

O Brahma e um microservico backend robusto para gerenciamento de vendas e e-commerce. Ele fornece toda a infraestrutura necessaria para gerenciar produtos, categorias, carrinho de compras e pedidos, com integracao nativa de pagamentos via Stripe.

Construido com arquitetura multi-tenant, o Brahma permite que multiplas lojas ou redes de vendas operem de forma isolada em uma unica instalacao. Utiliza uma abordagem hibrida de APIs: GraphQL (HotChocolate) para operacoes de leitura e REST para operacoes de escrita.

O Brahma e ideal para empresas que precisam de um backend de e-commerce flexivel, escalavel e integrado ao ecossistema Emagine.

---

## Principais Funcionalidades

- **Gerenciamento de produtos** — CRUD completo com geracao automatica de slug, upload de imagens e controle de estoque
- **Categorias com contagem automatica** — Organizacao de produtos com contagem automatica de itens por categoria
- **Carrinho de compras** — Funcionalidade completa de carrinho para experiencia de compra
- **Pagamentos via Stripe** — Integracao nativa com Stripe para processamento de pagamentos
- **API hibrida GraphQL + REST** — GraphQL para consultas otimizadas e REST para operacoes de escrita
- **Multi-tenant com isolamento** — Cada rede de vendas opera com dados isolados
- **Armazenamento AWS S3** — Upload e gerenciamento de imagens de produtos via S3
- **Integracao com zTools** — ChatGPT e servicos de email integrados
- **Seed de dados com IA** — Script Python para gerar dados de demonstracao com imagens geradas por IA

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
| Documentacao | Swagger/Swashbuckle |
| Testes | xUnit 2.9, Moq 4.20 |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Clean Architecture com 8 projetos: API, GraphQL, Application, Domain, Shared (DTOs/ACL), Infra.Interfaces, Infra e Tests.

O GraphQL via HotChocolate oferece consultas otimizadas com projection, filtering e sorting direto no banco via IQueryable do EF Core. O REST e utilizado para operacoes de escrita com validacao.

Autenticacao delegada ao NAuth com segredos JWT por tenant.

### Produtos Emagine Relacionados

- **NAuth** — Autenticacao e validacao de tokens JWT
- **zTools** — ChatGPT, upload S3, email e utilitarios
- **Lofn** — Produto complementar para catalogo e vendas
- **ProxyPay** — Gateway de pagamentos alternativo ao Stripe direto

---

## Casos de Uso

1. **Marketplace multi-vendor** — Multiplas lojas operando na mesma plataforma com dados isolados e gestao independente
2. **E-commerce B2C** — Loja online completa com catalogo, carrinho e checkout integrado via Stripe
3. **Gestao de pedidos B2B** — Gerenciamento de pedidos de compra entre empresas com controle de produtos e categorias
4. **Catalogo de produtos com API** — Aplicacoes frontend consomem o GraphQL para exibir catalogo com filtragem e paginacao otimizadas

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Qual a diferenca entre usar GraphQL e REST nesta API?**
R: GraphQL (endpoint /graphql) e utilizado para consultas de leitura com filtragem, ordenacao e paginacao otimizadas. REST e utilizado para operacoes de criacao, atualizacao e exclusao.

**P: Como funciona a integracao com Stripe?**
R: Produtos podem ter IDs de produto e preco do Stripe associados, permitindo checkout direto pela plataforma.

**P: Posso usar outro gateway de pagamento?**
R: O Brahma integra nativamente com Stripe. Para outros gateways, voce pode usar o ProxyPay da Emagine.

**P: Como funciona o multi-tenant?**
R: Cada requisicao inclui um identificador de tenant via header ou JWT. O middleware resolve o tenant e roteia para o banco de dados correto.

**P: Existe documentacao interativa da API?**
R: Sim, Swagger em /swagger/ui para REST e playground GraphQL em /graphql.

### Comerciais

**P: O Brahma pode ser usado independente do ecossistema Emagine?**
R: Sim, ele funciona como microservico independente, mas se beneficia da integracao com NAuth e zTools.

**P: Qual o modelo de precos?**
R: Entre em contato com a equipe comercial da Emagine para informacoes sobre planos e licenciamento.

---

## Como Comecar

1. Clone o repositorio e copie: `cp .env.example .env`
2. Configure as credenciais de banco de dados, AWS S3, Stripe e NAuth
3. Crie a rede Docker: `docker network create emagine-network`
4. Suba os servicos: `docker-compose up -d --build`
5. Acesse o Swagger em `http://localhost:5000/swagger/ui` e o GraphQL em `/graphql`

---

## Diferenciais Competitivos

- **API hibrida** — GraphQL para leitura otimizada + REST para escrita, o melhor dos dois mundos
- **Multi-tenant nativo** — Isolamento real por banco de dados, nao apenas filtros
- **Ecossistema completo** — Autenticacao (NAuth), pagamentos (Stripe/ProxyPay), armazenamento (S3) e IA (zTools) integrados
- **Seed com IA** — Dados de demonstracao com imagens geradas por DALL-E para prototipagem rapida

---

## Contato e Suporte

- **Website**: https://emagine.com.br/brahma
- **Repositorio**: https://github.com/emaginebr/Brahma
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
