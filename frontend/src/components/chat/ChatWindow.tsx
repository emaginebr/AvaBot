import { useEffect, useRef, useState } from 'react'
import MessageBubble from './MessageBubble'
import TypingIndicator from './TypingIndicator'
import ActionButtons from './ActionButtons'

interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

interface ChatWindowProps {
  messages: ChatMessage[]
  streaming: boolean
  onSendMessage: (content: string) => void
  agentName?: string
  agentAvatar?: string
  color?: string
  inputDisabled?: boolean
  showActionButtons?: boolean
  onActionSelect?: (optionText: string) => void
}

const ChatWindow = ({ messages, streaming, onSendMessage, agentName, agentAvatar, color, inputDisabled, showActionButtons, onActionSelect }: ChatWindowProps) => {
  const [input, setInput] = useState('')
  const messagesEndRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, streaming])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!input.trim() || streaming) return
    onSendMessage(input.trim())
    setInput('')
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex-1 overflow-y-auto p-4">
        {messages.map((msg, i) => (
          <MessageBubble key={i} role={msg.role} content={msg.content} color={color} agentAvatar={agentAvatar} />
        ))}
        {messages.length > 0 && messages[messages.length - 1].role === 'user' && (
          <TypingIndicator name={agentName} />
        )}
        <div ref={messagesEndRef} />
      </div>

      {showActionButtons && onActionSelect && (
        <ActionButtons onSelect={onActionSelect} color={color} />
      )}

      <form onSubmit={handleSubmit} className="border-t p-4 flex gap-2">
        <input
          type="text"
          value={input}
          onChange={e => setInput(e.target.value)}
          placeholder={inputDisabled ? '' : 'Digite sua mensagem...'}
          disabled={streaming || inputDisabled}
          className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 disabled:opacity-50"
          style={{ '--tw-ring-color': color ?? '#3b82f6' } as React.CSSProperties}
        />
        <button
          type="submit"
          disabled={streaming || inputDisabled || !input.trim()}
          className="px-4 py-2 text-white rounded-md disabled:opacity-50"
          style={{ backgroundColor: color ?? '#3668fc' }}
        >
          Enviar
        </button>
      </form>
    </div>
  )
}

export default ChatWindow
