# Quickstart: Autenticação JWT e Painel Administrativo

**Branch**: `007-admin-auth-panel` | **Date**: 2026-04-09

## Pré-requisitos

- Node.js 18+
- Backend Avachat rodando em `https://localhost:5000` (com endpoints de auth, agents, sessions, files, search)
- Variáveis de ambiente configuradas em `.env`

## Setup

```bash
# Clonar e instalar
git clone <repo-url>
cd avachat-app
git checkout 007-admin-auth-panel
npm install

# Verificar .env
# VITE_API_URL=https://localhost:5000
# VITE_WS_URL=wss://localhost:5000

# Rodar dev server
npm run dev
```

## Fluxo Principal

1. Acessar `http://localhost:5173/avachat/admin` → redireciona para `/avachat/login`
2. Fazer login com credenciais do backend → redirecionado para Dashboard
3. Selecionar agente na navbar → sidebar contextual ativada
4. Navegar entre: Configurações, Sessões, Base de Conhecimento

## Rotas

| Rota | Página | Auth |
|------|--------|------|
| `/` | LandingPage | Não |
| `/login` | LoginPage | Não |
| `/chat/:slug` | ChatPage | Não |
| `/admin` | DashboardPage | Sim |
| `/admin/agents` | AgentListPage | Sim |
| `/admin/agents/new` | AgentFormPage | Sim |
| `/admin/agents/:id/edit` | AgentFormPage | Sim |
| `/admin/settings` | AgentSettingsPage | Sim (requer agente selecionado) |
| `/admin/sessions` | SessionListPage | Sim (requer agente selecionado) |
| `/admin/sessions/:sessionId` | SessionDetailPage | Sim |
| `/admin/knowledge/files` | KnowledgeFilesPage | Sim (requer agente selecionado) |
| `/admin/knowledge/search` | KnowledgeSearchPage | Sim (requer agente selecionado) |

## Arquivos Novos

- `src/types/auth.ts` — Tipos de autenticação
- `src/Services/AuthService.ts` — Serviço de login/logout
- `src/stores/useAuthStore.ts` — Estado de autenticação (Zustand)
- `src/components/common/ProtectedRoute.tsx` — Guard de rotas
- `src/components/admin/AdminLayout.tsx` — Layout com sidebar + navbar
- `src/components/admin/AdminSidebar.tsx` — Menu lateral
- `src/components/admin/AdminNavbar.tsx` — Barra superior
- `src/components/admin/AgentSelector.tsx` — Seletor de agente
- `src/pages/auth/LoginPage.tsx` — Tela de login
- `src/pages/admin/DashboardPage.tsx` — Dashboard pós-login
- `src/pages/admin/AgentSettingsPage.tsx` — Configurações do agente
- `src/pages/admin/SessionListPage.tsx` — Lista de sessões
- `src/pages/admin/SessionDetailPage.tsx` — Histórico de conversa
- `src/pages/admin/KnowledgeSearchPage.tsx` — Busca na base

## Arquivos Modificados

- `src/App.tsx` — Novas rotas + AdminLayout wrapper
- `src/Services/AgentService.ts` — Corrigir endpoints + auth headers
- `src/Services/ChatHistoryService.ts` — Corrigir endpoints + params paginação
- `src/Services/KnowledgeFileService.ts` — Corrigir endpoints + auth headers
- `src/stores/useAgentStore.ts` — Adicionar selectedAgent
- `src/pages/admin/AgentListPage.tsx` — Melhorar layout
- `src/pages/admin/AgentFormPage.tsx` — Melhorar layout
- `src/components/admin/AgentForm.tsx` — Melhorar layout
