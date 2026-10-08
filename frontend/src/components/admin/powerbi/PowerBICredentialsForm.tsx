import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import type { PowerBIConnectionTestInfo } from '../../../types/powerbi'

interface PowerBICredentialsFormProps {
  slug: string
}

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
      toast.error('Informe o Tenant ID e o Client ID')
      return
    }
    if (!isConfigured && !clientSecret.trim()) {
      toast.error('Informe o Client Secret para a primeira configuração')
      return
    }

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
    return (
      <div className="flex items-center gap-2 text-gray-500">
        <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
        Carregando credenciais...
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900">Credenciais do aplicativo</h2>
        <p className="text-sm text-gray-500 mt-0.5 mb-6">
          Service principal do Entra ID usado para consultar o Power BI em nome deste agente.
        </p>

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Tenant ID</label>
            <input
              type="text"
              value={tenantId}
              onChange={(e) => setTenantId(e.target.value)}
              placeholder="d375fd58-0000-0000-0000-000000000000"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm font-mono focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent"
            />
            <p className="text-xs text-gray-400 mt-1">Directory (tenant) ID do aplicativo registrado</p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Client ID</label>
            <input
              type="text"
              value={clientId}
              onChange={(e) => setClientId(e.target.value)}
              placeholder="e686fb17-0000-0000-0000-000000000000"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm font-mono focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent"
            />
            <p className="text-xs text-gray-400 mt-1">Application (client) ID</p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Client Secret</label>
            <input
              type="password"
              value={clientSecret}
              onChange={(e) => setClientSecret(e.target.value)}
              placeholder={isConfigured ? (maskedSecret || '••••') : 'segredo do aplicativo'}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm font-mono focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent"
            />
            <p className="text-xs text-gray-400 mt-1">
              {isConfigured
                ? 'Deixe em branco para manter o segredo atual.'
                : 'O segredo é armazenado criptografado e nunca exibido novamente.'}
            </p>
          </div>
        </div>

        <div className="mt-6 pt-4 border-t border-gray-100 flex flex-wrap gap-3">
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-5 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {saving ? 'Salvando...' : 'Salvar'}
          </button>
          <button
            onClick={handleTest}
            disabled={testing || !isConfigured}
            className="px-5 py-2 border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            title={!isConfigured ? 'Salve as credenciais antes de testar' : ''}
          >
            {testing ? 'Testando...' : 'Testar conexão'}
          </button>
        </div>
      </div>

      {lastTestAt && !testResult && (
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Último teste</h2>
          <div className="flex items-center gap-2 text-sm">
            <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium ${
              lastTestSuccess
                ? 'bg-green-100 text-green-700'
                : 'bg-red-100 text-red-700'
            }`}>
              <span className={`w-1.5 h-1.5 rounded-full ${lastTestSuccess ? 'bg-green-500' : 'bg-red-500'}`} />
              {lastTestSuccess ? 'Sucesso' : 'Falhou'}
            </span>
            <span className="text-xs text-gray-400">{new Date(lastTestAt).toLocaleString('pt-BR')}</span>
          </div>
          {lastTestMessage && (
            <p className="text-sm text-gray-500 mt-2 whitespace-pre-wrap">{lastTestMessage}</p>
          )}
        </div>
      )}

      {testResult && (
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Resultado do teste</h2>
          <div className="space-y-2">
            {testResult.steps.map((step) => (
              <div key={step.step} className="flex items-start gap-2 text-sm">
                <span className={step.success ? 'text-green-600 shrink-0' : 'text-red-600 shrink-0'}>
                  {step.success ? '✓' : '✗'}
                </span>
                <div className="min-w-0">
                  <p className="font-medium text-gray-700 break-words">{step.step}</p>
                  {step.message && (
                    <p className="text-gray-500 text-xs mt-0.5 whitespace-pre-wrap">{step.message}</p>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

export default PowerBICredentialsForm
