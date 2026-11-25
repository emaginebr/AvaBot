import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { ChatHistoryService } from '../../Services/ChatHistoryService'
import type { ChatSessionInfo } from '../../types/chatSession'
import { ButtonLink } from '../../components/admin/ui/Button'
import { Card, CardHeader } from '../../components/admin/ui/Card'
import DataTable from '../../components/admin/ui/DataTable'
import type { DataColumn } from '../../components/admin/ui/DataTable'
import PageHeader from '../../components/admin/ui/PageHeader'
import Pagination from '../../components/admin/ui/Pagination'
import { EmptyState, ErrorState, LoadingState, NoAgentState } from '../../components/admin/ui/States'
import StatusBadge from '../../components/admin/ui/StatusBadge'

const pageSize = 20

const columns: DataColumn<ChatSessionInfo>[] = [
  {
    key: 'visitor',
    header: 'Visitante',
    kind: 'main',
    cell: (session) =>
      session.userName ? (
        <span className="block font-medium [overflow-wrap:anywhere] text-ink">{session.userName}</span>
      ) : (
        <span className="block text-ink-3">Anônimo</span>
      ),
  },
  { key: 'email', header: 'E-mail', cell: (session) => session.userEmail || '-' },
  {
    key: 'phone',
    header: 'Telefone',
    nowrap: true,
    cell: (session) => <span className="font-ui-mono tabular-nums">{session.userPhone || '-'}</span>,
  },
  {
    key: 'start',
    header: 'Início',
    nowrap: true,
    cell: (session) => (
      <span className="font-ui-mono text-[12.5px] tabular-nums text-ink-2">{new Date(session.startedAt).toLocaleString('pt-BR')}</span>
    ),
  },
  {
    key: 'end',
    header: 'Fim',
    nowrap: true,
    cell: (session) =>
      session.endedAt ? (
        <span className="font-ui-mono text-[12.5px] tabular-nums text-ink-2">{new Date(session.endedAt).toLocaleString('pt-BR')}</span>
      ) : (
        <StatusBadge tone="ok" pulse label="Em andamento" />
      ),
  },
  {
    key: 'messages',
    header: 'Msgs',
    label: 'Mensagens',
    align: 'right',
    cell: (session) => <span className="font-ui-mono tabular-nums">{session.messageCount ?? 0}</span>,
  },
  {
    key: 'actions',
    header: 'Ações',
    hideHeader: true,
    kind: 'actions',
    cell: (session) => (
      <ButtonLink size="sm" icon="chat" to={`/admin/sessions/${session.chatSessionId}`}>
        Ver conversa
      </ButtonLink>
    ),
  },
]

const SessionListPage = () => {
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const [sessions, setSessions] = useState<ChatSessionInfo[]>([])
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)
  // Chave da última consulta concluída: "carregando" é derivado dela, sem setState síncrono no effect.
  const [loadedKey, setLoadedKey] = useState<string | null>(null)

  const agentId = selectedAgent?.agentId
  const requestKey = agentId ? `${agentId}:${page}:${reloadToken}` : null
  const loading = requestKey !== null && requestKey !== loadedKey

  useEffect(() => {
    if (!selectedAgent) return
    const key = `${selectedAgent.agentId}:${page}:${reloadToken}`
    ChatHistoryService.getSessions(selectedAgent.agentId, page, pageSize)
      .then((result) => {
        if (result.sucesso) {
          setSessions(result.dados.items)
          setTotal(result.dados.total)
          setError(null)
        } else {
          toast.error(result.mensagem || 'Erro ao carregar sessões')
          console.error('[SessionListPage] getSessions — erro:', result.mensagem)
          setError(result.mensagem || 'Erro ao carregar sessões')
        }
      })
      .catch((err) => {
        console.error('[SessionListPage] getSessions — exceção:', err)
        toast.error('Erro de rede ao carregar sessões')
        setError('Erro de rede ao carregar sessões')
      })
      .finally(() => setLoadedKey(key))
  }, [selectedAgent, page, reloadToken])

  if (!selectedAgent) {
    return (
      <div className="flex flex-col gap-5">
        <PageHeader title="Sessões" description="Histórico de conversas do agente." />
        <NoAgentState purpose="ver as sessões" />
      </div>
    )
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        agentName={selectedAgent.name}
        title="Sessões"
        description={`Histórico de conversas do agente ${selectedAgent.name}.`}
      />

      <Card aria-labelledby="sessoes-lista-titulo">
        <CardHeader titleId="sessoes-lista-titulo" title="Conversas" subtitle="Mais recentes primeiro" />
        {loading ? (
          <LoadingState label="Carregando sessões…" rows={5} />
        ) : error && sessions.length === 0 ? (
          <ErrorState
            title="Não foi possível carregar as sessões."
            description={error}
            onRetry={() => setReloadToken((t) => t + 1)}
          />
        ) : sessions.length === 0 ? (
          <EmptyState
            icon="chat"
            title="Nenhuma sessão encontrada para este agente"
            description="As conversas aparecem aqui assim que alguém falar com o agente pelo site, Telegram ou WhatsApp."
          />
        ) : (
          <>
            <DataTable
              columns={columns}
              rows={sessions}
              rowKey={(session) => session.chatSessionId}
              caption="Sessões do agente"
            />
            {totalPages > 1 && (
              <Pagination
                summary={`Mostrando ${(page - 1) * pageSize + 1} a ${Math.min(page * pageSize, total)} de ${total} sessões`}
                onPrevious={() => setPage((p) => Math.max(1, p - 1))}
                onNext={() => setPage((p) => Math.min(totalPages, p + 1))}
                previousDisabled={page === 1}
                nextDisabled={page === totalPages}
              />
            )}
          </>
        )}
      </Card>
    </div>
  )
}

export default SessionListPage
