import { useEffect } from 'react'
import { useAgentStore } from '../../stores/useAgentStore'

const DashboardPage = () => {
  const { agents, loading, loadAgents } = useAgentStore()

  useEffect(() => {
    loadAgents()
  }, [loadAgents])

  const activeAgents = agents.filter((a) => a.status === 1)
  const inactiveAgents = agents.filter((a) => a.status === 0)

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Dashboard</h1>

      {loading ? (
        <div className="flex items-center gap-2 text-gray-500">
          <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
          Carregando...
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-5 mb-8">
            <div className="bg-white rounded-xl border border-gray-200 p-5">
              <p className="text-sm text-gray-500 mb-1">Total de Agentes</p>
              <p className="text-3xl font-bold text-gray-900">{agents.length}</p>
            </div>

            <div className="bg-white rounded-xl border border-gray-200 p-5">
              <p className="text-sm text-gray-500 mb-1">Agentes Ativos</p>
              <p className="text-3xl font-bold text-green-600">{activeAgents.length}</p>
            </div>

            <div className="bg-white rounded-xl border border-gray-200 p-5">
              <p className="text-sm text-gray-500 mb-1">Agentes Inativos</p>
              <p className="text-3xl font-bold text-gray-400">{inactiveAgents.length}</p>
            </div>
          </div>

          {agents.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-200">
              <div className="px-5 py-4 border-b border-gray-200">
                <h2 className="font-semibold text-gray-900">Agentes Cadastrados</h2>
              </div>
              <div className="divide-y divide-gray-100">
                {agents.map((agent) => (
                  <div key={agent.agentId} className="px-5 py-3 flex items-center justify-between">
                    <div>
                      <p className="font-medium text-gray-900">{agent.name}</p>
                      <p className="text-sm text-gray-500">{agent.description || 'Sem descrição'}</p>
                    </div>
                    <span
                      className={`px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        agent.status === 1
                          ? 'bg-green-100 text-green-700'
                          : 'bg-gray-100 text-gray-500'
                      }`}
                    >
                      {agent.status === 1 ? 'Ativo' : 'Inativo'}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default DashboardPage
