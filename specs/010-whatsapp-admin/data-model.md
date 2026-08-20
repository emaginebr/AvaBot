# Data Model: Área Administrativa WhatsApp

**Date**: 2026-04-12  
**Feature**: 010-whatsapp-admin

## Entidades

### WhatsappQrCodeInfo (novo tipo no frontend)

Resposta do endpoint de QR Code:

| Campo | Tipo | Descrição |
|-------|------|-----------|
| agentSlug | string | Slug do agente |
| qrCode | string | QR Code em formato base64 (imagem PNG) |

### WhatsappStatusInfo (novo tipo no frontend)

Resposta do endpoint de status:

| Campo | Tipo | Descrição |
|-------|------|-----------|
| agentSlug | string | Slug do agente |
| status | string | Status textual ("CONNECTED", "STARTING", "DISCONNECTED", etc.) |
| isConnected | boolean | Se a sessão está ativa |

### AgentInfo (sem alterações)

O modelo Agent do backend possui `WhatsappToken` mas este campo não é exibido nem manipulado diretamente pelo frontend nesta feature. O token é gerenciado internamente pelo WhatsappService do backend.

## Estados

### Estado da Sessão WhatsApp

```
[Desconectado] → (clica Iniciar Sessão) → [Iniciando]
[Iniciando] → (QR Code gerado) → [Aguardando Escaneamento]
[Aguardando Escaneamento] → (QR escaneado) → [Conectado]
[Aguardando Escaneamento] → (QR expirado / erro) → [Desconectado]
[Conectado] → (clica Desconectar) → [Desconectando]
[Desconectando] → (sucesso) → [Desconectado]
[Conectado] → (sessão encerrada externamente) → [Desconectado] (detectado via polling)
```

### Mapeamento de Status do Backend para UI

| Status Backend | Status UI | Cor | Descrição |
|---------------|-----------|-----|-----------|
| "CONNECTED" | Conectado | Verde | Sessão ativa e funcional |
| "STARTING" | Conectando... | Amarelo | Sessão em processo de inicialização |
| "QRCODE" | Aguardando QR | Amarelo | QR Code pronto para escaneamento |
| "DISCONNECTED" | Desconectado | Cinza | Sem sessão ativa |
| Outros | Desconectado | Cinza | Tratados como desconectado |
