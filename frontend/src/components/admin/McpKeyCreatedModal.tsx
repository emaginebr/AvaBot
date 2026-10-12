import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import type { McpAccessKeyCreatedInfo } from '../../types/mcpAccessKey'

interface McpKeyCreatedModalProps {
  created: McpAccessKeyCreatedInfo
  serverUrl: string
  onClose: () => void
}

type ClientTab = 'claude-code' | 'claude' | 'cursor' | 'vscode'

const TABS: { id: ClientTab; label: string }[] = [
  { id: 'claude-code', label: 'Claude Code' },
  { id: 'claude', label: 'Claude Desktop / claude.ai' },
  { id: 'cursor', label: 'Cursor' },
  { id: 'vscode', label: 'VS Code' },
]

const FOCUSABLE = 'button:not(:disabled), input:not(:disabled), select, textarea, [href]'

const snippetFor = (tab: ClientTab, serverUrl: string, key: string): string => {
  switch (tab) {
    case 'claude-code':
      return `claude mcp add --transport http avabot ${serverUrl} --header "Authorization: Bearer ${key}"`
    case 'claude':
      return [
        'Em Configurações > Conectores > Adicionar conector personalizado:',
        `  Nome: AvaBot`,
        `  URL do servidor: ${serverUrl}`,
        `  Cabeçalho: Authorization: Bearer ${key}`,
      ].join('\n')
    case 'cursor':
      return JSON.stringify(
        { mcpServers: { avabot: { url: serverUrl, headers: { Authorization: `Bearer ${key}` } } } },
        null,
        2,
      )
    case 'vscode':
      return JSON.stringify(
        { servers: { avabot: { type: 'http', url: serverUrl, headers: { Authorization: `Bearer ${key}` } } } },
        null,
        2,
      )
  }
}

const fileHint: Record<ClientTab, string> = {
  'claude-code': 'Rode no terminal, na pasta do projeto ou com --scope user.',
  claude: 'Conector remoto; o assistente pede confirmação na primeira conexão.',
  cursor: 'Arquivo .cursor/mcp.json (projeto) ou ~/.cursor/mcp.json (global).',
  vscode: 'Arquivo .vscode/mcp.json do workspace.',
}

const McpKeyCreatedModal = ({ created, serverUrl, onClose }: McpKeyCreatedModalProps) => {
  const [tab, setTab] = useState<ClientTab>('claude-code')
  const panelRef = useRef<HTMLDivElement>(null)
  const openerRef = useRef<Element | null>(null)

  useEffect(() => {
    openerRef.current = document.activeElement
    panelRef.current?.focus()

    return () => {
      const opener = openerRef.current as HTMLElement | null
      opener?.focus?.()
    }
  }, [])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
        return
      }

      if (event.key !== 'Tab' || !panelRef.current) return

      const focusable = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE))
      if (focusable.length === 0) return

      const first = focusable[0]
      const last = focusable[focusable.length - 1]

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  const copy = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(`${label} copiado`)
    } catch {
      toast.error('Não foi possível copiar. Selecione o texto e copie manualmente.')
    }
  }

  const snippet = snippetFor(tab, serverUrl, created.key)

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />

      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="mcp-key-created-title"
        tabIndex={-1}
        className="relative bg-white rounded-xl border border-gray-200 shadow-lg max-w-2xl w-full p-6 focus:outline-none max-h-[90vh] overflow-y-auto"
      >
        <h2 id="mcp-key-created-title" className="text-lg font-semibold text-gray-900">
          Chave &ldquo;{created.accessKey.name}&rdquo; criada
        </h2>
        <p className="text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2 mt-3">
          Esta é a única vez que a chave será exibida. Copie-a agora e guarde no assistente; depois de fechar, só é
          possível revogá-la e criar outra.
        </p>

        <div className="mt-4">
          <label className="block text-xs font-medium text-gray-500 mb-1">Chave de acesso</label>
          <div className="flex gap-2">
            <code className="flex-1 text-sm bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 break-all select-all">
              {created.key}
            </code>
            <button
              type="button"
              onClick={() => copy(created.key, 'Chave')}
              className="px-3 py-2 text-sm bg-ava-600 text-white rounded-lg hover:bg-ava-700 transition-colors whitespace-nowrap"
            >
              Copiar
            </button>
          </div>
        </div>

        <div className="mt-4">
          <label className="block text-xs font-medium text-gray-500 mb-1">Endereço do servidor MCP</label>
          <div className="flex gap-2">
            <code className="flex-1 text-sm bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 break-all select-all">
              {serverUrl}
            </code>
            <button
              type="button"
              onClick={() => copy(serverUrl, 'Endereço')}
              className="px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors whitespace-nowrap"
            >
              Copiar
            </button>
          </div>
        </div>

        <div className="mt-5">
          <p className="text-sm font-medium text-gray-900 mb-2">Configuração pronta para o seu assistente</p>
          <div className="flex flex-wrap gap-1 border-b border-gray-200" role="tablist">
            {TABS.map((t) => (
              <button
                key={t.id}
                type="button"
                role="tab"
                aria-selected={tab === t.id}
                onClick={() => setTab(t.id)}
                className={`px-3 py-1.5 text-sm rounded-t-lg border-b-2 -mb-px transition-colors ${
                  tab === t.id
                    ? 'border-ava-600 text-ava-700 font-medium'
                    : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                {t.label}
              </button>
            ))}
          </div>
          <pre className="mt-3 text-xs bg-gray-900 text-gray-100 rounded-lg p-3 overflow-x-auto whitespace-pre-wrap break-all">
            {snippet}
          </pre>
          <div className="flex items-center justify-between mt-2 gap-3">
            <p className="text-xs text-gray-400">{fileHint[tab]}</p>
            <button
              type="button"
              onClick={() => copy(snippet, 'Trecho')}
              className="px-3 py-1.5 text-xs border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors whitespace-nowrap"
            >
              Copiar trecho
            </button>
          </div>
        </div>

        <div className="flex justify-end mt-6">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 transition-colors"
          >
            Já copiei, fechar
          </button>
        </div>
      </div>
    </div>
  )
}

export default McpKeyCreatedModal
