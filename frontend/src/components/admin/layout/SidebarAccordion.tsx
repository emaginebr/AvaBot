import { useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import AdminIcon from '../AdminIcon'
import NavBadge from './NavBadge'
import { ADMIN_NAV, findActiveNav, isNavItemActive } from './adminNav'
import type { AdminNavItem } from './adminNav'

interface SidebarAccordionProps {
  pathname: string
  hash: string
  hasAgent: boolean
  /** Pedidos de acesso do WhatsApp pendentes para o agente ativo. */
  whatsappPending: number
  /** Chamado ao clicar em um item (fecha o drawer no mobile). */
  onNavigate?: () => void
}

/** Escolha manual do usuário, válida enquanto o grupo da rota ativa não mudar. */
interface ManualChoice {
  activeGroupId: string | null
  openGroupId: string | null
}

const itemBase =
  'relative flex min-h-9 items-center gap-2.5 rounded-lg pr-2 pl-2.5 text-[13px] transition-colors motion-reduce:transition-none max-lg:min-h-10 [@media(min-width:1024px)_and_(max-height:640px)]:min-h-8'

/**
 * Menu em sanfona da sidebar: um grupo aberto por vez; o grupo da rota ativa começa aberto.
 * A altura anima via grid-template-rows (0fr → 1fr) e respeita prefers-reduced-motion.
 */
const SidebarAccordion = ({ pathname, hash, hasAgent, whatsappPending, onNavigate }: SidebarAccordionProps) => {
  const activeGroupId = findActiveNav(pathname, hash)?.group.id ?? null
  const [manual, setManual] = useState<ManualChoice | null>(null)
  const headerRefs = useRef<(HTMLButtonElement | null)[]>([])

  // Ao entrar em outro grupo pela navegação, o grupo da nova rota abre (a escolha manual expira).
  const openGroupId = manual && manual.activeGroupId === activeGroupId ? manual.openGroupId : activeGroupId

  const toggle = (groupId: string) => {
    setManual({ activeGroupId, openGroupId: openGroupId === groupId ? null : groupId })
  }

  const handleHeaderKeyDown = (e: React.KeyboardEvent<HTMLButtonElement>, index: number) => {
    const last = ADMIN_NAV.length - 1
    const target = ({ ArrowDown: index + 1, ArrowUp: index - 1, Home: 0, End: last } as Record<string, number>)[e.key]
    if (target === undefined) return
    e.preventDefault()
    headerRefs.current[(target + ADMIN_NAV.length) % ADMIN_NAV.length]?.focus()
  }

  const badgeOf = (item: AdminNavItem) => (item.badge === 'whatsappPending' && hasAgent ? whatsappPending : 0)

  return (
    <nav aria-label="Seções do painel" className="flex flex-col gap-0.5">
      {ADMIN_NAV.map((group, index) => {
        const open = openGroupId === group.id
        const headerId = `acc-${group.id}`
        const panelId = `accp-${group.id}`
        const groupBadge = group.items.reduce((sum, item) => sum + badgeOf(item), 0)

        return (
          <div key={group.id}>
            <button
              ref={(el) => { headerRefs.current[index] = el }}
              type="button"
              id={headerId}
              aria-expanded={open}
              aria-controls={panelId}
              onClick={() => toggle(group.id)}
              onKeyDown={(e) => handleHeaderKeyDown(e, index)}
              className={`flex min-h-9 w-full items-center gap-2 rounded-lg px-2 text-left text-[11px] font-semibold tracking-[0.08em] uppercase transition-colors hover:bg-white/6 hover:text-white motion-reduce:transition-none max-lg:min-h-10 [@media(min-width:1024px)_and_(max-height:640px)]:min-h-8 ${
                open ? 'text-white' : 'text-ava-200'
              }`}
            >
              <span className="min-w-0 flex-1 truncate">{group.label}</span>
              {!open && groupBadge > 0 && (
                <NavBadge count={groupBadge} label={`${groupBadge} pedidos pendentes no WhatsApp`} />
              )}
              <AdminIcon
                name="chevron"
                className={`h-3.5 w-3.5 opacity-80 transition-transform duration-200 motion-reduce:transition-none ${open ? '' : '-rotate-90'}`}
              />
            </button>

            <div
              id={panelId}
              role="region"
              aria-labelledby={headerId}
              className={`grid motion-reduce:transition-none ${
                open
                  ? 'visible grid-rows-[1fr] [transition:grid-template-rows_.24s_ease,visibility_0s]'
                  : 'invisible grid-rows-[0fr] [transition:grid-template-rows_.24s_ease,visibility_0s_linear_.24s]'
              }`}
            >
              <div className="flex min-h-0 flex-col gap-px overflow-hidden after:block after:h-1 after:flex-none">
                {group.items.map((item) => {
                  const badge = badgeOf(item)
                  if (item.requiresAgent && !hasAgent) {
                    return (
                      <span
                        key={item.id}
                        aria-disabled="true"
                        title="Selecione um agente ativo acima"
                        className={`${itemBase} cursor-not-allowed text-white/35`}
                      >
                        <AdminIcon name={item.icon} className="h-4 w-4 opacity-60" />
                        {item.label}
                      </span>
                    )
                  }
                  const active = isNavItemActive(item, pathname, hash)
                  return (
                    <Link
                      key={item.id}
                      to={item.to}
                      onClick={onNavigate}
                      aria-current={active ? 'page' : undefined}
                      className={`${itemBase} ${
                        active
                          ? 'bg-ava-600 font-medium text-white shadow-[0_4px_12px_-4px_rgb(31_69_241/0.7)]'
                          : 'text-white/75 hover:bg-white/8 hover:text-white'
                      }`}
                    >
                      <AdminIcon name={item.icon} className={`h-4 w-4 ${active ? '' : 'opacity-85'}`} />
                      <span className="min-w-0 flex-1 truncate">{item.label}</span>
                      {badge > 0 && <NavBadge count={badge} label={`${badge} pedidos pendentes`} />}
                    </Link>
                  )
                })}
              </div>
            </div>
          </div>
        )
      })}
      {!hasAgent && (
        <p className="mt-1 px-2 text-[11.5px] leading-snug text-ava-200/80">
          Selecione um agente ativo para liberar os itens desabilitados.
        </p>
      )}
    </nav>
  )
}

export default SidebarAccordion
