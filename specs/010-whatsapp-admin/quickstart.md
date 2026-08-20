# Quickstart: Área Administrativa WhatsApp

**Date**: 2026-04-12  
**Feature**: 010-whatsapp-admin

## Pré-requisitos

- Node.js instalado
- Backend rodando com WPP Connect acessível
- Um agente criado no sistema
- Um celular com WhatsApp instalado (para testar escaneamento)

## Setup

```bash
cd /path/to/avabot-app
npm install
npm run dev
```

## Arquivos a Criar/Modificar

### Novos Arquivos
1. `src/pages/admin/WhatsappPage.tsx` — Página principal da feature

### Arquivos a Modificar
1. `src/types/agent.ts` — Adicionar interfaces WhatsappQrCodeInfo e WhatsappStatusInfo
2. `src/Services/AgentService.ts` — Adicionar métodos: startWhatsappSession, getWhatsappQrCode, getWhatsappStatus, disconnectWhatsapp
3. `src/components/admin/AdminSidebar.tsx` — Adicionar item "WhatsApp" no menu
4. `src/App.tsx` — Adicionar rota `/admin/whatsapp`

## Ordem de Implementação Sugerida

1. **Tipos** — Adicionar interfaces em `types/agent.ts`
2. **Service** — Adicionar métodos de API em `AgentService.ts`
3. **Rota** — Adicionar rota em `App.tsx`
4. **Menu** — Adicionar item no `AdminSidebar.tsx`
5. **Página** — Implementar `WhatsappPage.tsx`

## Teste Manual

1. Acessar o painel admin
2. Selecionar um agente
3. Clicar em "WhatsApp" no menu lateral
4. Verificar status inicial (deve mostrar "Desconectado")
5. Clicar "Iniciar Sessão" — verificar que QR Code é exibido
6. Escanear QR Code com celular — verificar que status muda para "Conectado"
7. Clicar "Desconectar" — confirmar — verificar que status volta para "Desconectado"
8. Testar troca de agente enquanto QR Code exibido — polling deve parar
