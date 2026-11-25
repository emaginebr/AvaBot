import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import { PowerBIQueryStatus } from '../../../types/powerbi'
import type { PowerBIQueryLogInfo } from '../../../types/powerbi'
import Button from '../ui/Button'
import { CodeChip } from '../ui/Chip'
import CodeBlock from '../ui/CodeBlock'
import DataTable from '../ui/DataTable'
import type { DataColumn } from '../ui/DataTable'
import Pagination from '../ui/Pagination'
import { EmptyState, LoadingState } from '../ui/States'
import StatusBadge from '../ui/StatusBadge'
import type { StatusTone } from '../ui/StatusBadge'

interface PowerBIQueryLogTableProps {
  slug: string
}

const statusConfig: Record<number, { label: string; tone: StatusTone }> = {
  [PowerBIQueryStatus.Success]: { label: 'Sucesso', tone: 'ok' },
  [PowerBIQueryStatus.Error]: { label: 'Erro', tone: 'err' },
  [PowerBIQueryStatus.Timeout]: { label: 'Timeout', tone: 'warn' },
}

const DetailLabel = ({ children, error = false }: { children: string; error?: boolean }) => (
  <p className={`m-0 mb-1 text-[11px] font-semibold tracking-[0.08em] uppercase ${error ? 'text-err' : 'text-ink-3'}`}>
    {children}
  </p>
)

const PowerBIQueryLogTable = ({ slug }: PowerBIQueryLogTableProps) => {
  const [logs, setLogs] = useState<PowerBIQueryLogInfo[]>([])
  const [loading, setLoading] = useState(false)
  const [loaded, setLoaded] = useState(false)
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [refreshKey, setRefreshKey] = useState(0)
  const pageSize = 20

  useEffect(() => {
    PowerBIService.getQueryLogs(slug, page, pageSize)
      .then((result) => {
        if (result.sucesso && result.dados) {
          setLogs(result.dados.items)
          setTotal(result.dados.total)
        } else {
          toast.error(result.mensagem || 'Erro ao carregar o histórico')
          console.error('[PowerBIQueryLogTable] loadLogs — erro:', result.mensagem, result.erros)
        }
      })
      .catch((err) => {
        console.error('[PowerBIQueryLogTable] loadLogs — exceção:', err)
        toast.error('Erro de rede ao carregar o histórico')
      })
      .finally(() => {
        setLoading(false)
        setLoaded(true)
      })
  }, [slug, page, refreshKey])

  const totalPages = Math.ceil(total / pageSize)

  const refresh = () => {
    setLoading(true)
    setRefreshKey((k) => k + 1)
  }

  const goToPage = (next: number) => {
    setLoading(true)
    setExpandedId(null)
    setPage(next)
  }

  if (!loaded) {
    return <LoadingState label="Carregando histórico..." rows={4} />
  }

  const columns: DataColumn<PowerBIQueryLogInfo>[] = [
    {
      key: 'date',
      header: 'Data',
      nowrap: true,
      cell: (log) => <span className="font-ui-mono text-[12.5px] tabular-nums text-ink-2">{new Date(log.createdAt).toLocaleString('pt-BR')}</span>,
    },
    {
      key: 'question',
      header: 'Pergunta',
      kind: 'main',
      cell: (log) => (
        <span className="line-clamp-2 max-w-[360px] text-ink max-md:max-w-none">{log.userQuestion || '—'}</span>
      ),
    },
    { key: 'dataset', header: 'Dataset', cell: (log) => log.datasetName || '—' },
    { key: 'tool', header: 'Ferramenta', cell: (log) => <CodeChip>{log.toolName}</CodeChip> },
    {
      key: 'status',
      header: 'Situação',
      cell: (log) => {
        const badge = statusConfig[log.status] ?? statusConfig[PowerBIQueryStatus.Error]
        return <StatusBadge tone={badge.tone} label={badge.label} />
      },
    },
    {
      key: 'duration',
      header: 'Duração',
      align: 'right',
      nowrap: true,
      cell: (log) => <span className="font-ui-mono tabular-nums text-ink-2">{log.durationMs} ms</span>,
    },
    {
      key: 'rows',
      header: 'Linhas',
      align: 'right',
      nowrap: true,
      cell: (log) => (
        <span className="font-ui-mono tabular-nums text-ink-2">
          {log.rowCount ?? '—'}
          {log.truncated && (
            <span className="ml-1 rounded bg-warn-bg px-1.5 py-0.5 font-ui text-xs text-warn">truncado</span>
          )}
        </span>
      ),
    },
  ]

  return (
    <div>
      <div className="mb-3.5 flex flex-wrap items-center justify-between gap-2.5">
        <p className="m-0 text-[12.5px] text-ink-3">
          Consultas executadas pelas ferramentas do agente nas últimas conversas. Clique numa linha para ver a consulta.
        </p>
        <Button size="sm" icon="refresh" onClick={refresh} loading={loading}>
          {loading ? 'Atualizando...' : 'Atualizar'}
        </Button>
      </div>

      {logs.length === 0 ? (
        <EmptyState
          icon="chart"
          title="Nenhuma consulta registrada"
          description="As consultas aparecem aqui assim que o agente usar o Power BI numa conversa."
        />
      ) : (
        <DataTable
          columns={columns}
          rows={logs}
          rowKey={(log) => log.powerBIQueryLogId}
          caption="Histórico de consultas Power BI"
          expandable={{
            isExpanded: (log) => expandedId === log.powerBIQueryLogId,
            onToggle: (log) => setExpandedId(expandedId === log.powerBIQueryLogId ? null : log.powerBIQueryLogId),
            render: (log) => (
              <div className="flex flex-col gap-3">
                <div>
                  <DetailLabel>Pergunta do usuário</DetailLabel>
                  <p className="m-0 text-[13px] whitespace-pre-wrap [overflow-wrap:anywhere] text-ink">{log.userQuestion || '—'}</p>
                </div>
                <div>
                  <DetailLabel>Consulta</DetailLabel>
                  <CodeBlock variant="plain">
                    {log.query || '—'}
                  </CodeBlock>
                </div>
                {log.errorMessage && (
                  <div>
                    <DetailLabel error>Erro</DetailLabel>
                    <p className="m-0 text-[13px] whitespace-pre-wrap [overflow-wrap:anywhere] text-err">{log.errorMessage}</p>
                  </div>
                )}
              </div>
            ),
          }}
        />
      )}

      {totalPages > 1 && (
        <Pagination
          summary={`Mostrando ${(page - 1) * pageSize + 1} a ${Math.min(page * pageSize, total)} de ${total} consultas`}
          onPrevious={() => goToPage(Math.max(1, page - 1))}
          onNext={() => goToPage(Math.min(totalPages, page + 1))}
          previousDisabled={page === 1}
          nextDisabled={page === totalPages}
        />
      )}
    </div>
  )
}

export default PowerBIQueryLogTable
