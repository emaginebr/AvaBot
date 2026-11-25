import type { Ref } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../stores/useAuthStore'
import AdminIcon from './AdminIcon'
import { resolveCrumbs } from './layout/adminNav'
import { initialsOf } from './layout/initials'

interface AdminNavbarProps {
  onToggleSidebar: () => void
  sidebarOpen: boolean
  sidebarId: string
  /** Recebe o foco de volta quando o drawer fecha com Esc. */
  menuButtonRef?: Ref<HTMLButtonElement>
}

/**
 * Cabeçalho do painel: branco translúcido com blur (como a navbar do site), botão do drawer
 * no mobile, trilha da página atual, usuário e "Sair".
 */
const AdminNavbar = ({ onToggleSidebar, sidebarOpen, sidebarId, menuButtonRef }: AdminNavbarProps) => {
  const logout = useAuthStore((state) => state.logout)
  const user = useAuthStore((state) => state.user)
  const navigate = useNavigate()
  const location = useLocation()
  const { trail } = resolveCrumbs(location.pathname, location.hash)

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="sticky top-0 z-20 flex items-center gap-3 border-b border-gray-100 bg-white/80 px-4 py-3 backdrop-blur-lg sm:px-8 lg:py-3.5">
      <button
        ref={menuButtonRef}
        type="button"
        onClick={onToggleSidebar}
        aria-label={sidebarOpen ? 'Fechar menu' : 'Abrir menu'}
        aria-controls={sidebarId}
        aria-expanded={sidebarOpen}
        className="grid h-10 w-10 flex-none place-items-center rounded-xl border border-gray-200 bg-white text-ink-2 transition-colors hover:border-ava-200 hover:bg-ava-50 hover:text-ava-700 lg:hidden"
      >
        <AdminIcon name="menu" className="h-5 w-5" />
      </button>

      <nav aria-label="Trilha" className="min-w-0 flex-1">
        <ol className="flex min-w-0 items-center gap-1.5 text-[13px] text-ink-3">
          {trail.map((crumb, i) => {
            const last = i === trail.length - 1
            return (
              <li key={`${crumb}-${i}`} className={`flex min-w-0 items-center gap-1.5 ${last ? '' : 'max-sm:hidden'}`}>
                {i > 0 && (
                  <AdminIcon name="chevron" className="h-3.5 w-3.5 -rotate-90 text-gray-300 max-sm:hidden" />
                )}
                <span
                  aria-current={last ? 'page' : undefined}
                  className={last ? 'truncate font-brand text-[15px] font-semibold tracking-[-0.01em] text-ink' : 'truncate'}
                >
                  {crumb}
                </span>
              </li>
            )
          })}
        </ol>
      </nav>

      <div className="ml-auto flex flex-none items-center gap-2">
        {user && (
          <Link
            to="/admin/account"
            title="Minha conta"
            className="flex max-w-[200px] items-center gap-2 rounded-xl px-1.5 py-1 text-[13px] text-ink-2 transition-colors hover:bg-ava-50 hover:text-ava-700"
          >
            <span
              aria-hidden="true"
              className="grid h-8 w-8 flex-none place-items-center rounded-full bg-ava-100 text-[11px] font-semibold text-ava-800"
            >
              {initialsOf(user.name)}
            </span>
            <span className="truncate max-sm:sr-only">{user.name}</span>
          </Link>
        )}

        <button
          type="button"
          onClick={handleLogout}
          className="inline-flex h-10 items-center gap-1.5 rounded-xl border border-gray-200 bg-white px-3 text-[13px] font-medium text-gray-700 transition-colors hover:border-gray-300 hover:bg-gray-50"
        >
          <AdminIcon name="logout" className="h-4 w-4" />
          Sair
        </button>
      </div>
    </header>
  )
}

export default AdminNavbar
