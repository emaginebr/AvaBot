import { useEffect, useRef, useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import AdminNavbar from './AdminNavbar'
import AdminSidebar from './AdminSidebar'
import AdminFooter from './layout/AdminFooter'

const SIDEBAR_ID = 'admin-sidebar'

/**
 * Casca de todas as páginas admin: sidebar azul-escura (drawer abaixo de lg), cabeçalho,
 * conteúdo e rodapé. Usa as fontes do painel (Geist/Bricolage) só aqui dentro.
 */
const AdminLayout = () => {
  const location = useLocation()
  const menuButtonRef = useRef<HTMLButtonElement>(null)

  // O drawer fica aberto só na rota em que foi aberto: navegar o fecha sem efeito extra.
  const [openAt, setOpenAt] = useState<string | null>(null)
  const sidebarOpen = openAt === location.pathname
  const closeSidebar = () => setOpenAt(null)
  const toggleSidebar = () => setOpenAt(sidebarOpen ? null : location.pathname)

  // Esc fecha o drawer e devolve o foco ao botão do menu; a página atrás não rola.
  useEffect(() => {
    if (!sidebarOpen) return
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key !== 'Escape' || e.defaultPrevented) return
      setOpenAt(null)
      menuButtonRef.current?.focus()
    }
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    window.addEventListener('keydown', onKeyDown)
    return () => {
      document.body.style.overflow = previousOverflow
      window.removeEventListener('keydown', onKeyDown)
    }
  }, [sidebarOpen])

  // Troca de página volta ao topo; âncoras (ex.: #chaves-mcp em Minha conta) rolam até a seção.
  useEffect(() => {
    if (location.hash) {
      const target = document.getElementById(decodeURIComponent(location.hash.slice(1)))
      if (target) {
        const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches
        target.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'start' })
        return
      }
    }
    window.scrollTo(0, 0)
  }, [location.pathname, location.hash])

  return (
    <div className="min-h-dvh bg-white font-ui text-ink antialiased lg:grid lg:grid-cols-[224px_minmax(0,1fr)]">
      <a
        href="#conteudo"
        className="sr-only focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-50 focus:rounded-lg focus:bg-white focus:px-3 focus:py-2 focus:text-sm focus:font-medium focus:text-ava-700 focus:shadow-lg"
      >
        Pular para o conteúdo
      </a>

      {sidebarOpen && (
        <div aria-hidden="true" className="fixed inset-0 z-30 bg-[rgb(15_20_48/0.45)] lg:hidden" onClick={closeSidebar} />
      )}

      <AdminSidebar id={SIDEBAR_ID} open={sidebarOpen} onNavigate={closeSidebar} />

      <div className="flex min-h-dvh min-w-0 flex-col">
        <AdminNavbar
          onToggleSidebar={toggleSidebar}
          sidebarOpen={sidebarOpen}
          sidebarId={SIDEBAR_ID}
          menuButtonRef={menuButtonRef}
        />

        <main id="conteudo" tabIndex={-1} className="flex-1 px-4 pt-6 pb-12 outline-none sm:px-8 lg:pt-8">
          <div className="w-full max-w-[1440px]">
            <Outlet />
          </div>
        </main>

        <AdminFooter />
      </div>
    </div>
  )
}

export default AdminLayout
