import { useState, useEffect, useRef, useCallback } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'
import type { WhatsappStatusInfo } from '../../types/agent'
import { useWhatsappAccessStore } from '../../stores/useWhatsappAccessStore'
import WhatsappAccessSettings from '../../components/admin/whatsapp/WhatsappAccessSettings'
import WhatsappAccessRequestList from '../../components/admin/whatsapp/WhatsappAccessRequestList'
import WhatsappContactList from '../../components/admin/whatsapp/WhatsappContactList'

// Pareamento com o WPP Connect:
// - start-session abre um navegador por agente; enquanto ele existe (INITIALIZING, QRCODE...)
//   um novo start nao faz nada no backend, entao o botao fica travado ate conectar ou desistir.
// - O WhatsApp troca o QR Code a cada ~20s e o WPP Connect acompanha: o status sempre traz o
//   QR atual, entao basta consultar o status periodicamente e trocar a imagem.
// - Sem leitura do QR, o WPP Connect fecha a sessao sozinho (~60s) e o status volta a CLOSED.
//   Nesse caso o painel reabre a sessao sozinho, depois de uma pausa para o navegador anterior
//   terminar de fechar (senao o WPP Connect acusa "The browser is already running").
const POLL_INTERVAL_MS = 3000
const RESTART_DELAY_MS = 10000
// Logo apos o start o status-session ainda pode responder CLOSED; nao e motivo para reiniciar.
const START_GRACE_MS = 8000
const MAX_AUTO_RESTARTS = 5
const IN_PROGRESS_STATUSES = ['INITIALIZING', 'QRCODE', 'PHONECODE', 'CLOSING']

const isInProgress = (status: string) => IN_PROGRESS_STATUSES.includes(status.toUpperCase())

