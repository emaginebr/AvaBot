import { create } from 'zustand'
import { AuthService } from '../Services/AuthService'
import type { AuthCredentials, PasswordChangeInfo, RegisterInfo, UserInfo } from '../types/auth'
import type { Result } from '../types/result'

interface AuthStoreState {
  token: string | null
  user: UserInfo | null
  isAuthenticated: boolean
  login: (credentials: AuthCredentials) => Promise<Result<unknown>>
  register: (info: RegisterInfo) => Promise<Result<unknown>>
  logout: () => void
  refreshUser: () => Promise<void>
  updateName: (name: string) => Promise<Result<UserInfo>>
  changePassword: (info: PasswordChangeInfo) => Promise<Result<null>>
  getAuthHeaders: () => HeadersInit
}

export const useAuthStore = create<AuthStoreState>((set) => ({
  token: AuthService.getToken(),
  user: AuthService.getUser(),
  isAuthenticated: AuthService.isAuthenticated(),

  login: async (credentials) => {
    const result = await AuthService.login(credentials)
    if (result.sucesso && result.dados?.token) {
      set({ token: result.dados.token, user: result.dados.user, isAuthenticated: true })
    }
    return result
  },

  register: async (info) => {
    const result = await AuthService.register(info)
    if (result.sucesso && result.dados?.token) {
      set({ token: result.dados.token, user: result.dados.user, isAuthenticated: true })
    }
    return result
  },

  logout: () => {
    AuthService.logout()
    set({ token: null, user: null, isAuthenticated: false })
  },

  refreshUser: async () => {
    const result = await AuthService.me()
    if (result.sucesso && result.dados) set({ user: result.dados })
  },

  updateName: async (name) => {
    const result = await AuthService.updateMe(name)
    if (result.sucesso && result.dados) set({ user: result.dados })
    return result
  },

  changePassword: (info) => AuthService.changePassword(info),

  getAuthHeaders: () => AuthService.getAuthHeaders(),
}))
