# Research: Landing Page com Chat Widget Bia

**Branch**: `005-landing-chat-widget` | **Date**: 2026-04-08

## R1: Stack Real vs Constitution

**Decision**: Seguir a stack real do projeto, nao a constitution
(que esta desatualizada).

**Realidade do projeto**:

| Tecnologia | Constitution | Real (package.json) |
|---|---|---|
| React | 18.x | 19.2.4 |
| React Router | 6.x | 7.14.0 |
| Vite | 6.x | 8.0.4 |
| TypeScript | 5.x | 6.0.2 |
| CSS Framework | Bootstrap 5.x | Tailwind CSS 4.2.2 |
| State Management | Context API | Zustand 5.0.12 |
| i18next | 25.x | Nao instalado |

**Rationale**: O codigo existente usa Tailwind + Zustand
consistentemente. Mudar para Bootstrap/Context API quebraria todo
o projeto. A constitution foi baseada em um template generico.

**Alternatives**: Nenhuma — manter consistencia com o existente.

## R2: Componentes Reutilizaveis

**Decision**: Reutilizar hooks e componentes de chat existentes.

**Componentes disponiveis**:
- `hooks/useChat.ts` — gerencia mensagens, streaming, coleta de dados
- `hooks/useWebSocket.ts` — conexao WebSocket de baixo nivel
- `components/chat/ChatWindow.tsx` — UI de mensagens + input
- `components/chat/MessageBubble.tsx` — bolha de mensagem individual
- `components/chat/TypingIndicator.tsx` — animacao de digitacao
- `Services/AgentService.ts` — `getChatConfig(slug)` para config

**Rationale**: Toda a logica de WebSocket, streaming e mensagens ja
esta implementada e funcional. Criar uma duplicata seria desperdicio.

**Alternatives**: Reimplementar do zero — rejeitado por duplicacao.

## R3: Coleta Conversacional vs Formulario

**Decision**: Implementar coleta de dados como conversa dentro do chat,
substituindo o UserDataForm atual no contexto do widget.

**Abordagem**:
- Quando o servidor envia `collect_data` com campos requeridos, o
  widget exibe mensagens do "assistente" pedindo cada campo.
- O usuario responde no chat normalmente.
- O frontend valida e coleta as respostas localmente.
- Apos coletar todos os campos, envia a mensagem `identify` ao servidor.
- O fluxo e gerenciado por um novo hook ou extensao do useChat.

**Rationale**: A spec exige coleta conversacional, nao formulario.
O UserDataForm existente pode continuar sendo usado no ChatPage
(rota /chat/:slug) sem alteracao.

**Alternatives**: Modificar o UserDataForm para parecer chat —
rejeitado por ser uma solucao visual, nao comportamental.

## R4: Arquitetura do Widget

**Decision**: Widget como componente autonomo com estado proprio.

**Estrutura**:
- `ChatBubble` — balao flutuante (trigger)
- `ChatWidget` — container do widget (overlay)
- O widget usa `useChat` internamente
- O estado (aberto/fechado, mensagens) persiste na sessao

**Rationale**: Componente separado permite reutilizacao futura em
outras paginas, conforme a spec.

**Alternatives**: Embutir diretamente na landing page — rejeitado
por violar o requisito FR-010 (componente separado e reutilizavel).

## R5: Landing Page Layout

**Decision**: Usar skill `frontend-design` para o layout, conforme
instrucao do usuario.

**Estrutura basica**:
- Navbar com logo e link para Admin
- Hero section com titulo e descricao do Avachat
- Secoes informativas sobre o produto
- Footer
- ChatBubble + ChatWidget flutuantes (position: fixed)

**Rationale**: Instrucao explicita do usuario.

**Alternatives**: Nenhuma — requisito do usuario.