const WhatsappPage = () => {
  const { selectedAgent } = useAgentStore()
  const resetAccess = useWhatsappAccessStore((s) => s.reset)
  const loadAccess = useWhatsappAccessStore((s) => s.loadAll)

  const [status, setStatus] = useState('')
  const [isConnected, setIsConnected] = useState(false)
  const [qrCode, setQrCode] = useState<string | null>(null)
  const [connecting, setConnecting] = useState(false)
  const [renewing, setRenewing] = useState(false)
  const [disconnecting, setDisconnecting] = useState(false)
  const [checkingStatus, setCheckingStatus] = useState(false)

  const slugRef = useRef<string | null>(null)
  const connectingRef = useRef(false)
  const startInFlightRef = useRef(false)
  const lastStartAtRef = useRef(0)
  const restartsRef = useRef(0)
  // So reabre a sessao sozinho se o pareamento foi pedido nesta tela (clique em Iniciar Sessao).
  const autoRestartRef = useRef(false)
  const pollingRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const restartTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const stopPolling = useCallback(() => {
    if (pollingRef.current) {
      clearInterval(pollingRef.current)
      pollingRef.current = null
    }
  }, [])

  const clearRestartTimer = useCallback(() => {
    if (restartTimerRef.current) {
      clearTimeout(restartTimerRef.current)
      restartTimerRef.current = null
    }
  }, [])

  const stopConnecting = useCallback(() => {
    connectingRef.current = false
    autoRestartRef.current = false
    setConnecting(false)
    setRenewing(false)
    stopPolling()
    clearRestartTimer()
  }, [stopPolling, clearRestartTimer])

  // As funcoes abaixo se chamam em ciclo (status -> reinicio -> start -> status); as
  // referencias evitam dependencias circulares entre os useCallback.
  const applyStatusRef = useRef<(info: WhatsappStatusInfo) => void>(() => {})
  const requestStartRef = useRef<(slug: string) => Promise<void>>(async () => {})

  const fetchStatus = useCallback(async (slug: string) => {
    try {
      const result = await AgentService.getWhatsappStatus(slug)
      if (slug !== slugRef.current) return
      if (result.sucesso && result.dados) {
        applyStatusRef.current(result.dados)
      } else if (connectingRef.current) {
        // Sem sessao no WPP Connect ainda (ex.: agente nunca pareado): equivale a fechada.
        applyStatusRef.current({ agentSlug: slug, status: 'CLOSED', isConnected: false, qrCode: null })
      }
    } catch (err) {
      console.error('[WhatsappPage] fetchStatus — exceção:', err)
    }
  }, [])

  const startPolling = useCallback((slug: string) => {
    if (pollingRef.current) return
    pollingRef.current = setInterval(() => {
      if (!document.hidden) fetchStatus(slug)
    }, POLL_INTERVAL_MS)
  }, [fetchStatus])

  const scheduleRestart = useCallback((slug: string) => {
    if (restartTimerRef.current || startInFlightRef.current) return
    if (Date.now() - lastStartAtRef.current < START_GRACE_MS) return

    if (!autoRestartRef.current) {
      // Pareamento adotado (aberto em outra aba ou pelo WPP Connect ao subir) que expirou.
      stopConnecting()
      setQrCode(null)
      return
    }

    if (restartsRef.current >= MAX_AUTO_RESTARTS) {
      stopConnecting()
      setQrCode(null)
      toast.error('Não foi possível conectar o WhatsApp. Tente novamente.')
      return
    }

    restartsRef.current += 1
    setRenewing(true)
    setQrCode(null)
    restartTimerRef.current = setTimeout(() => {
      restartTimerRef.current = null
      if (connectingRef.current && slug === slugRef.current) {
        requestStartRef.current(slug)
      }
    }, RESTART_DELAY_MS)
  }, [stopConnecting])

  const applyStatus = useCallback((info: WhatsappStatusInfo) => {
    const slug = info.agentSlug || slugRef.current
    if (!slug || slug !== slugRef.current) return

    setStatus(info.status)
    setIsConnected(info.isConnected)

    if (info.isConnected) {
      const wasConnecting = connectingRef.current
      stopConnecting()
      setQrCode(null)
      if (wasConnecting) toast.success('WhatsApp conectado!')
      return
    }

    if (info.qrCode) {
      setQrCode(info.qrCode)
      setRenewing(false)
    }

    if (isInProgress(info.status)) {
      // Pareamento em andamento (inclusive aberto em outra aba ou pelo proprio WPP Connect
      // ao subir): acompanha em vez de abrir outra sessao.
      if (!connectingRef.current) {
        connectingRef.current = true
        restartsRef.current = 0
        setConnecting(true)
      }
      if (info.status.toUpperCase() !== 'QRCODE') setQrCode(null)
      startPolling(slug)
      return
    }

    // CLOSED, ERROR, UNKNOWN...: a sessao acabou sem leitura do QR.
    setQrCode(null)
    if (connectingRef.current) scheduleRestart(slug)
  }, [stopConnecting, startPolling, scheduleRestart])

  const requestStart = useCallback(async (slug: string) => {
    if (startInFlightRef.current) return
    startInFlightRef.current = true
    lastStartAtRef.current = Date.now()
    try {
      const result = await AgentService.startWhatsappSession(slug)
      if (slug !== slugRef.current) return
      if (!result.sucesso || !result.dados) {
        console.error('[WhatsappPage] requestStart — erro:', result.mensagem, result.erros)
        stopConnecting()
        toast.error(result.mensagem || 'Erro ao iniciar sessão')
        return
      }
      applyStatusRef.current(result.dados)
      startPolling(slug)
    } catch (err) {
      console.error('[WhatsappPage] requestStart — exceção:', err)
      stopConnecting()
      toast.error('Erro de rede ao iniciar sessão')
    } finally {
      startInFlightRef.current = false
    }
  }, [stopConnecting, startPolling])

  useEffect(() => {
    applyStatusRef.current = applyStatus
    requestStartRef.current = requestStart
  }, [applyStatus, requestStart])

  // Ao abrir a pagina ou trocar de agente: le o status e, se ja houver um pareamento em
  // andamento, continua mostrando o QR dele.
  useEffect(() => {
    if (!selectedAgent) return
    const slug = selectedAgent.slug
    slugRef.current = slug
    setStatus('')
    setIsConnected(false)
    setQrCode(null)

    setCheckingStatus(true)
    fetchStatus(slug).finally(() => setCheckingStatus(false))

    return () => {
      slugRef.current = null
      stopConnecting()
    }
  }, [selectedAgent, fetchStatus, stopConnecting])

  // Controle de acesso: recarrega configuracao, pedidos e contatos ao trocar de agente.
  useEffect(() => {
    if (!selectedAgent) return
    resetAccess()
    loadAccess(selectedAgent.slug)
  }, [selectedAgent, resetAccess, loadAccess])

  // Ao voltar para a aba, atualiza na hora em vez de esperar o proximo ciclo.
  useEffect(() => {
    const handleVisibility = () => {
      if (!document.hidden && connectingRef.current && slugRef.current) {
        fetchStatus(slugRef.current)
      }
    }
    document.addEventListener('visibilitychange', handleVisibility)
    return () => document.removeEventListener('visibilitychange', handleVisibility)
  }, [fetchStatus])

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

  const handleStartSession = () => {
    // Um clique so: enquanto conecta, o botao fica desabilitado e o painel cuida do resto.
    if (connectingRef.current || isConnected) return
    connectingRef.current = true
    restartsRef.current = 0
    setConnecting(true)
    setRenewing(false)
    autoRestartRef.current = true
    setQrCode(null)
    requestStart(selectedAgent.slug)
  }

  const handleCancel = () => {
    stopConnecting()
    setQrCode(null)
  }

  const handleDisconnect = async () => {
    if (!confirm('Tem certeza que deseja desconectar a sessão WhatsApp?')) return

    setDisconnecting(true)
    try {
      const result = await AgentService.disconnectWhatsapp(selectedAgent.slug)
      if (result.sucesso) {
        toast.success('Sessão WhatsApp desconectada')
        stopConnecting()
        setIsConnected(false)
        setStatus('DISCONNECTED')
        setQrCode(null)
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
    if (connecting || isInProgress(status)) {
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
      {connecting && !isConnected && (
        <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Escaneie o QR Code</h2>
          <p className="text-sm text-gray-500 mb-4">Abra o WhatsApp no seu celular, acesse Configurações &gt; Dispositivos conectados &gt; Conectar dispositivo, e escaneie o QR Code abaixo.</p>
          <div className="flex justify-center">
            {qrCode ? (
              <img
                src={qrCode.startsWith('data:') ? qrCode : `data:image/png;base64,${qrCode}`}
                alt="QR Code WhatsApp"
                className="w-64 h-64 border border-gray-200 rounded-lg"
              />
            ) : (
              <div className="w-64 h-64 border border-dashed border-gray-300 rounded-lg flex flex-col items-center justify-center gap-3 text-gray-400">
                <span className="w-8 h-8 border-2 border-gray-300 border-t-ava-600 rounded-full animate-spin" />
                <span className="text-sm">{renewing ? 'Gerando um novo QR Code...' : 'Preparando o QR Code...'}</span>
              </div>
            )}
          </div>
          <p className="text-xs text-gray-400 text-center mt-3">O QR Code é renovado automaticamente e o status muda sozinho após o escaneamento.</p>
        </div>
      )}

      {/* Ações */}
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Ações</h2>

        <div className="flex flex-wrap gap-3">
          <button
            onClick={handleStartSession}
            disabled={connecting || isConnected}
            className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            title={isConnected ? 'Sessão já está conectada' : ''}
          >
            {connecting ? 'Conectando...' : 'Iniciar Sessão'}
          </button>

          {connecting && !isConnected && (
            <button
              onClick={handleCancel}
              className="px-4 py-2 border border-gray-200 text-gray-600 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors"
              title="Para de acompanhar o pareamento nesta tela"
            >
              Cancelar
            </button>
          )}

          <button
            onClick={handleDisconnect}
            disabled={disconnecting || !isConnected}
            className="px-4 py-2 border border-red-200 text-red-600 text-sm font-medium rounded-lg hover:bg-red-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            title={!isConnected ? 'Nenhuma sessão ativa para desconectar' : ''}
          >
            {disconnecting ? 'Desconectando...' : 'Desconectar'}
          </button>
        </div>

        {!isConnected && !connecting && (
          <p className="text-xs text-gray-400 mt-3">Clique em "Iniciar Sessão" para conectar o WhatsApp ao agente.</p>
        )}
      </div>

      {/* Controle de acesso (feature 018) */}
      <div className="mt-10 mb-4">
        <h2 className="text-xl font-bold text-gray-900">Acesso ao agente</h2>
        <p className="text-sm text-gray-500 mt-1">Restrinja o atendimento pelo WhatsApp a uma lista de contatos.</p>
      </div>
      <WhatsappAccessSettings slug={selectedAgent.slug} />
      <WhatsappAccessRequestList slug={selectedAgent.slug} />
      <WhatsappContactList slug={selectedAgent.slug} />
    </div>
  )
}

export default WhatsappPage
