import type { AuthCredentials, AuthResultInfo, PasswordChangeInfo, RegisterInfo, UserInfo } from '../types/auth'
import type { Result } from '../types/result'

const AUTH_STORAGE_KEY = 'avabot:auth-token'
const USER_STORAGE_KEY = 'avabot:auth-user'
const getApiUrl = () => import.meta.env.VITE_API_URL

const loginPath = (expired: boolean) => {
  const base = import.meta.env.VITE_SITE_BASENAME ? `${import.meta.env.VITE_SITE_BASENAME}/login` : '/login'
  return expired ? `${base}?expired=1` : base
}

const clearStorage = () => {
  localStorage.removeItem(AUTH_STORAGE_KEY)
  localStorage.removeItem(USER_STORAGE_KEY)
}

const storeAuth = (data: AuthResultInfo) => {
  localStorage.setItem(AUTH_STORAGE_KEY, data.token)
  localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(data.user))
}

// Le o "exp" do payload do JWT sem biblioteca (research R7). Null quando ilegivel.
const readTokenExpiration = (token: string): number | null => {
  try {
    const payload = token.split('.')[1]
    if (!payload) return null
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4)
    const decoded = JSON.parse(atob(padded)) as { exp?: number }
    return typeof decoded.exp === 'number' ? decoded.exp : null
  } catch {
    return null
  }
}

const failure = <T>(mensagem: string, erros: string[] = []): Result<T> =>
  ({ sucesso: false, mensagem, erros, dados: null as T })

// Respostas da API de auth: nunca registrar o corpo (pode trazer token) nem senhas no console.
const parseResult = async <T>(response: Response, action: string): Promise<Result<T>> => {
  let data: Result<T> | null = null
  try { data = await response.json() } catch { /* nao e JSON */ }

  if (!response.ok) {
    const mensagem = data?.mensagem || `Erro ${response.status}: ${response.statusText}`
    console.warn(`[AuthService] ${action} — HTTP ${response.status}: ${mensagem}`)
    return failure<T>(mensagem, data?.erros ?? [])
  }

  if (!data) return failure<T>('Resposta invalida do servidor')
  console.log(`[AuthService] ${action} — OK`)
  return data
}

const authenticatedResult = async <T>(response: Response, action: string): Promise<Result<T>> => {
  if (AuthService.handleUnauthorized(response)) {
    console.warn(`[AuthService] ${action} — 401, redirecionando para login`)
    return failure<T>('Sessão expirada')
  }
  return parseResult<T>(response, action)
}

export const AuthService = {
  login: async (credentials: AuthCredentials): Promise<Result<AuthResultInfo>> => {
    console.log('[AuthService] login — POST /auth/login')
    const response = await fetch(`${getApiUrl()}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials),
    })

    const result = await parseResult<AuthResultInfo>(response, 'login')
    if (result.sucesso && result.dados?.token) storeAuth(result.dados)
    return result
  },

  register: async (info: RegisterInfo): Promise<Result<AuthResultInfo>> => {
    console.log('[AuthService] register — POST /auth/register')
    const response = await fetch(`${getApiUrl()}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(info),
    })

    const result = await parseResult<AuthResultInfo>(response, 'register')
    if (result.sucesso && result.dados?.token) storeAuth(result.dados)
    return result
  },

  me: async (): Promise<Result<UserInfo>> => {
    console.log('[AuthService] me — GET /auth/me')
    const response = await fetch(`${getApiUrl()}/auth/me`, { headers: AuthService.getAuthHeaders() })
    const result = await authenticatedResult<UserInfo>(response, 'me')
    if (result.sucesso && result.dados) localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(result.dados))
    return result
  },

  updateMe: async (name: string): Promise<Result<UserInfo>> => {
    console.log('[AuthService] updateMe — PUT /auth/me')
    const response = await fetch(`${getApiUrl()}/auth/me`, {
      method: 'PUT',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify({ name }),
    })
    const result = await authenticatedResult<UserInfo>(response, 'updateMe')
    if (result.sucesso && result.dados) localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(result.dados))
    return result
  },

  changePassword: async (info: PasswordChangeInfo): Promise<Result<null>> => {
    console.log('[AuthService] changePassword — PUT /auth/me/password')
    const response = await fetch(`${getApiUrl()}/auth/me/password`, {
      method: 'PUT',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify(info),
    })
    return authenticatedResult<null>(response, 'changePassword')
  },

  logout: () => {
    console.log('[AuthService] logout — removendo token e usuário')
    clearStorage()
  },

  getToken: (): string | null => {
    return localStorage.getItem(AUTH_STORAGE_KEY)
  },

  getUser: (): UserInfo | null => {
    const raw = localStorage.getItem(USER_STORAGE_KEY)
    if (!raw) return null
    try {
      return JSON.parse(raw) as UserInfo
    } catch {
      return null
    }
  },

  // Token vencido ou ilegivel conta como ausente: o painel volta ao login sem gastar uma chamada (US3).
  isAuthenticated: (): boolean => {
    const token = localStorage.getItem(AUTH_STORAGE_KEY)
    if (!token) return false

    const exp = readTokenExpiration(token)
    if (exp === null || exp * 1000 <= Date.now()) {
      console.warn('[AuthService] isAuthenticated — token expirado ou inválido, limpando')
      clearStorage()
      return false
    }
    return true
  },

  getAuthHeaders: (): HeadersInit => {
    const token = localStorage.getItem(AUTH_STORAGE_KEY)
    return {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    }
  },

  getAuthHeadersWithoutContentType: (): HeadersInit => {
    const token = localStorage.getItem(AUTH_STORAGE_KEY)
    return token ? { Authorization: `Bearer ${token}` } : {}
  },

  handleUnauthorized: (response: Response): boolean => {
    if (response.status === 401) {
      clearStorage()
      window.location.href = loginPath(true)
      return true
    }
    return false
  },
}
