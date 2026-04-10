# DevBlog

> Plataforma de blog para desenvolvedores com autenticacao e gestao de conteudo.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/devblog)

---

## O que e o DevBlog?

O DevBlog e uma plataforma de blog construida como SPA React, projetada para desenvolvedores e empresas que desejam publicar artigos tecnicos, tutoriais e novidades. Oferece criacao e edicao de artigos com rich text, organizacao por categorias e tags, dashboard analitico e modo escuro.

A plataforma se integra com o ecossistema Emagine, utilizando nauth-react para autenticacao e nnews-react para gestao de conteudo (artigos, categorias e tags), operando como tenant isolado no NAuth e NNews.

---

## Principais Funcionalidades

- **Publicacao de artigos** — Criacao e edicao com editor rich text
- **Categorias e tags** — Organizacao completa de conteudo
- **Dashboard analitico** — Painel com metricas de publicacao e engajamento
- **Autenticacao completa** — Login, registro e gerenciamento de perfil via nauth-react
- **Modo escuro** — Suporte a dark mode via Tailwind CSS
- **Notificacoes toast** — Feedback visual de acoes via Sonner

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Framework | React 19, TypeScript 5.6 (strict mode) |
| Build | Vite 6 |
| Roteamento | React Router 7 |
| Estilizacao | Tailwind CSS 3.4 (typography, animation) |
| Autenticacao | nauth-react SDK |
| CMS | nnews-react SDK |
| Icones | Lucide React |
| Notificacoes | Sonner |

---

## Arquitetura e Integracoes

O DevBlog opera como tenant `devblog` no ecossistema multi-tenant Emagine. Todas as requisicoes incluem o header `X-Tenant-Id: devblog` para isolamento de dados.

Autenticacao via nauth-react (SDK NAuth) e gestao de conteudo via nnews-react (SDK NNews).

### Produtos Emagine Relacionados

- **nauth-react / NAuth** — Autenticacao e gerenciamento de usuarios
- **nnews-react / NNews** — Backend CMS para artigos, categorias e tags
- **Peleja** — Widget de comentarios que pode ser adicionado aos artigos
- **emagine-deploy** — Hospedagem do blog

---

## Casos de Uso

1. **Blog corporativo** — Empresa publica artigos sobre seus produtos e servicos
2. **Blog tecnico pessoal** — Desenvolvedor compartilha tutoriais e experiencias
3. **Portal de noticias** — Publicacao de noticias organizadas por categorias e tags

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Preciso de backend proprio?**
R: Nao, o DevBlog utiliza NAuth para autenticacao e NNews para gestao de conteudo, ambos como servicos remotos.

**P: Como configuro o blog?**
R: Configure `VITE_AUTH_API_URL` e `VITE_NEWS_API_URL` apontando para suas instancias NAuth e NNews.

**P: Posso adicionar comentarios nos artigos?**
R: Sim, integrando o widget Peleja nos artigos.

### Comerciais

**P: Posso usar para meu blog profissional?**
R: Sim, o DevBlog pode ser personalizado e implantado para qualquer blog.

---

## Como Comecar

1. Instale: `npm install`
2. Configure `.env` com `VITE_AUTH_API_URL` e `VITE_NEWS_API_URL`
3. Execute: `npm run dev`

---

## Diferenciais Competitivos

- **Ecossistema integrado** — Autenticacao e CMS prontos via SDKs Emagine
- **Multi-tenant** — Opera como tenant isolado, podendo coexistir com outros blogs na mesma infraestrutura
- **Stack moderna** — React 19, TypeScript strict mode, Tailwind CSS
- **Dark mode nativo** — Experiencia de leitura confortavel em qualquer ambiente

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/devblog
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
