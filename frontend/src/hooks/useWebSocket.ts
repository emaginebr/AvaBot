import { useCallback, useEffect, useRef, useState } from 'react'

interface WebSocketMessage {
  type: string
  [key: string]: unknown
}

interface UseWebSocketReturn {
  connected: boolean
  send: (data: WebSocketMessage) => void
  error: string | null
}

const useWebSocket = (
  url: string | null,
  onMessage: (msg: WebSocketMessage) => void,
): UseWebSocketReturn => {
  const wsRef = useRef<WebSocket | null>(null)
  const onMessageRef = useRef(onMessage)
  const [connected, setConnected] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Keep callback ref up to date without re-creating the WebSocket
  onMessageRef.current = onMessage

  useEffect(() => {
    if (!url) return

    const ws = new WebSocket(url)
    wsRef.current = ws

    console.log('[WebSocket] Conectando a', url)
    ws.onopen = () => {
      console.log('[WebSocket] Conectado')
      setConnected(true)
    }
    ws.onclose = (event) => {
      console.warn('[WebSocket] Desconectado', { code: event.code, reason: event.reason, wasClean: event.wasClean })
      setConnected(false)
    }
    ws.onerror = (event) => {
      console.error('[WebSocket] Erro na conexao', event)
      setError('Erro na conexao WebSocket')
    }
    ws.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data) as WebSocketMessage
        onMessageRef.current(data)
      } catch {
        console.error('[WebSocket] Mensagem invalida recebida:', event.data)
        setError('Mensagem invalida recebida')
      }
    }

    return () => {
      ws.close()
    }
  }, [url])

  const send = useCallback((data: WebSocketMessage) => {
    if (wsRef.current?.readyState === WebSocket.OPEN) {
      wsRef.current.send(JSON.stringify(data))
    }
  }, [])

  return { connected, send, error }
}

export default useWebSocket
