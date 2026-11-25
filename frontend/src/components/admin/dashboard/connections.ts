import type { AgentDashboardData } from '../../../hooks/useAgentDashboard'
import type { AgentInfo } from '../../../types/agent'
import { WHATSAPP_ACCESS_MODE } from '../../../types/whatsappAccess'
import type { ConnectionItem } from './ConnectionStatusList'
import { fmtRelative, parseApiDate, plural } from './format'

/** Estados do WPP Connect em que o pareamento ainda está em curso (os mesmos da WhatsappPage). */
const WHATSAPP_PAIRING = ['INITIALIZING', 'QRCODE', 'PHONECODE', 'CLOSING']

const whatsappItem = (data: AgentDashboardData): ConnectionItem => {
  const access = data.whatsappAccess
  const accessText = access
    ? access.mode === WHATSAPP_ACCESS_MODE.RESTRICTED
      ? `somente contatos autorizados (${plural(access.contactCount, 'contato', 'contatos')})`
      : 'aberto a todos os contatos'
    : null
  const base = { key: 'whatsapp', label: 'WhatsApp', icon: 'whatsapp' as const }
  const status = data.whatsappStatus
  if (!status) {
    return { ...base, tone: 'idle', status: 'Indisponível', description: 'Não foi possível consultar a sessão' }
  }
  if (status.isConnected) {
    return { ...base, tone: 'ok', status: 'Conectado', description: accessText ? `Sessão ativa · ${accessText}` : 'Sessão ativa' }
  }
  if (WHATSAPP_PAIRING.includes((status.status ?? '').toUpperCase())) {
    return { ...base, tone: 'warn', status: 'Conectando', description: 'Pareamento em andamento' }
  }
  return { ...base, tone: 'idle', status: 'Desconectado', description: 'Nenhuma sessão do WhatsApp ativa' }
}

const telegramItem = (agent: AgentInfo, data: AgentDashboardData): ConnectionItem => {
  const base = { key: 'telegram', label: 'Bot Telegram', icon: 'send' as const }
  const bot = agent.telegramBotName ? `@${agent.telegramBotName.replace(/^@/, '')}` : 'Bot configurado'
  if (!agent.telegramBotName && !agent.telegramBotToken) {
    return { ...base, tone: 'idle', status: 'Não configurado', description: 'Nenhum bot vinculado ao agente' }
  }
  if (!data.telegram) {
    return { ...base, tone: 'idle', status: 'Indisponível', description: `${bot} · webhook não consultado` }
  }
  return data.telegram.isConfigured
    ? { ...base, tone: 'ok', status: 'Ativo', description: `${bot} · webhook ativo` }
    : { ...base, tone: 'warn', status: 'Sem webhook', description: `${bot} · webhook não configurado` }
}

const powerBIItem = (agent: AgentInfo, data: AgentDashboardData): ConnectionItem => {
  const base = { key: 'powerbi', label: 'Power BI', icon: 'chart' as const }
  const config = data.powerBIConfig
  const enabled = config?.enabled ?? agent.powerBIEnabled
  if (!enabled) return { ...base, tone: 'idle', status: 'Desativado', description: 'Consultas Power BI desligadas' }
  if (!config) return { ...base, tone: 'idle', status: 'Indisponível', description: 'Não foi possível ler a configuração' }
  if (!config.isConfigured) {
    return { ...base, tone: 'warn', status: 'Incompleto', description: 'Credenciais do Entra ID pendentes' }
  }
  const testedAt = config.lastTestAt ? `Último teste ${fmtRelative(parseApiDate(config.lastTestAt), data.fetchedAt)}` : null
  if (config.lastTestSuccess === false) {
    return { ...base, tone: 'err', status: 'Falhou', description: config.lastTestMessage || testedAt || 'Teste de conexão falhou' }
  }
  if (config.lastTestSuccess === true) {
    return { ...base, tone: 'ok', status: 'Conectado', description: testedAt ?? 'Teste de conexão aprovado' }
  }
  return { ...base, tone: 'warn', status: 'Não testado', description: 'Rode o teste de conexão' }
}

const openAIItem = (agent: AgentInfo): ConnectionItem => ({
  key: 'openai',
  label: 'OpenAI',
  icon: 'key',
  tone: agent.hasOpenAIApiKey ? 'ok' : 'idle',
  status: agent.hasOpenAIApiKey ? 'Chave própria' : 'Padrão',
  description: `${agent.hasOpenAIApiKey ? 'Chave do agente' : 'Chave da plataforma'} · ${agent.chatModel}`,
})

const mcpItem = (data: AgentDashboardData): ConnectionItem => {
  const base = { key: 'mcp', label: 'Servidor MCP', icon: 'plug' as const }
  if (!data.mcpKeys) return { ...base, tone: 'idle', status: 'Indisponível', description: 'Não foi possível listar as chaves' }
  const active = data.mcpKeys.filter((k) => k.status === 1)
  if (active.length === 0) return { ...base, tone: 'idle', status: 'Sem chaves', description: 'Nenhuma chave de acesso ativa' }
  const lastUsed = active
    .map((k) => (k.lastUsedAt ? parseApiDate(k.lastUsedAt).getTime() : 0))
    .reduce((a, b) => Math.max(a, b), 0)
  const keys = plural(active.length, 'chave ativa', 'chaves ativas')
  if (!lastUsed) return { ...base, tone: 'idle', status: 'Nunca usado', description: keys }
  const recent = data.fetchedAt - lastUsed < 86_400_000
  return {
    ...base,
    tone: recent ? 'ok' : 'idle',
    status: recent ? 'Em uso' : 'Ocioso',
    description: `${keys} · último uso ${fmtRelative(new Date(lastUsed), data.fetchedAt)}`,
  }
}

/** Monta a lista do cartão "Status das conexões" a partir das consultas do agente ativo. */
export const buildConnectionItems = (agent: AgentInfo, data: AgentDashboardData): ConnectionItem[] => [
  whatsappItem(data),
  telegramItem(agent, data),
  powerBIItem(agent, data),
  openAIItem(agent),
  mcpItem(data),
]
