import type { AdminIconName } from '../AdminIcon'

/** Âncora da seção de chaves MCP dentro de "Minha conta" (McpAccessKeysPanel). */
export const MCP_KEYS_HASH = '#chaves-mcp'

export interface AdminNavItem {
  id: string
  label: string
  to: string
  icon: AdminIconName
  /** Itens que dependem do agente ativo ficam desabilitados sem agente selecionado. */
  requiresAgent: boolean
  /** Contador exibido ao lado do item (e no cabeçalho do grupo quando fechado). */
  badge?: 'whatsappPending'
}

export interface AdminNavGroup {
  id: string
  label: string
  items: AdminNavItem[]
}

/** Menu do painel: as mesmas rotas reais de App.tsx, organizadas nos grupos da proposta. */
export const ADMIN_NAV: AdminNavGroup[] = [
  {
    id: 'geral',
    label: 'Visão geral',
    items: [
      { id: 'painel', label: 'Painel', to: '/admin', icon: 'home', requiresAgent: false },
      { id: 'sessoes', label: 'Sessões', to: '/admin/sessions', icon: 'chat', requiresAgent: true },
    ],
  },
  {
    id: 'agentes',
    label: 'Agentes',
    items: [
      { id: 'agentes', label: 'Meus agentes', to: '/admin/agents', icon: 'bot', requiresAgent: false },
      { id: 'configuracoes', label: 'Configurações', to: '/admin/settings', icon: 'cog', requiresAgent: true },
      { id: 'teste', label: 'Teste do agente', to: '/admin/knowledge/test', icon: 'flask', requiresAgent: true },
    ],
  },
  {
    id: 'conhecimento',
    label: 'Conhecimento',
    items: [
      { id: 'arquivos', label: 'Arquivos', to: '/admin/knowledge/files', icon: 'file', requiresAgent: true },
      { id: 'busca', label: 'Busca na base', to: '/admin/knowledge/search', icon: 'search', requiresAgent: true },
    ],
  },
  {
    id: 'canais',
    label: 'Canais e integrações',
    items: [
      { id: 'telegram', label: 'Bot Telegram', to: '/admin/telegram', icon: 'send', requiresAgent: true },
      { id: 'whatsapp', label: 'WhatsApp', to: '/admin/whatsapp', icon: 'whatsapp', requiresAgent: true, badge: 'whatsappPending' },
      { id: 'powerbi', label: 'Power BI', to: '/admin/powerbi', icon: 'chart', requiresAgent: true },
    ],
  },
  {
    id: 'conta',
    label: 'Conta',
    items: [
      { id: 'conta', label: 'Minha conta', to: '/admin/account', icon: 'user', requiresAgent: false },
      { id: 'mcp', label: 'Chaves MCP', to: `/admin/account${MCP_KEYS_HASH}`, icon: 'key', requiresAgent: false },
    ],
  },
]

/** Indica se o item corresponde à rota atual (considera a âncora das chaves MCP). */
export const isNavItemActive = (item: AdminNavItem, pathname: string, hash: string): boolean => {
  if (item.id === 'painel') return pathname === '/admin'
  if (item.id === 'mcp') return pathname === '/admin/account' && hash === MCP_KEYS_HASH
  if (item.id === 'conta') return pathname === '/admin/account' && hash !== MCP_KEYS_HASH
  return pathname === item.to || pathname.startsWith(`${item.to}/`)
}

export interface ActiveNav {
  group: AdminNavGroup
  item: AdminNavItem
}

export const findActiveNav = (pathname: string, hash: string): ActiveNav | null => {
  for (const group of ADMIN_NAV) {
    const item = group.items.find((i) => isNavItemActive(i, pathname, hash))
    if (item) return { group, item }
  }
  return null
}

export interface PageCrumbs {
  /** Trilha do cabeçalho, do grupo até a página atual. */
  trail: string[]
}

/** Trilha exibida no cabeçalho; inclui as subpáginas que não aparecem no menu. */
export const resolveCrumbs = (pathname: string, hash: string): PageCrumbs => {
  const active = findActiveNav(pathname, hash)
  if (!active) return { trail: ['Painel'] }

  const trail = [active.group.label, active.item.label]
  if (pathname === '/admin/agents/new') trail.push('Novo agente')
  else if (/^\/admin\/agents\/[^/]+\/edit$/.test(pathname)) trail.push('Editar agente')
  else {
    const session = pathname.match(/^\/admin\/sessions\/([^/]+)$/)
    if (session) trail.push(`Conversa #${session[1]}`)
  }
  return { trail }
}
