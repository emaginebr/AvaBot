import { useState, useEffect } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'
import type { TelegramWebhookInfo } from '../../types/agent'
import AdminIcon from '../../components/admin/AdminIcon'
import Button from '../../components/admin/ui/Button'
import { Card, CardFooter, CardHeader, CardSection } from '../../components/admin/ui/Card'
import { CodeChip } from '../../components/admin/ui/Chip'
import Field from '../../components/admin/ui/Field'
import { Input } from '../../components/admin/ui/Input'
import KeyValueList from '../../components/admin/ui/KeyValueList'
import PageHeader from '../../components/admin/ui/PageHeader'
import PasswordInput from '../../components/admin/ui/PasswordInput'
import { NoAgentState } from '../../components/admin/ui/States'
import StatusBadge from '../../components/admin/ui/StatusBadge'
import { useConfirm } from '../../components/admin/ui/useConfirm'

const PAGE_TITLE = 'Bot Telegram'
const PAGE_DESCRIPTION = 'Configure a integração do agente com o Telegram e registre o webhook.'

const TelegramBotPage = () => {
  const { selectedAgent, loadAgents } = useAgentStore()

  const [botName, setBotName] = useState('')
  const [botToken, setBotToken] = useState('')
  const [saving, setSaving] = useState(false)
  const [settingUpWebhook, setSettingUpWebhook] = useState(false)
  const [checkingWebhook, setCheckingWebhook] = useState(false)
  const [regenerating, setRegenerating] = useState(false)
  const [webhookInfo, setWebhookInfo] = useState<TelegramWebhookInfo | null>(null)
  const [botNameError, setBotNameError] = useState('')
  const [confirm, confirmDialog] = useConfirm()

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
      <div className="flex flex-col gap-5">
        <PageHeader title={PAGE_TITLE} description={PAGE_DESCRIPTION} />
        <NoAgentState purpose="configurar o Bot Telegram" />
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
        // Esta tela não toca na credencial OpenAI: null + false preserva a chave salva.
        openAIApiKey: null,
        removeOpenAIApiKey: false,
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
    const ok = await confirm({
      title: 'Regenerar o webhook secret?',
      message: 'Tem certeza que deseja regenerar o Webhook Secret? O webhook será re-registrado automaticamente.',
      confirmLabel: 'Regenerar',
      icon: 'refresh',
    })
    if (!ok) return

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
  const botUrlVisible = botName.trim() && /bot$/i.test(botName.trim())

  const webhookBadge = !webhookInfo ? (
    <StatusBadge tone="idle" label="Webhook não verificado" />
  ) : webhookInfo.isConfigured ? (
    <StatusBadge tone="ok" label="Webhook configurado" />
  ) : (
    <StatusBadge tone="warn" label="Webhook não configurado" />
  )

  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        agentName={selectedAgent.name}
        title={PAGE_TITLE}
        description={PAGE_DESCRIPTION}
        actions={<span role="status">{webhookBadge}</span>}
      />

      <div className="grid grid-cols-1 gap-5 xl:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)]">
        <Card aria-labelledby="telegram-config-titulo">
          <CardHeader
            titleId="telegram-config-titulo"
            icon="send"
            title="Configuração do bot"
            subtitle="Dados do bot criado no @BotFather."
          />
          <div className="flex flex-col gap-4">
            <Field
              id="telegram-bot-name"
              label="Nome do bot"
              hint='O nome deve terminar com "bot" (ex.: MeuBot_bot, AvaAssistBot).'
              error={botNameError || undefined}
            >
              <Input
                type="text"
                value={botName}
                onChange={(e) => handleBotNameChange(e.target.value)}
                onBlur={() => validateBotName(botName)}
                placeholder="MeuBot_bot"
              />
            </Field>

            <Field id="telegram-bot-token" label="Token do bot" hint="Obtido via @BotFather no Telegram.">
              <PasswordInput
                mono
                value={botToken}
                onChange={(e) => setBotToken(e.target.value)}
                placeholder="123456789:ABCdefGHIjklMNOpqrsTUVwxyz"
                autoComplete="off"
                spellCheck={false}
              />
            </Field>

            <Field id="telegram-webhook-secret" label="Webhook secret" hint="Usado para validar as requisições do Telegram.">
              <div className="flex min-w-0 gap-2 max-md:flex-col">
                <Input
                  mono
                  type="text"
                  value={selectedAgent.telegramWebhookSecret || ''}
                  readOnly
                  placeholder="Gerado automaticamente ao salvar"
                  className="flex-1"
                />
                <Button
                  variant="dangerGhost"
                  icon="refresh"
                  onClick={handleRegenerateSecret}
                  loading={regenerating}
                  disabled={!selectedAgent.telegramWebhookSecret}
                >
                  {regenerating ? 'Regenerando...' : 'Regenerar'}
                </Button>
              </div>
            </Field>

            {botUrlVisible && (
              <div className="flex flex-col gap-1.5">
                <span className="text-[13px] font-medium text-ink">URL do bot</span>
                <a
                  href={`https://t.me/${botName.trim()}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 self-start rounded-md font-medium [overflow-wrap:anywhere] text-ava-700 hover:text-ava-900 hover:underline hover:underline-offset-[3px]"
                >
                  https://t.me/{botName.trim()}
                  <AdminIcon name="external" className="h-3.5 w-3.5" />
                  <span className="sr-only">(abre em nova aba)</span>
                </a>
              </div>
            )}
          </div>
          <CardFooter>
            <Button variant="primary" onClick={handleSave} loading={saving}>
              {saving ? 'Salvando...' : 'Salvar'}
            </Button>
          </CardFooter>
        </Card>

        <Card aria-labelledby="telegram-webhook-titulo">
          <CardHeader
            titleId="telegram-webhook-titulo"
            icon="link"
            title="Webhook"
            subtitle="Registra no Telegram o endereço que entrega as mensagens ao agente."
          />
          <div className="flex flex-wrap gap-2.5">
            <Button
              variant="primary"
              onClick={handleSetupWebhook}
              loading={settingUpWebhook}
              disabled={!hasRequiredFields}
              title={!hasRequiredFields ? 'Preencha o nome do bot e o token antes de configurar o webhook' : ''}
            >
              {settingUpWebhook ? 'Configurando...' : 'Configurar webhook'}
            </Button>
            <Button
              onClick={handleCheckWebhook}
              loading={checkingWebhook}
              disabled={!hasRequiredFields}
              title={!hasRequiredFields ? 'Preencha o nome do bot e o token antes de verificar o webhook' : ''}
            >
              {checkingWebhook ? 'Verificando...' : 'Verificar webhook'}
            </Button>
          </div>

          {!hasRequiredFields && (
            <p className="m-0 mt-2.5 text-[12.5px] text-ink-3">
              Preencha o nome do bot e o token para habilitar as ações de webhook.
            </p>
          )}

          {webhookInfo && (
            <CardSection>
              <h3 className="m-0 mb-2.5 text-sm font-semibold text-ink">Status do webhook</h3>
              <KeyValueList
                items={[
                  {
                    label: 'Situação',
                    value: webhookInfo.isConfigured ? (
                      <StatusBadge tone="ok" label="Configurado" />
                    ) : (
                      <StatusBadge tone="idle" label="Não configurado" />
                    ),
                  },
                  ...(webhookInfo.webhookUrl
                    ? [{ label: 'URL', value: <CodeChip>{webhookInfo.webhookUrl}</CodeChip> }]
                    : []),
                ]}
              />
            </CardSection>
          )}
        </Card>
      </div>
      {confirmDialog}
    </div>
  )
}

export default TelegramBotPage
