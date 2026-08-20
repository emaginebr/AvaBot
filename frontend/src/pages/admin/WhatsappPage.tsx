import { useState, useEffect, useRef, useCallback } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'

// O WhatsApp Web expira/rotaciona o QR Code a cada ~20s.
const QR_TTL_SECONDS = 20

const WhatsappPage = () => {
  const { selectedAgent } = useAgentStore()

  const [status, setStatus] = useState('')
  const [isConnected, setIsConnected] = useState(false)
  const [qrCode, setQrCode] = useState<string | null>(null)
  const [starting, setStarting] = useState(false)
  const [disconnecting, setDisconnecting] = useState(false)
  const [checkingStatus, setCheckingStatus] = useState(false)
  const [qrSecondsLeft, setQrSecondsLeft] = useState(QR_TTL_SECONDS)
  const pollingRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const qrTimerRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const qrCodeRef = useRef<string | null>(null)

  useEffect(() => {
    qrCodeRef.current = qrCode
  }, [qrCode])

  const stopQrTimer = useCallback(() => {
    if (qrTimerRef.current) {
      clearInterval(qrTimerRef.current)
      qrTimerRef.current = null
    }
  }, [])

  const stopPolling = useCallback(() => {
    if (pollingRef.current) {
      clearInterval(pollingRef.current)
      pollingRef.current = null
    }
  }, [])

  const expireQrCode = useCallback(() => {
    stopQrTimer()
    stopPolling()
    setQrCode(null)
    setStatus('DISCONNECTED')
    setIsConnected(false)
  }, [stopQrTimer, stopPolling])

  const startQrTimer = useCallback(() => {
    stopQrTimer()
    setQrSecondsLeft(QR_TTL_SECONDS)
    qrTimerRef.current = setInterval(() => {
      setQrSecondsLeft((prev) => {
        if (prev <= 1) {
          expireQrCode()
          return 0
        }
        return prev - 1
      })
    }, 1000)
  }, [stopQrTimer, expireQrCode])

  // checkStatus nao pode depender de "qrCode" (estado reativo): isso mudaria sua
  // identidade a cada QR gerado, forcando o efeito de montagem abaixo a re-rodar
  // e disparar sua cleanup (stopQrTimer) logo apos o cronometro ser iniciado.
  const checkStatus = useCallback(async (slug: string) => {
    try {
      const result = await AgentService.getWhatsappStatus(slug)
      if (result.sucesso && result.dados) {
        setStatus(result.dados.status)
        setIsConnected(result.dados.isConnected)

        if (result.dados.isConnected) {
          stopPolling()
          stopQrTimer()
          setQrCode(null)
          if (qrCodeRef.current) {
            toast.success('WhatsApp conectado!')
          }
        }
      }
    } catch (err) {
      console.error('[WhatsappPage] checkStatus — exceção:', err)
    }
  }, [stopPolling, stopQrTimer])

  const startPolling = useCallback((slug: string) => {
    stopPolling()
    pollingRef.current = setInterval(() => {
      checkStatus(slug)
    }, 3000)
  }, [stopPolling, checkStatus])

  // Check status on mount and when agent changes
  useEffect(() => {
    if (!selectedAgent) return

    setCheckingStatus(true)
    checkStatus(selectedAgent.slug).finally(() => setCheckingStatus(false))

    return () => {
      stopPolling()
      stopQrTimer()
    }
  }, [selectedAgent, checkStatus, stopPolling, stopQrTimer])

  // Pause polling when page is hidden
  useEffect(() => {
    const handleVisibility = () => {
      if (document.hidden) {
        stopPolling()
      } else if (qrCode && selectedAgent && !isConnected) {
        startPolling(selectedAgent.slug)
      }
    }
    document.addEventListener('visibilitychange', handleVisibility)
    return () => document.removeEventListener('visibilitychange', handleVisibility)
  })

  if (!selectedAgent) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-400 mb-2">Nenhum agente selecionado</p>
          <p className="text-sm text-gray-300">Selecione um agente na navbar para configurar o WhatsApp.</p>
        </div>
      </div>
    )
  }

  const handleStartSession = async () => {
    setStarting(true)
    try {
      const startResult = await AgentService.startWhatsappSession(selectedAgent.slug)
      if (!startResult.sucesso) {
        toast.error(startResult.mensagem || 'Erro ao iniciar sessão')
        console.error('[WhatsappPage] handleStartSession — erro:', startResult.mensagem, startResult.erros)
        return
      }

      // O backend aguarda internamente ate o WPP Connect ter um QR Code pronto
      // (nao adianta buscar antes: client.urlcode so existe apos o primeiro catchQR).
      const qrResult = await AgentService.getWhatsappQrCode(selectedAgent.slug)
      if (qrResult.sucesso && qrResult.dados) {
        setQrCode(qrResult.dados.qrCode)
        toast.success('QR Code gerado! Escaneie com seu WhatsApp.')
        startQrTimer()
        startPolling(selectedAgent.slug)
      } else {
        toast.error(qrResult.mensagem || 'Erro ao obter QR Code')
        console.error('[WhatsappPage] handleStartSession — erro QR:', qrResult.mensagem, qrResult.erros)
      }
    } catch (err) {
      console.error('[WhatsappPage] handleStartSession — exceção:', err)
      toast.error('Erro de rede ao iniciar sessão')
    } finally {
      setStarting(false)
      await checkStatus(selectedAgent.slug)
    }
  }

  const handleDisconnect = async () => {
    if (!confirm('Tem certeza que deseja desconectar a sessão WhatsApp?')) return

    setDisconnecting(true)
    try {
      const result = await AgentService.disconnectWhatsapp(selectedAgent.slug)
      if (result.sucesso) {
        toast.success('Sessão WhatsApp desconectada')
        setIsConnected(false)
        setStatus('DISCONNECTED')
        setQrCode(null)
        stopPolling()
        stopQrTimer()
      } else {
        toast.error(result.mensagem || 'Erro ao desconectar')
        console.error('[WhatsappPage] handleDisconnect — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[WhatsappPage] handleDisconnect — exceção:', err)
      toast.error('Erro de rede ao desconectar')
    } finally {
      setDisconnecting(false)
    }
  }

  const getStatusDisplay = () => {
    if (isConnected) {
      return { label: 'Conectado', color: 'bg-green-100 text-green-700', dot: 'bg-green-500' }
    }
    if (status === 'STARTING' || status === 'QRCODE' || qrCode) {
      return { label: 'Conectando...', color: 'bg-yellow-100 text-yellow-700', dot: 'bg-yellow-500 animate-pulse' }
    }
    return { label: 'Desconectado', color: 'bg-gray-100 text-gray-500', dot: 'bg-gray-400' }
  }

  const statusDisplay = getStatusDisplay()

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-gray-900">WhatsApp</h1>
      <p className="text-sm text-gray-500 mt-1 mb-6">Gerencie a conexão WhatsApp do agente.</p>

      {/* Status da Conexão */}
      <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Status da Conexão</h2>
            <p className="text-sm text-gray-500 mt-0.5">Agente: {selectedAgent.name} ({selectedAgent.slug})</p>
          </div>
          <span className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-sm font-medium ${statusDisplay.color}`}>
            <span className={`w-2 h-2 rounded-full ${statusDisplay.dot}`} />
            {checkingStatus ? 'Verificando...' : statusDisplay.label}
          </span>
        </div>
      </div>

      {/* QR Code */}
      {qrCode && !isConnected && (
        <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
          <div className="flex items-center justify-between mb-2">
            <h2 className="text-lg font-semibold text-gray-900">Escaneie o QR Code</h2>
            <span className={`text-sm font-medium tabular-nums ${qrSecondsLeft <= 5 ? 'text-red-500' : 'text-gray-500'}`}>
              {qrSecondsLeft}s
            </span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-1 mb-4">
            <div
              className={`h-1 rounded-full transition-all duration-1000 ease-linear ${qrSecondsLeft <= 5 ? 'bg-red-500' : 'bg-ava-600'}`}
              style={{ width: `${(qrSecondsLeft / QR_TTL_SECONDS) * 100}%` }}
            />
          </div>
          <p className="text-sm text-gray-500 mb-4">Abra o WhatsApp no seu celular, acesse Configurações &gt; Dispositivos conectados &gt; Conectar dispositivo, e escaneie o QR Code abaixo.</p>
          <div className="flex justify-center">
            <img
              src={qrCode.startsWith('data:') ? qrCode : `data:image/png;base64,${qrCode}`}
              alt="QR Code WhatsApp"
              className="w-64 h-64 border border-gray-200 rounded-lg"
            />
          </div>
          <p className="text-xs text-gray-400 text-center mt-3">O status será atualizado automaticamente após o escaneamento.</p>
        </div>
      )}

      {/* Ações */}
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Ações</h2>

        <div className="flex flex-wrap gap-3">
          <button
            onClick={handleStartSession}
            disabled={starting || isConnected}
            className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            title={isConnected ? 'Sessão já está conectada' : ''}
          >
            {starting ? 'Iniciando...' : 'Iniciar Sessão'}
          </button>

          <button
            onClick={handleDisconnect}
            disabled={disconnecting || !isConnected}
            className="px-4 py-2 border border-red-200 text-red-600 text-sm font-medium rounded-lg hover:bg-red-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            title={!isConnected ? 'Nenhuma sessão ativa para desconectar' : ''}
          >
            {disconnecting ? 'Desconectando...' : 'Desconectar'}
          </button>
        </div>

        {!isConnected && !qrCode && (
          <p className="text-xs text-gray-400 mt-3">Clique em "Iniciar Sessão" para conectar o WhatsApp ao agente.</p>
        )}
      </div>
    </div>
  )
}

export default WhatsappPage
