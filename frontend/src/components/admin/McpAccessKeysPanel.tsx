import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { McpAccessKeyService } from '../../Services/McpAccessKeyService'
import { useMcpAccessKeyStore } from '../../stores/useMcpAccessKeyStore'
import type { McpAccessKeyCreatedInfo, McpAccessKeyInfo } from '../../types/mcpAccessKey'
import AdminIcon from './AdminIcon'
import McpKeyCreatedModal from './McpKeyCreatedModal'
import Alert from './ui/Alert'
import Button from './ui/Button'
import { CodeChip } from './ui/Chip'
import DataTable from './ui/DataTable'
import type { DataColumn } from './ui/DataTable'
import Field from './ui/Field'
import { Input } from './ui/Input'
import { EmptyState, ErrorState, LoadingState } from './ui/States'
import StatusBadge from './ui/StatusBadge'
import { useConfirm } from './ui/useConfirm'

const MAX_NAME = 100

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
  const [nameError, setNameError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [revokingId, setRevokingId] = useState<number | null>(null)
  // A chave em claro vive só aqui, enquanto o modal está aberto; nunca no store.
  const [created, setCreated] = useState<McpAccessKeyCreatedInfo | null>(null)
  const [confirm, confirmDialog] = useConfirm()

  useEffect(() => {
    loadKeys()
  }, [loadKeys])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    const trimmed = name.trim()
    if (trimmed.length === 0) {
      setNameError('Dê um nome à chave (por exemplo, o assistente ou a máquina que vai usá-la).')
      toast.error('Dê um nome à chave (por exemplo, o assistente ou a máquina que vai usá-la)')
      return
    }
    if (trimmed.length > MAX_NAME) {
      setNameError(`O nome deve ter no máximo ${MAX_NAME} caracteres.`)
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
    const ok = await confirm({
      title: 'Revogar chave?',
      message: `Revogar a chave "${key.name}"? O assistente que a usa perde o acesso imediatamente.`,
      confirmLabel: 'Revogar',
    })
    if (!ok) return

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

  const columns: DataColumn<McpAccessKeyInfo>[] = [
    {
      key: 'name',
      header: 'Chave',
      kind: 'main',
      cell: (key) => {
        const revoked = key.status === 0
        return (
          <>
            <span className={`block font-medium [overflow-wrap:anywhere] ${revoked ? 'text-ink-3 line-through' : 'text-ink'}`}>
              {key.name}
            </span>
            <CodeChip className="mt-1 inline-block">avabot_mcp_{key.keyPrefix}…</CodeChip>
          </>
        )
      },
    },
    {
      key: 'created',
      header: 'Criada em',
      nowrap: true,
      cell: (key) => <span className="font-ui-mono tabular-nums">{formatDateTime(key.createdAt)}</span>,
    },
    {
      key: 'lastUsed',
      header: 'Último uso',
      nowrap: true,
      cell: (key) =>
        formatDateTime(key.lastUsedAt) ? (
          <span className="font-ui-mono tabular-nums">{formatDateTime(key.lastUsedAt)}</span>
        ) : (
          <span className="text-ink-3">nunca usada</span>
        ),
    },
    {
      key: 'status',
      header: 'Situação',
      cell: (key) =>
        key.status === 0 ? (
          <StatusBadge
            tone="idle"
            icon="x"
            label={key.revokedAt ? `Revogada em ${formatDateTime(key.revokedAt)}` : 'Revogada'}
          />
        ) : (
          <StatusBadge tone="ok" label="Ativa" />
        ),
    },
    {
      key: 'actions',
      header: 'Ações',
      hideHeader: true,
      kind: 'actions',
      cell: (key) =>
        key.status !== 0 && (
          <Button
            size="sm"
            variant="dangerGhost"
            onClick={() => handleRevoke(key)}
            loading={revokingId === key.mcpAccessKeyId}
            aria-label={`Revogar a chave ${key.name}`}
          >
            {revokingId === key.mcpAccessKeyId ? 'Revogando...' : 'Revogar'}
          </Button>
        ),
    },
  ]

  return (
    <div className="flex flex-col gap-[18px]">
      <Alert tone="info" icon="shield">
        O assistente nunca exclui agentes nem lê ou grava credenciais; isso continua sendo feito só aqui no painel. Passo a
        passo em{' '}
        <a
          href="https://github.com/emaginebr/AvaBot/blob/main/docs/MCP_SETUP.md"
          target="_blank"
          rel="noreferrer"
          className="inline-flex items-center gap-1 font-medium text-ava-700 underline underline-offset-2 hover:text-ava-900"
        >
          docs/MCP_SETUP.md
          <AdminIcon name="external" className="h-3.5 w-3.5" />
          <span className="sr-only">(abre em nova aba)</span>
        </a>
        .
      </Alert>

      <form onSubmit={handleCreate} noValidate className="flex flex-col gap-3 md:flex-row md:items-start">
        <Field id="mcpKeyName" label="Nome da chave" error={nameError} className="flex-1">
          <Input
            type="text"
            value={name}
            onChange={(e) => {
              setName(e.target.value)
              setNameError(null)
            }}
            placeholder="Ex.: Claude do notebook"
            maxLength={MAX_NAME}
          />
        </Field>
        <Button type="submit" variant="primary" icon="plus" loading={creating} className="md:mt-[25px]">
          {creating ? 'Criando...' : 'Criar chave'}
        </Button>
      </form>

      {loading ? (
        <LoadingState label="Carregando chaves…" rows={3} />
      ) : error ? (
        <ErrorState title="Não foi possível carregar as chaves." description={error} onRetry={() => loadKeys()} />
      ) : keys.length === 0 ? (
        <EmptyState
          icon="key"
          title="Nenhuma chave criada"
          description="Crie uma chave acima e cole a configuração no seu assistente."
        />
      ) : (
        <DataTable columns={columns} rows={keys} rowKey={(key) => key.mcpAccessKeyId} caption="Chaves de acesso MCP" />
      )}

      {created && (
        <McpKeyCreatedModal
          created={created}
          serverUrl={McpAccessKeyService.getServerUrl()}
          onClose={() => setCreated(null)}
        />
      )}
      {confirmDialog}
    </div>
  )
}

export default McpAccessKeysPanel
