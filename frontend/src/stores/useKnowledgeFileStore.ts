import { create } from 'zustand'
import type { KnowledgeFileInfo } from '../types/knowledgeFile'
import { KnowledgeFileService } from '../Services/KnowledgeFileService'

interface KnowledgeFileStoreState {
  files: KnowledgeFileInfo[]
  loading: boolean
  error: string | null
  loadFiles: (agentId: number) => Promise<void>
  clearError: () => void
}

export const useKnowledgeFileStore = create<KnowledgeFileStoreState>((set) => ({
  files: [],
  loading: false,
  error: null,

  loadFiles: async (agentId: number) => {
    set({ loading: true, error: null })
    try {
      const result = await KnowledgeFileService.getByAgent(agentId)
      if (result.sucesso) {
        set({ files: result.dados, loading: false })
      } else {
        set({ error: result.mensagem, loading: false })
      }
    } catch {
      set({ error: 'Erro ao carregar arquivos', loading: false })
    }
  },

  clearError: () => set({ error: null }),
}))
