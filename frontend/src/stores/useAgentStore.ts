import { create } from 'zustand'
import type { AgentInfo } from '../types/agent'
import { AgentService } from '../Services/AgentService'

const SELECTED_AGENT_KEY = 'avabot:selected-agent'

const loadSelectedAgent = (): AgentInfo | null => {
  try {
    const stored = localStorage.getItem(SELECTED_AGENT_KEY)
    return stored ? JSON.parse(stored) : null
  } catch {
    return null
  }
}

interface AgentStoreState {
  agents: AgentInfo[]
  selectedAgent: AgentInfo | null
  loading: boolean
  error: string | null
  loadAgents: () => Promise<void>
  selectAgent: (agent: AgentInfo | null) => void
  clearError: () => void
}

export const useAgentStore = create<AgentStoreState>((set) => ({
  agents: [],
  selectedAgent: loadSelectedAgent(),
  loading: false,
  error: null,

  loadAgents: async () => {
    console.log('[useAgentStore] loadAgents — carregando...')
    set({ loading: true, error: null })
    try {
      const result = await AgentService.getAll()
      if (result.sucesso) {
        console.log(`[useAgentStore] loadAgents — ${result.dados.length} agentes carregados`)
        set({ agents: result.dados, loading: false })
      } else {
        console.error('[useAgentStore] loadAgents — erro:', result.mensagem)
        set({ error: result.mensagem, loading: false })
      }
    } catch (err) {
      console.error('[useAgentStore] loadAgents — exceção:', err)
      set({ error: 'Erro ao carregar agentes', loading: false })
    }
  },

  selectAgent: (agent: AgentInfo | null) => {
    console.log('[useAgentStore] selectAgent:', agent ? agent.name : 'null')
    if (agent) {
      localStorage.setItem(SELECTED_AGENT_KEY, JSON.stringify(agent))
    } else {
      localStorage.removeItem(SELECTED_AGENT_KEY)
    }
    set({ selectedAgent: agent })
  },

  clearError: () => set({ error: null }),
}))
