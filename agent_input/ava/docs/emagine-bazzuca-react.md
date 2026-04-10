# Bazzuca React

> Biblioteca React para gestao de redes sociais, clientes e agendamento de posts.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/bazzuca-react)

---

## O que e o Bazzuca React?

O Bazzuca React e um pacote NPM que fornece componentes React prontos para o sistema de gerenciamento de redes sociais BazzucaMedia. Ele inclui gestao de clientes, integracao com 9 plataformas sociais, agendamento de posts em calendario e publicacao de conteudo.

A biblioteca oferece componentes visuais completos, hooks para logica de negocio, servicos de API e tipos TypeScript, tudo pronto para construir um frontend de gestao de redes sociais.

---

## Principais Funcionalidades

- **Gestao de clientes** — CRUD completo com tabela e modal de criacao/edicao
- **9 plataformas sociais** — X, Instagram, Facebook, LinkedIn, TikTok, YouTube, WhatsApp, SMS e Email
- **Calendario de posts** — Visualizacao e agendamento interativo em calendario
- **Editor de posts** — Formulario de criacao e edicao com suporte a midia
- **Visualizador de posts** — Componente para detalhes do post
- **Publicacao** — Acoes de publicar diretamente dos componentes
- **Design responsivo** — Mobile, tablet e desktop
- **Dark mode** — Suporte via variaveis CSS HSL
- **TypeScript completo** — Tipos, enums e type guards exportados
- **Validacoes** — CPF, CNPJ, email e telefone

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Framework | React 18, TypeScript 5.8 |
| Build | Vite 5 (ES + CJS) |
| Estilizacao | Tailwind CSS 3.4, Radix UI, shadcn/ui |
| Formularios | React Hook Form + Zod |
| Icones | Lucide React |
| HTTP | Axios |
| Datas | date-fns |
| Markdown | react-markdown, highlight.js |
| Testes | Vitest, Testing Library |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Padrao Provider-Hook-Component. Envolva a aplicacao com `BazzucaProvider` configurado com apiUrl e opcoes.

Hooks: `useClients` (CRUD clientes), `useSocialNetworks(clientId)` (CRUD redes por cliente), `usePosts(month, year)` (CRUD posts por periodo).

Servicos via contexto: `clientApi`, `socialNetworkApi`, `postApi`.

### Produtos Emagine Relacionados

- **BazzucaMedia** — Backend API que esta biblioteca consome
- **nauth-react** — Autenticacao (usado em conjunto)
- **NAuth** — Backend de autenticacao

---

## Casos de Uso

1. **Painel de gestao de redes sociais** — Interface completa para agencias gerenciarem conteudo de clientes
2. **Calendario editorial** — Planejamento visual de conteudo com agendamento por data
3. **Dashboard de publicacao** — Visualize, crie e publique posts em multiplas plataformas

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como instalo?**
R: `npm install bazzuca-react` e envolva a app com `BazzucaProvider`.

**P: Quais plataformas sao suportadas?**
R: X, Instagram, Facebook, LinkedIn, TikTok, YouTube, WhatsApp, SMS e Email (enum SocialNetworkEnum).

**P: Quais status de post existem?**
R: Draft (rascunho), Scheduled (agendado), ScheduledOnNetwork (agendado na rede), Posted (publicado) e Canceled (cancelado).

**P: Posso usar os hooks sem os componentes visuais?**
R: Sim, hooks e servicos sao independentes dos componentes.

### Comerciais

**P: O pacote e gratuito?**
R: O pacote NPM e publico. Requer backend BazzucaMedia configurado.

---

## Como Comecar

1. Instale: `npm install bazzuca-react`
2. Configure: `<BazzucaProvider config={{ apiUrl: "..." }}>`
3. Importe estilos: `import 'bazzuca-react/styles'`
4. Use componentes: `<ClientList />`, `<PostCalendar />`, `<PostEditor />`

---

## Diferenciais Competitivos

- **9 plataformas** — Suporte a mais plataformas que a maioria das bibliotecas similares
- **Calendario interativo** — Agendamento visual intuitivo de posts
- **Componentes completos** — UI pronta para uso, nao apenas hooks
- **TypeScript rigoroso** — Tipos, enums e type guards para desenvolvimento seguro

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/bazzuca-react
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
