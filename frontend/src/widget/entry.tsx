import { createRoot } from 'react-dom/client'
import ChatWidget from '../components/chat/ChatWidget'
import AvatarBubble from '../components/chat/AvatarBubble'
import { setApiUrl } from '../Services/AgentService'
import { setWsUrl } from '../hooks/useChatWidget'
import widgetStyles from './widget.css?inline'

interface AvabotInitOptions {
  slug: string
  greeting?: string
  color?: string
  agentAvatar?: string
  avatarImage?: string
  apiUrl?: string
  wsUrl?: string
}

let destroyFn: (() => void) | null = null

function init(options: AvabotInitOptions) {
  if (destroyFn) {
    console.warn('[Avabot] Widget já inicializado. Chame Avabot.destroy() antes de reinicializar.')
    return
  }

  const {
    slug,
    greeting = 'Olá! Como posso ajudar?',
    color,
    agentAvatar,
    avatarImage,
    apiUrl = 'https://avabot.net/api',
    wsUrl = 'wss://avabot.net/api',
  } = options

  setApiUrl(apiUrl)
  setWsUrl(wsUrl)

  // Host element
  const host = document.createElement('div')
  host.id = 'avabot-widget-host'
  host.style.cssText = 'position:fixed;z-index:2147483647;pointer-events:none;inset:0;'
  document.body.appendChild(host)

  // Shadow DOM
  const shadow = host.attachShadow({ mode: 'open' })

  // Inject compiled Tailwind CSS
  const styleEl = document.createElement('style')
  styleEl.textContent = widgetStyles
  shadow.appendChild(styleEl)

  // Google Fonts (must be in light DOM for font loading)
  if (!document.getElementById('avabot-fonts')) {
    const link = document.createElement('link')
    link.id = 'avabot-fonts'
    link.rel = 'stylesheet'
    link.href = 'https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,400;0,9..40,500;0,9..40,600;0,9..40,700;1,9..40,400&family=Instrument+Serif:ital@0;1&display=swap'
    document.head.appendChild(link)
  }

  // Mount point inside shadow
  const mountPoint = document.createElement('div')
  mountPoint.style.cssText = 'pointer-events:auto;font-family:DM Sans,system-ui,sans-serif;'
  shadow.appendChild(mountPoint)

  // Render
  const root = createRoot(mountPoint)
  root.render(
    <ChatWidget
      slug={slug}
      greeting={greeting}
      color={color}
      agentAvatar={agentAvatar}
      renderBubble={avatarImage ? (props) => (
        <AvatarBubble
          message={props.message}
          onClick={props.onClick}
          isOpen={props.isOpen}
          color={props.color}
          avatarSrc={avatarImage}
        />
      ) : undefined}
    />,
  )

  destroyFn = () => {
    root.unmount()
    host.remove()
    const fontLink = document.getElementById('avabot-fonts')
    if (fontLink) fontLink.remove()
    destroyFn = null
  }
}

function destroy() {
  if (destroyFn) {
    destroyFn()
  }
}

;(window as unknown as Record<string, unknown>).Avabot = { init, destroy }
