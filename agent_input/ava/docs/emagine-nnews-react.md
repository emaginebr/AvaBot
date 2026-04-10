# NNews React

> Biblioteca React de componentes para CMS com edicao rich text e geracao de conteudo por IA.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/nnews-react)

---

## O que e o NNews React?

O NNews React e um pacote NPM que fornece componentes React prontos para integracao com o CMS NNews. Ele oferece CRUD completo de artigos, categorias e tags, com editores rich text (Quill), Markdown e texto simples, alem de geracao de conteudo assistida por IA (ChatGPT e DALL-E 3).

A biblioteca inclui suporte a temas (claro/escuro/sistema), internacionalizacao (portugues e ingles) e multi-tenancy via headers automaticos.

---

## Principais Funcionalidades

- **CRUD completo** — Componentes prontos para artigos, categorias e tags
- **Editores multiplos** — Rich text (Quill), Markdown e texto simples
- **Geracao com IA** — Criacao e atualizacao de artigos via ChatGPT e imagens via DALL-E 3
- **Temas** — Suporte a modo claro, escuro e deteccao automatica do sistema
- **Internacionalizacao** — Portugues e ingles inclusos, extensivel via i18next
- **Multi-tenant** — Headers X-Tenant-Id injetados automaticamente
- **Controle de acesso** — Suporte a roles para permissoes
- **TypeScript completo** — Tipagem total

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Framework | React 18+, TypeScript 5 |
| Build | Vite |
| Estilizacao | Tailwind CSS, Radix UI |
| Editor Rich Text | react-quill-new |
| Markdown | remark-gfm, syntax highlighting |
| i18n | i18next |

---

## Arquitetura e Integracoes

Padrao Provider-Hook-Component. Envolva a aplicacao com `NNewsProvider` configurado com apiUrl, tenantId, headers de autenticacao, idioma e tema.

Hooks: `useArticles`, `useCategories`, `useTags` para CRUD completo.

### Produtos Emagine Relacionados

- **NNews** — Backend CMS que esta biblioteca consome
- **devblog** — Blog que utiliza nnews-react para gerenciar conteudo
- **NAuth / nauth-react** — Autenticacao de usuarios

---

## Casos de Uso

1. **Frontend de blog** — Construa a interface de um blog completo com componentes prontos
2. **Painel editorial** — Gestao de artigos com edicao rich text e geracao assistida por IA
3. **Portal de noticias** — Interface para publicacao e organizacao de noticias por categorias e tags

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como instalo?**
R: `npm install nnews-react` e envolva a app com `NNewsProvider`.

**P: Posso gerar artigos com IA pelo frontend?**
R: Sim, a biblioteca expoe funcoes de criacao e atualizacao com IA via hooks.

**P: Quais editores de texto estao disponiveis?**
R: React Quill (rich text), Markdown com GFM e syntax highlighting, e texto simples.

**P: Posso customizar o tema?**
R: Sim, suporte a claro, escuro e deteccao automatica do sistema.

### Comerciais

**P: O pacote e gratuito?**
R: O pacote NPM e publico. Requer backend NNews configurado.

---

## Como Comecar

1. Instale: `npm install nnews-react`
2. Configure: `<NNewsProvider apiUrl="..." tenantId="...">`
3. Use hooks: `useArticles()`, `useCategories()`, `useTags()`

---

## Diferenciais Competitivos

- **IA integrada** — Geracao de conteudo com ChatGPT e imagens com DALL-E direto do frontend
- **Editores multiplos** — Rich text, Markdown e plain text em um unico pacote
- **i18n nativo** — Portugues e ingles inclusos
- **Temas automaticos** — Detecta tema do sistema e adapta automaticamente

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/nnews-react
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
