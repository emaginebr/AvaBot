import { AuthService } from './AuthService'
import type { McpAccessKeyCreatedInfo, McpAccessKeyInfo } from '../types/mcpAccessKey'
import type { Result } from '../types/result'

const getApiUrl = () => import.meta.env.VITE_API_URL

const failure = <T>(mensagem: string, erros: string[] = []): Result<T> =>
  ({ sucesso: false, mensagem, erros, dados: null as T })

// Nunca registrar o corpo das respostas: o POST traz a chave em claro (mesma regra das senhas).
const parseResult = async <T>(response: Response, action: string): Promise<Result<T>> => {
  if (AuthService.handleUnauthorized(response)) {
    console.warn(`[McpAccessKeyService] ${action} — 401, redirecionando para login`)
    return failure<T>('Sessão expirada')
  }

  let data: Result<T> | null = null
  try { data = await response.json() } catch { /* não é JSON */ }

  if (!response.ok) {
    const mensagem = data?.mensagem || `Erro ${response.status}: ${response.statusText}`
    console.warn(`[McpAccessKeyService] ${action} — HTTP ${response.status}: ${mensagem}`)
    return failure<T>(mensagem, data?.erros ?? [])
  }

  if (!data) return failure<T>('Resposta invalida do servidor')
  console.log(`[McpAccessKeyService] ${action} — OK`)
  return data
}

export const McpAccessKeyService = {
  /** Endereço do servidor MCP mostrado ao usuário (deriva de VITE_API_URL). */
  getServerUrl: (): string => `${getApiUrl()}/mcp`,

  list: async (): Promise<Result<McpAccessKeyInfo[]>> => {
    const response = await fetch(`${getApiUrl()}/auth/me/mcp-keys`, {
      headers: AuthService.getAuthHeaders(),
    })
    return parseResult<McpAccessKeyInfo[]>(response, 'list')
  },

  create: async (name: string): Promise<Result<McpAccessKeyCreatedInfo>> => {
    const response = await fetch(`${getApiUrl()}/auth/me/mcp-keys`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify({ name }),
    })
    return parseResult<McpAccessKeyCreatedInfo>(response, 'create')
  },

  revoke: async (id: number): Promise<Result<McpAccessKeyInfo>> => {
    const response = await fetch(`${getApiUrl()}/auth/me/mcp-keys/${id}`, {
      method: 'DELETE',
      headers: AuthService.getAuthHeaders(),
    })
    return parseResult<McpAccessKeyInfo>(response, 'revoke')
  },
}
