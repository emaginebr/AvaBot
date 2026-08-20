import { create } from 'zustand'
import { AuthService } from '../Services/AuthService'
import type { AuthCredentials } from '../types/auth'

interface AuthStoreState {
  token: string | null
  isAuthenticated: boolean
  login: (credentials: AuthCredentials) => Promise<boolean>
  logout: () => void
  getAuthHeaders: () => HeadersInit
}

export const useAuthStore = create<AuthStoreState>((set) => ({
  token: AuthService.getToken(),
  isAuthenticated: AuthService.isAuthenticated(),

  login: async (credentials: AuthCredentials): Promise<boolean> => {
    const result = await AuthService.login(credentials)
    if (result.sucesso && result.token) {
      set({ token: result.token, isAuthenticated: true })
      return true
    }
    return false
  },

  logout: () => {
    AuthService.logout()
    set({ token: null, isAuthenticated: false })
  },

  getAuthHeaders: () => AuthService.getAuthHeaders(),
}))
