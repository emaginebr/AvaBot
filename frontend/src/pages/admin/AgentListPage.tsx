import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'

const AgentListPage = () => {
  const { agents, loading, error, loadAgents } = useAgentStore()
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)

  useEffect(() => {
    loadAgents()
  }, [loadAgents])

  useEffect(() => {
    if (error) toast.error(error)
  }, [error])

  const handleToggleStatus = async (id: number) => {
    try {
      const result = await AgentService.toggleStatus(id)
      if (!result.sucesso) {
        toast.error(result.mensagem || 'Erro ao alterar status do agente')
        return
      }
      toast.success('Status do agente alterado com sucesso')
      await loadAgents()
    } catch (err) {
      console.error('[AgentListPage] handleToggleStatus error:', err)
      toast.error('Erro de rede ao alterar status do agente')
    }
  }

  const handleDelete = async (id: number) => {
    try {
      const result = await AgentService.delete(id)
      if (!result.sucesso) {
        toast.error(result.mensagem || 'Erro ao remover agente')
        setDeleteConfirm(null)
        return
      }
      toast.success('Agente removido com sucesso')
      setDeleteConfirm(null)
      await loadAgents()
    } catch (err) {
      console.error('[AgentListPage] handleDelete error:', err)
      toast.error('Erro de rede ao remover agente')
      setDeleteConfirm(null)
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Agentes</h1>
          <p className="text-sm text-gray-500 mt-1">Gerencie seus agentes de atendimento</p>
        </div>
        <Link
          to="/admin/agents/new"
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-ava-600 text-white rounded-lg font-medium hover:bg-ava-700 transition-colors shadow-sm"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clipRule="evenodd" />
          </svg>
          Novo Agente
        </Link>
      </div>

      {loading ? (
        <div className="flex items-center gap-2 text-gray-500">
          <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
          Carregando...
        </div>
      ) : agents.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border border-gray-200">
          <p className="text-gray-400 mb-4">Nenhum agente cadastrado</p>
          <Link
            to="/admin/agents/new"
            className="text-ava-600 hover:text-ava-700 font-medium text-sm"
          >
            Criar primeiro agente
          </Link>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Nome</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Slug</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Coleta</th>
                  <th className="text-right px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {agents.map((agent) => (
                  <tr key={agent.agentId} className="hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-4">
                      <div>
                        <p className="font-medium text-gray-900">{agent.name}</p>
                        {agent.description && (
                          <p className="text-sm text-gray-500 mt-0.5 truncate max-w-xs">{agent.description}</p>
                        )}
                      </div>
                    </td>
                    <td className="px-5 py-4">
                      <code className="text-sm text-gray-500 bg-gray-100 px-2 py-0.5 rounded">{agent.slug}</code>
                    </td>
                    <td className="px-5 py-4">
                      <span className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        agent.status === 1
                          ? 'bg-green-100 text-green-700'
                          : 'bg-gray-100 text-gray-500'
                      }`}>
                        <span className={`w-1.5 h-1.5 rounded-full ${agent.status === 1 ? 'bg-green-500' : 'bg-gray-400'}`} />
                        {agent.status === 1 ? 'Ativo' : 'Inativo'}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <div className="flex gap-1.5">
                        {agent.collectName && <span className="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded">Nome</span>}
                        {agent.collectEmail && <span className="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded">Email</span>}
                        {agent.collectPhone && <span className="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded">Tel</span>}
                        {!agent.collectName && !agent.collectEmail && !agent.collectPhone && (
                          <span className="text-xs text-gray-400">Nenhum</span>
                        )}
                      </div>
                    </td>
                    <td className="px-5 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Link
                          to={`/admin/agents/${agent.agentId}/edit`}
                          className="text-sm text-ava-600 hover:text-ava-700 font-medium"
                        >
                          Editar
                        </Link>
                        <button
                          onClick={() => handleToggleStatus(agent.agentId)}
                          className="text-sm text-amber-600 hover:text-amber-700 font-medium"
                        >
                          {agent.status === 1 ? 'Desativar' : 'Ativar'}
                        </button>
                        {deleteConfirm === agent.agentId ? (
                          <div className="flex items-center gap-1">
                            <button
                              onClick={() => handleDelete(agent.agentId)}
                              className="text-sm text-white bg-red-600 hover:bg-red-700 px-2 py-0.5 rounded font-medium"
                            >
                              Confirmar
                            </button>
                            <button
                              onClick={() => setDeleteConfirm(null)}
                              className="text-sm text-gray-500 hover:text-gray-700 font-medium"
                            >
                              Cancelar
                            </button>
                          </div>
                        ) : (
                          <button
                            onClick={() => setDeleteConfirm(agent.agentId)}
                            className="text-sm text-red-600 hover:text-red-700 font-medium"
                          >
                            Remover
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

export default AgentListPage
