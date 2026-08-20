import { Link, useLocation } from 'react-router-dom'
import { useAgentStore } from '../../stores/useAgentStore'

interface MenuItem {
  label: string
  path: string
  requiresAgent: boolean
}

const menuItems: MenuItem[] = [
  { label: 'Dashboard', path: '/admin', requiresAgent: false },
  { label: 'Agentes', path: '/admin/agents', requiresAgent: false },
  { label: 'Configurações', path: '/admin/settings', requiresAgent: true },
  { label: 'Bot Telegram', path: '/admin/telegram', requiresAgent: true },
  { label: 'WhatsApp', path: '/admin/whatsapp', requiresAgent: true },
  { label: 'Sessões', path: '/admin/sessions', requiresAgent: true },
  { label: 'Arquivos', path: '/admin/knowledge/files', requiresAgent: true },
  { label: 'Busca na Base', path: '/admin/knowledge/search', requiresAgent: true },
  { label: 'Teste do Agente', path: '/admin/knowledge/test', requiresAgent: true },
]

const AdminSidebar = () => {
  const location = useLocation()
  const selectedAgent = useAgentStore((state) => state.selectedAgent)

  const isActive = (path: string) => {
    if (path === '/admin') return location.pathname === '/admin'
    return location.pathname.startsWith(path)
  }

  return (
    <aside className="w-60 bg-white border-r border-gray-200 shrink-0 flex flex-col">
      {selectedAgent && (
        <div className="px-4 py-3 border-b border-gray-200 bg-ava-50">
          <p className="text-xs text-gray-500">Agente ativo</p>
          <p className="text-sm font-medium text-ava-800 truncate">{selectedAgent.name}</p>
        </div>
      )}

      <nav className="flex-1 py-3">
        <div className="px-3 mb-2">
          <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wider px-3">Geral</p>
        </div>
        {menuItems.filter((item) => !item.requiresAgent).map((item) => (
          <Link
            key={item.path}
            to={item.path}
            className={`flex items-center px-6 py-2 text-sm transition-colors ${
              isActive(item.path)
                ? 'text-ava-700 bg-ava-50 font-medium border-r-2 border-ava-600'
                : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
            }`}
          >
            {item.label}
          </Link>
        ))}

        <div className="px-3 mt-5 mb-2">
          <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wider px-3">Agente</p>
        </div>
        {menuItems.filter((item) => item.requiresAgent).map((item) => {
          const disabled = !selectedAgent
          if (disabled) {
            return (
              <span
                key={item.path}
                className="flex items-center px-6 py-2 text-sm text-gray-300 cursor-not-allowed"
                title="Selecione um agente na navbar"
              >
                {item.label}
              </span>
            )
          }
          return (
            <Link
              key={item.path}
              to={item.path}
              className={`flex items-center px-6 py-2 text-sm transition-colors ${
                isActive(item.path)
                  ? 'text-ava-700 bg-ava-50 font-medium border-r-2 border-ava-600'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
              }`}
            >
              {item.label}
            </Link>
          )
        })}

        {!selectedAgent && (
          <p className="px-6 mt-2 text-xs text-gray-400">
            Selecione um agente na navbar para acessar estas opções
          </p>
        )}
      </nav>
    </aside>
  )
}

export default AdminSidebar
