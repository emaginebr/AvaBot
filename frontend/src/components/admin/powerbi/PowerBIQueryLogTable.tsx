import { Fragment, useEffect, useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import { PowerBIQueryStatus } from '../../../types/powerbi'
import type { PowerBIQueryLogInfo } from '../../../types/powerbi'

interface PowerBIQueryLogTableProps {
  slug: string
}

const statusConfig = {
  [PowerBIQueryStatus.Success]: { label: 'Sucesso', className: 'bg-green-100 text-green-700' },
  [PowerBIQueryStatus.Error]: { label: 'Erro', className: 'bg-red-100 text-red-700' },
  [PowerBIQueryStatus.Timeout]: { label: 'Timeout', className: 'bg-yellow-100 text-yellow-700' },
}

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
    return (
      <div className="flex items-center gap-2 text-gray-500">
        <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
        Carregando histórico...
      </div>
    )
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <p className="text-sm text-gray-500">
          Consultas executadas pelas ferramentas do agente nas últimas conversas.
        </p>
        <button
          onClick={refresh}
          disabled={loading}
          className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors whitespace-nowrap"
        >
          {loading ? 'Atualizando...' : 'Atualizar'}
        </button>
      </div>

      {logs.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <p className="text-sm text-gray-400">Nenhuma consulta registrada.</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Data</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Pergunta</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Dataset</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Tool</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                  <th className="text-right px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Duração</th>
                  <th className="text-right px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Linhas</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {logs.map((log) => {
                  const badge = statusConfig[log.status] ?? statusConfig[PowerBIQueryStatus.Error]
                  const isExpanded = expandedId === log.powerBIQueryLogId

                  return (
                    <Fragment key={log.powerBIQueryLogId}>
                      <tr
                        onClick={() => setExpandedId(isExpanded ? null : log.powerBIQueryLogId)}
                        className="hover:bg-gray-50 transition-colors cursor-pointer align-top"
                      >
                        <td className="px-5 py-3 text-sm text-gray-500 whitespace-nowrap">
                          {new Date(log.createdAt).toLocaleString('pt-BR')}
                        </td>
                        <td className="px-5 py-3 text-sm text-gray-700 max-w-xs">
                          <span className="line-clamp-2">{log.userQuestion || '—'}</span>
                        </td>
                        <td className="px-5 py-3 text-sm text-gray-700">{log.datasetName || '—'}</td>
                        <td className="px-5 py-3">
                          <code className="text-xs bg-gray-100 px-2 py-0.5 rounded">{log.toolName}</code>
                        </td>
                        <td className="px-5 py-3">
                          <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${badge.className}`}>
                            {badge.label}
                          </span>
                        </td>
                        <td className="px-5 py-3 text-sm text-gray-500 text-right tabular-nums">{log.durationMs} ms</td>
                        <td className="px-5 py-3 text-sm text-gray-500 text-right tabular-nums">
                          {log.rowCount ?? '—'}
                          {log.truncated && (
                            <span className="ml-1 text-xs text-yellow-700 bg-yellow-100 px-1.5 py-0.5 rounded">truncado</span>
                          )}
                        </td>
                      </tr>

                      {isExpanded && (
                        <tr className="bg-gray-50">
                          <td colSpan={7} className="px-5 py-4">
                            <div className="space-y-3">
                              <div>
                                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                                  Pergunta do usuário
                                </p>
                                <p className="text-sm text-gray-700 whitespace-pre-wrap">
                                  {log.userQuestion || '—'}
                                </p>
                              </div>
                              <div>
                                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                                  Consulta
                                </p>
                                <pre className="text-xs text-gray-600 bg-white border border-gray-200 rounded-lg p-3 overflow-x-auto whitespace-pre-wrap">{log.query || '—'}</pre>
                              </div>
                              {log.errorMessage && (
                                <div>
                                  <p className="text-xs font-semibold text-red-500 uppercase tracking-wider mb-1">
                                    Erro
                                  </p>
                                  <p className="text-sm text-red-600 whitespace-pre-wrap">{log.errorMessage}</p>
                                </div>
                              )}
                            </div>
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4">
          <p className="text-sm text-gray-500">
            Mostrando {(page - 1) * pageSize + 1} a {Math.min(page * pageSize, total)} de {total} consultas
          </p>
          <div className="flex gap-2">
            <button
              onClick={() => goToPage(Math.max(1, page - 1))}
              disabled={page === 1}
              className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
            >
              Anterior
            </button>
            <button
              onClick={() => goToPage(Math.min(totalPages, page + 1))}
              disabled={page === totalPages}
              className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
            >
              Próxima
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

export default PowerBIQueryLogTable
