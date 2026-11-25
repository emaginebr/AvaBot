import { useEffect } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useAgentStore } from '../../stores/useAgentStore'
import { useAuthStore } from '../../stores/useAuthStore'
import { useWhatsappPendingStore } from '../../stores/useWhatsappPendingStore'
import BrandLogo from '../common/BrandLogo'
import AgentSelector from './AgentSelector'
import SidebarAccordion from './layout/SidebarAccordion'
import { initialsOf } from './layout/initials'

interface AdminSidebarProps {
  id: string
  /** Drawer aberto (só tem efeito abaixo de lg; no desktop a sidebar é fixa). */
  open: boolean
  onNavigate: () => void
}

/** Sidebar azul-escura e compacta do painel: logomarca, agente ativo, menu em sanfona e usuário. */
const AdminSidebar = ({ id, open, onNavigate }: AdminSidebarProps) => {
  const location = useLocation()
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const user = useAuthStore((state) => state.user)
  const pending = useWhatsappPendingStore((state) => (state.slug === selectedAgent?.slug ? state.count : 0))
  const refreshPending = useWhatsappPendingStore((state) => state.refresh)

  // Contador do badge: consulta leve ao trocar de agente e a cada navegação (com intervalo mínimo).
  const slug = selectedAgent?.slug ?? null
  useEffect(() => {
    refreshPending(slug)
  }, [slug, location.pathname, refreshPending])

  return (
    <aside
      id={id}
      aria-label="Navegação principal"
      className={`fixed inset-y-0 left-0 z-40 flex h-dvh w-[min(82vw,224px)] flex-col gap-3 overflow-x-hidden overflow-y-auto scrollbar-none border-r border-white/6 bg-[linear-gradient(180deg,var(--color-ava-950)_0%,#161f66_60%,var(--color-ava-900)_140%)] px-2.5 pt-3.5 pb-2.5 text-white/75 transition-[translate,visibility] duration-[250ms] ease-out motion-reduce:transition-none lg:sticky lg:top-0 lg:z-auto lg:visible lg:w-auto lg:translate-x-0 [@media(min-width:1024px)_and_(max-height:640px)]:gap-2 [@media(min-width:1024px)_and_(max-height:640px)]:pt-2.5 [&_:focus-visible]:outline-ava-300 ${
        open ? 'visible translate-x-0' : 'invisible -translate-x-full'
      }`}
    >
      <Link
        to="/admin"
        onClick={onNavigate}
        aria-label="AvaBot — painel"
        className="rounded-lg px-1.5 py-0.5"
      >
        <BrandLogo tone="light" size="sm" />
      </Link>

      <AgentSelector variant="sidebar" />

      <SidebarAccordion
        pathname={location.pathname}
        hash={location.hash}
        hasAgent={!!selectedAgent}
        whatsappPending={pending}
        onNavigate={onNavigate}
      />

      {user && (
        <Link
          to="/admin/account"
          onClick={onNavigate}
          className="mt-auto flex items-center gap-2.5 rounded-lg border-t border-white/10 px-1.5 pt-2.5 pb-0.5 transition-colors hover:bg-white/5"
        >
          <span
            aria-hidden="true"
            className="grid h-7 w-7 flex-none place-items-center rounded-full bg-ava-600 text-[10.5px] font-semibold text-white"
          >
            {initialsOf(user.name)}
          </span>
          <span className="min-w-0">
            <span className="block truncate text-[12.5px] leading-tight font-medium text-white">{user.name}</span>
            <small className="block truncate text-[11px] text-ava-200">{user.email}</small>
          </span>
        </Link>
      )}
    </aside>
  )
}

export default AdminSidebar
