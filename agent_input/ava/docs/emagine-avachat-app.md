# Avachat App

> Frontend moderno para criacao e gerenciamento de chatbots com inteligencia artificial.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/avachat-app) | [Website](https://emagine.com.br/avachat)

---

## O que e o Avachat App?

O Avachat App e a interface frontend da plataforma Avachat. E uma aplicacao React moderna que permite criar, configurar e gerenciar agentes de IA conversacionais de forma visual e intuitiva.

Atraves dele, usuarios podem criar agentes com prompts personalizados, fazer upload de bases de conhecimento, acompanhar conversas em tempo real, coletar leads e incorporar widgets de chat em qualquer site. A comunicacao com o backend acontece via API REST e WebSocket para streaming de respostas.

O Avachat App e a camada de apresentacao do ecossistema Avachat, consumindo a API backend para todas as operacoes.

---

## Principais Funcionalidades

- **Criacao visual de agentes** — Interface intuitiva para criar e configurar agentes de IA com prompts personalizados
- **Upload de base de conhecimento** — Arraste e solte documentos para treinar o agente, com acompanhamento do status de processamento
- **Chat em tempo real** — Conversa via WebSocket com streaming de respostas palavra por palavra
- **Coleta de leads** — Captura automatica de nome, email e telefone durante as conversas
- **Widget embarcavel e customizavel** — Personalize cores e avatar do widget para combinar com a identidade visual do site
- **Historico de conversas** — Visualize sessoes anteriores com paginacao de mensagens
- **Acesso por slug** — Cada agente possui URL unica para acesso direto (/chat/:slug)
- **Ativacao/desativacao** — Controle quais agentes estao ativos com um toggle simples

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Framework | React 19, TypeScript |
| Build Tool | Vite 8 |
| Estilizacao | Tailwind CSS 4, Typography Plugin |
| Estado | Zustand |
| Comunicacao | WebSocket nativo, API REST |
| Roteamento | React Router DOM 7 |
| Markdown | React Markdown |
| Upload | React Dropzone |
| Infraestrutura | Docker (Node + Nginx), GitHub Actions |

---

## Arquitetura e Integracoes

O frontend e uma SPA (Single Page Application) servida por Nginx em producao. Todas as requisicoes para `/api/` e `/ws/` sao redirecionadas via reverse proxy para o backend Avachat na porta 8080.

A comunicacao em tempo real utiliza WebSocket nativo com eventos estruturados: `ready` (conexao estabelecida), `message` (mensagem do usuario), `chunk` (token da resposta), `done` (resposta completa) e `error` (erro).

### Produtos Emagine Relacionados

- **Avachat** — API backend que processa as conversas e gerencia a IA
- **emagine-deploy** — Sistema de deploy que hospeda a aplicacao

---

## Casos de Uso

1. **Painel de gerenciamento de chatbots** — Equipe de atendimento cria e configura multiplos agentes para diferentes departamentos
2. **Demonstracao para clientes** — Acesse qualquer agente pela URL unica para demonstrar as capacidades do chatbot
3. **Treinamento de agentes** — Faca upload de documentos e teste as respostas do agente em tempo real antes de publicar

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Quais navegadores sao suportados?**
R: Todos os navegadores modernos com suporte a WebSocket (Chrome, Firefox, Safari, Edge).

**P: Como configuro o frontend para apontar para meu backend?**
R: Configure as variaveis de ambiente `VITE_API_URL` e `VITE_WS_URL` no arquivo `.env`.

**P: O widget pode ser incorporado em sites externos?**
R: Sim, o widget e embarcavel e customizavel, podendo ser adicionado a qualquer pagina web.

**P: Como funciona o deploy?**
R: O build gera arquivos estaticos servidos por Nginx. O Docker multi-stage cuida da compilacao e do servidor.

### Comerciais

**P: O frontend e vendido separadamente do backend?**
R: O Avachat App faz parte do pacote Avachat. Entre em contato com a equipe comercial para detalhes.

**P: Posso customizar a interface com minha marca?**
R: Sim, o widget permite personalizacao de cores e avatar. Customizacoes mais profundas podem ser feitas sob demanda.

---

## Como Comecar

1. Clone o repositorio e instale as dependencias: `npm install`
2. Configure o `.env` com `VITE_API_URL` e `VITE_WS_URL` apontando para o backend
3. Inicie o desenvolvimento: `npm run dev`
4. Para producao com Docker: `docker build -t avachat-app . && docker run -d -p 80:80 avachat-app`

---

## Diferenciais Competitivos

- **Interface moderna e responsiva** — Construido com React 19 e Tailwind CSS para uma experiencia de usuario fluida
- **Streaming em tempo real** — Respostas aparecem em tempo real via WebSocket, sem delays
- **Widget plug-and-play** — Incorpore o chat em qualquer site com configuracao minima
- **Upload intuitivo** — Drag-and-drop para adicionar documentos a base de conhecimento

---

## Contato e Suporte

- **Website**: https://emagine.com.br/avachat
- **Repositorio**: https://github.com/emaginebr/avachat-app
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
