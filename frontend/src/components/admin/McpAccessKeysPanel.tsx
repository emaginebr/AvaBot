import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { McpAccessKeyService } from '../../Services/McpAccessKeyService'
import { useMcpAccessKeyStore } from '../../stores/useMcpAccessKeyStore'
import type { McpAccessKeyCreatedInfo, McpAccessKeyInfo } from '../../types/mcpAccessKey'
import McpKeyCreatedModal from './McpKeyCreatedModal'

const MAX_NAME = 100

const inputClass =
  'w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow'

const buttonClass =
  'px-5 py-2.5 bg-ava-600 text-white rounded-lg font-medium hover:bg-ava-700 focus:outline-none focus:ring-2 focus:ring-ava-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors'

const formatDateTime = (value: string | null) =>
  value ? new Date(value).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' }) : null

const McpAccessKeysPanel = () => {
  const keys = useMcpAccessKeyStore((state) => state.keys)
  const loading = useMcpAccessKeyStore((state) => state.loading)
  const error = useMcpAccessKeyStore((state) => state.error)
  const loadKeys = useMcpAccessKeyStore((state) => state.loadKeys)
  const createKey = useMcpAccessKeyStore((state) => state.createKey)
  const revokeKey = useMcpAccessKeyStore((state) => state.revokeKey)

  const [name, setName] = useState('')
  const [creating, setCreating] = useState(false)
  const [revokingId, setRevokingId] = useState<number | null>(null)
  // A chave em claro vive so aqui, enquanto o modal esta aberto; nunca no store.
  const [created, setCreated] = useState<McpAccessKeyCreatedInfo | null>(null)

  useEffect(() => {
    loadKeys()
  }, [loadKeys])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    const trimmed = name.trim()
    if (trimmed.length === 0) {
      toast.error('Dê um nome à chave (por exemplo, o assistente ou a máquina que vai usá-la)')
      return
    }
    if (trimmed.length > MAX_NAME) {
      toast.error(`O nome deve ter no máximo ${MAX_NAME} caracteres`)
      return
    }

    setCreating(true)
    try {
      const result = await createKey(trimmed)
      if (result.sucesso && result.dados) {
        setCreated(result.dados)
        setName('')
      } else {
        toast.error(result.mensagem || 'Não foi possível criar a chave')
      }
    } catch {
      toast.error('Erro ao conectar com o servidor')
    } finally {
      setCreating(false)
    }
  }

  const handleRevoke = async (key: McpAccessKeyInfo) => {
    if (!window.confirm(`Revogar a chave "${key.name}"? O assistente que a usa perde o acesso imediatamente.`)) return

    setRevokingId(key.mcpAccessKeyId)
    try {
      const result = await revokeKey(key.mcpAccessKeyId)
      if (result.sucesso) toast.success('Chave revogada')
      else toast.error(result.mensagem || 'Não foi possível revogar a chave')
    } catch {
      toast.error('Erro ao conectar com o servidor')
    } finally {
      setRevokingId(null)
    }
  }

  return (
    <div className="space-y-5">
      <form onSubmit={handleCreate} className="flex flex-col sm:flex-row gap-3 sm:items-end">
        <div className="flex-1">
          <label htmlFor="mcpKeyName" className="block text-sm font-medium text-gray-700 mb-1.5">
            Nome da chave
          </label>
          <input
            id="mcpKeyName"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Ex.: Claude do notebook"
            maxLength={MAX_NAME}
            className={inputClass}
          />
        </div>
        <button type="submit" disabled={creating} className={buttonClass}>
          {creating ? 'Criando...' : 'Criar chave'}
        </button>
      </form>

      {loading ? (
        <div className="flex items-center gap-2 text-sm text-gray-500">
          <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
          Carregando chaves...
        </div>
      ) : error ? (
        <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700 flex items-center justify-between gap-3">
          <span>{error}</span>
          <button type="button" onClick={() => loadKeys()} className="text-sm underline">
            Tentar de novo
          </button>
        </div>
      ) : keys.length === 0 ? (
        <p className="text-sm text-gray-500">Nenhuma chave criada.</p>
      ) : (
        <ul className="divide-y divide-gray-100 border border-gray-200 rounded-lg">
          {keys.map((key) => {
            const revoked = key.status === 0
            return (
              <li key={key.mcpAccessKeyId} className="p-4 flex flex-col sm:flex-row sm:items-center gap-3">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className={`font-medium truncate ${revoked ? 'text-gray-400 line-through' : 'text-gray-900'}`}>
                      {key.name}
                    </span>
                    {revoked && (
                      <span className="text-xs px-2 py-0.5 rounded-full bg-gray-100 text-gray-500">revogada</span>
                    )}
                  </div>
                  <div className="text-xs text-gray-500 mt-1 space-x-3">
                    <code className="text-gray-600">avabot_mcp_{key.keyPrefix}…</code>
                    <span>criada em {formatDateTime(key.createdAt)}</span>
                    <span>
                      último uso: {formatDateTime(key.lastUsedAt) ?? <em className="not-italic text-gray-400">nunca usada</em>}
                    </span>
                    {revoked && key.revokedAt && <span>revogada em {formatDateTime(key.revokedAt)}</span>}
                  </div>
                </div>
                {!revoked && (
                  <button
                    type="button"
                    onClick={() => handleRevoke(key)}
                    disabled={revokingId === key.mcpAccessKeyId}
                    className="px-3 py-1.5 text-sm border border-red-300 text-red-700 rounded-lg hover:bg-red-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    {revokingId === key.mcpAccessKeyId ? 'Revogando...' : 'Revogar'}
                  </button>
                )}
              </li>
            )
          })}
        </ul>
      )}

      {created && (
        <McpKeyCreatedModal
          created={created}
          serverUrl={McpAccessKeyService.getServerUrl()}
          onClose={() => setCreated(null)}
        />
      )}
    </div>
  )
}

export default McpAccessKeysPanel
