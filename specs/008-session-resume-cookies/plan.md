# Implementation Plan: Renomeação para AvaBot + Gestão de Sessão com Cookies e Retomada de Conversa

**Branch**: `008-session-resume-cookies` | **Date**: 2026-04-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-session-resume-cookies/spec.md`

## Summary

Renomear o projeto frontend de "avachat" para "avabot" em todas as referências (incluindo API pública do widget), implementar persistência de dados de sessão do usuário em cookies do navegador separados por agente (slug), e criar fluxo de retomada de conversa usando o endpoint `resumeSession` do backend, com opções interativas (botões) para o usuário retornante escolher entre retomar a última conversa ou iniciar uma nova.

## Technical Context

**Language/Version**: TypeScript 6.0.2 + React 19.x  
**Primary Dependencies**: React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, react-markdown 10.x, Vite 8.x  
**Storage**: Cookies (dados de sessão por agente), localStorage (auth token admin)  
**Testing**: Nenhum framework configurado (validação manual)  
**Target Platform**: Web (SPA + widget embeddable IIFE)  
**Project Type**: Web application (SPA + widget standalone)  
**Performance Goals**: Reconhecimento de retornante < 2s, carregamento de mensagens < 3s  
**Constraints**: Sem Docker no ambiente local, prefixo `VITE_` para env vars  
**Scale/Scope**: ~50 arquivos TypeScript, 24 ocorrências de "avachat" para renomear

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Notas |
|-----------|--------|-------|
| I. Stack Tecnológica | ✅ PASS | Nenhuma dependência nova adicionada. Usa React 19, TS 6, Tailwind 4, Zustand 5, Vite 8. |
| II. Case Sensitivity | ✅ PASS | Novo `CookieService.ts` vai em `src/Services/` (Uppercase S). Novos types em `src/types/` (lowercase t). |
| III. Convenções de Código | ✅ PASS | Interfaces com `interface`, arrow functions, camelCase para funções, PascalCase para componentes. |
| IV. Autenticação e Segurança | ⚠️ JUSTIFIED | O resumeToken será armazenado em cookies, não localStorage. **Justificativa**: resumeToken não é token de autenticação — é identificador de sessão pública (`[AllowAnonymous]`). O token de auth admin permanece em localStorage. Ver research.md R6. |
| V. Variáveis de Ambiente | ✅ PASS | Nenhuma variável nova. Usa `VITE_API_URL` e `VITE_WS_URL` existentes. |
| Skills Obrigatórias | ✅ N/A | `react-architecture` é para criar Types+Service+Context+Hook+Provider de novas entidades. Esta feature modifica hooks/services existentes e adiciona um CookieService utilitário — não cria uma nova entidade com Context/Provider. |

**Post-Phase 1 Re-check**: Mesmas conclusões. Nenhuma nova violação introduzida pelo design.

## Project Structure

### Documentation (this feature)

```text
specs/008-session-resume-cookies/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── session-service.md  # Phase 1 output
├── checklists/
│   └── requirements.md  # Quality checklist
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── components/
│   └── chat/
│       ├── ChatWidget.tsx        # Modificar: adicionar lógica de retornante
│       ├── ChatWindow.tsx        # Modificar: suporte a inputDisabled e botões
│       ├── ActionButtons.tsx     # NOVO: componente de botões de ação
│       ├── ChatBubble.tsx        # Sem alteração
│       ├── AvatarBubble.tsx      # Sem alteração
│       └── MessageBubble.tsx     # Sem alteração
├── hooks/
│   ├── useChatWidget.ts          # Modificar: nova fase 'returning', lógica de cookies
│   ├── useChat.ts                # Modificar: suporte a carregar mensagens históricas
│   └── useWebSocket.ts           # Sem alteração
├── Services/
│   ├── AgentService.ts           # Modificar: novo método resumeSession()
│   ├── CookieService.ts          # NOVO: gerenciamento de cookies por agente
│   ├── AuthService.ts            # Modificar: renomear chave storage
│   ├── ChatHistoryService.ts     # Sem alteração
│   └── KnowledgeFileService.ts   # Sem alteração
├── stores/
│   ├── useAgentStore.ts          # Modificar: renomear chave storage
│   └── useAuthStore.ts           # Sem alteração
├── types/
│   ├── chatSession.ts            # Modificar: adicionar ChatSessionResumeInfo
│   └── chatMessage.ts            # Sem alteração
├── widget/
│   └── entry.tsx                 # Modificar: renomear IDs, interface, global
├── pages/
│   └── (sem alteração nos componentes de página)
├── App.tsx                       # Sem alteração
├── main.tsx                      # Modificar: renomear basename
└── index.css                     # Sem alteração

# Arquivos de configuração na raiz
├── package.json                  # Modificar: renomear nome do pacote
├── vite.config.ts                # Modificar: renomear base path
├── vite.config.widget.ts         # Modificar: renomear library name
├── nginx.conf                    # Modificar: renomear paths
├── CLAUDE.md                     # Modificar: renomear título
└── README.md                     # Modificar: renomear referências
```

**Structure Decision**: Projeto mantém estrutura existente de SPA + widget. Nenhum novo diretório de alto nível. Novos arquivos: `src/Services/CookieService.ts` e `src/components/chat/ActionButtons.tsx`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Cookies para resumeToken (Constitution IV) | Usuário solicitou explicitamente cookies. resumeToken é identificador de sessão pública, não token de auth. | localStorage funcionaria mas conflita com requisito explícito do usuário. Auth token admin permanece em localStorage. |
