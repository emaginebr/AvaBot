# Implementation Plan: Landing Page com Chat Widget Bia

**Branch**: `005-landing-chat-widget` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-landing-chat-widget/spec.md`

## Summary

Criar uma landing page para o Avachat com um chat widget flutuante
que conecta ao agente Bia (slug: `bia`). O widget aparece como um
balao no canto inferior direito e, ao ser clicado, abre um painel
de chat com coleta conversacional de dados e streaming de respostas.

## Technical Context

**Language/Version**: TypeScript 6.0.2
**Primary Dependencies**: React 19.x, React Router 7.x, Tailwind CSS 4.x
**Storage**: N/A (estado em memoria, sessao WebSocket no backend)
**Testing**: Manual (nao solicitado testes automatizados)
**Target Platform**: Web browser (desktop + mobile)
**Project Type**: web-service (SPA frontend)
**Performance Goals**: Widget abre em < 1s, resposta streaming sem delay
**Constraints**: Sem novas dependencias; reutilizar hooks existentes
**Scale/Scope**: 1 landing page, 1 widget, 3 componentes novos

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Status | Notas |
|---|---|---|
| I. Stack Tecnologica | DIVERGENTE | Constitution diz Bootstrap/Context API, projeto real usa Tailwind/Zustand. Seguimos o real. |
| II. Case Sensitivity | OK | Services/ (S maiusculo), hooks/ (h minusculo), types/ (t minusculo) |
| III. Convencoes de Codigo | OK | PascalCase componentes, camelCase funcoes, arrow functions, const |
| IV. Autenticacao | N/A | Landing page e chat sao publicos |
| V. Variaveis de Ambiente | OK | VITE_API_URL e VITE_WS_URL com prefixo VITE_ |

**Nota sobre divergencia da Stack**: A constitution v1.0.0 foi criada
com base em um template generico. O projeto real usa React 19, Vite 8,
Tailwind CSS 4, Zustand 5 e React Router 7. Manter consistencia com o
codigo existente e mais importante que a constitution. Recomenda-se
atualizar a constitution para refletir a stack real.

## Project Structure

### Documentation (this feature)

```text
specs/005-landing-chat-widget/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── widget-api.md    # Component API contracts
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
src/
├── components/
│   └── chat/
│       ├── ChatBubble.tsx          # NOVO - Balao flutuante
│       ├── ChatWidget.tsx          # NOVO - Widget container
│       ├── ChatWindow.tsx          # EXISTENTE - UI de mensagens
│       ├── MessageBubble.tsx       # EXISTENTE - Bolha de mensagem
│       ├── TypingIndicator.tsx     # EXISTENTE - Animacao
│       └── UserDataForm.tsx        # EXISTENTE - Form (nao usado no widget)
├── hooks/
│   ├── useChat.ts                  # EXISTENTE - Logica de chat
│   ├── useChatWidget.ts            # NOVO - Coleta conversacional
│   └── useWebSocket.ts             # EXISTENTE - WebSocket
├── pages/
│   ├── LandingPage.tsx             # NOVO - Landing page
│   └── chat/
│       └── ChatPage.tsx            # EXISTENTE - Chat full-page
├── Services/
│   └── AgentService.ts             # EXISTENTE - getChatConfig
└── types/
    └── agent.ts                    # EXISTENTE - AgentChatConfigInfo
```

**Structure Decision**: Adicionar 3 novos arquivos (ChatBubble,
ChatWidget, LandingPage) + 1 novo hook (useChatWidget) ao projeto
existente. Modificar App.tsx para a nova rota. Reutilizar todos os
componentes e hooks de chat existentes.

## Complexity Tracking

| Aspecto | Decisao | Justificativa |
|---|---|---|
| Coleta conversacional | Hook dedicado (useChatWidget) | Separa logica de coleta da logica de chat |
| Widget flutuante | CSS position:fixed | Requisito explicito, sem lib extra |
| Layout landing page | Skill frontend-design | Instrucao do usuario |

## Arquitetura de Componentes

```
LandingPage
├── Navbar (link Admin)
├── Hero Section
├── Content Sections
├── Footer
└── ChatWidget (slug="bia")
    ├── ChatBubble (quando fechado)
    └── Widget Panel (quando aberto)
        ├── Header (nome agente + botao fechar)
        ├── ChatWindow (reuso) ou Coleta Conversacional
        └── Input (reuso do ChatWindow)
```

## Fluxo do Widget

```
1. LandingPage renderiza ChatWidget com slug="bia"
2. ChatWidget busca chat-config via AgentService
3. Exibe ChatBubble com greeting
4. Usuario clica → widget abre
5. Conecta WebSocket: ws://{VITE_WS_URL}/ws/chat/bia
6. Servidor envia collect_data (se necessario)
7. useChatWidget gerencia coleta conversacional:
   a. Exibe mensagem "Qual seu nome?" no chat
   b. Usuario responde → armazena
   c. Exibe mensagem "Qual seu email?"
   d. Usuario responde → armazena
   e. Exibe mensagem "Qual seu telefone?"
   f. Usuario responde → armazena
   g. Envia identify com todos os dados
8. Servidor envia ready
9. Chat normal via useChat (streaming de chunks)
```

## Arquivos a Modificar

| Arquivo | Acao | Descricao |
|---|---|---|
| `src/App.tsx` | MODIFICAR | Trocar redirect `/` para LandingPage |
| `src/pages/LandingPage.tsx` | CRIAR | Landing page com widget |
| `src/components/chat/ChatBubble.tsx` | CRIAR | Balao flutuante |
| `src/components/chat/ChatWidget.tsx` | CRIAR | Widget container |
| `src/hooks/useChatWidget.ts` | CRIAR | Hook de coleta conversacional |
