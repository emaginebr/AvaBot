# API Contract: WhatsApp Admin

**Date**: 2026-04-12  
**Feature**: 010-whatsapp-admin

## Endpoints Consumidos pelo Frontend

Todos os endpoints requerem header `Authorization: Basic {token}` via `AuthService.getAuthHeaders()`.
Todos usam o **slug** do agente como identificador (não o ID numérico).

### 1. Iniciar Sessão

```
POST /whatsapp/{slug}/start-session
Authorization: Basic {token}

Request Body: (nenhum)

Response 200: Result<object>
{
  "sucesso": true,
  "mensagem": "string",
  "erros": [],
  "dados": {}
}

Response 404: Agente não encontrado
Response 500: Erro ao iniciar sessão
```

### 2. Obter QR Code

```
GET /whatsapp/{slug}/qrcode
Authorization: Basic {token}

Response 200: Result<WhatsappQrCodeInfo>
{
  "sucesso": true,
  "mensagem": "string",
  "erros": [],
  "dados": {
    "agentSlug": "string",
    "qrCode": "string (base64 encoded PNG image)"
  }
}

Response 404: Agente não encontrado
Response 400: Sessão não iniciada / QR Code não disponível
Response 500: Erro ao obter QR Code
```

### 3. Verificar Status

```
GET /whatsapp/{slug}/status
Authorization: Basic {token}

Response 200: Result<WhatsappStatusInfo>
{
  "sucesso": true,
  "mensagem": "string",
  "erros": [],
  "dados": {
    "agentSlug": "string",
    "status": "string (CONNECTED | STARTING | DISCONNECTED | QRCODE)",
    "isConnected": boolean
  }
}

Response 404: Agente não encontrado
Response 500: Erro ao verificar status
```

### 4. Desconectar Sessão

```
POST /whatsapp/{slug}/disconnect
Authorization: Basic {token}

Request Body: (nenhum)

Response 200: Result<object>
{
  "sucesso": true,
  "mensagem": "string",
  "erros": [],
  "dados": {}
}

Response 404: Agente não encontrado
Response 500: Erro ao desconectar
```

## Tipos TypeScript (a adicionar no frontend)

```typescript
interface WhatsappQrCodeInfo {
  agentSlug: string;
  qrCode: string;
}

interface WhatsappStatusInfo {
  agentSlug: string;
  status: string;
  isConnected: boolean;
}
```
