# Implementation Plan: Área Administrativa WhatsApp

**Branch**: `010-whatsapp-admin` | **Date**: 2026-04-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-whatsapp-admin/spec.md`

## Summary

Criar uma página administrativa "WhatsApp" no painel admin do frontend, posicionada logo abaixo de "Bot Telegram" no menu lateral. A página permite iniciar uma sessão WhatsApp, exibir QR Code para escaneamento, monitorar o status da conexão via polling, e desconectar a sessão. O backend já possui todos os endpoints necessários via WPP Connect — esta feature é 100% frontend.

## Technical Context

**Language/Version**: TypeScript 6.x + React 19.x  
**Primary Dependencies**: React Router 7.x, Zustand 5.x, Tailwind CSS 4.x, sonner 2.x  
**Storage**: N/A (dados persistidos via API backend existente)  
**Testing**: Manual (padrão do projeto atual)  
**Target Platform**: Web SPA (navegador desktop/mobile)  
**Project Type**: Web application (frontend SPA)  
**Performance Goals**: QR Code visível em < 10s, polling de status a cada 3s durante conexão  
**Constraints**: Seguir padrões existentes do projeto (constitution), sem novas dependências  
**Scale/Scope**: 1 nova página, 1 novo menu item, ~4 novos métodos no service, ~2 novos tipos

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Status | Notas |
|-----------|--------|-------|
| I. Stack Tecnológica | PASS | React 19, TS 6, Tailwind 4, Zustand 5, Vite 8 — nenhuma nova dependência |
| II. Case Sensitivity | PASS | Novos arquivos seguirão `Services/`, `pages/admin/`, `types/` |
| III. Convenções de Código | PASS | PascalCase componentes, camelCase funções, interface (não type) |
| IV. Autenticação | PASS | Usa AuthService.getAuthHeaders() existente |
| V. Variáveis de Ambiente | PASS | Usa VITE_API_URL existente |
| Skill react-architecture | N/A | Não cria nova entidade/contexto — reutiliza AgentService e useAgentStore existentes |

**Gate Result**: PASS — nenhuma violação.

## Project Structure

### Documentation (this feature)

```text
specs/010-whatsapp-admin/
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
├── pages/admin/
│   └── WhatsappPage.tsx             # Nova página admin
├── components/admin/
│   └── AdminSidebar.tsx             # Adicionar menu item
├── Services/
│   └── AgentService.ts              # Adicionar métodos WhatsApp
├── types/
│   └── agent.ts                     # Adicionar tipos WhatsApp
└── App.tsx                          # Adicionar rota
```

**Structure Decision**: Segue a estrutura existente do projeto. Apenas 1 nova página e extensões em arquivos existentes. Padrão idêntico à feature 005-telegram-bot-admin.
