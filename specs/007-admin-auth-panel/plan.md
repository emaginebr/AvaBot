# Implementation Plan: Autenticação JWT e Painel Administrativo

**Branch**: `007-admin-auth-panel` | **Date**: 2026-04-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-admin-auth-panel/spec.md`

## Summary

Implementar autenticação JWT com tela de login, reestruturar a área administrativa com sidebar de navegação, seletor de agente na navbar, dashboard pós-login, visualização de histórico de conversa em página dedicada, e corrigir endpoints do frontend para corresponder ao backend real. A abordagem utiliza Zustand para estado de autenticação, rotas protegidas via componente wrapper, e layout administrativo com sidebar persistente.

## Technical Context

**Language/Version**: TypeScript 6.0.2 + React 19.x  
**Primary Dependencies**: React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, react-markdown 10.x, react-dropzone 15.x  
**Storage**: localStorage (JWT token)  
**Testing**: ESLint (linting)  
**Target Platform**: Web (SPA) — browsers modernos  
**Project Type**: web-service (frontend SPA)  
**Performance Goals**: Carregamento < 3s, interações < 1s  
**Constraints**: Sem Docker local, sem CSS frameworks além de Tailwind, Zustand como state manager  
**Scale/Scope**: Usuário administrativo único, ~10 telas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Notas |
|-----------|--------|-------|
| I. Stack Tecnológica | PASS | React 19, TS 6, Vite 8, Tailwind 4, Zustand 5 — tudo conforme |
| II. Case Sensitivity | PASS | Services/, hooks/, types/, Contexts/ — respeitados |
| III. Convenções de Código | PASS | PascalCase componentes, camelCase funções, const, arrow functions |
| IV. Autenticação | NEEDS UPDATE | Constitution diz `Basic {token}` mas JWT usa `Bearer {token}`. Token key precisa ser atualizada para JWT |
| V. Variáveis de Ambiente | PASS | VITE_API_URL e VITE_WS_URL já existem |
| Skill react-architecture | WILL USE | Para novas entidades (Auth) |

**Nota sobre Auth (Princípio IV)**: A constitution atual define `Authorization: Basic {token}` e key `"login-with-metamask:auth"`. O backend JWT retorna token Bearer. O plano usará `Authorization: Bearer {token}` e uma nova storage key `"avachat:auth-token"`. A constitution deve ser atualizada após implementação.

## Project Structure

### Documentation (this feature)

```text
specs/007-admin-auth-panel/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── components/
│   ├── admin/
│   │   ├── AgentForm.tsx              # Existente — melhorar layout
│   │   ├── FileStatusBadge.tsx        # Existente
│   │   ├── FileUpload.tsx             # Existente
│   │   ├── AdminLayout.tsx            # NOVO — layout com sidebar + navbar
│   │   ├── AdminSidebar.tsx           # NOVO — menu lateral de navegação
│   │   ├── AdminNavbar.tsx            # NOVO — navbar com seletor de agente e logout
│   │   └── AgentSelector.tsx          # NOVO — dropdown seletor de agente na navbar
│   ├── chat/                          # Existente — sem alterações
│   └── common/
│       ├── NotFoundPage.tsx           # Existente
│       ├── UnavailablePage.tsx        # Existente
│       └── ProtectedRoute.tsx         # NOVO — wrapper de rota protegida
│
├── pages/
│   ├── LandingPage.tsx                # Existente — sem alterações
│   ├── AbipescaPage.tsx               # Existente — sem alterações
│   ├── auth/
│   │   └── LoginPage.tsx              # NOVO — tela de login
│   ├── admin/
│   │   ├── DashboardPage.tsx          # NOVO — dashboard com resumo
│   │   ├── AgentListPage.tsx          # Existente — melhorar layout
│   │   ├── AgentFormPage.tsx          # Existente — melhorar layout
│   │   ├── AgentSettingsPage.tsx      # NOVO — editar/excluir agente selecionado
│   │   ├── SessionListPage.tsx        # NOVO — renomear/refatorar ChatHistoryPage
│   │   ├── SessionDetailPage.tsx      # NOVO — histórico de conversa (página dedicada)
│   │   ├── KnowledgeFilesPage.tsx     # Existente — sem grandes alterações
│   │   └── KnowledgeSearchPage.tsx    # NOVO — busca na base de conhecimento
│   └── chat/
│       └── ChatPage.tsx               # Existente — sem alterações
│
├── Services/
│   ├── AgentService.ts                # Existente — corrigir endpoints + adicionar auth headers
│   ├── AuthService.ts                 # NOVO — login, token management
│   ├── ChatHistoryService.ts          # Existente — corrigir endpoints e params de paginação
│   └── KnowledgeFileService.ts        # Existente — corrigir endpoints + adicionar auth headers
│
├── stores/
│   ├── useAgentStore.ts               # Existente — adicionar agente selecionado
│   ├── useAuthStore.ts                # NOVO — estado de autenticação
│   └── useKnowledgeFileStore.ts       # Existente
│
├── types/
│   ├── agent.ts                       # Existente
│   ├── auth.ts                        # NOVO — tipos de autenticação
│   ├── chatMessage.ts                 # Existente
│   ├── chatSession.ts                 # Existente
│   ├── knowledgeFile.ts               # Existente
│   └── result.ts                      # Existente
│
└── hooks/
    ├── useChat.ts                     # Existente
    ├── useChatWidget.ts               # Existente
    └── useWebSocket.ts                # Existente
```

**Structure Decision**: Frontend SPA existente. Novos arquivos seguem a estrutura já estabelecida com separação por domínio (admin, auth, chat). Layout administrativo via componente wrapper (AdminLayout) com sidebar e navbar.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Auth header diverge da Constitution (Bearer vs Basic) | Backend JWT retorna Bearer token | Não é possível usar Basic com JWT — padrão do protocolo |
| Storage key diferente da Constitution | Key atual é para MetaMask, não aplicável | Manter key antiga causaria confusão semântica |
