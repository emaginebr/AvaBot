import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import type { AgentInsertInfo } from '../../types/agent'

interface AgentFormProps {
  initialData?: AgentInsertInfo
  onSubmit: (data: AgentInsertInfo) => Promise<void>
  loading: boolean
}

const AgentForm = ({ initialData, onSubmit, loading }: AgentFormProps) => {
  const navigate = useNavigate()
  const chatModels = [
    { value: 'gpt-5.4', label: 'GPT-5.4' },
    { value: 'gpt-5.4-pro', label: 'GPT-5.4 Pro' },
    { value: 'gpt-5.4-mini', label: 'GPT-5.4 Mini' },
    { value: 'gpt-5.4-nano', label: 'GPT-5.4 Nano' },
    { value: 'gpt-5.3', label: 'GPT-5.3' },
    { value: 'gpt-5.2', label: 'GPT-5.2' },
    { value: 'gpt-5.1', label: 'GPT-5.1' },
    { value: 'gpt-5', label: 'GPT-5' },
    { value: 'gpt-4o', label: 'GPT-4o' },
    { value: 'gpt-4.1', label: 'GPT-4.1' },
    { value: 'gpt-4.1-mini', label: 'GPT-4.1 Mini' },
  ]

  const [formData, setFormData] = useState<AgentInsertInfo>({
    name: '',
    description: null,
    systemPrompt: '',
    collectName: false,
    collectEmail: false,
    collectPhone: false,
    chatModel: 'gpt-4o',
    telegramBotName: null,
    telegramBotToken: null,
  })

  useEffect(() => {
    if (initialData) setFormData(initialData)
  }, [initialData])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value, type } = e.target
    if (type === 'checkbox') {
      setFormData((prev) => ({ ...prev, [name]: (e.target as HTMLInputElement).checked }))
    } else {
      setFormData((prev) => ({ ...prev, [name]: value || null }))
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    await onSubmit(formData)
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="bg-white rounded-xl border border-gray-200 p-6 space-y-5">
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1.5">
            Nome do Agente <span className="text-red-500">*</span>
          </label>
          <input
            id="name"
            type="text"
            name="name"
            value={formData.name}
            onChange={handleChange}
            required
            className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow"
            placeholder="Ex: Bia, Assistente de Vendas"
          />
        </div>

        <div>
          <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1.5">
            Descrição
          </label>
          <textarea
            id="description"
            name="description"
            value={formData.description ?? ''}
            onChange={handleChange}
            rows={2}
            className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow resize-none"
            placeholder="Breve descrição do agente"
          />
        </div>

        <div>
          <label htmlFor="systemPrompt" className="block text-sm font-medium text-gray-700 mb-1.5">
            Prompt do Sistema <span className="text-red-500">*</span>
          </label>
          <textarea
            id="systemPrompt"
            name="systemPrompt"
            value={formData.systemPrompt}
            onChange={handleChange}
            required
            rows={8}
            className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow font-mono text-sm resize-y"
            placeholder="Você é um assistente especializado em..."
          />
          <p className="mt-1.5 text-xs text-gray-400">
            Instruções que definem o comportamento e personalidade do agente
          </p>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <h3 className="text-sm font-semibold text-gray-900 mb-4">Modelo de IA</h3>
        <div>
          <label htmlFor="chatModel" className="block text-sm font-medium text-gray-700 mb-1.5">
            Modelo <span className="text-red-500">*</span>
          </label>
          <select
            id="chatModel"
            name="chatModel"
            value={formData.chatModel}
            onChange={(e) => setFormData((prev) => ({ ...prev, chatModel: e.target.value }))}
            className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow bg-white"
          >
            {chatModels.map((model) => (
              <option key={model.value} value={model.value}>
                {model.label}
              </option>
            ))}
          </select>
          <p className="mt-1.5 text-xs text-gray-400">
            Modelo utilizado para gerar as respostas do agente
          </p>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <h3 className="text-sm font-semibold text-gray-900 mb-4">Coleta de Dados do Visitante</h3>
        <p className="text-xs text-gray-500 mb-4">
          Selecione quais informações o agente deve solicitar antes de iniciar a conversa
        </p>
        <div className="space-y-3">
          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              name="collectName"
              checked={formData.collectName}
              onChange={handleChange}
              className="w-4 h-4 rounded border-gray-300 text-ava-600 focus:ring-ava-500"
            />
            <span className="text-sm text-gray-700">Solicitar nome</span>
          </label>

          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              name="collectEmail"
              checked={formData.collectEmail}
              onChange={handleChange}
              className="w-4 h-4 rounded border-gray-300 text-ava-600 focus:ring-ava-500"
            />
            <span className="text-sm text-gray-700">Solicitar e-mail</span>
          </label>

          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              name="collectPhone"
              checked={formData.collectPhone}
              onChange={handleChange}
              className="w-4 h-4 rounded border-gray-300 text-ava-600 focus:ring-ava-500"
            />
            <span className="text-sm text-gray-700">Solicitar telefone</span>
          </label>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={loading}
          className="px-6 py-2.5 bg-ava-600 text-white rounded-lg font-medium hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-sm"
        >
          {loading ? 'Salvando...' : 'Salvar'}
        </button>
        <button
          type="button"
          onClick={() => navigate('/admin/agents')}
          className="px-6 py-2.5 text-gray-700 bg-white border border-gray-300 rounded-lg font-medium hover:bg-gray-50 transition-colors"
        >
          Cancelar
        </button>
      </div>
    </form>
  )
}

export default AgentForm
