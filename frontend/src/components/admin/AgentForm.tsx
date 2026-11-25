import { useState } from 'react'
import OpenAIConnectionDiagnosticModal from './OpenAIConnectionDiagnosticModal'
import type { AgentInsertInfo } from '../../types/agent'
import Button, { ButtonLink } from './ui/Button'
import { Card, CardFooter, CardHeader } from './ui/Card'
import Checkbox from './ui/Checkbox'
import Field from './ui/Field'
import { Input, Select, Textarea } from './ui/Input'
import PasswordInput from './ui/PasswordInput'
import StatusBadge from './ui/StatusBadge'

interface AgentFormProps {
  initialData?: AgentInsertInfo
  onSubmit: (data: AgentInsertInfo) => Promise<void>
  loading: boolean
  agentId?: number
  hasSavedOpenAIApiKey?: boolean
}

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

const EMPTY_AGENT: AgentInsertInfo = {
  name: '',
  description: null,
  systemPrompt: '',
  collectName: false,
  collectEmail: false,
  collectPhone: false,
  chatModel: 'gpt-4o',
  telegramBotName: null,
  telegramBotToken: null,
  openAIApiKey: null,
  removeOpenAIApiKey: false,
}

/**
 * Formulário de criação/edição. Os dados iniciais valem na montagem: a página remonta o
 * formulário (key) quando o agente termina de carregar, em vez de copiar a prop num effect.
 */
const AgentForm = ({ initialData, onSubmit, loading, agentId, hasSavedOpenAIApiKey = false }: AgentFormProps) => {
  const [formData, setFormData] = useState<AgentInsertInfo>(() => initialData ?? EMPTY_AGENT)
  const [diagnosing, setDiagnosing] = useState(false)

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

  const keySaved = hasSavedOpenAIApiKey && !formData.removeOpenAIApiKey

  const keyHint = formData.removeOpenAIApiKey ? (
    <span className="text-err">
      A chave salva será removida ao salvar. Sem chave, este agente não usa chat, busca semântica nem ingestão de arquivos.
    </span>
  ) : hasSavedOpenAIApiKey ? (
    'Uma chave está salva e não é exibida aqui. Deixe o campo em branco para mantê-la.'
  ) : (
    'Cada agente usa a sua própria chave. Sem chave, os recursos de IA ficam indisponíveis para este agente.'
  )

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5">
      <Card aria-labelledby="agente-identidade-titulo">
        <CardHeader
          titleId="agente-identidade-titulo"
          icon="bot"
          title="Identidade e comportamento"
          subtitle="Como o agente se apresenta e como deve responder."
        />
        <div className="grid grid-cols-1 gap-x-[18px] gap-y-4 md:grid-cols-2">
          <Field id="name" label="Nome do agente" required>
            <Input
              type="text"
              name="name"
              value={formData.name}
              onChange={handleChange}
              required
              placeholder="Ex.: Bia, Assistente de Vendas"
            />
          </Field>

          <Field id="description" label="Descrição" tag="opcional">
            <Textarea
              name="description"
              value={formData.description ?? ''}
              onChange={handleChange}
              rows={2}
              placeholder="Breve descrição do agente"
            />
          </Field>

          <Field
            id="systemPrompt"
            label="Prompt do sistema"
            required
            hint="Instruções que definem o comportamento e a personalidade do agente."
            className="md:col-span-2"
          >
            <Textarea
              name="systemPrompt"
              value={formData.systemPrompt}
              onChange={handleChange}
              required
              rows={8}
              mono
              placeholder="Você é um assistente especializado em..."
            />
          </Field>
        </div>
      </Card>

      <Card aria-labelledby="agente-modelo-titulo">
        <CardHeader
          titleId="agente-modelo-titulo"
          icon="spark"
          title="Modelo de IA"
          subtitle="Modelo e credencial OpenAI usados por este agente."
        />
        <div className="grid grid-cols-1 gap-x-[18px] gap-y-4 md:grid-cols-2">
          <Field id="chatModel" label="Modelo" required hint="Modelo utilizado para gerar as respostas do agente.">
            <Select
              name="chatModel"
              value={formData.chatModel}
              onChange={(e) => setFormData((prev) => ({ ...prev, chatModel: e.target.value }))}
            >
              {chatModels.map((model) => (
                <option key={model.value} value={model.value}>
                  {model.label}
                </option>
              ))}
            </Select>
          </Field>

          <Field
            id="openAIApiKey"
            label="Chave OpenAI"
            labelAside={keySaved ? <StatusBadge tone="ok" icon="lock" label="Chave salva" /> : undefined}
            hint={keyHint}
            footer={
              <div className="mt-1 flex flex-wrap items-center gap-2.5">
                {hasSavedOpenAIApiKey && (
                  <Button
                    size="sm"
                    variant={formData.removeOpenAIApiKey ? 'secondary' : 'dangerGhost'}
                    aria-pressed={formData.removeOpenAIApiKey}
                    onClick={() => setFormData((prev) => ({ ...prev, removeOpenAIApiKey: !prev.removeOpenAIApiKey }))}
                  >
                    {formData.removeOpenAIApiKey ? 'Cancelar remoção' : 'Remover chave salva'}
                  </Button>
                )}
                <Button
                  size="sm"
                  icon="shield"
                  onClick={() => setDiagnosing(true)}
                  disabled={!agentId}
                  title={!agentId ? 'Salve o agente antes de diagnosticar' : ''}
                >
                  Diagnóstico
                </Button>
              </div>
            }
          >
            <PasswordInput
              name="openAIApiKey"
              mono
              value={formData.openAIApiKey ?? ''}
              onChange={handleChange}
              autoComplete="new-password"
              spellCheck={false}
              revealable={(formData.openAIApiKey ?? '') !== ''}
              placeholder={keySaved ? '••••••••••••' : 'sk-...'}
            />
          </Field>
        </div>
      </Card>

      <Card aria-labelledby="agente-coleta-titulo">
        <CardHeader
          titleId="agente-coleta-titulo"
          icon="user"
          title="Coleta de dados do visitante"
          subtitle="Selecione quais informações o agente deve solicitar antes de iniciar a conversa."
        />
        <fieldset className="m-0 min-w-0 border-0 p-0">
          <legend className="sr-only">Dados solicitados ao visitante</legend>
          <div className="flex flex-wrap gap-2.5">
            <Checkbox name="collectName" checked={formData.collectName} onChange={handleChange} label="Solicitar nome" />
            <Checkbox name="collectEmail" checked={formData.collectEmail} onChange={handleChange} label="Solicitar e-mail" />
            <Checkbox name="collectPhone" checked={formData.collectPhone} onChange={handleChange} label="Solicitar telefone" />
          </div>
        </fieldset>
        <CardFooter
          note={
            <>
              Campos com <span className="text-err">*</span> são obrigatórios.
            </>
          }
        >
          <ButtonLink to="/admin/agents">Cancelar</ButtonLink>
          <Button type="submit" variant="primary" loading={loading}>
            {loading ? 'Salvando...' : 'Salvar'}
          </Button>
        </CardFooter>
      </Card>

      {diagnosing && agentId && (
        <OpenAIConnectionDiagnosticModal
          agentId={agentId}
          apiKey={formData.openAIApiKey}
          hasSavedKey={hasSavedOpenAIApiKey && !formData.removeOpenAIApiKey}
          onClose={() => setDiagnosing(false)}
        />
      )}
    </form>
  )
}

export default AgentForm
