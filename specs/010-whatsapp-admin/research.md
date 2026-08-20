# Research: Área Administrativa WhatsApp

**Date**: 2026-04-12  
**Feature**: 010-whatsapp-admin

## 1. Backend API Endpoints Disponíveis

**Decision**: Utilizar os 4 endpoints WhatsApp já existentes no backend.

**Rationale**: O backend já implementa toda a integração com WPP Connect. Nenhum novo endpoint é necessário.

**Endpoints confirmados** (todos usam slug do agente, todos autenticados exceto webhook):

| Endpoint | Método | Resposta |
|----------|--------|----------|
| `/whatsapp/{slug}/start-session` | POST | `Result<object>` (inicia sessão no WPP Connect) |
| `/whatsapp/{slug}/qrcode` | GET | `Result<WhatsappQrCodeInfo>` (QR Code base64) |
| `/whatsapp/{slug}/status` | GET | `Result<WhatsappStatusInfo>` (status da conexão) |
| `/whatsapp/{slug}/disconnect` | POST | `Result<object>` (encerra sessão) |

**Alternatives considered**: Criar endpoint único que retorna status + QR Code — rejeitado, pois os endpoints separados já existem e seguem responsabilidade única.

## 2. DTOs do Backend

**Decision**: Criar interfaces TypeScript correspondentes aos DTOs do backend.

**WhatsappQrCodeInfo**:
- `agentSlug` (string) — identificador do agente
- `qrCode` (string) — imagem QR Code em base64

**WhatsappStatusInfo**:
- `agentSlug` (string) — identificador do agente
- `status` (string) — texto do status ("CONNECTED", "STARTING", "DISCONNECTED", etc.)
- `isConnected` (boolean) — se a sessão está ativa

## 3. Fluxo de Conexão WhatsApp

**Decision**: Implementar fluxo sequencial: Start Session → Get QR Code → Poll Status → Connected.

**Rationale**: O WPP Connect requer que a sessão seja iniciada primeiro, depois o QR Code é gerado, e o status muda quando escaneado. O polling é necessário pois não há WebSocket/SSE disponível no frontend.

**Flow**:
1. Usuário clica "Iniciar Sessão" → `POST /whatsapp/{slug}/start-session`
2. Após sucesso, buscar QR Code → `GET /whatsapp/{slug}/qrcode`
3. Iniciar polling de status a cada 3 segundos → `GET /whatsapp/{slug}/status`
4. Quando `isConnected === true`, parar polling e esconder QR Code
5. Exibir status "Conectado"

**Alternatives considered**: WebSocket para status em tempo real — rejeitado, pois o projeto não usa WebSocket no admin panel e o polling é suficiente para este caso.

## 4. Formato do QR Code

**Decision**: Renderizar base64 como imagem usando tag `<img>` com data URI.

**Rationale**: O backend retorna o QR Code como string base64 (via WppConnectService que extrai o campo `qrcode` ou `base64` da resposta). Pode ser renderizado diretamente como `<img src="data:image/png;base64,{qrCode}" />`.

**Nota**: Se o QR Code vier como texto (não base64 de imagem), pode ser necessário usar uma biblioteca como `qrcode.react`. Porém, baseado no código do backend (`GetQrCodeAsync` retorna campo `qrcode` do WPP Connect), o formato é base64 de imagem PNG.

## 5. Polling Strategy

**Decision**: Usar `setInterval` com cleanup no `useEffect`, intervalo de 3 segundos.

**Rationale**: 3 segundos é rápido o suficiente para UX responsiva sem sobrecarregar o backend. O polling para quando: (a) `isConnected === true`, (b) o componente desmonta, (c) o agente muda.

**Alternatives considered**: `setTimeout` recursivo — equivalente funcional, mas `setInterval` é mais simples para intervalo fixo.

## 6. Diferença de Identificador: Slug vs ID

**Decision**: Usar o `slug` do agente selecionado (de `useAgentStore`) nas chamadas API.

**Rationale**: Diferente dos endpoints Telegram que usam `agentId` (numérico), os endpoints WhatsApp usam `slug` (string). O `selectedAgent` do store já possui o campo `slug`.
