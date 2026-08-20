# Quickstart: Landing Page com Chat Widget Bia

**Branch**: `005-landing-chat-widget` | **Date**: 2026-04-08

## Pre-requisitos

1. Backend Avachat rodando com agente "Bia" (slug: `bia`) ativo
2. Variaveis de ambiente configuradas:
   - `VITE_API_URL` — URL da API (ex: `http://localhost:5000`)
   - `VITE_WS_URL` — URL do WebSocket (ex: `ws://localhost:5000`)

## Setup

```bash
npm install
npm run dev
```

## Verificacao

1. Acessar `http://localhost:5173/`
   - Deve exibir a landing page com menu e conteudo
   - Menu deve ter link para "Admin" (`/admin/agents`)

2. Verificar balao de chat
   - Canto inferior direito
   - Mensagem: "Oi, eu sou a Bia. Em que posso ajudar?"

3. Clicar no balao
   - Widget de chat deve abrir
   - Se agente exige dados, assistente pergunta no chat
   - Responder as perguntas e conversar normalmente

4. Fechar e reabrir
   - Clicar no X do widget
   - Balao reaparece
   - Clicar no balao — historico de mensagens preservado

5. Navegar para admin
   - Clicar em "Admin" no menu
   - Deve ir para `/admin/agents`

## Estrutura de Arquivos (novos)

```
src/
├── components/
│   └── chat/
│       ├── ChatBubble.tsx          # Balao flutuante
│       └── ChatWidget.tsx          # Widget completo
├── hooks/
│   └── useChatWidget.ts            # Hook para coleta conversacional
└── pages/
    └── LandingPage.tsx             # Landing page principal
```

## Rotas

| Rota | Componente | Descricao |
|---|---|---|
| `/` | LandingPage | Landing page com widget |
| `/admin/agents` | AgentListPage | Lista de agentes (existente) |
| `/chat/:slug` | ChatPage | Chat full-page (existente) |
