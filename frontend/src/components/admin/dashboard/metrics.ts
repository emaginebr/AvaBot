import type { DashboardData } from '../../../hooks/useDashboardData'
import type { AgentRankingItem } from './AgentRanking'
import type { ChartDay } from './ChannelChart'
import type { RecentSessionRow } from './RecentSessionsTable'
import { dayStart, parseApiDate, plural, startOfDay } from './format'

const DAY_MS = 86_400_000

export interface DashboardMetrics {
  days: ChartDay[]
  dailySessions: number[]
  dailyMessages: number[]
  sessions: number
  messages: number
  openSessions: number
  /** Variação vs. período anterior; null quando não há base confiável. */
  sessionsDelta: number | null
  messagesDelta: number | null
  /** A janela atual não foi coberta por inteiro (histórico grande demais para o painel). */
  partial: boolean
  ranking: AgentRankingItem[]
  recent: RecentSessionRow[]
  failedAgents: number
}

const delta = (current: number, previous: number, covered: boolean) =>
  covered && previous > 0 ? (current - previous) / previous : null

/**
 * Deriva os números do dashboard das sessões carregadas: contagens por dia, totais,
 * comparação com o período anterior, ranking de agentes e sessões recentes.
 */
export const computeMetrics = (data: DashboardData, period: number, recentLimit = 6): DashboardMetrics => {
  const now = data.fetchedAt
  const currentStart = dayStart(now, period - 1)
  const previousStart = dayStart(now, period * 2 - 1)
  const loaded = data.agentStats.filter((s) => !s.failed)

  const covers = (since: number) =>
    loaded.every((s) => s.complete || s.coveredSince === null || s.coveredSince <= since)

  const dailySessions = Array.from({ length: period }, () => 0)
  const dailyMessages = Array.from({ length: period }, () => 0)
  let previousSessions = 0
  let previousMessages = 0
  let openSessions = 0
  const ranking: AgentRankingItem[] = []
  const recent: RecentSessionRow[] = []

  for (const stats of loaded) {
    let agentSessions = 0
    let agentMessages = 0
    for (const session of stats.sessions) {
      const started = parseApiDate(session.startedAt).getTime()
      const messages = session.messageCount ?? 0
      recent.push({ session, agentName: stats.agent.name })
      if (started >= currentStart) {
        const index = Math.round((startOfDay(started) - currentStart) / DAY_MS)
        if (index >= 0 && index < period) {
          dailySessions[index]++
          dailyMessages[index] += messages
        }
        agentSessions++
        agentMessages += messages
        if (!session.endedAt) openSessions++
      } else if (started >= previousStart) {
        previousSessions++
        previousMessages += messages
      }
    }
    ranking.push({
      id: stats.agent.agentId,
      name: stats.agent.name,
      detail: plural(agentMessages, 'mensagem', 'mensagens'),
      value: agentSessions,
    })
  }

  const sessions = dailySessions.reduce((a, b) => a + b, 0)
  const messages = dailyMessages.reduce((a, b) => a + b, 0)
  const previousCovered = covers(previousStart)

  recent.sort((a, b) => parseApiDate(b.session.startedAt).getTime() - parseApiDate(a.session.startedAt).getTime())
  ranking.sort((a, b) => b.value - a.value)

  return {
    days: dailySessions.map((count, i) => ({ date: new Date(dayStart(now, period - 1 - i)), values: { sessions: count } })),
    dailySessions,
    dailyMessages,
    sessions,
    messages,
    openSessions,
    sessionsDelta: delta(sessions, previousSessions, previousCovered),
    messagesDelta: delta(messages, previousMessages, previousCovered),
    partial: !covers(currentStart),
    ranking: ranking.filter((r) => r.value > 0).slice(0, 5),
    recent: recent.slice(0, recentLimit),
    failedAgents: data.agentStats.length - loaded.length,
  }
}
