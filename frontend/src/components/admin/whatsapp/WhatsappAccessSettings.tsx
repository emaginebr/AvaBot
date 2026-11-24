import { useState } from 'react'
import { toast } from 'sonner'
import { useWhatsappAccessStore } from '../../../stores/useWhatsappAccessStore'
import { WHATSAPP_ACCESS_MODE, WHATSAPP_MESSAGE_MAX_LENGTH } from '../../../types/whatsappAccess'
import type { WhatsappAccessConfigInfo, WhatsappAccessMessageInfo } from '../../../types/whatsappAccess'

interface WhatsappAccessSettingsProps {
  slug: string
}

type MessageKey = 'offerMessage' | 'confirmationMessage' | 'waitingMessage' | 'declinedMessage'

const MESSAGES: { key: MessageKey; label: string; hint: string }[] = [
  { key: 'offerMessage', label: 'Oferta de acesso', hint: 'Primeira mensagem para quem não está na lista. Peça uma resposta "sim" ou "não".' },
  { key: 'confirmationMessage', label: 'Confirmação', hint: 'Enviada quando a pessoa responde "sim".' },
  { key: 'waitingMessage', label: 'Aguardando aprovação', hint: 'Enviada enquanto o pedido está pendente (no máximo 1 vez por hora).' },
  { key: 'declinedMessage', label: 'Recusa', hint: 'Enviada quando a pessoa responde "não".' },
]

/** Valor editado: null = usar o padrão. */
type Draft = Record<MessageKey, string | null>

const toDraft = (config: WhatsappAccessConfigInfo): Draft => {
  const pick = (m: WhatsappAccessMessageInfo) => (m.isDefault ? null : m.text)
  return {
    offerMessage: pick(config.offerMessage),
    confirmationMessage: pick(config.confirmationMessage),
    waitingMessage: pick(config.waitingMessage),
    declinedMessage: pick(config.declinedMessage),
  }
}

interface SettingsFormProps {
  slug: string
  config: WhatsappAccessConfigInfo
}

const SettingsForm = ({ slug, config }: SettingsFormProps) => {
  const { saveConfig } = useWhatsappAccessStore()
  const [mode, setMode] = useState(config.mode)
  const [draft, setDraft] = useState<Draft>(() => toDraft(config))
  const [saving, setSaving] = useState(false)

  const emptyFields = MESSAGES.filter((m) => draft[m.key] !== null && draft[m.key]!.trim() === '')
  const showEmptyListWarning = mode === WHATSAPP_ACCESS_MODE.RESTRICTED && config.contactCount === 0

  const handleSave = async () => {
    if (emptyFields.length) {
      toast.error('Preencha as frases ou use "Restaurar padrão".')
      return
    }
    setSaving(true)
    const result = await saveConfig(slug, { mode, ...draft })
    setSaving(false)
    if (result.sucesso) toast.success('Controle de acesso salvo')
    else toast.error(result.mensagem || 'Não foi possível salvar')
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
      <h2 className="text-lg font-semibold text-gray-900">Controle de acesso</h2>
      <p className="text-sm text-gray-500 mt-0.5 mb-4">Defina quem pode usar o agente pelo WhatsApp.</p>

      <fieldset className="space-y-2 mb-4">
        <legend className="sr-only">Modo de acesso</legend>
        <label className="flex items-start gap-2 cursor-pointer">
          <input
            type="radio"
            name="whatsapp-access-mode"
            checked={mode === WHATSAPP_ACCESS_MODE.ALL}
            onChange={() => setMode(WHATSAPP_ACCESS_MODE.ALL)}
            className="mt-1"
          />
          <span>
            <span className="text-sm font-medium text-gray-900">Todos</span>
            <span className="block text-xs text-gray-500">Qualquer pessoa que escrever é atendida pela IA.</span>
          </span>
        </label>
        <label className="flex items-start gap-2 cursor-pointer">
          <input
            type="radio"
            name="whatsapp-access-mode"
            checked={mode === WHATSAPP_ACCESS_MODE.RESTRICTED}
            onChange={() => setMode(WHATSAPP_ACCESS_MODE.RESTRICTED)}
            className="mt-1"
          />
          <span>
            <span className="text-sm font-medium text-gray-900">Somente contatos autorizados</span>
            <span className="block text-xs text-gray-500">
              Quem não está na lista recebe apenas as frases abaixo e pode solicitar acesso. Nenhum token de IA é gasto com essas pessoas.
            </span>
          </span>
        </label>
      </fieldset>

      {showEmptyListWarning && (
        <div className="mb-4 px-3 py-2 rounded-lg bg-yellow-50 border border-yellow-200 text-sm text-yellow-800">
          Nenhum contato está autorizado; todos receberão apenas as frases de solicitação.
        </div>
      )}

      <div className="space-y-4">
        {MESSAGES.map((m) => {
          const info = config[m.key]
          const value = draft[m.key] ?? info.defaultText
          const isDefault = draft[m.key] === null
          const isEmpty = emptyFields.some((f) => f.key === m.key)
          return (
            <div key={m.key}>
              <div className="flex items-center justify-between">
                <label htmlFor={`wa-${m.key}`} className="text-sm font-medium text-gray-700">
                  {m.label}
                  {isDefault && <span className="ml-2 text-xs font-normal text-gray-400">(padrão)</span>}
                </label>
                <button
                  type="button"
                  disabled={isDefault}
                  onClick={() => setDraft((d) => ({ ...d, [m.key]: null }))}
                  className="text-xs text-ava-600 hover:underline disabled:text-gray-300 disabled:no-underline"
                >
                  Restaurar padrão
                </button>
              </div>
              <textarea
                id={`wa-${m.key}`}
                value={value}
                maxLength={WHATSAPP_MESSAGE_MAX_LENGTH}
                rows={2}
                onChange={(e) => setDraft((d) => ({ ...d, [m.key]: e.target.value }))}
                className={`mt-1 w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ava-500 ${
                  isEmpty ? 'border-red-300' : 'border-gray-300'
                }`}
              />
              <div className="flex justify-between text-xs">
                <span className={isEmpty ? 'text-red-600' : 'text-gray-400'}>{isEmpty ? 'A frase não pode ficar vazia.' : m.hint}</span>
                <span className="text-gray-400 tabular-nums">{value.length}/{WHATSAPP_MESSAGE_MAX_LENGTH}</span>
              </div>
            </div>
          )
        })}
      </div>

      <div className="mt-5">
        <button
          onClick={handleSave}
          disabled={saving}
          className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50"
        >
          {saving ? 'Salvando...' : 'Salvar'}
        </button>
      </div>
    </div>
  )
}

const WhatsappAccessSettings = ({ slug }: WhatsappAccessSettingsProps) => {
  const { config, loadingConfig } = useWhatsappAccessStore()

  if (!config) {
    return (
      <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900">Controle de acesso</h2>
        <p className="text-sm text-gray-400 mt-2">{loadingConfig ? 'Carregando...' : 'Configuração indisponível.'}</p>
      </div>
    )
  }

  // Remonta o formulário quando a configuração salva muda (troca de agente ou após salvar).
  const formKey = `${slug}-${config.mode}-${config.offerMessage.text}-${config.confirmationMessage.text}-${config.waitingMessage.text}-${config.declinedMessage.text}`
  return <SettingsForm key={formKey} slug={slug} config={config} />
}

export default WhatsappAccessSettings
