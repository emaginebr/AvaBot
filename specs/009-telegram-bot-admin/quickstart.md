# Quickstart: Área Administrativa Bot Telegram

**Date**: 2026-04-11  
**Feature**: 009-telegram-bot-admin

## Pré-requisitos

- Node.js instalado
- Backend rodando (endpoints Telegram disponíveis)
- Um agente criado no sistema
- Um bot Telegram criado via BotFather (para testar)

## Setup

```bash
cd frontend
npm install
npm run dev
```

## Arquivos a Criar/Modificar

### Novos Arquivos
1. `src/pages/admin/TelegramBotPage.tsx` — Página principal da feature

### Arquivos a Modificar
1. `src/types/agent.ts` — Adicionar campos Telegram em AgentInfo/AgentInsertInfo + novo tipo TelegramWebhookInfo
2. `src/Services/AgentService.ts` — Adicionar métodos: setupTelegramWebhook, getTelegramWebhookInfo, regenerateTelegramSecret
3. `src/components/admin/AdminSidebar.tsx` — Adicionar item "Bot Telegram" no menu
4. `src/App.tsx` — Adicionar rota `/admin/telegram`

## Ordem de Implementação Sugerida

1. **Tipos** — Adicionar interfaces/campos em `types/agent.ts`
2. **Service** — Adicionar métodos de API em `AgentService.ts`
3. **Rota** — Adicionar rota em `App.tsx`
4. **Menu** — Adicionar item no `AdminSidebar.tsx`
5. **Página** — Implementar `TelegramBotPage.tsx`

## Teste Manual

1. Acessar o painel admin
2. Selecionar um agente
3. Clicar em "Bot Telegram" no menu lateral
4. Preencher nome do bot (ex: "MeuBotTest_bot") e token
5. Salvar — verificar que os dados persistem
6. Clicar "Configurar Webhook" — verificar feedback
7. Clicar "Verificar Webhook" — verificar informações exibidas
8. Clicar "Regenerar" no secret — verificar que mudou
