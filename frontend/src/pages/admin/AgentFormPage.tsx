import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import AgentForm from '../../components/admin/AgentForm'
import PageHeader from '../../components/admin/ui/PageHeader'
import { AgentService } from '../../Services/AgentService'
import type { AgentInsertInfo } from '../../types/agent'

const AgentFormPage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [initialData, setInitialData] = useState<AgentInsertInfo | undefined>()
  const [hasSavedOpenAIApiKey, setHasSavedOpenAIApiKey] = useState(false)
  const [loading, setLoading] = useState(false)
  // Muda a cada carga do agente; remonta o formulário para ele partir dos dados recebidos.
  const [formVersion, setFormVersion] = useState(0)
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
              // A chave salva nunca volta para o cliente: o campo começa vazio e
              // deixar em branco preserva o que está no banco.
              openAIApiKey: null,
              removeOpenAIApiKey: false,
            })
            setHasSavedOpenAIApiKey(agent.hasOpenAIApiKey)
            setFormVersion((v) => v + 1)
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
    <div className="flex flex-col gap-5">
      <PageHeader
        backTo={{ to: '/admin/agents', label: 'Meus agentes' }}
        title={isEditing ? 'Editar agente' : 'Novo agente'}
        description={isEditing ? 'Atualize as informações do agente.' : 'Preencha os dados para criar um novo agente.'}
      />

      <div className="w-full max-w-[960px]">
        <AgentForm
          key={formVersion}
          initialData={initialData}
          onSubmit={handleSubmit}
          loading={loading}
          agentId={isEditing ? Number(id) : undefined}
          hasSavedOpenAIApiKey={hasSavedOpenAIApiKey}
        />
      </div>
    </div>
  )
}

export default AgentFormPage
