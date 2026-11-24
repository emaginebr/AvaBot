import { AuthService } from './AuthService'
import type {
  WhatsappAccessConfigInfo,
  WhatsappAccessConfigUpdateInfo,
  WhatsappAccessRequestInfo,
  WhatsappContactInfo,
  WhatsappContactInsertInfo,
} from '../types/whatsappAccess'
import type { Result } from '../types/result'

const getApiUrl = () => import.meta.env.VITE_API_URL

const failure = <T>(mensagem: string, erros: string[] = []): Result<T> =>
  ({ sucesso: false, mensagem, erros, dados: null as T })

const parseResult = async <T>(response: Response, action: string): Promise<Result<T>> => {
  if (AuthService.handleUnauthorized(response)) {
    console.warn(`[WhatsappAccessService] ${action} — 401, redirecionando para login`)
    return failure<T>('Sessão expirada')
  }

  let data: Result<T> | null = null
  try { data = await response.json() } catch { /* não é JSON */ }

  if (!response.ok) {
    const mensagem = data?.mensagem || `Erro ${response.status}: ${response.statusText}`
    console.warn(`[WhatsappAccessService] ${action} — HTTP ${response.status}: ${mensagem}`)
    return failure<T>(mensagem, data?.erros ?? [])
  }

  if (!data) return failure<T>('Resposta inválida do servidor')
  console.log(`[WhatsappAccessService] ${action} — OK`)
  return data
}

const base = (slug: string) => `${getApiUrl()}/whatsapp/${encodeURIComponent(slug)}`

const send = async <T>(url: string, action: string, method = 'GET', body?: unknown): Promise<Result<T>> => {
  try {
    const response = await fetch(url, {
      method,
      headers: AuthService.getAuthHeaders(),
      body: body === undefined ? undefined : JSON.stringify(body),
    })
    return parseResult<T>(response, action)
  } catch (err) {
    console.error(`[WhatsappAccessService] ${action} — exceção:`, err)
    return failure<T>('Erro ao conectar com o servidor')
  }
}

export const WhatsappAccessService = {
  getConfig: (slug: string) =>
    send<WhatsappAccessConfigInfo>(`${base(slug)}/access`, 'getConfig'),

  updateConfig: (slug: string, info: WhatsappAccessConfigUpdateInfo) =>
    send<WhatsappAccessConfigInfo>(`${base(slug)}/access`, 'updateConfig', 'PUT', info),

  listContacts: (slug: string) =>
    send<WhatsappContactInfo[]>(`${base(slug)}/contacts`, 'listContacts'),

  addContact: (slug: string, info: WhatsappContactInsertInfo) =>
    send<WhatsappContactInfo>(`${base(slug)}/contacts`, 'addContact', 'POST', info),

  updateContact: (slug: string, id: number, info: WhatsappContactInsertInfo) =>
    send<WhatsappContactInfo>(`${base(slug)}/contacts/${id}`, 'updateContact', 'PUT', info),

  removeContact: (slug: string, id: number) =>
    send<boolean>(`${base(slug)}/contacts/${id}`, 'removeContact', 'DELETE'),

  listRequests: (slug: string, status?: number) =>
    send<WhatsappAccessRequestInfo[]>(
      `${base(slug)}/access-requests${status ? `?status=${status}` : ''}`,
      'listRequests',
    ),

  approveRequest: (slug: string, id: number, name?: string) =>
    send<WhatsappContactInfo>(`${base(slug)}/access-requests/${id}/approve`, 'approveRequest', 'POST', { name: name?.trim() || null }),

  rejectRequest: (slug: string, id: number) =>
    send<WhatsappAccessRequestInfo>(`${base(slug)}/access-requests/${id}/reject`, 'rejectRequest', 'POST'),
}
