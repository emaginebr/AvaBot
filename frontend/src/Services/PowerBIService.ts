import type {
  PowerBIConfigInfo,
  PowerBIConfigUpdateInfo,
  PowerBIConnectionTestInfo,
  PowerBIWorkspaceInfo,
  PowerBIDatasetInfo,
  PowerBIDatasetInsertInfo,
  PowerBIDatasetSchemaInfo,
  PowerBISchemaDescriptionUpdateInfo,
  PowerBIQueryLogPageInfo,
} from '../types/powerbi'
import type { Result } from '../types/result'
import { AuthService } from './AuthService'

const getApiUrl = () => import.meta.env.VITE_API_URL

const handleResponse = async <T>(response: Response, action: string): Promise<Result<T>> => {
  if (AuthService.handleUnauthorized(response)) {
    console.warn(`[PowerBIService] ${action} — 401 Unauthorized, redirecionando para login`)
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
    console.error(`[PowerBIService] ${action} — HTTP ${response.status}:`, mensagem, errorBody?.erros)
    return { sucesso: false, mensagem, erros: errorBody?.erros || [], dados: null as T }
  }

  const data: Result<T> = await response.json()
  if (!data.sucesso) {
    console.warn(`[PowerBIService] ${action} — API retornou erro:`, data.mensagem, data.erros)
  } else {
    console.log(`[PowerBIService] ${action} — OK`)
  }
  return data
}

export const PowerBIService = {
  getConfig: async (slug: string): Promise<Result<PowerBIConfigInfo>> => {
    console.log(`[PowerBIService] getConfig — GET /powerbi/${slug}/config`)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/config`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getConfig')
  },

  saveConfig: async (slug: string, data: PowerBIConfigUpdateInfo): Promise<Result<PowerBIConfigInfo>> => {
    console.log(`[PowerBIService] saveConfig — PUT /powerbi/${slug}/config`, data)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/config`, {
      method: 'PUT',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify(data),
    })
    return handleResponse(response, 'saveConfig')
  },

  testConnection: async (slug: string): Promise<Result<PowerBIConnectionTestInfo>> => {
    console.log(`[PowerBIService] testConnection — POST /powerbi/${slug}/test`)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/test`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'testConnection')
  },

  setEnabled: async (slug: string, enabled: boolean): Promise<Result<PowerBIConfigInfo>> => {
    console.log(`[PowerBIService] setEnabled — PUT /powerbi/${slug}/enabled`, { enabled })
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/enabled`, {
      method: 'PUT',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify({ enabled }),
    })
    return handleResponse(response, 'setEnabled')
  },

  getWorkspaces: async (slug: string): Promise<Result<PowerBIWorkspaceInfo[]>> => {
    console.log(`[PowerBIService] getWorkspaces — GET /powerbi/${slug}/workspaces`)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/workspaces`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getWorkspaces')
  },

  getDatasets: async (slug: string): Promise<Result<PowerBIDatasetInfo[]>> => {
    console.log(`[PowerBIService] getDatasets — GET /powerbi/${slug}/datasets`)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/datasets`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getDatasets')
  },

  createDataset: async (slug: string, data: PowerBIDatasetInsertInfo): Promise<Result<PowerBIDatasetInfo>> => {
    console.log(`[PowerBIService] createDataset — POST /powerbi/${slug}/datasets`, data)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/datasets`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify(data),
    })
    return handleResponse(response, 'createDataset')
  },

  updateDataset: async (slug: string, id: number, data: PowerBIDatasetInsertInfo): Promise<Result<PowerBIDatasetInfo>> => {
    console.log(`[PowerBIService] updateDataset — PUT /powerbi/${slug}/datasets/${id}`, data)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/datasets/${id}`, {
      method: 'PUT',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify(data),
    })
    return handleResponse(response, 'updateDataset')
  },

  deleteDataset: async (slug: string, id: number): Promise<Result<boolean>> => {
    console.log(`[PowerBIService] deleteDataset — DELETE /powerbi/${slug}/datasets/${id}`)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/datasets/${id}`, {
      method: 'DELETE',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'deleteDataset')
  },

  generateSchema: async (slug: string, id: number): Promise<Result<PowerBIDatasetSchemaInfo>> => {
    console.log(`[PowerBIService] generateSchema — POST /powerbi/${slug}/datasets/${id}/schema/generate`)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/datasets/${id}/schema/generate`, {
      method: 'POST',
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'generateSchema')
  },

  getSchema: async (slug: string, id: number): Promise<Result<PowerBIDatasetSchemaInfo>> => {
    console.log(`[PowerBIService] getSchema — GET /powerbi/${slug}/datasets/${id}/schema`)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/datasets/${id}/schema`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getSchema')
  },

  updateSchemaDescriptions: async (slug: string, id: number, data: PowerBISchemaDescriptionUpdateInfo): Promise<Result<PowerBIDatasetSchemaInfo>> => {
    console.log(`[PowerBIService] updateSchemaDescriptions — PUT /powerbi/${slug}/datasets/${id}/schema/descriptions`, data)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/datasets/${id}/schema/descriptions`, {
      method: 'PUT',
      headers: AuthService.getAuthHeaders(),
      body: JSON.stringify(data),
    })
    return handleResponse(response, 'updateSchemaDescriptions')
  },

  getQueryLogs: async (slug: string, page = 1, pageSize = 20): Promise<Result<PowerBIQueryLogPageInfo>> => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    console.log(`[PowerBIService] getQueryLogs — GET /powerbi/${slug}/query-logs?page=${page}&pageSize=${pageSize}`)
    const response = await fetch(`${getApiUrl()}/powerbi/${slug}/query-logs?${params}`, {
      headers: AuthService.getAuthHeaders(),
    })
    return handleResponse(response, 'getQueryLogs')
  },
}
