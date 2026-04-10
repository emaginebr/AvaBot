# Dedalo App

> Frontend React para criacao e edicao visual de websites com o CMS Dedalo.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/dedalo-app)

---

## O que e o Dedalo App?

O Dedalo App e a interface frontend do CMS Dedalo. E uma aplicacao React que permite criar, editar e gerenciar websites de forma visual, com drag-and-drop para organizacao de conteudo, sistema de templates e resolucao multi-tenant por dominio.

Atraves dele, proprietarios de sites podem gerenciar paginas, menus e componentes de conteudo (hero, texto, imagem, galeria, video, formulario) sem necessidade de conhecimento tecnico, utilizando editores visuais intuitivos.

---

## Principais Funcionalidades

- **Resolucao multi-tenant** — Detecta o tenant automaticamente por dominio, subdominio ou caminho
- **Edicao visual drag-and-drop** — Arraste e reorganize componentes de conteudo na pagina (restrito a proprietarios)
- **Sistema de templates** — Templates registraveis com layouts customizaveis (starter-blog, business, etc.)
- **CRUD de paginas** — Crie e gerencie paginas com suporte a multiplos templates
- **Menus hierarquicos** — Gestao de menus com links internos/externos e criacao automatica de paginas
- **Componentes de conteudo** — Hero, texto, imagem, galeria, video e formulario com editores visuais
- **Autenticacao NAuth** — Login, registro, recuperacao de senha e perfil integrados
- **Dark mode** — Suporte a tema escuro

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Framework | React 19, TypeScript 5.9 |
| Build | Vite 8 |
| Roteamento | React Router DOM 7 |
| Drag-and-drop | @dnd-kit/core 6, @dnd-kit/sortable 10 |
| Estilizacao | Tailwind CSS 4 |
| Autenticacao | nauth-react 0.7 |
| Icones | Lucide React |
| Notificacoes | Sonner |
| Infraestrutura | Nginx, GitHub Actions |

---

## Arquitetura e Integracoes

Hierarquia de contextos: BrowserRouter -> NAuthProvider -> WebsiteProvider -> PageProvider -> MenuProvider -> ContentProvider -> EditModeProvider.

Camada de servicos encapsula fetch com injecao automatica de Authorization e X-Tenant-Id.

### Produtos Emagine Relacionados

- **Dedalo** — Backend API que este frontend consome
- **NAuth / nauth-react** — Autenticacao de usuarios
- **zTools** — Upload de arquivos e imagens

---

## Casos de Uso

1. **Editor de website visual** — Proprietarios editam conteudo das paginas com drag-and-drop sem codigo
2. **Gerenciamento de sites multi-tenant** — Uma aplicacao serve multiplos sites com resolucao automatica de tenant
3. **Criacao rapida de landing pages** — Use templates prontos para criar paginas de captura rapidamente

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como funciona a resolucao de tenant?**
R: O app detecta automaticamente o tenant pelo dominio, subdominio ou variavel de ambiente VITE_TENANT_ID.

**P: Quais tipos de conteudo posso adicionar?**
R: Hero, texto, imagem, galeria, video e formulario, todos com editores visuais dedicados.

**P: Posso criar meus proprios templates?**
R: Sim, o sistema de templates e registravel e extensivel.

### Comerciais

**P: Preciso do backend Dedalo?**
R: Sim, o Dedalo App consome a API do backend Dedalo para todas as operacoes.

---

## Como Comecar

1. Clone e instale: `npm install`
2. Configure `.env`: `VITE_API_URL`, `VITE_NAUTH_API_URL`, `VITE_TENANT_ID`, `VITE_BASE_DOMAIN`
3. Execute: `npm run dev`

---

## Diferenciais Competitivos

- **Drag-and-drop nativo** — Edicao visual intuitiva com @dnd-kit
- **Multi-tenant transparente** — Resolucao automatica por dominio sem configuracao por site
- **Templates extensiveis** — Sistema de templates registraveis para diferentes tipos de site
- **Stack moderna** — React 19, TypeScript, Vite 8 e Tailwind CSS 4

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/dedalo-app
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
