# Lofn React

> SDK e componentes React para construcao de frontends de e-commerce com a API Lofn.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/lofn-react) | [Website](https://emagine.com.br/lofn)

---

## O que e o Lofn React?

O Lofn React e um pacote NPM que fornece uma biblioteca completa de componentes React, hooks e servicos para integracao com a plataforma de e-commerce Lofn. Ele inclui tudo o que voce precisa para construir um frontend de loja virtual: gestao de lojas, produtos, categorias, imagens, carrinho de compras e membros.

A biblioteca utiliza GraphQL para consultas publicas e administrativas, REST para operacoes de escrita, e se integra nativamente com nauth-react para autenticacao.

---

## Principais Funcionalidades

- **Gestao de lojas** — CRUD de lojas, upload de logo, consultas GraphQL e gestao de status
- **Catalogo de produtos** — Criacao, edicao, busca paginada, multiplas imagens e produtos em destaque
- **Categorias** — Gerenciamento vinculado a lojas com contagem de produtos
- **Upload de imagens** — Multipart upload com ordenacao e gestao visual
- **Carrinho de compras** — Persistencia local (localStorage) + sincronizacao com backend
- **Membros de loja** — Gestao de usuarios e vendedores por loja
- **Componentes admin prontos** — UI completa com dark mode e design responsivo
- **TypeScript completo** — Tipagem total para seguranca em desenvolvimento

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Framework | React 18/19, TypeScript 5.8+ |
| HTTP | Axios 1.11+ |
| Estilizacao | Tailwind CSS 3.4 (dark mode) |
| Componentes UI | Radix UI, class-variance-authority |
| Build | Vite 5.4 (ES + CJS) |
| GraphQL | HotChocolate (via queries HTTP) |
| Testes | Vitest, Testing Library, Storybook 7.6 |
| Autenticacao | nauth-react (peer dependency) |

---

## Arquitetura e Integracoes

Hierarquia de Providers: NAuthProvider -> LofnProvider -> StoreProvider -> ProductProvider -> CategoryProvider -> ImageProvider -> ShopCarProvider -> StoreUserProvider.

O LofnProvider cria instancia Axios compartilhada com headers de autenticacao. O ShopCarProvider persiste dados no localStorage.

### Produtos Emagine Relacionados

- **Lofn** — Backend API que esta biblioteca consome
- **nauth-react** — Autenticacao (peer dependency obrigatoria)
- **NAuth** — Backend de autenticacao

---

## Casos de Uso

1. **Frontend de loja virtual** — Construa um e-commerce completo usando componentes prontos
2. **Painel administrativo de loja** — Use os componentes admin para gerenciar produtos, categorias e pedidos
3. **App mobile com e-commerce** — Integre os hooks e servicos em aplicacoes React Native ou Capacitor

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Quais sao as dependencias obrigatorias?**
R: React 18+, nauth-react e react-router-dom. Tailwind CSS e recomendado para estilizacao.

**P: Posso usar apenas os hooks sem os componentes visuais?**
R: Sim, hooks e servicos sao exportados separadamente e podem ser usados sem os componentes UI.

**P: O carrinho funciona offline?**
R: Parcialmente. O carrinho persiste no localStorage e sincroniza com o backend quando ha conexao.

**P: Existe app de exemplo?**
R: Sim, o repositorio inclui um example-app completo com Docker multi-stage (Node 20 + Nginx).

### Comerciais

**P: O pacote e gratuito?**
R: O pacote NPM esta disponivel publicamente. Para suporte em producao, entre em contato com a Emagine.

---

## Como Comecar

1. Instale: `npm install lofn-react nauth-react`
2. Configure `.env` com `VITE_API_URL`, `VITE_LOFN_API_URL` e `VITE_TENANT_ID`
3. Envolva a app com os Providers (NAuth -> Lofn -> Store -> Product -> ...)
4. Use os componentes ou hooks conforme necessidade

---

## Diferenciais Competitivos

- **Biblioteca completa** — Componentes visuais + hooks + servicos em um unico pacote
- **Dark mode nativo** — Suporte a tema escuro via Tailwind CSS
- **Carrinho persistente** — localStorage + sincronizacao backend para experiencia sem interrupcao
- **Storybook documentado** — Componentes visualizaveis e testaveis no Storybook

---

## Contato e Suporte

- **Website**: https://emagine.com.br/lofn
- **Repositorio**: https://github.com/emaginebr/lofn-react
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
