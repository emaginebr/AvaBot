# Implementation Plan: Fluxo Start Session no Chat Widget

**Branch**: `006-chat-start-session` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-chat-start-session/spec.md`

## Summary

Refatorar o fluxo do chat widget para usar o novo endpoint REST
`POST /api/agents/{slug}/sessions` antes de conectar o WebSocket.
A coleta de dados e feita no frontend com mensagens fixas, e so
apos o start-session o WebSocket conecta com `?sessionId=X`.

## Technical Context

**Language/Version**: TypeScript 6.0.2
**Primary Dependencies**: React 19.x, React Router 7.x, Tailwind CSS 4.x
**Storage**: N/A (estado em memoria)
**Testing**: Manual
**Target Platform**: Web browser (desktop + mobile)
**Project Type**: web-service (SPA frontend)
**Performance Goals**: Start-session < 3s, chat abre < 1s
**Constraints**: Sem novas dependencias; refatorar hooks existentes
**Scale/Scope**: Refatoracao de 2 hooks + 1 service + 1 componente

## Constitution Check

| Principio | Status | Notas |
|---|---|---|
| I. Stack Tecnologica | OK | Tailwind, Zustand, React 19, Fetch API |
| II. Case Sensitivity | OK | Services/ (S), hooks/ (h), types/ (t) |
| III. Convencoes de Codigo | OK | PascalCase, camelCase, arrow functions |
| IV. Autenticacao | N/A | Endpoint publico |
| V. Variaveis de Ambiente | OK | VITE_API_URL, VITE_WS_URL |

## Project Structure

### Source Code (arquivos afetados)

```text
src/
├── Services/
│   └── AgentService.ts             # MODIFICAR - adicionar startSession()
├── hooks/
│   ├── useChat.ts                  # MODIFICAR - remover logica collect_data
│   ├── useChatWidget.ts            # MODIFICAR - refatorar fluxo completo
│   └── useWebSocket.ts             # SEM ALTERACAO
├── components/
│   └── chat/
│       └── ChatWidget.tsx          # MODIFICAR - novo fluxo de conexao
└── types/
    └── chatSession.ts              # MODIFICAR - adicionar ChatSessionStartInfo
```

## Fluxo Novo vs Antigo

### Antigo (remover):
```
Widget abre → WebSocket conecta → collect_data → identify → ready → chat
```

### Novo (implementar):
```
Widget abre → greeting → coleta conversacional (frontend-only)
→ POST start-session → "Muito obrigado..." → WebSocket(?sessionId=X) → ready → chat
```

## Mudancas Detalhadas

### 1. AgentService.ts
Adicionar metodo `startSession(slug, data)` que chama
`POST /api/agents/{slug}/sessions` com `ChatSessionStartInfo`.

### 2. useChatWidget.ts (refatoracao principal)
- Nao conectar WebSocket ao abrir
- Gerenciar coleta conversacional com mensagens fixas:
  - "Qual seu nome?"
  - "Bem vindo {nome}, qual seu email?"
  - "Qual seu numero de telefone? Com DDD"
- Apos coleta, chamar AgentService.startSession()
- Receber sessionId, adicionar mensagem "Muito obrigado..."
- So entao setar wsUrl com ?sessionId=X para conectar WebSocket

### 3. useChat.ts
- Remover logica de `collect_data` e `identify` (nao mais usados)
- Manter apenas: `chunk`, `done`, `error`, `ready`
- Simplificar: nao precisa mais de `fieldsToCollect`

### 4. ChatWidget.tsx
- Nao conectar WebSocket no handleOpen
- Passar config (campos de coleta) para useChatWidget
- Bloquear input ate sessao estar ativa

### 5. types/chatSession.ts
- Adicionar interface ChatSessionStartInfo

## Arquivos a Modificar

| Arquivo | Acao | Descricao |
|---|---|---|
| `src/Services/AgentService.ts` | MODIFICAR | Adicionar startSession() |
| `src/types/chatSession.ts` | MODIFICAR | Adicionar ChatSessionStartInfo |
| `src/hooks/useChat.ts` | MODIFICAR | Remover collect_data/identify |
| `src/hooks/useChatWidget.ts` | MODIFICAR | Novo fluxo REST + WebSocket |
| `src/components/chat/ChatWidget.tsx` | MODIFICAR | Adaptar ao novo fluxo |
