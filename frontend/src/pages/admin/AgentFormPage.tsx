import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import AgentForm from '../../components/admin/AgentForm'
import { AgentService } from '../../Services/AgentService'
import type { AgentInsertInfo } from '../../types/agent'

const AgentFormPage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [initialData, setInitialData] = useState<AgentInsertInfo | undefined>()
  const [loading, setLoading] = useState(false)
  const isEditing = Boolean(id)

  useEffect(() => {
    if (id) {
      AgentService.getAll().then((result) => {
        if (result.sucesso) {
          const agent = result.dados.find((a) => a.agentId === Number(id))
          if (agent) {
            setInitialData({
              name: agent.name,
              description: agent.description,
              systemPrompt: agent.systemPrompt,
              collectName: agent.collectName,
              collectEmail: agent.collectEmail,
              collectPhone: agent.collectPhone,
              chatModel: agent.chatModel || 'gpt-4o',
              telegramBotName: agent.telegramBotName,
              telegramBotToken: agent.telegramBotToken,
            })
          } else {
            toast.error('Agente não encontrado')
          }
        } else {
          toast.error(result.mensagem || 'Erro ao carregar agente')
        }
      }).catch(() => {
        toast.error('Erro de rede ao carregar agente')
      })
    }
  }, [id])

  const handleSubmit = async (data: AgentInsertInfo) => {
    setLoading(true)
    try {
      const result = isEditing
        ? await AgentService.update(Number(id), data)
        : await AgentService.create(data)

      if (result.sucesso) {
        toast.success(isEditing ? 'Agente atualizado com sucesso' : 'Agente criado com sucesso')
        navigate('/admin/agents')
      } else {
        toast.error(result.mensagem || 'Erro ao salvar agente')
      }
    } catch {
      toast.error('Erro de rede ao salvar agente')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-3xl">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">
          {isEditing ? 'Editar Agente' : 'Novo Agente'}
        </h1>
        <p className="text-sm text-gray-500 mt-1">
          {isEditing ? 'Atualize as informações do agente' : 'Preencha os dados para criar um novo agente'}
        </p>
      </div>

      <AgentForm
        initialData={initialData}
        onSubmit={handleSubmit}
        loading={loading}
      />
    </div>
  )
}

export default AgentFormPage
