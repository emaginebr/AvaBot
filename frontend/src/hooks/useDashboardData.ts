import { useCallback, useEffect, useState } from 'react'
import { ChatHistoryService } from '../Services/ChatHistoryService'
import { KnowledgeFileService } from '../Services/KnowledgeFileService'
import { useAgentStore } from '../stores/useAgentStore'
import type { AgentInfo } from '../types/agent'
import type { ChatSessionInfo } from '../types/chatSession'
import type { KnowledgeFileInfo } from '../types/knowledgeFile'

/** Página máxima aceita pela API de sessões (SessionQueryService.MaxSessionPageSize). */
const SESSION_PAGE_SIZE = 100
/** Limite de páginas por agente, para o painel não varrer históricos enormes. */
const MAX_SESSION_PAGES = 3
/** Janela coberta: 30 dias do maior período + 30 dias anteriores para a comparação. */
export const DASHBOARD_WINDOW_DAYS = 60

export interface AgentSessionStats {
  agent: AgentInfo
  /** Total de sessões do agente (todas as datas), informado pela API. */
  totalSessions: number
  /** Sessões mais recentes carregadas (ordem decrescente de início). */
  sessions: ChatSessionInfo[]
  /** Início da sessão mais antiga carregada; tudo depois dele está completo. */
  coveredSince: number | null
  /** true quando a janela de 60 dias (ou todo o histórico) foi carregada. */
  complete: boolean
  failed: boolean
}

export interface DashboardData {
  /** Momento da consulta: referência para períodos e tempos relativos. */
  fetchedAt: number
  agentStats: AgentSessionStats[]
  files: KnowledgeFileInfo[]
  filesFailed: boolean
}

const toTime = (value: string) => new Date(/[zZ]$|[+-]\d{2}:?\d{2}$/.test(value) ? value : `${value}Z`).getTime()

const loadAgentSessions = async (agent: AgentInfo, cutoff: number): Promise<AgentSessionStats> => {
  const sessions: ChatSessionInfo[] = []
  let total = 0
  let complete = false
  try {
    for (let page = 1; page <= MAX_SESSION_PAGES; page++) {
      const result = await ChatHistoryService.getSessions(agent.agentId, page, SESSION_PAGE_SIZE)
      if (!result.sucesso) throw new Error(result.mensagem)
      const items = result.dados?.items ?? []
      total = result.dados?.total ?? 0
      sessions.push(...items)
      const oldest = items.at(-1)
      if (items.length < SESSION_PAGE_SIZE || sessions.length >= total || (oldest && toTime(oldest.startedAt) < cutoff)) {
        complete = true
        break
      }
    }
  } catch (err) {
    console.warn(`[useDashboardData] sessões do agente ${agent.agentId} indisponíveis:`, err)
    return { agent, totalSessions: 0, sessions: [], coveredSince: null, complete: false, failed: true }
  }
  const oldest = sessions.at(-1)
  return {
    agent,
    totalSessions: total,
    sessions,
    coveredSince: oldest ? toTime(oldest.startedAt) : null,
    complete,
    failed: false,
  }
}

const loadAgentFiles = async (agent: AgentInfo): Promise<KnowledgeFileInfo[] | null> => {
  try {
    const result = await KnowledgeFileService.getByAgent(agent.agentId)
    return result.sucesso ? result.dados ?? [] : null
  } catch {
    return null
  }
}

const loadDashboard = async (agents: AgentInfo[]): Promise<DashboardData> => {
  const fetchedAt = Date.now()
  const cutoff = fetchedAt - DASHBOARD_WINDOW_DAYS * 86_400_000
  const [agentStats, fileLists] = await Promise.all([
    Promise.all(agents.map((agent) => loadAgentSessions(agent, cutoff))),
    Promise.all(agents.map(loadAgentFiles)),
  ])
  return {
    fetchedAt,
    agentStats,
    files: fileLists.flatMap((list) => list ?? []),
    filesFailed: fileLists.some((list) => list === null),
  }
}

/**
 * Dados agregados de todos os agentes do usuário para o dashboard: sessões recentes
 * (até 60 dias, paginadas) e arquivos da base de conhecimento. Não há endpoint de
 * métricas: tudo é derivado das listagens existentes.
 */
export const useDashboardData = () => {
  const loadAgents = useAgentStore((state) => state.loadAgents)
  const agentsError = useAgentStore((state) => state.error)
  const agents = useAgentStore((state) => state.agents)
  const [agentsReady, setAgentsReady] = useState(false)
  const [reloadToken, setReloadToken] = useState(0)
  const [state, setState] = useState<{ key: string; data: DashboardData } | null>(null)

  // Lista de agentes atualizada ao abrir o painel (como a página anterior fazia).
  useEffect(() => {
    let alive = true
    loadAgents().finally(() => {
      if (alive) setAgentsReady(true)
    })
    return () => {
      alive = false
    }
  }, [loadAgents])

  const agentKey = agents.map((a) => a.agentId).join(',')
  const key = `${agentsReady}|${agentKey}|${reloadToken}`

  useEffect(() => {
    if (!agentsReady) return
    let alive = true
    loadDashboard(useAgentStore.getState().agents).then((data) => {
      if (alive) setState({ key, data })
    })
    return () => {
      alive = false
    }
  }, [agentsReady, key])

  /** Recarrega a lista de agentes e, em seguida, sessões e arquivos. */
  const reload = useCallback(() => {
    loadAgents().finally(() => setReloadToken((t) => t + 1))
  }, [loadAgents])

  return {
    agents,
    agentsError,
    data: state?.data ?? null,
    loading: !state || state.key !== key,
    reload,
  }
}
