# Quickstart: 008-session-resume-cookies

**Date**: 2026-04-10  
**Branch**: `008-session-resume-cookies`

## Pré-requisitos

- Node.js instalado
- Backend AvaBot rodando em `VITE_API_URL` com endpoint `GET /sessions/resume/{slug}` disponível
- Branch `008-session-resume-cookies` checked out

## Setup

```bash
npm install
npm run dev
```

## Validação rápida

### 1. Renomeação (User Story 4)
- Verificar que `package.json` tem `"name": "avabot-app"`
- Verificar que `vite.config.ts` tem `base: '/avabot/'`
- Verificar que `window.Avabot.init()` funciona no widget
- Verificar que nenhuma referência a "avachat" existe no código

### 2. Primeiro acesso com cookies (User Story 3)
- Limpar cookies do navegador
- Abrir o chat na landing page
- Completar identificação (nome, email, telefone)
- Verificar cookies criados: `avabot_{slug}_resumeToken`, `avabot_{slug}_userName`, etc.

### 3. Retomada de sessão (User Story 1)
- Fechar e reabrir o navegador (ou fechar/reabrir o chat)
- Verificar mensagem "Bem-vindo de volta, [nome]"
- Verificar botões "Desejo retomar nossa última conversa" e "Iniciar uma nova conversa"
- Verificar que campo de entrada está desabilitado
- Clicar em "Desejo retomar nossa última conversa"
- Verificar que últimas 10 mensagens aparecem
- Verificar que chat funciona normalmente

### 4. Nova conversa (User Story 2)
- Fechar e reabrir o chat
- Clicar em "Iniciar uma nova conversa"
- Verificar que nova sessão foi criada sem pedir dados novamente
- Verificar que cookie `resumeToken` foi atualizado

### 5. Edge cases
- Deletar cookie `resumeToken` manualmente e reabrir → deve iniciar fluxo de primeiro acesso
- Usar token inválido → deve limpar cookies e iniciar identificação
- Fechar chat sem escolher opção, reabrir → deve mostrar botões novamente

## Arquivos-chave modificados

| Arquivo | Mudança |
|---------|---------|
| `package.json` | Nome do pacote → `avabot-app` |
| `vite.config.ts` | Base path → `/avabot/` |
| `vite.config.widget.ts` | Library name → `Avabot` |
| `src/widget/entry.tsx` | IDs e interface renomeados |
| `src/main.tsx` | Basename → `/avabot` |
| `src/Services/AuthService.ts` | Storage key → `avabot:auth-token` |
| `src/stores/useAgentStore.ts` | Storage key → `avabot:selected-agent` |
| `src/Services/AgentService.ts` | Novo método `resumeSession()` |
| `src/Services/CookieService.ts` | **Novo** — gerenciamento de cookies por agente |
| `src/hooks/useChatWidget.ts` | Nova fase `returning`, lógica de botões e retomada |
| `src/types/chatSession.ts` | Tipo `ChatSessionResumeInfo` adicionado |
| `nginx.conf` | Paths → `/avabot/` |
