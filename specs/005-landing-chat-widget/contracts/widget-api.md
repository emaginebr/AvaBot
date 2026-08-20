# Chat Widget Component API

**Branch**: `005-landing-chat-widget` | **Date**: 2026-04-08

## ChatBubble

Balao flutuante que serve como trigger para o widget.

```tsx
interface ChatBubbleProps {
  message: string       // Mensagem exibida no balao
  onClick: () => void   // Callback ao clicar
  isOpen: boolean       // Se o widget esta aberto (oculta o balao)
}
```

**Posicao**: `position: fixed`, bottom-right.
**Comportamento**: Oculto quando o widget esta aberto.

## ChatWidget

Container principal do widget de chat.

```tsx
interface ChatWidgetProps {
  slug: string          // Slug do agente (ex: "bia")
  greeting: string      // Mensagem de boas-vindas no balao
}
```

**Responsabilidades**:
- Buscar `chat-config` do agente via AgentService
- Gerenciar estado aberto/fechado
- Renderizar ChatBubble quando fechado
- Renderizar painel de chat quando aberto
- Gerenciar coleta conversacional de dados
- Conectar via WebSocket usando useChat

## Coleta Conversacional

Fluxo interno (nao exposto como prop):

1. Recebe `collect_data` com campos
2. Exibe mensagem do assistente pedindo primeiro campo
3. Aguarda resposta do usuario no chat
4. Valida e armazena resposta
5. Repete para proximo campo
6. Envia `identify` com todos os dados
7. Transiciona para chat normal apos `ready`

## Integracao na Landing Page

```tsx
// Uso na LandingPage:
<ChatWidget slug="bia" greeting="Oi, eu sou a Bia. Em que posso ajudar?" />
```

O widget e totalmente autonomo — a landing page so precisa
renderiza-lo com o slug e a mensagem de greeting.
