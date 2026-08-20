import type { AgentInfo, AgentInsertInfo, AgentChatConfigInfo, AgentTestResult, TelegramWebhookInfo, WhatsappQrCodeInfo, WhatsappStatusInfo } from '../types/agent'
import type { ChatSessionStartInfo, ChatSessionInfo, ChatSessionResumeInfo } from '../types/chatSession'
import type { Result } from '../types/result'
import { AuthService } from './AuthService'

let _apiUrl: string | undefined
export function setApiUrl(url: string) { _apiUrl = url }
const getApiUrl = () => _apiUrl ?? import.meta.env.VITE_API_URL

const getPublicHeaders = (): HeadersInit => ({
  'Content-Type': 'application/json',
})

const handleResponse = async <T>(response: Response, action: string): Promise<Result<T>> => {
  if (AuthService.handleUnauthorized(response)) {
    console.warn(`[AgentService] ${action} — 401 Unauthorized, redirecionando para login`)
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
    console.error(`[AgentService] ${action} — HTTP ${response.status}:`, mensagem, errorBody?.erros)
    return { sucesso: false, mensagem, erros: errorBody?.erros || [], dados: null as T }
  }

  const data: Result<T> = await response.json()
  if (!data.sucesso) {
    console.warn(`[AgentService] ${action} — API retornou erro:`, data.mensagem, data.erros)
  } else {
    console.log(`[AgentService] ${action} — OK`)
  }
  return data
}

export const AgentService = {
  getAll: async (): Promise<Result<AgentInfo[]>> => {
    console.log('[AgentService] getAll — GET /agents')
    const response = await fetch(`${getApiUrl()}/agents`, { headers: AuthService.getAuthHeaders() })
    return handleResponse(response, 'getAll')
  },

  getBySlug: async (slug: string): Promise<Result<AgentInfo>> => {
    console.log(`[AgentService] getBySlug — GET /agents/${slug}`)
    const response = await fetch(`${getApiUrl()}/agents/${slug}`, { headers: getPublicHeaders() })
    return handleResponse(response, 'getBySlug')
  },

  getChatConfig: async (slug: string): Promise<Result<AgentChatConfigInfo>> => {
    console.log(`[AgentService] getChatConfig — GET /agents/${slug}/chat-config`)
    const response = await fetch(`${getApiUrl()}/agents/${slug}/chat-config`, { headers: getPublicHeaders() })
    return handleResponse(response, 'getChatConfig')
  },

  create: async (data: AgentInsertInfo): Promise<Result<AgentInfo>> => {
    console.log('[AgentService] create — POST /agents', data)
    const response = await fetch(`${getApiUrl()}/agents`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify(data),
    })
    return handleResponse(response, 'create')
  },

  update: async (id: number, data: AgentInsertInfo): Promise<Result<AgentInfo>> => {
    console.log(`[AgentService] update — PUT /agents/${id}`, data)
    const response = await fetch(`${getApiUrl()}/agents/${id}`, {
      method: 'PUT',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify(data),
    })
    return handleResponse(response, 'update')
  },

  delete: async (id: number): Promise<Result<null>> => {
    console.log(`[AgentService] delete — DELETE /agents/${id}`)
    const response = await fetch(`${getApiUrl()}/agents/${id}`, {
      method: 'DELETE',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'delete')
  },

  startSession: async (slug: string, data: ChatSessionStartInfo): Promise<Result<ChatSessionInfo>> => {
    console.log(`[AgentService] startSession — POST /sessions/agents/${slug}`, data)
    const response = await fetch(`${getApiUrl()}/sessions/agents/${slug}`, {
      method: 'POST',
      headers: getPublicHeaders(),
      body: JSON.stringify(data),
    })
    return handleResponse(response, 'startSession')
  },

  toggleStatus: async (id: number): Promise<Result<AgentInfo>> => {
    console.log(`[AgentService] toggleStatus — PATCH /agents/${id}/status`)
    const response = await fetch(`${getApiUrl()}/agents/${id}/status`, {
      method: 'PATCH',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'toggleStatus')
  },

  test: async (agentId: number, query: string): Promise<Result<AgentTestResult>> => {
    console.log(`[AgentService] test — POST /agents/${agentId}/test`, { query })
    const response = await fetch(`${getApiUrl()}/agents/${agentId}/test`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify({ query }),
    })
    return handleResponse(response, 'test')
  },

  resumeSession: async (slug: string, resumeToken: string): Promise<Result<ChatSessionResumeInfo>> => {
    console.log(`[AgentService] resumeSession — GET /sessions/resume/${slug}`)
    const response = await fetch(`${getApiUrl()}/sessions/resume/${slug}`, {
      headers: {
        ...getPublicHeaders(),
        'X-Resume-Token': resumeToken,
      },
    })
    return handleResponse(response, 'resumeSession')
  },

  search: async (agentId: number, query: string, topK = 5): Promise<Result<string[]>> => {
    console.log(`[AgentService] search — GET /agents/${agentId}/search?query=${query}&topK=${topK}`)
    const params = new URLSearchParams({ query, topK: String(topK) })
    const response = await fetch(`${getApiUrl()}/agents/${agentId}/search?${params}`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'search')
  },

  setupTelegramWebhook: async (agentId: number): Promise<Result<TelegramWebhookInfo>> => {
    console.log(`[AgentService] setupTelegramWebhook — POST /telegram/${agentId}/setup-webhook`)
    const response = await fetch(`${getApiUrl()}/telegram/${agentId}/setup-webhook`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'setupTelegramWebhook')
  },

  getTelegramWebhookInfo: async (agentId: number): Promise<Result<TelegramWebhookInfo>> => {
    console.log(`[AgentService] getTelegramWebhookInfo — GET /telegram/${agentId}/webhook-info`)
    const response = await fetch(`${getApiUrl()}/telegram/${agentId}/webhook-info`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getTelegramWebhookInfo')
  },

  regenerateTelegramSecret: async (agentId: number): Promise<Result<TelegramWebhookInfo>> => {
    console.log(`[AgentService] regenerateTelegramSecret — POST /telegram/${agentId}/regenerate-secret`)
    const response = await fetch(`${getApiUrl()}/telegram/${agentId}/regenerate-secret`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'regenerateTelegramSecret')
  },

  startWhatsappSession: async (slug: string): Promise<Result<object>> => {
    console.log(`[AgentService] startWhatsappSession — POST /whatsapp/${slug}/start-session`)
    const response = await fetch(`${getApiUrl()}/whatsapp/${slug}/start-session`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'startWhatsappSession')
  },

  getWhatsappQrCode: async (slug: string): Promise<Result<WhatsappQrCodeInfo>> => {
    console.log(`[AgentService] getWhatsappQrCode — GET /whatsapp/${slug}/qrcode`)
    const response = await fetch(`${getApiUrl()}/whatsapp/${slug}/qrcode`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getWhatsappQrCode')
  },

  getWhatsappStatus: async (slug: string): Promise<Result<WhatsappStatusInfo>> => {
    console.log(`[AgentService] getWhatsappStatus — GET /whatsapp/${slug}/status`)
    const response = await fetch(`${getApiUrl()}/whatsapp/${slug}/status`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getWhatsappStatus')
  },

  disconnectWhatsapp: async (slug: string): Promise<Result<object>> => {
    console.log(`[AgentService] disconnectWhatsapp — POST /whatsapp/${slug}/disconnect`)
    const response = await fetch(`${getApiUrl()}/whatsapp/${slug}/disconnect`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'disconnectWhatsapp')
  },
}
