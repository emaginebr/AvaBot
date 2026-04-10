# Peleja React

> Widget React de comentarios e discussoes para integracao em qualquer site.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/peleja-react)

---

## O que e o Peleja React?

O Peleja React e o frontend do sistema de comentarios Peleja. E um widget React leve que pode ser incorporado em qualquer aplicacao web para adicionar funcionalidades de comentarios, respostas, curtidas e GIFs.

O widget se comunica com a API backend do Peleja e utiliza o NAuth para autenticacao de usuarios, oferecendo uma experiencia completa de discussoes out-of-the-box.

---

## Principais Funcionalidades

- **Comentarios e respostas** — Interface completa para criar, editar e responder comentarios
- **Curtidas** — Toggle de like com contagem visual
- **GIFs** — Busca e insercao de GIFs nas discussoes
- **Internacionalizacao** — Suporte multi-idioma via i18next
- **Responsivo** — Layout adaptavel a qualquer tamanho de tela
- **Customizavel** — Estilizacao via Bootstrap 5

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Framework | React 18, TypeScript 5 |
| Roteamento | React Router 6 |
| Build | Vite 6 |
| Estilizacao | Bootstrap 5 |
| i18n | i18next 25 |

---

## Arquitetura e Integracoes

Organizado com pastas Contexts/, Services/, hooks/, types/, components/ e pages/. Compativel com Linux/Docker gracas a convencao de nomes em maiuscula.

### Produtos Emagine Relacionados

- **Peleja** — Backend API que este widget consome
- **NAuth / nauth-react** — Autenticacao de usuarios

---

## Casos de Uso

1. **Widget de comentarios para blog** — Adicione discussoes em qualquer pagina de artigo
2. **Feedback em plataformas** — Permita que usuarios comentem sobre conteudo ou produtos
3. **Integracao em CMS** — Incorpore no Dedalo ou qualquer CMS para funcionalidade de comentarios

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como configuro o widget?**
R: Configure as variaveis de ambiente `VITE_API_URL` (URL do backend Peleja) e opcionalmente `VITE_SITE_BASENAME` para o basepath do React Router.

**P: Preciso do backend Peleja?**
R: Sim, o widget depende da API backend do Peleja para funcionar.

### Comerciais

**P: Posso usar em qualquer site?**
R: Sim, o widget pode ser incorporado em qualquer aplicacao React.

---

## Como Comecar

1. Clone o repositorio e instale: `npm install`
2. Configure `.env` com `VITE_API_URL`
3. Execute: `npm run dev`

---

## Diferenciais Competitivos

- **Leve e embarcavel** — Widget compacto para incorporar em qualquer aplicacao React
- **i18n nativo** — Suporte multi-idioma incluido
- **Ecossistema integrado** — Funciona nativamente com Peleja backend e NAuth

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/peleja-react
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
