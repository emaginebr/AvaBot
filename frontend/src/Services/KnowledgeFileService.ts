import type { KnowledgeFileInfo } from '../types/knowledgeFile'
import type { Result } from '../types/result'
import { AuthService } from './AuthService'

const getApiUrl = () => import.meta.env.VITE_API_URL

const handleResponse = async <T>(response: Response, action: string): Promise<Result<T>> => {
  if (AuthService.handleUnauthorized(response)) {
    console.warn(`[KnowledgeFileService] ${action} — 401 Unauthorized, redirecionando para login`)
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
    console.error(`[KnowledgeFileService] ${action} — HTTP ${response.status}:`, mensagem, errorBody?.erros)
    return { sucesso: false, mensagem, erros: errorBody?.erros || [], dados: null as T }
  }

  const data: Result<T> = await response.json()
  if (!data.sucesso) {
    console.warn(`[KnowledgeFileService] ${action} — API retornou erro:`, data.mensagem, data.erros)
  } else {
    console.log(`[KnowledgeFileService] ${action} — OK`)
  }
  return data
}

export const KnowledgeFileService = {
  getByAgent: async (agentId: number): Promise<Result<KnowledgeFileInfo[]>> => {
    console.log(`[KnowledgeFileService] getByAgent — GET /files/${agentId}`)
    const response = await fetch(`${getApiUrl()}/files/${agentId}`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getByAgent')
  },

  upload: async (agentId: number, file: File): Promise<Result<KnowledgeFileInfo>> => {
    console.log(`[KnowledgeFileService] upload — POST /files/${agentId} (${file.name}, ${file.size} bytes)`)
    const formData = new FormData()
    formData.append('file', file)
    const response = await fetch(`${getApiUrl()}/files/${agentId}`, {
      method: 'POST',
      headers: AuthService.getAuthHeadersWithoutContentType(),
      body: formData,
    })
    return handleResponse(response, 'upload')
  },

  delete: async (agentId: number, fileId: number): Promise<Result<null>> => {
    console.log(`[KnowledgeFileService] delete — DELETE /files/${agentId}/${fileId}`)
    const response = await fetch(`${getApiUrl()}/files/${agentId}/${fileId}`, {
      method: 'DELETE',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'delete')
  },

  reprocess: async (agentId: number, fileId: number): Promise<Result<null>> => {
    console.log(`[KnowledgeFileService] reprocess — POST /files/${agentId}/${fileId}/reprocess`)
    const response = await fetch(`${getApiUrl()}/files/${agentId}/${fileId}/reprocess`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'reprocess')
  },
}
