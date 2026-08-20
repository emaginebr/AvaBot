import type { ChatSessionInfo } from '../types/chatSession'
import type { ChatMessageInfo } from '../types/chatMessage'
import type { Result, PaginatedResult } from '../types/result'
import { AuthService } from './AuthService'

const getApiUrl = () => import.meta.env.VITE_API_URL

const handleResponse = async <T>(response: Response, action: string): Promise<Result<T>> => {
  if (AuthService.handleUnauthorized(response)) {
    console.warn(`[ChatHistoryService] ${action} — 401 Unauthorized, redirecionando para login`)
    return { sucesso: false, mensagem: 'Sessão expirada', erros: [], dados: null as T }
  }

  if (!response.ok) {
    let errorBody: Result<T> | null = null
    try {
      errorBody = await response.json()
    } catch {
      // response não é JSON
    }
    const mensagem = errorBody?.mensagem || `Erro ${response.status}: ${response.statusText}`
    console.error(`[ChatHistoryService] ${action} — HTTP ${response.status}:`, mensagem, errorBody?.erros)
    return { sucesso: false, mensagem, erros: errorBody?.erros || [], dados: null as T }
  }

  const data: Result<T> = await response.json()
  if (!data.sucesso) {
    console.warn(`[ChatHistoryService] ${action} — API retornou erro:`, data.mensagem, data.erros)
  } else {
    console.log(`[ChatHistoryService] ${action} — OK`)
  }
  return data
}

export const ChatHistoryService = {
  getSessions: async (agentId: number, page = 1, maxPage = 20): Promise<Result<PaginatedResult<ChatSessionInfo>>> => {
    console.log(`[ChatHistoryService] getSessions — GET /sessions/agents/${agentId}?page=${page}&maxPage=${maxPage}`)
    const params = new URLSearchParams({ page: String(page), maxPage: String(maxPage) })
    const response = await fetch(`${getApiUrl()}/sessions/agents/${agentId}?${params}`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getSessions')
  },

  getMessages: async (sessionId: number, page = 1, maxPage = 50): Promise<Result<PaginatedResult<ChatMessageInfo>>> => {
    console.log(`[ChatHistoryService] getMessages — GET /sessions/${sessionId}/messages?page=${page}&maxPage=${maxPage}`)
    const params = new URLSearchParams({ page: String(page), maxPage: String(maxPage) })
    const response = await fetch(`${getApiUrl()}/sessions/${sessionId}/messages?${params}`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getMessages')
  },
}
