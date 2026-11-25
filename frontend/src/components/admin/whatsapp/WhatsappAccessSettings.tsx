import { useState } from 'react'
import { toast } from 'sonner'
import { useWhatsappAccessStore } from '../../../stores/useWhatsappAccessStore'
import { WHATSAPP_ACCESS_MODE, WHATSAPP_MESSAGE_MAX_LENGTH } from '../../../types/whatsappAccess'
import type { WhatsappAccessConfigInfo, WhatsappAccessMessageInfo } from '../../../types/whatsappAccess'
import Alert from '../ui/Alert'
import Button from '../ui/Button'
import { CardFooter } from '../ui/Card'
import CharCounter from '../ui/CharCounter'
import Field from '../ui/Field'
import { Textarea } from '../ui/Input'
import RadioCardGroup from '../ui/RadioCardGroup'
import { ErrorState, LoadingState } from '../ui/States'

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
    <div className="flex flex-col gap-4">
      <RadioCardGroup
        name="whatsapp-access-mode"
        legend="Modo de acesso"
        value={mode}
        onChange={setMode}
        options={[
          {
            value: WHATSAPP_ACCESS_MODE.ALL,
            title: 'Todos',
            description: 'Qualquer pessoa que escrever é atendida pela IA.',
          },
          {
            value: WHATSAPP_ACCESS_MODE.RESTRICTED,
            title: 'Somente contatos autorizados',
            description:
              'Quem não está na lista recebe apenas as frases abaixo e pode solicitar acesso. Nenhum token de IA é gasto com essas pessoas.',
          },
        ]}
      />

      {showEmptyListWarning && (
        <Alert tone="warn">Nenhum contato está autorizado; todos receberão apenas as frases de solicitação.</Alert>
      )}

      <p className="m-0 mt-1 text-[11px] font-semibold tracking-[0.08em] text-ink-3 uppercase">
        Frases enviadas a quem não está na lista
      </p>

      <div className="grid grid-cols-1 gap-x-[18px] gap-y-4 md:grid-cols-2">
        {MESSAGES.map((m) => {
          const info = config[m.key]
          const value = draft[m.key] ?? info.defaultText
          const isDefault = draft[m.key] === null
          const isEmpty = emptyFields.some((f) => f.key === m.key)
          return (
            <Field
              key={m.key}
              id={`wa-${m.key}`}
              label={m.label}
              tag={isDefault ? '(padrão)' : undefined}
              hint={isEmpty ? undefined : m.hint}
              error={isEmpty ? 'A frase não pode ficar vazia.' : undefined}
              labelAside={
                <Button
                  size="xs"
                  variant="ghost"
                  disabled={isDefault}
                  onClick={() => setDraft((d) => ({ ...d, [m.key]: null }))}
                  aria-label={`Restaurar padrão: ${m.label}`}
                >
                  Restaurar padrão
                </Button>
              }
              footer={<CharCounter value={value.length} max={WHATSAPP_MESSAGE_MAX_LENGTH} />}
            >
              <Textarea
                value={value}
                maxLength={WHATSAPP_MESSAGE_MAX_LENGTH}
                rows={2}
                onChange={(e) => setDraft((d) => ({ ...d, [m.key]: e.target.value }))}
              />
            </Field>
          )
        })}
      </div>

      <CardFooter note={`Até ${WHATSAPP_MESSAGE_MAX_LENGTH} caracteres por frase.`}>
        <Button variant="primary" onClick={handleSave} loading={saving}>
          {saving ? 'Salvando...' : 'Salvar'}
        </Button>
      </CardFooter>
    </div>
  )
}

const WhatsappAccessSettings = ({ slug }: WhatsappAccessSettingsProps) => {
  const { config, loadingConfig, loadConfig } = useWhatsappAccessStore()

  if (!config) {
    return loadingConfig ? (
      <LoadingState label="Carregando configuração de acesso…" rows={3} />
    ) : (
      <ErrorState title="Configuração indisponível." onRetry={() => loadConfig(slug)} />
    )
  }

  // Remonta o formulário quando a configuração salva muda (troca de agente ou após salvar).
  const formKey = `${slug}-${config.mode}-${config.offerMessage.text}-${config.confirmationMessage.text}-${config.waitingMessage.text}-${config.declinedMessage.text}`
  return <SettingsForm key={formKey} slug={slug} config={config} />
}

export default WhatsappAccessSettings
