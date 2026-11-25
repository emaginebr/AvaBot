import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import type { PowerBIConnectionTestInfo } from '../../../types/powerbi'
import AdminIcon from '../AdminIcon'
import Button from '../ui/Button'
import Field from '../ui/Field'
import { Input } from '../ui/Input'
import PasswordInput from '../ui/PasswordInput'
import { LoadingState } from '../ui/States'
import StatusBadge from '../ui/StatusBadge'

interface PowerBICredentialsFormProps {
  slug: string
}

type CredentialErrors = Partial<Record<'tenant' | 'client' | 'secret', string>>

const PowerBICredentialsForm = ({ slug }: PowerBICredentialsFormProps) => {
  const [tenantId, setTenantId] = useState('')
  const [clientId, setClientId] = useState('')
  const [clientSecret, setClientSecret] = useState('')
  const [maskedSecret, setMaskedSecret] = useState<string | null>(null)
  const [isConfigured, setIsConfigured] = useState(false)
  const [lastTestAt, setLastTestAt] = useState<string | null>(null)
  const [lastTestSuccess, setLastTestSuccess] = useState<boolean | null>(null)
  const [lastTestMessage, setLastTestMessage] = useState<string | null>(null)
  const [testResult, setTestResult] = useState<PowerBIConnectionTestInfo | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [testing, setTesting] = useState(false)
  const [errors, setErrors] = useState<CredentialErrors>({})

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      try {
        const result = await PowerBIService.getConfig(slug)
        if (result.sucesso && result.dados) {
          const config = result.dados
          setTenantId(config.tenantId || '')
          setClientId(config.clientId || '')
          setClientSecret('')
          setMaskedSecret(config.clientSecretMasked)
          setIsConfigured(config.isConfigured)
          setLastTestAt(config.lastTestAt)
          setLastTestSuccess(config.lastTestSuccess)
          setLastTestMessage(config.lastTestMessage)
        } else {
          toast.error(result.mensagem || 'Erro ao carregar as credenciais')
          console.error('[PowerBICredentialsForm] loadConfig — erro:', result.mensagem, result.erros)
        }
      } catch (err) {
        console.error('[PowerBICredentialsForm] loadConfig — exceção:', err)
        toast.error('Erro de rede ao carregar as credenciais')
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [slug])

  const handleSave = async () => {
    if (!tenantId.trim() || !clientId.trim()) {
      setErrors({
        tenant: tenantId.trim() ? undefined : 'Informe o Tenant ID.',
        client: clientId.trim() ? undefined : 'Informe o Client ID.',
      })
      toast.error('Informe o Tenant ID e o Client ID')
      return
    }
    if (!isConfigured && !clientSecret.trim()) {
      setErrors({ secret: 'Informe o Client Secret para a primeira configuração.' })
      toast.error('Informe o Client Secret para a primeira configuração')
      return
    }
    setErrors({})

    setSaving(true)
    try {
      const result = await PowerBIService.saveConfig(slug, {
        tenantId: tenantId.trim(),
        clientId: clientId.trim(),
        clientSecret: clientSecret.trim() || null,
      })

      if (result.sucesso && result.dados) {
        toast.success('Credenciais do Power BI salvas com sucesso')
        setClientSecret('')
        setMaskedSecret(result.dados.clientSecretMasked)
        setIsConfigured(result.dados.isConfigured)
        setLastTestAt(result.dados.lastTestAt)
        setLastTestSuccess(result.dados.lastTestSuccess)
        setLastTestMessage(result.dados.lastTestMessage)
        setTestResult(null)
      } else {
        toast.error(result.mensagem || 'Erro ao salvar as credenciais')
        console.error('[PowerBICredentialsForm] handleSave — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[PowerBICredentialsForm] handleSave — exceção:', err)
      toast.error('Erro de rede ao salvar as credenciais')
    } finally {
      setSaving(false)
    }
  }

  const handleTest = async () => {
    setTesting(true)
    try {
      const result = await PowerBIService.testConnection(slug)
      if (result.sucesso && result.dados) {
        setTestResult(result.dados)
        setLastTestSuccess(result.dados.success)
        if (result.dados.success) {
          toast.success('Conexão com o Power BI funcionando')
        } else {
          toast.error('A conexão falhou em pelo menos uma etapa')
        }
      } else {
        toast.error(result.mensagem || 'Erro ao testar a conexão')
        console.error('[PowerBICredentialsForm] handleTest — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[PowerBICredentialsForm] handleTest — exceção:', err)
      toast.error('Erro de rede ao testar a conexão')
    } finally {
      setTesting(false)
    }
  }

  if (loading) {
    return <LoadingState label="Carregando credenciais..." rows={4} />
  }

  const hasSidePanel = Boolean((lastTestAt && !testResult) || testResult)

  return (
    <div className={hasSidePanel ? 'grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)]' : ''}>
      <div className="flex min-w-0 flex-col gap-4">
        <div>
          <h2 className="font-brand text-[17px] leading-[1.3] font-semibold tracking-[-0.01em] text-ink">Credenciais do aplicativo</h2>
          <p className="m-0 mt-[3px] text-[12.5px] text-ink-3">
            Service principal do Entra ID usado para consultar o Power BI em nome deste agente.
          </p>
        </div>

        <div className="grid grid-cols-1 gap-x-[18px] gap-y-4 md:grid-cols-2">
          <Field
            id="powerbi-tenant"
            label="Tenant ID"
            required
            hint="Directory (tenant) ID do aplicativo registrado."
            error={errors.tenant}
          >
            <Input
              mono
              type="text"
              value={tenantId}
              onChange={(e) => {
                setTenantId(e.target.value)
                setErrors((prev) => ({ ...prev, tenant: undefined }))
              }}
              placeholder="d375fd58-0000-0000-0000-000000000000"
              spellCheck={false}
            />
          </Field>

          <Field id="powerbi-client" label="Client ID" required hint="Application (client) ID." error={errors.client}>
            <Input
              mono
              type="text"
              value={clientId}
              onChange={(e) => {
                setClientId(e.target.value)
                setErrors((prev) => ({ ...prev, client: undefined }))
              }}
              placeholder="e686fb17-0000-0000-0000-000000000000"
              spellCheck={false}
            />
          </Field>

          <Field
            id="powerbi-secret"
            label="Client secret"
            required={!isConfigured}
            className="md:col-span-2"
            hint={
              isConfigured
                ? 'Deixe em branco para manter o segredo atual.'
                : 'O segredo é armazenado criptografado e nunca exibido novamente.'
            }
            error={errors.secret}
          >
            <PasswordInput
              mono
              value={clientSecret}
              onChange={(e) => {
                setClientSecret(e.target.value)
                setErrors((prev) => ({ ...prev, secret: undefined }))
              }}
              placeholder={isConfigured ? (maskedSecret || '••••') : 'segredo do aplicativo'}
              autoComplete="new-password"
              spellCheck={false}
              revealable={clientSecret !== ''}
            />
          </Field>
        </div>

        <div className="flex flex-wrap gap-2.5">
          <Button variant="primary" onClick={handleSave} loading={saving}>
            {saving ? 'Salvando...' : 'Salvar'}
          </Button>
          <Button
            icon="refresh"
            onClick={handleTest}
            loading={testing}
            disabled={!isConfigured}
            title={!isConfigured ? 'Salve as credenciais antes de testar' : ''}
          >
            {testing ? 'Testando...' : 'Testar conexão'}
          </Button>
        </div>
      </div>

      {lastTestAt && !testResult && (
        <div className="self-start rounded-2xl border border-gray-200 bg-gray-50 p-5" aria-live="polite">
          <h3 className="m-0 mb-2 text-sm font-semibold text-ink">Último teste</h3>
          <div className="flex flex-wrap items-center gap-2.5">
            {lastTestSuccess ? <StatusBadge tone="ok" label="Sucesso" /> : <StatusBadge tone="err" label="Falhou" />}
            <span className="font-ui-mono text-[12.5px] tabular-nums text-ink-3">
              {new Date(lastTestAt).toLocaleString('pt-BR')}
            </span>
          </div>
          {lastTestMessage && (
            <p className="m-0 mt-2 text-[12.5px] whitespace-pre-wrap [overflow-wrap:anywhere] text-ink-2">{lastTestMessage}</p>
          )}
        </div>
      )}

      {testResult && (
        <div className="self-start rounded-2xl border border-gray-200 bg-gray-50 p-5" aria-live="polite">
          <h3 className="m-0 mb-3 text-sm font-semibold text-ink">Resultado do teste</h3>
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {testResult.steps.map((step) => (
              <li key={step.step} className="flex items-start gap-2.5 text-[13px]">
                <span
                  className={`grid h-[22px] w-[22px] flex-none place-items-center rounded-full ${
                    step.success ? 'bg-ok-bg text-ok' : 'bg-err-bg text-err'
                  }`}
                >
                  <AdminIcon name={step.success ? 'check' : 'x'} className="h-3 w-3" />
                  <span className="sr-only">{step.success ? 'Sucesso:' : 'Falha:'}</span>
                </span>
                <div className="min-w-0">
                  <p className="m-0 font-medium [overflow-wrap:anywhere] text-ink">{step.step}</p>
                  {step.message && (
                    <p className="m-0 mt-0.5 text-xs whitespace-pre-wrap [overflow-wrap:anywhere] text-ink-3">{step.message}</p>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}

export default PowerBICredentialsForm
