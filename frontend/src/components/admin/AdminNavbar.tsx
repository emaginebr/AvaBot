import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../stores/useAuthStore'
import AgentSelector from './AgentSelector'

interface AdminNavbarProps {
  onToggleSidebar?: () => void
}

const AdminNavbar = ({ onToggleSidebar }: AdminNavbarProps) => {
  const logout = useAuthStore((state) => state.logout)
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-4 sm:px-6 shrink-0">
      <div className="flex items-center gap-3">
        <button
          onClick={onToggleSidebar}
          className="lg:hidden p-1.5 text-gray-500 hover:text-gray-700 rounded-md"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M3 5a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zM3 10a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zM3 15a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" clipRule="evenodd" />
          </svg>
        </button>
        <h1 className="font-display text-xl text-ava-900 font-semibold">AvaBot</h1>
        <span className="text-xs text-gray-400 bg-gray-100 px-2 py-0.5 rounded">Admin</span>
      </div>

      <div className="flex items-center gap-4">
        <AgentSelector />
      </div>

      <button
        onClick={handleLogout}
        className="text-sm text-gray-500 hover:text-gray-700 transition-colors flex items-center gap-1.5"
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path fillRule="evenodd" d="M3 3a1 1 0 011-1h12a1 1 0 011 1v12a1 1 0 01-1 1H4a1 1 0 01-1-1V3zm10.293 9.293a1 1 0 001.414 1.414l3-3a1 1 0 000-1.414l-3-3a1 1 0 10-1.414 1.414L14.586 9H7a1 1 0 100 2h7.586l-1.293 1.293z" clipRule="evenodd" />
        </svg>
        Sair
      </button>
    </header>
  )
}

export default AdminNavbar
