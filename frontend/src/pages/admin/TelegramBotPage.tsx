import { useState, useEffect } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'
import type { TelegramWebhookInfo } from '../../types/agent'

const TelegramBotPage = () => {
  const { selectedAgent, loadAgents } = useAgentStore()

  const [botName, setBotName] = useState('')
  const [botToken, setBotToken] = useState('')
  const [showToken, setShowToken] = useState(false)
  const [saving, setSaving] = useState(false)
  const [settingUpWebhook, setSettingUpWebhook] = useState(false)
  const [checkingWebhook, setCheckingWebhook] = useState(false)
  const [regenerating, setRegenerating] = useState(false)
  const [webhookInfo, setWebhookInfo] = useState<TelegramWebhookInfo | null>(null)
  const [botNameError, setBotNameError] = useState('')

  useEffect(() => {
    if (selectedAgent) {
      setBotName(selectedAgent.telegramBotName || '')
      setBotToken(selectedAgent.telegramBotToken || '')
      setWebhookInfo(null)
      setBotNameError('')
    }
  }, [selectedAgent])

  if (!selectedAgent) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-400 mb-2">Nenhum agente selecionado</p>
          <p className="text-sm text-gray-300">Selecione um agente na navbar para configurar o Bot Telegram.</p>
        </div>
      </div>
    )
  }

  const validateBotName = (name: string): boolean => {
    if (!name.trim()) return true
    if (!/bot$/i.test(name.trim())) {
      setBotNameError('O nome do bot deve terminar com "bot" (ex: MeuBot_bot)')
      return false
    }
    setBotNameError('')
    return true
  }

  const handleBotNameChange = (value: string) => {
    setBotName(value)
    if (botNameError) validateBotName(value)
  }

  const handleSave = async () => {
    if (!validateBotName(botName)) return

    setSaving(true)
    try {
      const result = await AgentService.update(selectedAgent.agentId, {
        name: selectedAgent.name,
        description: selectedAgent.description,
        systemPrompt: selectedAgent.systemPrompt,
        collectName: selectedAgent.collectName,
        collectEmail: selectedAgent.collectEmail,
        collectPhone: selectedAgent.collectPhone,
        chatModel: selectedAgent.chatModel,
        telegramBotName: botName.trim() || null,
        telegramBotToken: botToken.trim() || null,
      })

      if (result.sucesso) {
        toast.success('Configurações do Bot Telegram salvas com sucesso')
        await loadAgents()
        const updated = useAgentStore.getState().agents.find((a) => a.agentId === selectedAgent.agentId)
        if (updated) useAgentStore.getState().selectAgent(updated)
      } else {
        toast.error(result.mensagem || 'Erro ao salvar configurações')
        console.error('[TelegramBotPage] handleSave — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[TelegramBotPage] handleSave — exceção:', err)
      toast.error('Erro de rede ao salvar configurações')
    } finally {
      setSaving(false)
    }
  }

  const handleSetupWebhook = async () => {
    setSettingUpWebhook(true)
    try {
      const result = await AgentService.setupTelegramWebhook(selectedAgent.agentId)
      if (result.sucesso && result.dados) {
        toast.success(`Webhook configurado com sucesso: ${result.dados.webhookUrl}`)
        setWebhookInfo(result.dados)
      } else {
        toast.error(result.mensagem || 'Erro ao configurar webhook')
        console.error('[TelegramBotPage] handleSetupWebhook — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[TelegramBotPage] handleSetupWebhook — exceção:', err)
      toast.error('Erro de rede ao configurar webhook')
    } finally {
      setSettingUpWebhook(false)
    }
  }

  const handleCheckWebhook = async () => {
    setCheckingWebhook(true)
    try {
      const result = await AgentService.getTelegramWebhookInfo(selectedAgent.agentId)
      if (result.sucesso && result.dados) {
        setWebhookInfo(result.dados)
      } else {
        toast.error(result.mensagem || 'Erro ao verificar webhook')
        console.error('[TelegramBotPage] handleCheckWebhook — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[TelegramBotPage] handleCheckWebhook — exceção:', err)
      toast.error('Erro de rede ao verificar webhook')
    } finally {
      setCheckingWebhook(false)
    }
  }

  const handleRegenerateSecret = async () => {
    if (!confirm('Tem certeza que deseja regenerar o Webhook Secret? O webhook será re-registrado automaticamente.')) {
      return
    }

    setRegenerating(true)
    try {
      const result = await AgentService.regenerateTelegramSecret(selectedAgent.agentId)
      if (result.sucesso && result.dados) {
        toast.success('Webhook Secret regenerado e webhook re-registrado com sucesso')
        setWebhookInfo(result.dados)
        await loadAgents()
        const updated = useAgentStore.getState().agents.find((a) => a.agentId === selectedAgent.agentId)
        if (updated) useAgentStore.getState().selectAgent(updated)
      } else {
        toast.error(result.mensagem || 'Erro ao regenerar secret')
        console.error('[TelegramBotPage] handleRegenerateSecret — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[TelegramBotPage] handleRegenerateSecret — exceção:', err)
      toast.error('Erro de rede ao regenerar secret')
    } finally {
      setRegenerating(false)
    }
  }

  const hasRequiredFields = botName.trim() && botToken.trim()

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-gray-900">Bot Telegram</h1>
      <p className="text-sm text-gray-500 mt-1 mb-6">Configure a integração do agente com o Telegram.</p>

      {/* Formulário de configuração */}
      <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Configuração do Bot</h2>

        <div className="space-y-4">
          {/* Nome do Bot */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Nome do Bot</label>
            <input
              type="text"
              value={botName}
              onChange={(e) => handleBotNameChange(e.target.value)}
              onBlur={() => validateBotName(botName)}
              placeholder="MeuBot_bot"
              className={`w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent ${
                botNameError ? 'border-red-300 bg-red-50' : 'border-gray-300'
              }`}
            />
            {botNameError && (
              <p className="text-xs text-red-500 mt-1">{botNameError}</p>
            )}
            <p className="text-xs text-gray-400 mt-1">O nome deve terminar com "bot" (ex: MeuBot_bot, AvaAssistBot)</p>
          </div>

          {/* Bot Token */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Bot Token</label>
            <div className="relative">
              <input
                type={showToken ? 'text' : 'password'}
                value={botToken}
                onChange={(e) => setBotToken(e.target.value)}
                placeholder="123456789:ABCdefGHIjklMNOpqrsTUVwxyz"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm font-mono focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent pr-20"
              />
              <button
                type="button"
                onClick={() => setShowToken(!showToken)}
                className="absolute right-2 top-1/2 -translate-y-1/2 px-2 py-1 text-xs text-gray-500 hover:text-gray-700 bg-gray-100 rounded"
              >
                {showToken ? 'Ocultar' : 'Mostrar'}
              </button>
            </div>
            <p className="text-xs text-gray-400 mt-1">Obtido via @BotFather no Telegram</p>
          </div>

          {/* Webhook Secret */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Webhook Secret</label>
            <div className="flex gap-2">
              <input
                type="text"
                value={selectedAgent.telegramWebhookSecret || ''}
                readOnly
                className="flex-1 px-3 py-2 border border-gray-200 rounded-lg text-sm font-mono bg-gray-50 text-gray-500"
                placeholder="Gerado automaticamente ao salvar"
              />
              <button
                type="button"
                onClick={handleRegenerateSecret}
                disabled={regenerating || !selectedAgent.telegramWebhookSecret}
                className="px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors whitespace-nowrap"
              >
                {regenerating ? 'Regenerando...' : 'Regenerar'}
              </button>
            </div>
            <p className="text-xs text-gray-400 mt-1">Usado para validar requisições do Telegram</p>
          </div>

          {/* URL do Bot */}
          {botName.trim() && /bot$/i.test(botName.trim()) && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">URL do Bot</label>
              <a
                href={`https://t.me/${botName.trim()}`}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1 text-sm text-ava-600 hover:text-ava-700 hover:underline"
              >
                https://t.me/{botName.trim()}
                <svg xmlns="http://www.w3.org/2000/svg" className="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                  <path d="M11 3a1 1 0 100 2h2.586l-6.293 6.293a1 1 0 101.414 1.414L15 6.414V9a1 1 0 102 0V4a1 1 0 00-1-1h-5z" />
                  <path d="M5 5a2 2 0 00-2 2v8a2 2 0 002 2h8a2 2 0 002-2v-3a1 1 0 10-2 0v3H5V7h3a1 1 0 000-2H5z" />
                </svg>
              </a>
            </div>
          )}
        </div>

        {/* Botão Salvar */}
        <div className="mt-6 pt-4 border-t border-gray-100">
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-5 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50 transition-colors"
          >
            {saving ? 'Salvando...' : 'Salvar'}
          </button>
        </div>
      </div>

      {/* Ações do Webhook */}
      <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Webhook</h2>

        <div className="flex flex-wrap gap-3">
          <button
            onClick={handleSetupWebhook}
            disabled={settingUpWebhook || !hasRequiredFields}
            className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            title={!hasRequiredFields ? 'Preencha o nome do bot e o token antes de configurar o webhook' : ''}
          >
            {settingUpWebhook ? 'Configurando...' : 'Configurar Webhook'}
          </button>

          <button
            onClick={handleCheckWebhook}
            disabled={checkingWebhook || !hasRequiredFields}
            className="px-4 py-2 border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            title={!hasRequiredFields ? 'Preencha o nome do bot e o token antes de verificar o webhook' : ''}
          >
            {checkingWebhook ? 'Verificando...' : 'Verificar Webhook'}
          </button>
        </div>

        {!hasRequiredFields && (
          <p className="text-xs text-gray-400 mt-2">Preencha o nome do bot e o token para habilitar as ações de webhook.</p>
        )}

        {/* Resultado da verificação do webhook */}
        {webhookInfo && (
          <div className="mt-4 p-4 bg-gray-50 rounded-lg border border-gray-200">
            <h3 className="text-sm font-medium text-gray-700 mb-2">Status do Webhook</h3>
            <div className="space-y-2 text-sm">
              <div className="flex items-center gap-2">
                <span className="text-gray-500">Status:</span>
                <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium ${
                  webhookInfo.isConfigured
                    ? 'bg-green-100 text-green-700'
                    : 'bg-gray-100 text-gray-500'
                }`}>
                  <span className={`w-1.5 h-1.5 rounded-full ${
                    webhookInfo.isConfigured ? 'bg-green-500' : 'bg-gray-400'
                  }`} />
                  {webhookInfo.isConfigured ? 'Configurado' : 'Não configurado'}
                </span>
              </div>
              {webhookInfo.webhookUrl && (
                <div className="flex items-start gap-2">
                  <span className="text-gray-500 shrink-0">URL:</span>
                  <code className="text-xs bg-gray-100 px-2 py-0.5 rounded break-all">{webhookInfo.webhookUrl}</code>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export default TelegramBotPage
