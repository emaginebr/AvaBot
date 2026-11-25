import { useState, useEffect, useRef, useCallback } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'
import type { WhatsappStatusInfo } from '../../types/agent'
import { useWhatsappAccessStore } from '../../stores/useWhatsappAccessStore'
import WhatsappAccessSettings from '../../components/admin/whatsapp/WhatsappAccessSettings'
import WhatsappAccessRequestList from '../../components/admin/whatsapp/WhatsappAccessRequestList'
import WhatsappContactList from '../../components/admin/whatsapp/WhatsappContactList'
import Alert from '../../components/admin/ui/Alert'
import Button from '../../components/admin/ui/Button'
import { Card, CardFooter, CardHeader } from '../../components/admin/ui/Card'
import PageHeader from '../../components/admin/ui/PageHeader'
import Spinner from '../../components/admin/ui/Spinner'
import { NoAgentState } from '../../components/admin/ui/States'
import StatusBadge from '../../components/admin/ui/StatusBadge'
import Tabs, { TabPanel } from '../../components/admin/ui/Tabs'
import { useConfirm } from '../../components/admin/ui/useConfirm'

type AccessTab = 'modo' | 'pedidos' | 'contatos'

const PAGE_TITLE = 'WhatsApp'
const PAGE_DESCRIPTION = 'Conecte o número do agente e defina quem pode conversar com ele.'

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
  // Só apresentação: aba ativa do cartão "Acesso ao agente" e diálogo de confirmação.
  const [accessTab, setAccessTab] = useState<AccessTab>('modo')
  const pendingRequests = useWhatsappAccessStore((s) => s.config?.pendingRequestCount ?? 0)
  const [confirm, confirmDialog] = useConfirm()

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
      <div className="flex flex-col gap-5">
        <PageHeader title={PAGE_TITLE} description={PAGE_DESCRIPTION} />
        <NoAgentState purpose="configurar o WhatsApp" />
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
    const ok = await confirm({
      title: 'Desconectar o WhatsApp?',
      message: 'Tem certeza que deseja desconectar a sessão WhatsApp?',
      confirmLabel: 'Desconectar',
      icon: 'power',
    })
    if (!ok) return

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

  const statusBadge = checkingStatus ? (
    <StatusBadge tone="idle" pulse label="Verificando..." />
  ) : isConnected ? (
    <StatusBadge tone="ok" label="Conectado" />
  ) : connecting || isInProgress(status) ? (
    <StatusBadge tone="warn" pulse label="Conectando..." />
  ) : (
    <StatusBadge tone="idle" icon="power" label="Desconectado" />
  )

  const showQr = connecting && !isConnected

  return (
    <div className="flex flex-col gap-5">
      <PageHeader agentName={selectedAgent.name} title={PAGE_TITLE} description={PAGE_DESCRIPTION} />

      {/* Conexão: status ao vivo, QR Code e ações (a lógica de pareamento acima não muda). */}
      <Card aria-labelledby="whatsapp-conexao-titulo">
        <CardHeader
          titleId="whatsapp-conexao-titulo"
          icon="whatsapp"
          iconTone="ok"
          title="Conexão"
          subtitle={
            <>
              Agente: {selectedAgent.name} (<code className="font-ui-mono">{selectedAgent.slug}</code>)
            </>
          }
          actions={<span role="status">{statusBadge}</span>}
        />

        {showQr && (
          <div className="grid grid-cols-1 items-center justify-items-center gap-6 md:grid-cols-[auto_minmax(0,1fr)] md:justify-items-stretch">
            <div className="grid h-[232px] w-[232px] place-items-center rounded-2xl border border-gray-200 bg-white">
              {qrCode ? (
                <img
                  src={qrCode.startsWith('data:') ? qrCode : `data:image/png;base64,${qrCode}`}
                  alt="QR Code WhatsApp"
                  className="h-[200px] w-[200px]"
                />
              ) : (
                <div role="status" className="flex flex-col items-center gap-2.5 px-4 text-center text-[13px] text-ink-2">
                  <Spinner className="h-7 w-7 text-ava-600" />
                  {renewing ? 'Gerando um novo QR Code...' : 'Preparando o QR Code...'}
                </div>
              )}
            </div>
            <div className="flex min-w-0 flex-col gap-4">
              <h3 className="m-0 text-[15px] font-semibold text-ink">Escaneie o QR Code</h3>
              <ol className="m-0 flex flex-col gap-1.5 pl-5 text-[13.5px] text-ink-2">
                <li>
                  Abra o <b className="font-semibold text-ink">WhatsApp</b> no celular.
                </li>
                <li>
                  Acesse <b className="font-semibold text-ink">Configurações › Dispositivos conectados</b>.
                </li>
                <li>
                  Toque em <b className="font-semibold text-ink">Conectar dispositivo</b> e aponte para o código.
                </li>
              </ol>
              <Alert tone="info" icon="refresh">
                O QR Code é renovado automaticamente e o status muda sozinho após o escaneamento.
              </Alert>
            </div>
          </div>
        )}

        {!isConnected && !connecting && (
          <p className="m-0 text-[12.5px] text-ink-3">Clique em “Iniciar sessão” para conectar o WhatsApp ao agente.</p>
        )}

        <CardFooter>
          <Button
            variant="dangerGhost"
            icon="power"
            onClick={handleDisconnect}
            loading={disconnecting}
            disabled={!isConnected}
            title={!isConnected ? 'Nenhuma sessão ativa para desconectar' : ''}
            className="md:mr-auto"
          >
            {disconnecting ? 'Desconectando...' : 'Desconectar'}
          </Button>
          {showQr && (
            <Button onClick={handleCancel} title="Para de acompanhar o pareamento nesta tela">
              Cancelar
            </Button>
          )}
          <Button
            variant="primary"
            icon={connecting ? undefined : 'qr'}
            onClick={handleStartSession}
            loading={connecting}
            disabled={isConnected}
            title={isConnected ? 'Sessão já está conectada' : ''}
          >
            {connecting ? 'Conectando...' : 'Iniciar sessão'}
          </Button>
        </CardFooter>
      </Card>

      {/* Controle de acesso (feature 018): os três painéis ficam montados para não perder rascunhos. */}
      <Card aria-labelledby="whatsapp-acesso-titulo">
        <CardHeader
          titleId="whatsapp-acesso-titulo"
          icon="shield"
          title="Acesso ao agente"
          subtitle="Restrinja o atendimento pelo WhatsApp a uma lista de contatos."
        />
        <Tabs
          idPrefix="whatsapp-acesso"
          label="Acesso ao agente"
          value={accessTab}
          onChange={setAccessTab}
          items={[
            { id: 'modo', label: 'Modo e frases' },
            { id: 'pedidos', label: 'Pedidos de acesso', count: pendingRequests, countLabel: 'pendentes' },
            { id: 'contatos', label: 'Contatos autorizados' },
          ]}
        />
        <TabPanel idPrefix="whatsapp-acesso" tabId="modo" hidden={accessTab !== 'modo'}>
          <WhatsappAccessSettings slug={selectedAgent.slug} />
        </TabPanel>
        <TabPanel idPrefix="whatsapp-acesso" tabId="pedidos" hidden={accessTab !== 'pedidos'}>
          <WhatsappAccessRequestList slug={selectedAgent.slug} />
        </TabPanel>
        <TabPanel idPrefix="whatsapp-acesso" tabId="contatos" hidden={accessTab !== 'contatos'}>
          <WhatsappContactList slug={selectedAgent.slug} />
        </TabPanel>
      </Card>
      {confirmDialog}
    </div>
  )
}

export default WhatsappPage
