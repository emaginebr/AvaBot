# Produtos Emagine

> Indice de todos os produtos da Emagine com documentacao disponivel.

**Ultima atualizacao:** 2026-04-10

---

## Produtos

| Produto | Descricao | Tipo | Documentacao |
|---------|-----------|------|-------------|
| Avachat | Plataforma de chatbots com IA (backend) | API | [Ver docs](emagine-avachat.md) |
| Avachat App | Frontend para chatbots com IA | App | [Ver docs](emagine-avachat-app.md) |
| NAuth | Framework de autenticacao multi-tenant | API | [Ver docs](emagine-nauth.md) |
| NAuth React | Componentes React de autenticacao | Pacote NPM | [Ver docs](emagine-nauth-react.md) |
| Brahma | Microservico de pedidos e e-commerce | API | [Ver docs](emagine-brahma.md) |
| Lofn | Microservico de catalogo e vendas | API | [Ver docs](emagine-lofn.md) |
| Lofn React | SDK React para e-commerce | Pacote NPM | [Ver docs](emagine-lofn-react.md) |
| Dedalo | CMS multi-tenant (backend) | API | [Ver docs](emagine-dedalo.md) |
| Dedalo App | Editor visual de websites (frontend) | App | [Ver docs](emagine-dedalo-app.md) |
| NNews | CMS de noticias e blogs com IA | API | [Ver docs](emagine-nnews.md) |
| NNews React | Componentes React para CMS | Pacote NPM | [Ver docs](emagine-nnews-react.md) |
| ProxyPay | Proxy para gateways de pagamento | API | [Ver docs](emagine-proxypay.md) |
| ProxyPay React | Componentes React de pagamento | Pacote NPM | [Ver docs](emagine-proxypay-react.md) |
| BazzucaMedia | Gestao e publicacao em redes sociais | API | [Ver docs](emagine-bazzucamedia.md) |
| Bazzuca React | Componentes React para redes sociais | Pacote NPM | [Ver docs](emagine-bazzuca-react.md) |
| Peleja | Widget de comentarios (backend) | API | [Ver docs](emagine-peleja.md) |
| Peleja React | Widget React de comentarios | Pacote NPM | [Ver docs](emagine-peleja-react.md) |
| zTools | API de utilitarios (IA, email, S3) | API | [Ver docs](emagine-ztools.md) |
| MonexUp | Plataforma de marketing multinivel | SaaS | [Ver docs](emagine-monexup.md) |
| Viralt | Campanhas virais e gamificacao | SaaS | [Ver docs](emagine-viralt.md) |
| DevBlog | Blog para desenvolvedores | App | [Ver docs](emagine-devblog.md) |
| Abipesca | Aplicativo da ABIPESCA | App | [Ver docs](emagine-abipesca.md) |
| Emagine Deploy | Deploy multi-site centralizado | Infraestrutura | [Ver docs](emagine-deploy.md) |

---

## Categorias

### APIs e Microservicos
- **Avachat** — Chatbots com IA e RAG
- **NAuth** — Autenticacao e gerenciamento de usuarios multi-tenant
- **Brahma** — Pedidos de compra e e-commerce (GraphQL + REST)
- **Lofn** — Catalogo de produtos e vendas (GraphQL + REST)
- **Dedalo** — CMS multi-tenant para websites
- **NNews** — CMS de noticias com geracao de conteudo por IA
- **ProxyPay** — Proxy unificado para gateways de pagamento
- **BazzucaMedia** — Gestao e publicacao em redes sociais
- **Peleja** — Widget de comentarios e discussoes
- **zTools** — Utilitarios compartilhados (IA, email, S3, validacao)

### Aplicacoes Frontend
- **Avachat App** — Interface de criacao e gestao de chatbots
- **Dedalo App** — Editor visual de websites com drag-and-drop
- **DevBlog** — Plataforma de blog para desenvolvedores
- **Abipesca** — Aplicativo customizado da ABIPESCA
- **MonexUp** — Plataforma MLM com frontend React e app mobile
- **Viralt** — Plataforma SaaS de campanhas virais

### Pacotes e Bibliotecas (NPM)
- **nauth-react** — Componentes de autenticacao (login, registro, roles)
- **lofn-react** — SDK de e-commerce (lojas, produtos, carrinho)
- **nnews-react** — Componentes CMS (artigos, categorias, tags, IA)
- **proxypay-react** — Componentes de pagamento (PIX, Invoice, Billing)
- **bazzuca-react** — Gestao de redes sociais (clientes, posts, calendario)
- **peleja-react** — Widget de comentarios e discussoes

### Infraestrutura e DevOps
- **Emagine Deploy** — Deploy centralizado multi-site com Nginx, monitoramento e CI/CD

---

## Ecossistema

Todos os produtos compartilham:
- **NAuth** para autenticacao multi-tenant
- **zTools** para IA (ChatGPT/DALL-E), email, S3 e utilitarios
- **Docker + Docker Compose** para infraestrutura
- **GitHub Actions + GitVersion** para CI/CD com versionamento semantico
- **PostgreSQL** como banco de dados
- **Arquitetura multi-tenant** com isolamento por banco de dados
- **Clean Architecture** nos backends .NET
