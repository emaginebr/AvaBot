import { create } from 'zustand'
import { McpAccessKeyService } from '../Services/McpAccessKeyService'
import type { McpAccessKeyCreatedInfo, McpAccessKeyInfo } from '../types/mcpAccessKey'
import type { Result } from '../types/result'

interface McpAccessKeyStoreState {
  keys: McpAccessKeyInfo[]
  loading: boolean
  error: string | null
  loadKeys: () => Promise<void>
  /** Devolve o Result com a chave em claro ao chamador; o valor não fica no estado. */
  createKey: (name: string) => Promise<Result<McpAccessKeyCreatedInfo>>
  revokeKey: (id: number) => Promise<Result<McpAccessKeyInfo>>
}

export const useMcpAccessKeyStore = create<McpAccessKeyStoreState>((set) => ({
  keys: [],
  loading: false,
  error: null,

  loadKeys: async () => {
    set({ loading: true, error: null })
    try {
      const result = await McpAccessKeyService.list()
      if (result.sucesso) set({ keys: result.dados ?? [], loading: false })
      else set({ error: result.mensagem || 'Não foi possível carregar as chaves', loading: false })
    } catch {
      set({ error: 'Erro ao conectar com o servidor', loading: false })
    }
  },

  createKey: async (name) => {
    const result = await McpAccessKeyService.create(name)
    if (result.sucesso && result.dados) {
      const created = result.dados.accessKey
      set((state) => ({ keys: [created, ...state.keys] }))
    }
    return result
  },

  revokeKey: async (id) => {
    const result = await McpAccessKeyService.revoke(id)
    if (result.sucesso && result.dados) {
      const revoked = result.dados
      set((state) => ({ keys: state.keys.map((k) => (k.mcpAccessKeyId === id ? revoked : k)) }))
    }
    return result
  },
}))
