import { Routes, Route } from 'react-router-dom'
import { Toaster } from 'sonner'
import LandingPage from './pages/LandingPage'
import LoginPage from './pages/auth/LoginPage'
import ProtectedRoute from './components/common/ProtectedRoute'
import AdminLayout from './components/admin/AdminLayout'
import DashboardPage from './pages/admin/DashboardPage'
import AgentListPage from './pages/admin/AgentListPage'
import AgentFormPage from './pages/admin/AgentFormPage'
import AgentSettingsPage from './pages/admin/AgentSettingsPage'
import SessionListPage from './pages/admin/SessionListPage'
import SessionDetailPage from './pages/admin/SessionDetailPage'
import KnowledgeFilesPage from './pages/admin/KnowledgeFilesPage'
import KnowledgeSearchPage from './pages/admin/KnowledgeSearchPage'
import AgentTestPage from './pages/admin/AgentTestPage'
import TelegramBotPage from './pages/admin/TelegramBotPage'
import WhatsappPage from './pages/admin/WhatsappPage'
import ChatPage from './pages/chat/ChatPage'
import AbipescaPage from './pages/AbipescaPage'
import NotFoundPage from './components/common/NotFoundPage'

const App = () => {
  return (
    <>
    <Toaster position="bottom-right" richColors />
    <Routes>
      {/* Public routes */}
      <Route path="/" element={<LandingPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/chat/:slug" element={<ChatPage />} />
      <Route path="/abipesca" element={<AbipescaPage />} />

      {/* Protected admin routes */}
      <Route element={<ProtectedRoute />}>
        <Route element={<AdminLayout />}>
          <Route path="/admin" element={<DashboardPage />} />
          <Route path="/admin/agents" element={<AgentListPage />} />
          <Route path="/admin/agents/new" element={<AgentFormPage />} />
          <Route path="/admin/agents/:id/edit" element={<AgentFormPage />} />
          <Route path="/admin/settings" element={<AgentSettingsPage />} />
          <Route path="/admin/telegram" element={<TelegramBotPage />} />
          <Route path="/admin/whatsapp" element={<WhatsappPage />} />
          <Route path="/admin/sessions" element={<SessionListPage />} />
          <Route path="/admin/sessions/:sessionId" element={<SessionDetailPage />} />
          <Route path="/admin/knowledge/files" element={<KnowledgeFilesPage />} />
          <Route path="/admin/knowledge/search" element={<KnowledgeSearchPage />} />
          <Route path="/admin/knowledge/test" element={<AgentTestPage />} />
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
    </>
  )
}

export default App
