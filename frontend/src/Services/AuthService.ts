import type { AuthCredentials, AuthResponse } from '../types/auth'

const AUTH_STORAGE_KEY = 'avabot:auth-token'
const getApiUrl = () => import.meta.env.VITE_API_URL

export const AuthService = {
  login: async (credentials: AuthCredentials): Promise<AuthResponse> => {
    console.log('[AuthService] login — POST /auth/login')
    const response = await fetch(`${getApiUrl()}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials),
    })

    if (!response.ok) {
      let data: AuthResponse | null = null
      try { data = await response.json() } catch { /* não é JSON */ }
      console.error(`[AuthService] login — HTTP ${response.status}:`, data)
      return data ?? { sucesso: false, token: '' }
    }

    const data: AuthResponse = await response.json()
    if (data.sucesso && data.token) {
      localStorage.setItem(AUTH_STORAGE_KEY, data.token)
      console.log('[AuthService] login — OK, token armazenado')
    } else {
      console.warn('[AuthService] login — credenciais inválidas')
    }
    return data
  },

  logout: () => {
    console.log('[AuthService] logout — removendo token')
    localStorage.removeItem(AUTH_STORAGE_KEY)
  },

  getToken: (): string | null => {
    return localStorage.getItem(AUTH_STORAGE_KEY)
  },

  isAuthenticated: (): boolean => {
    return localStorage.getItem(AUTH_STORAGE_KEY) !== null
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
      localStorage.removeItem(AUTH_STORAGE_KEY)
      window.location.href = import.meta.env.VITE_SITE_BASENAME
        ? `${import.meta.env.VITE_SITE_BASENAME}/login`
        : '/login'
      return true
    }
    return false
  },
}
