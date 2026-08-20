import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'

const AgentSettingsPage = () => {
  const { selectedAgent, selectAgent, loadAgents } = useAgentStore()
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const navigate = useNavigate()

  if (!selectedAgent) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-400 mb-2">Nenhum agente selecionado</p>
          <p className="text-sm text-gray-300">Selecione um agente na navbar para acessar as configurações.</p>
        </div>
      </div>
    )
  }

  const handleDelete = async () => {
    setDeleting(true)
    try {
      const result = await AgentService.delete(selectedAgent.agentId)
      if (result.sucesso) {
        toast.success('Agente excluído com sucesso')
        selectAgent(null)
        await loadAgents()
        navigate('/admin/agents')
      } else {
        toast.error(result.mensagem || 'Erro ao excluir agente')
        console.error('[AgentSettingsPage] handleDelete — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[AgentSettingsPage] handleDelete — exceção:', err)
      toast.error('Erro de rede ao excluir agente')
    } finally {
      setDeleting(false)
      setShowDeleteConfirm(false)
    }
  }

  const handleToggleStatus = async () => {
    try {
      const result = await AgentService.toggleStatus(selectedAgent.agentId)
      if (!result.sucesso) {
        toast.error(result.mensagem || 'Erro ao alterar status do agente')
        console.error('[AgentSettingsPage] handleToggleStatus — erro:', result.mensagem, result.erros)
        return
      }
      toast.success('Status do agente alterado com sucesso')
      await loadAgents()
      const updated = useAgentStore.getState().agents.find((a) => a.agentId === selectedAgent.agentId)
      if (updated) selectAgent(updated)
    } catch (err) {
      console.error('[AgentSettingsPage] handleToggleStatus — exceção:', err)
      toast.error('Erro de rede ao alterar status do agente')
    }
  }

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Configurações do Agente</h1>

      <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">{selectedAgent.name}</h2>
            <p className="text-sm text-gray-500 mt-0.5">{selectedAgent.description || 'Sem descrição'}</p>
            <p className="text-xs text-gray-400 mt-1">
              Slug: <code className="bg-gray-100 px-1.5 py-0.5 rounded">{selectedAgent.slug}</code>
            </p>
          </div>
          <span className={`px-2.5 py-0.5 rounded-full text-xs font-medium ${
            selectedAgent.status === 1
              ? 'bg-green-100 text-green-700'
              : 'bg-gray-100 text-gray-500'
          }`}>
            {selectedAgent.status === 1 ? 'Ativo' : 'Inativo'}
          </span>
        </div>

        <div className="border-t border-gray-100 pt-4 space-y-2 text-sm text-gray-600">
          <p>Coleta: {[
            selectedAgent.collectName && 'Nome',
            selectedAgent.collectEmail && 'Email',
            selectedAgent.collectPhone && 'Telefone',
          ].filter(Boolean).join(', ') || 'Nenhum campo'}</p>
          <p>Criado em: {new Date(selectedAgent.createdAt).toLocaleString('pt-BR')}</p>
        </div>
      </div>

      <div className="space-y-3">
        <button
          onClick={() => navigate(`/admin/agents/${selectedAgent.agentId}/edit`)}
          className="w-full flex items-center justify-between px-5 py-3 bg-white border border-gray-200 rounded-xl hover:bg-gray-50 transition-colors"
        >
          <div className="text-left">
            <p className="font-medium text-gray-900">Editar Agente</p>
            <p className="text-sm text-gray-500">Alterar nome, descrição, prompt e configurações de coleta</p>
          </div>
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clipRule="evenodd" />
          </svg>
        </button>

        <button
          onClick={handleToggleStatus}
          className="w-full flex items-center justify-between px-5 py-3 bg-white border border-gray-200 rounded-xl hover:bg-gray-50 transition-colors"
        >
          <div className="text-left">
            <p className="font-medium text-gray-900">
              {selectedAgent.status === 1 ? 'Desativar Agente' : 'Ativar Agente'}
            </p>
            <p className="text-sm text-gray-500">
              {selectedAgent.status === 1
                ? 'O agente não estará disponível para novos atendimentos'
                : 'O agente ficará disponível para atendimentos'}
            </p>
          </div>
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clipRule="evenodd" />
          </svg>
        </button>

        {showDeleteConfirm ? (
          <div className="px-5 py-4 bg-red-50 border border-red-200 rounded-xl">
            <p className="text-sm text-red-700 mb-3">
              Tem certeza que deseja remover o agente <strong>{selectedAgent.name}</strong>? Esta ação não pode ser desfeita.
            </p>
            <div className="flex gap-2">
              <button
                onClick={handleDelete}
                disabled={deleting}
                className="px-4 py-2 bg-red-600 text-white text-sm rounded-lg hover:bg-red-700 disabled:opacity-50 font-medium"
              >
                {deleting ? 'Removendo...' : 'Sim, remover'}
              </button>
              <button
                onClick={() => setShowDeleteConfirm(false)}
                className="px-4 py-2 bg-white text-gray-700 text-sm rounded-lg border border-gray-300 hover:bg-gray-50 font-medium"
              >
                Cancelar
              </button>
            </div>
          </div>
        ) : (
          <button
            onClick={() => setShowDeleteConfirm(true)}
            className="w-full flex items-center justify-between px-5 py-3 bg-white border border-red-200 rounded-xl hover:bg-red-50 transition-colors"
          >
            <div className="text-left">
              <p className="font-medium text-red-600">Excluir Agente</p>
              <p className="text-sm text-red-400">Remover permanentemente este agente e todos os seus dados</p>
            </div>
            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clipRule="evenodd" />
            </svg>
          </button>
        )}
      </div>
    </div>
  )
}

export default AgentSettingsPage
