import { useCallback, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { useAgentDashboard } from '../../hooks/useAgentDashboard'
import { useDashboardData } from '../../hooks/useDashboardData'
import { WhatsappAccessService } from '../../Services/WhatsappAccessService'
import { useAgentStore } from '../../stores/useAgentStore'
import { useAuthStore } from '../../stores/useAuthStore'
import { useWhatsappPendingStore } from '../../stores/useWhatsappPendingStore'
import { ProcessingStatus } from '../../types/knowledgeFile'
import { WHATSAPP_ACCESS_MODE } from '../../types/whatsappAccess'
import type { WhatsappAccessRequestInfo } from '../../types/whatsappAccess'
import AccessRequestList from '../../components/admin/dashboard/AccessRequestList'
import AgentRanking from '../../components/admin/dashboard/AgentRanking'
import ChannelChart from '../../components/admin/dashboard/ChannelChart'
import ConnectionStatusList from '../../components/admin/dashboard/ConnectionStatusList'
import DashboardCard from '../../components/admin/dashboard/DashboardCard'
import DashboardHero from '../../components/admin/dashboard/DashboardHero'
import EmptyState from '../../components/admin/dashboard/EmptyState'
import KnowledgeSummary from '../../components/admin/dashboard/KnowledgeSummary'
import KpiCard from '../../components/admin/dashboard/KpiCard'
import PeriodToggle from '../../components/admin/dashboard/PeriodToggle'
import PowerBIQueryList from '../../components/admin/dashboard/PowerBIQueryList'
import RecentSessionsTable from '../../components/admin/dashboard/RecentSessionsTable'
import Skeleton, { SkeletonRows } from '../../components/admin/ui/Skeleton'
import { buildConnectionItems } from '../../components/admin/dashboard/connections'
import { fmtBytes, fmtDecimal, fmtInt, plural } from '../../components/admin/dashboard/format'
import { computeMetrics } from '../../components/admin/dashboard/metrics'
import type { DashboardPeriod } from '../../components/admin/dashboard/period'
import Button from '../../components/admin/ui/Button'
import Modal from '../../components/admin/ui/Modal'

/** Pedidos exibidos no cartão; o restante fica na página do WhatsApp. */
const VISIBLE_REQUESTS = 3

const greetingFor = (date: Date) => {
  const hour = date.getHours()
  if (hour < 12) return 'Bom dia'
  if (hour < 18) return 'Boa tarde'
  return 'Boa noite'
}

const inlineLink = 'font-medium text-ava-700 hover:text-ava-900 hover:underline hover:underline-offset-[3px]'

/**
 * Painel inicial do admin. Tudo vem das listagens já existentes na API (agentes, sessões,
 * arquivos, WhatsApp, Telegram, Power BI, chaves MCP); não há endpoint de métricas, então
 * números sem fonte (tempo de resposta, canal por sessão) não são exibidos.
 */
const DashboardPage = () => {
  const user = useAuthStore((state) => state.user)
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const refreshPending = useWhatsappPendingStore((state) => state.refresh)
  const { agents, agentsError, data, loading, reload } = useDashboardData()

  // Prefere a versão recém-carregada do agente ativo (a do localStorage pode estar defasada).
  const activeAgent = selectedAgent
    ? agents.find((a) => a.agentId === selectedAgent.agentId) ?? selectedAgent
    : null
  const agentDash = useAgentDashboard(activeAgent)

  const [period, setPeriod] = useState<DashboardPeriod>(14)
  const [today] = useState(() => new Date())
  const [busyId, setBusyId] = useState<number | null>(null)
  const [rejecting, setRejecting] = useState<WhatsappAccessRequestInfo | null>(null)

  const metrics = useMemo(() => (data ? computeMetrics(data, period) : null), [data, period])
  const kpiLoading = loading || !metrics

  const activeCount = agents.filter((a) => a.status === 1).length
  const powerBICount = agents.filter((a) => a.powerBIEnabled).length
  const files = data?.files ?? []
  const fileErrors = files.filter((f) => f.processingStatus === ProcessingStatus.Error).length
  const pendingRequests = agentDash.data?.pendingRequests ?? []

  // Destaque do título: só afirma "tudo em ordem" depois de olhar os dados.
  const attention = fileErrors + pendingRequests.length
  const highlight =
    loading || (activeAgent && agentDash.loading)
      ? undefined
      : attention === 0
        ? 'Tudo em ordem.'
        : `${plural(attention, 'item pede', 'itens pedem')} atenção.`

  const firstName = user?.name?.trim().split(/\s+/)[0]
  const greeting = `${greetingFor(today)}${firstName ? `, ${firstName}` : ''}.`
  const dateLabel = today.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
  const pill = agents.length ? `${plural(activeCount, 'agente ativo', 'agentes ativos')} de ${fmtInt(agents.length)}` : undefined

  const afterDecision = () => {
    agentDash.reload()
    refreshPending(activeAgent?.slug ?? null, true)
  }

  const handleApprove = async (request: WhatsappAccessRequestInfo) => {
    if (!activeAgent) return
    const name = request.profileName?.trim() || request.phone || 'O contato'
    setBusyId(request.whatsappAccessRequestId)
    const result = await WhatsappAccessService.approveRequest(
      activeAgent.slug,
      request.whatsappAccessRequestId,
      request.profileName ?? request.phone ?? undefined,
    )
    setBusyId(null)
    if (result.sucesso) {
      toast.success(`${name} foi adicionado aos contatos autorizados.`)
      afterDecision()
    } else {
      toast.error(result.mensagem || 'Não foi possível aprovar o pedido')
    }
  }

  const closeReject = useCallback(() => setRejecting(null), [])

  const confirmReject = async () => {
    if (!activeAgent || !rejecting) return
    const request = rejecting
    const name = request.profileName?.trim() || request.phone || 'contato'
    setBusyId(request.whatsappAccessRequestId)
    const result = await WhatsappAccessService.rejectRequest(activeAgent.slug, request.whatsappAccessRequestId)
    setBusyId(null)
    setRejecting(null)
    if (result.sucesso) {
      toast.success(`Pedido de ${name} recusado.`)
      afterDecision()
    } else {
      toast.error(result.mensagem || 'Não foi possível recusar o pedido')
    }
  }

  const noAgentHint = (what: string) => (
    <EmptyState>
      Selecione um agente no menu lateral para ver {what}.
    </EmptyState>
  )

  return (
    <div className="flex flex-col gap-5">
      <DashboardHero
        greeting={greeting}
        highlight={highlight}
        pill={pill}
        dateLabel={dateLabel}
        actions={<PeriodToggle value={period} onChange={setPeriod} />}
      />

      {agentsError && (
        <div role="alert" className="flex flex-wrap items-center gap-3 rounded-xl border border-[#f6c9c5] bg-err-bg px-4 py-3 text-[13px] text-err">
          Não foi possível carregar os agentes: {agentsError}
          <button type="button" onClick={reload} className="ml-auto font-medium underline underline-offset-2">
            Tentar de novo
          </button>
        </div>
      )}

      {/* KPIs */}
      <section aria-label="Indicadores do período" className="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          icon="bot"
          label="Agentes ativos"
          loading={!data && loading}
          value={fmtInt(activeCount)}
          suffix={`de ${fmtInt(agents.length)}`}
          meta={`${plural(agents.length - activeCount, 'inativo', 'inativos')} · ${fmtInt(powerBICount)} com Power BI`}
        />
        <KpiCard
          icon="chat"
          label="Sessões"
          numeric
          loading={kpiLoading}
          value={fmtInt(metrics?.sessions ?? 0)}
          delta={metrics?.sessionsDelta}
          meta={metrics?.sessionsDelta == null ? `últimos ${period} dias` : `vs. ${period} dias anteriores`}
          spark={metrics?.dailySessions}
        />
        <KpiCard
          icon="message"
          label="Mensagens"
          numeric
          loading={kpiLoading}
          value={fmtInt(metrics?.messages ?? 0)}
          delta={metrics?.messagesDelta}
          meta={
            metrics && metrics.sessions > 0
              ? `${fmtDecimal(metrics.messages / metrics.sessions)} por sessão`
              : `últimos ${period} dias`
          }
          spark={metrics?.dailyMessages}
        />
        <KpiCard
          icon="clock"
          label="Sessões em andamento"
          numeric
          loading={kpiLoading}
          value={fmtInt(metrics?.openSessions ?? 0)}
          meta="iniciadas no período, sem encerramento"
        />
      </section>

      {/* Gráfico + conexões */}
      <section className="grid grid-cols-1 gap-5 xl:grid-cols-[minmax(0,1.75fr)_minmax(0,1fr)]">
        <DashboardCard
          title="Sessões por dia"
          subtitle={`Últimos ${period} dias · todos os agentes`}
          action={{ to: '/admin/sessions', label: 'Ver sessões' }}
        >
          {kpiLoading || !metrics ? (
            <div role="status">
              <span className="sr-only">Carregando gráfico…</span>
              <Skeleton className="mb-3.5 h-5 w-48" />
              <Skeleton className="h-[260px] w-full" />
            </div>
          ) : metrics.sessions === 0 ? (
            <EmptyState>Nenhuma sessão iniciada nos últimos {period} dias.</EmptyState>
          ) : (
            <ChannelChart
              days={metrics.days}
              series={[{ key: 'sessions', label: 'Todos os canais', color: 'var(--color-ava-700)' }]}
              description={`Gráfico de barras com o número diário de sessões iniciadas nos últimos ${period} dias, somando todos os agentes e canais.`}
            />
          )}
          <p className="mt-3 text-xs text-ink-3">
            A divisão por canal (Web, WhatsApp, Telegram) aparece quando a API passar a informar o canal de cada sessão.
            {metrics?.partial && ' Período parcial: o painel lê até 300 sessões recentes por agente.'}
          </p>
        </DashboardCard>

        <DashboardCard
          title="Status das conexões"
          subtitle={activeAgent ? `Agente: ${activeAgent.name}` : 'Nenhum agente ativo'}
        >
          {!activeAgent ? (
            noAgentHint('as conexões')
          ) : agentDash.loading || !agentDash.data ? (
            <SkeletonRows rows={5} className="h-[38px]" />
          ) : (
            <ConnectionStatusList items={buildConnectionItems(activeAgent, agentDash.data)} />
          )}
        </DashboardCard>
      </section>

      {/* Sessões recentes + agentes */}
      <section className="grid grid-cols-1 gap-5 xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
        <DashboardCard
          title="Sessões recentes"
          subtitle={
            data
              ? `Todos os agentes · atualizado às ${new Date(data.fetchedAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`
              : 'Todos os agentes'
          }
          action={{ to: '/admin/sessions', label: 'Ver todas' }}
        >
          {kpiLoading || !metrics || !data ? (
            <SkeletonRows rows={5} className="h-11" />
          ) : metrics.recent.length === 0 ? (
            <EmptyState>Nenhuma sessão registrada ainda.</EmptyState>
          ) : (
            <RecentSessionsTable rows={metrics.recent} now={data.fetchedAt} />
          )}
        </DashboardCard>

        <DashboardCard title="Agentes mais usados" subtitle="Por número de sessões no período">
          {kpiLoading || !metrics ? (
            <SkeletonRows rows={4} className="h-9" />
          ) : metrics.ranking.length === 0 ? (
            <EmptyState>Nenhum agente teve sessões nos últimos {period} dias.</EmptyState>
          ) : (
            <AgentRanking items={metrics.ranking} />
          )}
          {metrics && metrics.failedAgents > 0 && (
            <p className="mt-3 text-xs text-warn">
              {plural(metrics.failedAgents, 'agente não respondeu', 'agentes não responderam')}; os números podem estar incompletos.
            </p>
          )}
        </DashboardCard>
      </section>

      {/* Base, pedidos, Power BI */}
      <section className="grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-3">
        <DashboardCard
          title="Base de conhecimento"
          subtitle={
            data
              ? `${plural(files.length, 'documento', 'documentos')} · ${fmtBytes(files.reduce((sum, f) => sum + f.fileSize, 0))}`
              : 'Todos os agentes'
          }
          action={{ to: '/admin/knowledge/files', label: 'Arquivos' }}
        >
          {loading && !data ? (
            <SkeletonRows rows={4} className="h-8" />
          ) : files.length === 0 ? (
            <EmptyState>
              {data?.filesFailed ? 'Não foi possível carregar os arquivos.' : 'Nenhum arquivo enviado à base ainda.'}
            </EmptyState>
          ) : (
            <KnowledgeSummary files={files} />
          )}
        </DashboardCard>

        <DashboardCard
          title="Pedidos de acesso"
          subtitle={
            activeAgent && agentDash.data
              ? `WhatsApp · ${fmtInt(pendingRequests.length)} aguardando aprovação`
              : 'WhatsApp'
          }
          action={activeAgent ? { to: '/admin/whatsapp', label: 'Contatos' } : undefined}
        >
          {!activeAgent ? (
            noAgentHint('os pedidos de acesso')
          ) : agentDash.loading || !agentDash.data ? (
            <SkeletonRows rows={2} className="h-[86px]" />
          ) : agentDash.data.pendingRequests === null ? (
            <EmptyState>Não foi possível carregar os pedidos.</EmptyState>
          ) : pendingRequests.length === 0 ? (
            <EmptyState>
              {agentDash.data.whatsappAccess?.mode === WHATSAPP_ACCESS_MODE.ALL
                ? 'O WhatsApp deste agente atende todos os contatos, sem pedidos de acesso.'
                : 'Nenhum pedido pendente. Novos contatos aparecem aqui.'}
            </EmptyState>
          ) : (
            <>
              <AccessRequestList
                requests={pendingRequests.slice(0, VISIBLE_REQUESTS)}
                now={agentDash.data.fetchedAt}
                busyId={busyId}
                onApprove={handleApprove}
                onReject={setRejecting}
              />
              {pendingRequests.length > VISIBLE_REQUESTS && (
                <p className="mt-3 text-xs text-ink-3">
                  Mais {plural(pendingRequests.length - VISIBLE_REQUESTS, 'pedido', 'pedidos')} na{' '}
                  <Link to="/admin/whatsapp" className={inlineLink}>página do WhatsApp</Link>.
                </p>
              )}
            </>
          )}
        </DashboardCard>

        <DashboardCard
          title="Consultas Power BI"
          subtitle={activeAgent ? `Agente: ${activeAgent.name}` : 'Power BI'}
          action={activeAgent?.powerBIEnabled ? { to: '/admin/powerbi', label: 'Registros' } : undefined}
          className="md:col-span-2 xl:col-span-1"
        >
          {!activeAgent ? (
            noAgentHint('as consultas Power BI')
          ) : !activeAgent.powerBIEnabled ? (
            <EmptyState>
              Power BI desativado para este agente.{' '}
              <Link to="/admin/powerbi" className={inlineLink}>Configurar</Link>
            </EmptyState>
          ) : agentDash.loading || !agentDash.data ? (
            <SkeletonRows rows={3} className="h-12" />
          ) : !agentDash.data.powerBILogs ? (
            <EmptyState>Não foi possível carregar o registro de consultas.</EmptyState>
          ) : agentDash.data.powerBILogs.total === 0 ? (
            <EmptyState>Nenhuma consulta registrada ainda.</EmptyState>
          ) : (
            <PowerBIQueryList page={agentDash.data.powerBILogs} now={agentDash.data.fetchedAt} />
          )}
        </DashboardCard>
      </section>

      {rejecting && (
        <Modal
          role="alertdialog"
          title="Recusar pedido de acesso"
          icon="alert"
          iconTone="err"
          onClose={closeReject}
          description={
            <>
              Recusar o pedido de <strong>{rejecting.profileName?.trim() || rejecting.phone || 'este contato'}</strong>? As
              próximas mensagens desse número serão ignoradas, sem aviso.
            </>
          }
          footer={
            <>
              <Button data-autofocus onClick={closeReject}>
                Cancelar
              </Button>
              <Button variant="danger" onClick={confirmReject} loading={busyId !== null}>
                {busyId !== null ? 'Recusando…' : 'Recusar'}
              </Button>
            </>
          }
        />
      )}
    </div>
  )
}

export default DashboardPage
