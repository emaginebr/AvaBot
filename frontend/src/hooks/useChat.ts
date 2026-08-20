import { useCallback, useEffect, useRef, useState } from 'react'
import useWebSocket from './useWebSocket'

interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

interface UseChatReturn {
  messages: ChatMessage[]
  streaming: boolean
  ready: boolean
  error: string | null
  sendMessage: (content: string) => void
  loadHistoricalMessages: (historical: ChatMessage[]) => void
}

const useChat = (wsUrl: string | null): UseChatReturn => {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [streaming, setStreaming] = useState(false)
  const [ready, setReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const streamBufferRef = useRef('')

  const handleMessage = useCallback((msg: { type: string; [key: string]: unknown }) => {
    console.log('[Chat] Recebido:', msg)
    switch (msg.type) {
      case 'ready':
        setReady(true)
        break
      case 'chunk':
        setStreaming(true)
        streamBufferRef.current += msg.content as string
        {
          const buffered = streamBufferRef.current
          setMessages(prev => {
            const updated = [...prev]
            if (updated.length > 0 && updated[updated.length - 1].role === 'assistant') {
              updated[updated.length - 1] = { role: 'assistant', content: buffered }
            } else {
              updated.push({ role: 'assistant', content: buffered })
            }
            return updated
          })
        }
        break
      case 'done':
        setStreaming(false)
        streamBufferRef.current = ''
        break
      case 'error':
        setError(msg.message as string)
        setStreaming(false)
        streamBufferRef.current = ''
        break
    }
  }, [])

  const { send, error: wsError } = useWebSocket(wsUrl, handleMessage)

  useEffect(() => {
    if (wsError) setError(wsError)
  }, [wsError])

  const sendMessage = useCallback((content: string) => {
    if (!ready || streaming) return
    console.log('[Chat] Enviado:', { type: 'message', content })
    setMessages(prev => [...prev, { role: 'user', content }])
    streamBufferRef.current = ''
    send({ type: 'message', content })
  }, [ready, streaming, send])

  const loadHistoricalMessages = useCallback((historical: ChatMessage[]) => {
    setMessages(historical)
  }, [])

  return { messages, streaming, ready, error, sendMessage, loadHistoricalMessages }
}

export default useChat
