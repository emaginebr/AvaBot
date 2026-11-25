import { useCallback, useEffect, useState } from 'react'
import { AgentService } from '../Services/AgentService'
import { McpAccessKeyService } from '../Services/McpAccessKeyService'
import { PowerBIService } from '../Services/PowerBIService'
import { WhatsappAccessService } from '../Services/WhatsappAccessService'
import type { AgentInfo, TelegramWebhookInfo, WhatsappStatusInfo } from '../types/agent'
import type { McpAccessKeyInfo } from '../types/mcpAccessKey'
import type { PowerBIConfigInfo, PowerBIQueryLogPageInfo } from '../types/powerbi'
import type { Result } from '../types/result'
import { WHATSAPP_REQUEST_STATUS } from '../types/whatsappAccess'
import type { WhatsappAccessConfigInfo, WhatsappAccessRequestInfo } from '../types/whatsappAccess'

/** Quantas consultas Power BI recentes entram nas médias do cartão. */
const POWERBI_SAMPLE = 20

export interface AgentDashboardData {
  agentId: number
  fetchedAt: number
  /** null = consulta falhou ou não se aplica. */
  whatsappStatus: WhatsappStatusInfo | null
  whatsappAccess: WhatsappAccessConfigInfo | null
  pendingRequests: WhatsappAccessRequestInfo[] | null
  telegram: TelegramWebhookInfo | null
  powerBIConfig: PowerBIConfigInfo | null
  powerBILogs: PowerBIQueryLogPageInfo | null
  mcpKeys: McpAccessKeyInfo[] | null
}

/** Executa a chamada e devolve os dados ou null (falha de rede ou resposta sem sucesso). */
const settle = async <T>(call: () => Promise<Result<T>>): Promise<T | null> => {
  try {
    const result = await call()
    return result.sucesso ? result.dados : null
  } catch {
    return null
  }
}

const loadAgentDashboard = async (agent: AgentInfo): Promise<AgentDashboardData> => {
  const slug = agent.slug
  const [whatsappStatus, whatsappAccess, pendingRequests, telegram, powerBIConfig, powerBILogs, mcpKeys] =
    await Promise.all([
      settle(() => AgentService.getWhatsappStatus(slug)),
      settle(() => WhatsappAccessService.getConfig(slug)),
      settle(() => WhatsappAccessService.listRequests(slug, WHATSAPP_REQUEST_STATUS.PENDING)),
      agent.telegramBotName || agent.telegramBotToken
        ? settle(() => AgentService.getTelegramWebhookInfo(agent.agentId))
        : Promise.resolve(null),
      settle(() => PowerBIService.getConfig(slug)),
      agent.powerBIEnabled ? settle(() => PowerBIService.getQueryLogs(slug, 1, POWERBI_SAMPLE)) : Promise.resolve(null),
      settle(() => McpAccessKeyService.list()),
    ])
  return {
    agentId: agent.agentId,
    fetchedAt: Date.now(),
    whatsappStatus,
    whatsappAccess,
    pendingRequests,
    telegram,
    powerBIConfig,
    powerBILogs,
    mcpKeys,
  }
}

/** Conexões, pedidos de acesso e consultas Power BI do agente ativo. */
export const useAgentDashboard = (agent: AgentInfo | null) => {
  const [reloadToken, setReloadToken] = useState(0)
  const [state, setState] = useState<{ key: string; data: AgentDashboardData } | null>(null)
  // Só os campos que mudam as consultas entram na chave (id, Telegram, Power BI) + recarga manual.
  const key = agent
    ? `${agent.agentId}|${agent.slug}|${!!(agent.telegramBotName || agent.telegramBotToken)}|${agent.powerBIEnabled}|${reloadToken}`
    : `none|${reloadToken}`

  useEffect(() => {
    if (!agent) return
    let alive = true
    loadAgentDashboard(agent).then((data) => {
      if (alive) setState({ key, data })
    })
    return () => {
      alive = false
    }
    // O agente é identificado pela chave; outros campos (nome, prompt...) não refazem a consulta.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key])

  const reload = useCallback(() => setReloadToken((t) => t + 1), [])
  const current = state && agent && state.data.agentId === agent.agentId ? state.data : null

  return {
    data: current,
    loading: !!agent && (!state || state.key !== key),
    reload,
  }
}
