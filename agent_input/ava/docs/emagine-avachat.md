# Avachat

> Plataforma de chatbots inteligentes com IA para atendimento, vendas e suporte ao cliente.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/Avachat) | [Website](https://emagine.com.br/avachat)

---

## O que e o Avachat?

O Avachat e a plataforma de chatbots com inteligencia artificial da Emagine. Ele permite que empresas criem agentes de IA personalizados que conversam com visitantes do site em tempo real, respondendo duvidas, coletando leads e oferecendo suporte automatizado 24 horas por dia.

Cada agente pode ter sua propria base de conhecimento, personalidade e aparencia visual. O sistema utiliza RAG (Retrieval-Augmented Generation) para buscar informacoes relevantes na base de conhecimento antes de responder, garantindo respostas precisas e contextualizadas.

O Avachat e composto por uma API backend robusta (C#/.NET) e um frontend moderno (React/TypeScript), ambos disponiveis como produtos separados que trabalham em conjunto.

---

## Principais Funcionalidades

- **Agentes de IA personalizaveis** — Crie multiplos agentes com prompts, bases de conhecimento e aparencias diferentes para cada necessidade
- **Base de conhecimento com RAG** — Faca upload de documentos (.md) que sao automaticamente processados, indexados e utilizados para enriquecer as respostas da IA
- **Chat em tempo real via WebSocket** — Respostas com streaming token-a-token para uma experiencia de conversa natural e fluida
- **Coleta automatica de leads** — Capture nome, email e telefone dos visitantes durante a conversa
- **Widget embarcavel** — Widget de chat customizavel (cores, avatar) que pode ser incorporado em qualquer site
- **Historico completo de conversas** — Sessoes e mensagens paginadas para acompanhamento e analise
- **Busca hibrida inteligente** — Combinacao de busca vetorial (kNN) e textual (BM25) via Elasticsearch para encontrar as informacoes mais relevantes
- **Ativacao/desativacao de agentes** — Controle quais agentes estao ativos a qualquer momento
- **Acesso por URL unica** — Cada agente possui um slug unico para acesso direto (/chat/:slug)

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | ASP.NET Core 9.0, C# |
| Frontend | React 19, TypeScript, Vite |
| Banco de Dados | PostgreSQL 17 |
| Busca e Indexacao | Elasticsearch 8.17 |
| IA / LLM | OpenAI GPT-4o (chat), text-embedding-3-small (embeddings) |
| Comunicacao em tempo real | WebSocket nativo |
| Estilizacao | Tailwind CSS 4 |
| Estado (frontend) | Zustand |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions |
| Testes | xUnit, Moq, FluentAssertions |

---

## Arquitetura e Integracoes

O Avachat segue uma arquitetura Clean Architecture com camadas bem definidas: Domain, Application, Infrastructure e API.

O fluxo RAG funciona assim: a mensagem do usuario e convertida em embedding via OpenAI, depois e feita uma busca hibrida no Elasticsearch (vetorial + textual), os resultados mais relevantes sao incluidos no prompt, e a resposta e gerada pelo GPT-4o com streaming via WebSocket.

A API REST expoe endpoints para CRUD de agentes, sessoes, mensagens e arquivos de conhecimento. O WebSocket em `/ws/chat/{slug}` permite chat em tempo real com eventos de `ready`, `message`, `chunk`, `done` e `error`.

### Produtos Emagine Relacionados

- **avachat-app** — Frontend React que consome a API do Avachat
- **NAuth** — Autenticacao e gerenciamento de usuarios
- **emagine-deploy** — Deploy e hospedagem do servico

---

## Casos de Uso

1. **Atendimento ao cliente automatizado** — Uma empresa configura um agente com sua base de conhecimento (FAQs, manuais, politicas) e oferece suporte 24/7 sem necessidade de atendentes humanos
2. **Captura de leads qualificados** — O chatbot conversa com visitantes do site, coleta informacoes de contato e qualifica o interesse antes de encaminhar para a equipe de vendas
3. **Assistente de vendas** — Um agente treinado com catalogo de produtos e tabelas de precos ajuda clientes a encontrar o produto ideal e tira duvidas tecnicas
4. **Suporte tecnico de primeiro nivel** — O agente responde duvidas tecnicas comuns, reduzindo a carga sobre a equipe de suporte humano
5. **Onboarding de novos clientes** — Um agente guia novos usuarios pelos primeiros passos de configuracao e uso do produto

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Quais formatos de arquivo posso usar na base de conhecimento?**
R: Atualmente o sistema aceita arquivos no formato Markdown (.md), com tamanho maximo de 10MB por arquivo.

**P: Qual modelo de IA e utilizado?**
R: O Avachat utiliza o GPT-4o da OpenAI para geracao de respostas e o modelo text-embedding-3-small para embeddings dos documentos.

**P: Como funciona a busca na base de conhecimento?**
R: Utilizamos busca hibrida combinando kNN (busca vetorial por similaridade semantica) e BM25 (busca textual por palavras-chave) via Elasticsearch, garantindo resultados precisos e relevantes.

**P: Posso ter multiplos agentes simultaneamente?**
R: Sim, voce pode criar quantos agentes precisar, cada um com sua propria base de conhecimento, prompt de sistema e configuracao visual.

**P: Quais sao os requisitos de infraestrutura?**
R: O sistema requer PostgreSQL 17, Elasticsearch 8.17 e uma chave de API da OpenAI. Tudo pode ser executado via Docker Compose.

**P: O chat suporta streaming de respostas?**
R: Sim, as respostas sao transmitidas token-a-token via WebSocket, proporcionando uma experiencia de conversa natural e responsiva.

**P: Existe documentacao da API?**
R: Sim, a API possui documentacao Swagger acessivel em /swagger quando o servico esta em execucao.

### Comerciais

**P: Como funciona o modelo de licenciamento?**
R: Entre em contato com a equipe comercial da Emagine para informacoes sobre planos e precos.

**P: E possivel personalizar a aparencia do widget?**
R: Sim, o widget de chat permite customizacao completa de cores, avatar e posicionamento na pagina.

**P: Qual o suporte oferecido?**
R: A Emagine oferece suporte tecnico para implantacao e manutencao. Entre em contato pelo site para mais detalhes.

**P: O sistema pode ser hospedado em infraestrutura propria?**
R: Sim, o Avachat pode ser implantado em qualquer infraestrutura que suporte Docker, incluindo servidores proprios ou nuvem.

---

## Como Comecar

1. Clone o repositorio e copie o arquivo de configuracao: `cp .env.example .env`
2. Configure a chave da API OpenAI no arquivo `.env`
3. Suba os servicos com Docker: `docker compose up -d --build`
4. Acesse o Swagger em `http://localhost:5000/swagger` para explorar a API
5. Crie seu primeiro agente via API e faca upload de documentos para a base de conhecimento
6. Integre o widget do avachat-app no seu site

---

## Diferenciais Competitivos

- **RAG com busca hibrida** — Combinacao de busca semantica e textual garante respostas mais precisas do que solucoes que usam apenas um tipo de busca
- **Streaming em tempo real** — Respostas aparecem palavra por palavra, como uma conversa natural, melhorando a experiencia do usuario
- **Multi-agente** — Uma unica instalacao pode servir multiplos agentes para diferentes propositos ou clientes
- **Ecossistema integrado** — Funciona nativamente com outros produtos Emagine (NAuth, emagine-deploy) para autenticacao e deploy simplificados
- **Codigo aberto e extensivel** — Arquitetura limpa que permite customizacao e extensao conforme necessidades especificas

---

## Contato e Suporte

- **Website**: https://emagine.com.br/avachat
- **Repositorio**: https://github.com/emaginebr/Avachat
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
