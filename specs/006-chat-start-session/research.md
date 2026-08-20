# Research: Fluxo Start Session

**Branch**: `006-chat-start-session` | **Date**: 2026-04-08

## R1: Endpoint Start Session

**Decision**: Usar `POST /api/agents/{slug}/sessions`

**Contrato do backend**:
- Request: `{ userName?: string, userEmail?: string, userPhone?: string }`
- Response 201: `{ sucesso: true, dados: { chatSessionId: number, ... } }`
- Response 400: campos obrigatorios faltando ou agente inativo
- Response 404: agente nao encontrado

**Rationale**: Endpoint ja existe no backend. Validacao server-side
garante que campos obrigatorios estao presentes.

## R2: WebSocket com Session ID

**Decision**: Conectar com `?sessionId=X` na query string.

**Comportamento do backend**:
- Com sessionId: envia `ready` imediatamente, sem `collect_data`
- Sem sessionId: fluxo legado (collect_data + identify)

**Rationale**: O novo fluxo e mais limpo — separa coleta (REST)
de chat (WebSocket). O frontend controla a coleta.

## R3: Remocao do Fluxo Legado no Frontend

**Decision**: Remover `collect_data`/`identify` do useChat. O
backend mantem compatibilidade, mas o frontend nao usa mais.

**Rationale**: Simplifica o codigo. A coleta agora e 100% frontend.

## R4: Mensagens Fixas de Coleta

**Decision**: Usar mensagens fixas definidas no frontend:
- Nome: "Qual seu nome?"
- Email: "Bem vindo {nome}, qual seu email?"
- Telefone: "Qual seu numero de telefone? Com DDD"
- Conclusao: "Muito obrigado pelas informacoes, agora em que posso ajudar?"

**Rationale**: Requisito explicito do usuario. Mensagens nao vem
do backend.
