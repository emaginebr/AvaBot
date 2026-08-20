import { useEffect, useState } from 'react'
import { AgentService } from '../../Services/AgentService'
import type { AgentChatConfigInfo } from '../../types/agent'
import useChatWidget from '../../hooks/useChatWidget'
import ChatBubble from './ChatBubble'
import ChatWindow from './ChatWindow'

interface BubbleProps {
  message: string
  onClick: () => void
  isOpen: boolean
  color?: string
}

interface ChatWidgetProps {
  slug: string
  greeting: string
  color?: string
  agentAvatar?: string
  renderBubble?: (props: BubbleProps) => React.ReactNode
}

const ChatWidget = ({ slug, greeting, color = '#3668fc', agentAvatar, renderBubble }: ChatWidgetProps) => {
  const [isOpen, setIsOpen] = useState(false)
  const [panelVisible, setPanelVisible] = useState(false)
  const [config, setConfig] = useState<AgentChatConfigInfo | null>(null)
  const [status, setStatus] = useState<'loading' | 'ok' | 'error' | 'unavailable'>('loading')

  const { messages, streaming, error, isCollecting, showActionButtons, inputDisabled, sendMessage, handleActionSelect } = useChatWidget(slug, greeting, config, isOpen)

  useEffect(() => {
    AgentService.getChatConfig(slug).then(result => {
      if (result.sucesso && result.dados) {
        setConfig(result.dados)
        setStatus('ok')
      } else if (result.mensagem?.includes('indisponivel')) {
        setStatus('unavailable')
      } else {
        setStatus('error')
      }
    }).catch(() => setStatus('error'))
  }, [slug])

  const handleOpen = () => {
    setIsOpen(true)
    // Delay para o avatar sair antes do painel aparecer
    setTimeout(() => setPanelVisible(true), renderBubble ? 800 : 0)
  }
  const handleClose = () => {
    setPanelVisible(false)
    setIsOpen(false)
  }

  return (
    <>
      {renderBubble
        ? renderBubble({ message: greeting, onClick: handleOpen, isOpen, color })
        : <ChatBubble message={greeting} onClick={handleOpen} isOpen={isOpen} color={color} agentAvatar={agentAvatar} />
      }

      {isOpen && (
        <div
          className="fixed bottom-6 right-6 z-50 flex flex-col overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-2xl sm:h-[520px] sm:w-[400px] max-sm:inset-4 max-sm:bottom-4 max-sm:right-4 max-sm:left-4 max-sm:top-auto max-sm:h-[70vh]"
          style={{
            opacity: panelVisible ? 1 : 0,
            transform: panelVisible ? 'translateY(0)' : 'translateY(30px)',
            transition: 'opacity 400ms ease-out, transform 400ms ease-out',
          }}
        >
          {/* Header */}
          <div
            className="flex items-center justify-between border-b border-gray-100 px-4 py-3"
            style={{ backgroundColor: color }}
          >
            <div className="flex items-center gap-2.5">
              {agentAvatar ? (
                <img src={agentAvatar} alt="" className="h-8 w-8 rounded-full bg-gray-100 object-cover ring-2 ring-white/30" />
              ) : (
                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-white/20 text-white">
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4">
                    <path d="M4.913 2.658c2.075-.27 4.19-.408 6.337-.408 2.147 0 4.262.139 6.337.408 1.922.25 3.291 1.861 3.405 3.727a4.403 4.403 0 00-1.032-.211 50.89 50.89 0 00-8.42 0c-2.358.196-4.04 2.19-4.04 4.434v4.286a4.47 4.47 0 002.433 3.984L7.28 21.53A.75.75 0 016 21v-4.03a48.527 48.527 0 01-1.087-.128C2.905 16.58 1.5 14.833 1.5 12.862V6.638c0-1.97 1.405-3.718 3.413-3.979z" />
                    <path d="M15.75 7.5c-1.376 0-2.739.057-4.086.169C10.124 7.797 9 9.103 9 10.609v4.285c0 1.507 1.128 2.814 2.67 2.94 1.243.102 2.5.157 3.768.165l2.782 2.781a.75.75 0 001.28-.53v-2.39l.33-.026c1.542-.125 2.67-1.433 2.67-2.94v-4.286c0-1.505-1.125-2.811-2.664-2.94A49.392 49.392 0 0015.75 7.5z" />
                  </svg>
                </div>
              )}
              <div>
                <p className="text-sm font-semibold text-white">{config?.name ?? 'Assistente'}</p>
                {isCollecting && <p className="text-xs text-white/70">Coletando dados...</p>}
              </div>
            </div>
            <button
              onClick={handleClose}
              className="flex h-8 w-8 items-center justify-center rounded-full text-white/80 transition-colors hover:bg-white/20 hover:text-white"
              aria-label="Fechar chat"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="h-5 w-5">
                <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
              </svg>
            </button>
          </div>

          {/* Body */}
          <div className="flex-1 overflow-hidden">
            {status === 'loading' && (
              <div className="flex h-full items-center justify-center">
                <div
                  className="h-6 w-6 animate-spin rounded-full border-2 border-t-2"
                  style={{ borderColor: `${color}33`, borderTopColor: color }}
                />
              </div>
            )}

            {status === 'error' && (
              <div className="flex h-full items-center justify-center p-6 text-center">
                <p className="text-sm text-gray-500">Nao foi possivel conectar ao agente. Tente novamente mais tarde.</p>
              </div>
            )}

            {status === 'unavailable' && (
              <div className="flex h-full items-center justify-center p-6 text-center">
                <p className="text-sm text-gray-500">Este agente esta indisponivel no momento.</p>
              </div>
            )}

            {status === 'ok' && (
              <>
                {error && (
                  <div className="mx-3 mt-3 rounded-lg bg-red-50 px-3 py-2 text-xs text-red-600">
                    {error}
                  </div>
                )}
                <ChatWindow
                  messages={messages}
                  streaming={streaming}
                  onSendMessage={sendMessage}
                  agentName={config?.name}
                  agentAvatar={agentAvatar}
                  color={color}
                  inputDisabled={inputDisabled}
                  showActionButtons={showActionButtons}
                  onActionSelect={handleActionSelect}
                />
              </>
            )}
          </div>
        </div>
      )}
    </>
  )
}

export default ChatWidget
