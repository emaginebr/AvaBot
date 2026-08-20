# Research: 008-session-resume-cookies

**Date**: 2026-04-10  
**Branch**: `008-session-resume-cookies`

## R1: Cookies vs localStorage para dados de sessão do chat

**Decision**: Usar cookies do navegador para armazenar resumeToken e dados do usuário, separados por slug do agente.

**Rationale**:
- O usuário solicitou explicitamente armazenamento em cookies.
- O resumeToken NÃO é um token de autenticação (JWT/auth) — é um identificador de sessão de chat para retomada de conversa. A regra da constitution "NUNCA armazenar tokens em cookies" refere-se a tokens de autenticação/autorização (JWT, Bearer tokens). O resumeToken é semanticamente diferente: é um identificador de sessão pública que não concede acesso administrativo.
- Cookies oferecem controle nativo de expiração (max-age/expires), eliminando necessidade de lógica manual de limpeza.
- O widget pode ser embarcado em sites terceiros — cookies com escopo adequado funcionam melhor neste contexto.

**Alternatives considered**:
- localStorage: Persistente, mas sem controle de expiração nativo. A regra da constitution favorece localStorage para tokens de auth, mas o resumeToken não é auth.
- sessionStorage: Descartado — não persiste entre sessões do navegador, inviabilizando o reconhecimento de usuários retornantes.

## R2: Estrutura de cookies por agente (slug)

**Decision**: Nomear cookies com prefixo baseado no slug do agente. Exemplo: `avabot_{slug}_resumeToken`, `avabot_{slug}_userName`, etc.

**Rationale**:
- Permite sessões independentes por agente no mesmo domínio.
- Evita conflitos quando o usuário interage com múltiplos agentes (ex: Ava na landing, Biia na Abipesca).
- Prefixo `avabot_` alinhado com a renomeação do projeto.

**Alternatives considered**:
- Cookie único JSON: Um cookie contendo objeto JSON com todos os agentes. Descartado por complexidade e risco de corrupção parcial.
- localStorage com chave composta: Funcionaria, mas conflita com requisito explícito de cookies.

## R3: Endpoint resumeSession do backend

**Decision**: Integrar com `GET /sessions/resume/{slug}` usando header `X-Resume-Token`.

**Rationale**:
- O endpoint já existe e está funcional no backend AvaBot.
- Retorna `ChatSessionResumeInfo` contendo: chatSessionId, agentId, userName, userEmail, userPhone, resumeToken, startedAt, endedAt, messageCount, e lista de messages (últimas 10).
- O endpoint é `[AllowAnonymous]` — não requer JWT, alinhado com o fluxo público do widget.

**Alternatives considered**: Nenhuma — endpoint já implementado e alinhado com os requisitos.

## R4: Fluxo de retomada — reconexão WebSocket

**Decision**: Após carregar mensagens via resumeSession, reconectar WebSocket usando o sessionId retornado.

**Rationale**:
- O WebSocket atual conecta via `/ws/chat/{slug}?sessionId={id}`.
- O resumeSession retorna o `chatSessionId`, que pode ser usado para reconectar.
- As mensagens carregadas são exibidas no histórico, e novas mensagens fluem pelo WebSocket.

**Alternatives considered**: Nenhuma — fluxo natural com a arquitetura existente.

## R5: Escopo da renomeação avachat → avabot

**Decision**: Renomear todas as referências, incluindo API pública do widget.

**Rationale**:
- Clarificação Q2 confirmou: renomear tudo, incluindo `window.Avabot`, `avabot-widget-host`, `AvabotInitOptions`.
- Backend já foi renomeado para AvaBot — consistência é prioridade.

**Locais identificados para renomeação** (24 ocorrências):
1. `package.json`: nome do pacote (`chatbot-app` → `avabot-app`)
2. `vite.config.ts`: `base: '/avachat/'` → `base: '/avabot/'`
3. `vite.config.widget.ts`: `name: 'Avachat'` → `name: 'Avabot'`
4. `src/widget/entry.tsx`: `avachat-widget-host` → `avabot-widget-host`, `avachat-fonts` → `avabot-fonts`, interface `AvachatInitOptions` → `AvabotInitOptions`, `window.Avachat` → `window.Avabot`
5. `src/main.tsx`: `basename="/avachat"` → `basename="/avabot"`
6. `src/stores/useAgentStore.ts`: `'avachat:selected-agent'` → `'avabot:selected-agent'`
7. `src/Services/AuthService.ts`: `'avachat:auth-token'` → `'avabot:auth-token'`
8. `nginx.conf`: `/avachat/` → `/avabot/`
9. `CLAUDE.md`: título do guidelines
10. `README.md`: referências ao nome do projeto e Docker
11. `Dockerfile`: se contém referências

**Alternatives considered**: Manter API pública com alias de compatibilidade — descartado por decisão do usuário.

## R6: Violação de constitution — cookies para resumeToken

**Decision**: Aceitar uso de cookies para resumeToken com justificativa documentada.

**Rationale**:
- A regra "NUNCA armazenar tokens em cookies — usar localStorage" na seção IV (Autenticação e Segurança) refere-se a **tokens de autenticação** (JWT, Bearer tokens para acesso admin).
- O resumeToken é um **identificador de sessão de chat pública** — não concede acesso autenticado a nenhum recurso protegido. O endpoint `resume/{slug}` é `[AllowAnonymous]`.
- O token de autenticação admin (`avabot:auth-token`) continuará em localStorage conforme a constitution.
- A distinção: auth token = acesso privilegiado → localStorage; resumeToken = continuidade de chat público → cookies (por solicitação explícita do usuário).

**Alternatives considered**: Usar localStorage para resumeToken — tecnicamente possível mas conflita com requisito explícito do usuário.
