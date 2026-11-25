import { useState } from 'react'
import { toast } from 'sonner'
import type { McpAccessKeyCreatedInfo } from '../../types/mcpAccessKey'
import Button from './ui/Button'
import CodeBlock from './ui/CodeBlock'
import CopyField from './ui/CopyField'
import Modal from './ui/Modal'
import Tabs, { TabPanel } from './ui/Tabs'

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
    <Modal
      size="lg"
      icon="key"
      iconTone="ok"
      showClose={false}
      onClose={onClose}
      title={<>Chave &ldquo;{created.accessKey.name}&rdquo; criada</>}
      description="Esta é a única vez que a chave será exibida. Copie-a agora e guarde no assistente; depois de fechar, só é possível revogá-la e criar outra."
      footer={
        <Button variant="primary" onClick={onClose}>
          Já copiei, fechar
        </Button>
      }
    >
      <div className="flex flex-col gap-1.5">
        <span className="text-[13px] font-medium text-ink">Chave de acesso</span>
        <CopyField value={created.key} onCopy={() => copy(created.key, 'Chave')} ariaLabel="Copiar chave de acesso" />
      </div>

      <div className="flex flex-col gap-1.5">
        <span className="text-[13px] font-medium text-ink">Endereço do servidor MCP</span>
        <CopyField value={serverUrl} onCopy={() => copy(serverUrl, 'Endereço')} ariaLabel="Copiar endereço do servidor MCP" />
      </div>

      <div>
        <p className="m-0 mb-2 text-[13px] font-medium text-ink">Configuração pronta para o seu assistente</p>
        <Tabs idPrefix="mcp-snippet" label="Assistente" items={TABS} value={tab} onChange={setTab} />
        <TabPanel idPrefix="mcp-snippet" tabId={tab} className="flex flex-col gap-2.5">
          <CodeBlock wrap>{snippet}</CodeBlock>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="m-0 text-[12.5px] text-ink-3">{fileHint[tab]}</p>
            <Button size="sm" icon="copy" onClick={() => copy(snippet, 'Trecho')}>
              Copiar trecho
            </Button>
          </div>
        </TabPanel>
      </div>
    </Modal>
  )
}

export default McpKeyCreatedModal
