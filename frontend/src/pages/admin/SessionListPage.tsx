import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { ChatHistoryService } from '../../Services/ChatHistoryService'
import type { ChatSessionInfo } from '../../types/chatSession'

const SessionListPage = () => {
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const [sessions, setSessions] = useState<ChatSessionInfo[]>([])
  const [loading, setLoading] = useState(false)
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const pageSize = 20

  useEffect(() => {
    if (!selectedAgent) return
    setLoading(true)
    ChatHistoryService.getSessions(selectedAgent.agentId, page, pageSize)
      .then((result) => {
        if (result.sucesso) {
          setSessions(result.dados.items)
          setTotal(result.dados.total)
        } else {
          toast.error(result.mensagem || 'Erro ao carregar sessões')
          console.error('[SessionListPage] getSessions — erro:', result.mensagem)
        }
      })
      .catch((err) => {
        console.error('[SessionListPage] getSessions — exceção:', err)
        toast.error('Erro de rede ao carregar sessões')
      })
      .finally(() => setLoading(false))
  }, [selectedAgent, page])

  if (!selectedAgent) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-400 mb-2">Nenhum agente selecionado</p>
          <p className="text-sm text-gray-300">Selecione um agente na navbar para ver as sessões.</p>
        </div>
      </div>
    )
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Sessões</h1>
        <p className="text-sm text-gray-500 mt-1">Histórico de conversas do agente {selectedAgent.name}</p>
      </div>

      {loading ? (
        <div className="flex items-center gap-2 text-gray-500">
          <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
          Carregando...
        </div>
      ) : sessions.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border border-gray-200">
          <p className="text-gray-400">Nenhuma sessão encontrada para este agente.</p>
        </div>
      ) : (
        <>
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-gray-200">
                    <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Visitante</th>
                    <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Email</th>
                    <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Telefone</th>
                    <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Início</th>
                    <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Fim</th>
                    <th className="text-center px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Msgs</th>
                    <th className="text-right px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Ações</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {sessions.map((session) => (
                    <tr key={session.chatSessionId} className="hover:bg-gray-50 transition-colors">
                      <td className="px-5 py-3 text-sm font-medium text-gray-900">
                        {session.userName || <span className="text-gray-400">Anônimo</span>}
                      </td>
                      <td className="px-5 py-3 text-sm text-gray-500">{session.userEmail || '-'}</td>
                      <td className="px-5 py-3 text-sm text-gray-500">{session.userPhone || '-'}</td>
                      <td className="px-5 py-3 text-sm text-gray-500">
                        {new Date(session.startedAt).toLocaleString('pt-BR')}
                      </td>
                      <td className="px-5 py-3 text-sm text-gray-500">
                        {session.endedAt
                          ? new Date(session.endedAt).toLocaleString('pt-BR')
                          : <span className="text-green-600 text-xs font-medium">Em andamento</span>}
                      </td>
                      <td className="px-5 py-3 text-sm text-gray-500 text-center">
                        <span className="bg-gray-100 px-2 py-0.5 rounded-full text-xs font-medium">
                          {session.messageCount ?? 0}
                        </span>
                      </td>
                      <td className="px-5 py-3 text-right">
                        <Link
                          to={`/admin/sessions/${session.chatSessionId}`}
                          className="text-sm text-ava-600 hover:text-ava-700 font-medium"
                        >
                          Ver conversa
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <p className="text-sm text-gray-500">
                Mostrando {(page - 1) * pageSize + 1} a {Math.min(page * pageSize, total)} de {total} sessões
              </p>
              <div className="flex gap-2">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  Anterior
                </button>
                <button
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                  className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  Próxima
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default SessionListPage
