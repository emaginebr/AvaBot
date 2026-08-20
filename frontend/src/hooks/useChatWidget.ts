import { useCallback, useEffect, useRef, useState } from 'react'
import type { AgentChatConfigInfo } from '../types/agent'
import type { ChatSessionStartInfo } from '../types/chatSession'
import { AgentService } from '../Services/AgentService'
import { CookieService } from '../Services/CookieService'
import useChat from './useChat'

interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

type Phase = 'greeting' | 'collecting' | 'starting' | 'returning' | 'ready'

interface CollectState {
  phase: Phase
  pendingFields: string[]
  currentField: string | null
  collectedData: ChatSessionStartInfo
}

const FIELD_MAP: Record<string, keyof ChatSessionStartInfo> = {
  name: 'userName',
  email: 'userEmail',
  phone: 'userPhone',
}

const FIELD_VALIDATION_ERRORS: Record<string, string> = {
  email: 'Por favor, informe um email valido (ex: nome@email.com)',
  phone: 'Por favor, informe um telefone valido (somente numeros, com DDD)',
}

const validateField = (field: string, value: string): boolean => {
  const trimmed = value.trim()
  if (!trimmed) return false
  if (field === 'email') return trimmed.includes('@') && trimmed.includes('.')
  if (field === 'phone') return /^\+?[\d\s()-]{10,}$/.test(trimmed)
  return true
}

const getPrompt = (field: string, collectedData: ChatSessionStartInfo): string => {
  if (field === 'name') return 'Qual seu nome?'
  if (field === 'email') {
    const name = collectedData.userName
    return name ? `Bem vindo ${name}, qual seu email?` : 'Qual seu email?'
  }
  if (field === 'phone') return 'Qual seu numero de telefone? Com DDD'
  return `Qual seu ${field}?`
}

interface UseChatWidgetReturn {
  messages: ChatMessage[]
  streaming: boolean
  ready: boolean
  error: string | null
  isCollecting: boolean
  showActionButtons: boolean
  inputDisabled: boolean
  sendMessage: (content: string) => void
  handleActionSelect: (optionText: string) => void
}

let _wsUrl: string | undefined
export function setWsUrl(url: string) { _wsUrl = url }
const WS_URL_FALLBACK = () => _wsUrl ?? import.meta.env.VITE_WS_URL

const useChatWidget = (
  slug: string,
  greeting: string,
  config: AgentChatConfigInfo | null,
  widgetOpen?: boolean,
): UseChatWidgetReturn => {
  const [collectState, setCollectState] = useState<CollectState>({
    phase: 'greeting',
    pendingFields: [],
    currentField: null,
    collectedData: {},
  })
  const [collectMessages, setCollectMessages] = useState<ChatMessage[]>([])
  const [wsUrl, setWsUrl] = useState<string | null>(null)
  const [startError, setStartError] = useState<string | null>(null)
  const [showActionButtons, setShowActionButtons] = useState(false)
  const [inputDisabled, setInputDisabled] = useState(false)
  const initializedRef = useRef(false)
  const choiceMadeRef = useRef(false)

  const chat = useChat(wsUrl)

  // Reset state when widget is closed and reopened (if no choice was made)
  useEffect(() => {
    if (widgetOpen && !choiceMadeRef.current && config) {
      const cookies = CookieService.getCookies(slug)
      if (cookies) {
        // Reinitialize returning flow
        setCollectMessages([{ role: 'assistant', content: `Bem-vindo de volta, ${cookies.userName}` }])
        setCollectState({ phase: 'returning', pendingFields: [], currentField: null, collectedData: {} })
        setShowActionButtons(true)
        setInputDisabled(true)
        setWsUrl(null)
      }
    }
  }, [widgetOpen, slug, config])

  // When config loads, determine flow: returning user or new user
  useEffect(() => {
    if (!config || initializedRef.current) return
    initializedRef.current = true

    // Check for existing cookies (returning user)
    const cookies = CookieService.getCookies(slug)
    if (cookies) {
      // Returning user flow
      setCollectMessages([{ role: 'assistant', content: `Bem-vindo de volta, ${cookies.userName}` }])
      setCollectState({ phase: 'returning', pendingFields: [], currentField: null, collectedData: {} })
      setShowActionButtons(true)
      setInputDisabled(true)
      return
    }

    // New user flow — determine fields to collect
    const fields: string[] = []
    if (config.collectName) fields.push('name')
    if (config.collectEmail) fields.push('email')
    if (config.collectPhone) fields.push('phone')

    if (fields.length === 0) {
      setCollectState({ phase: 'starting', pendingFields: [], currentField: null, collectedData: {} })
    } else {
      const firstField = fields[0]
      const remaining = fields.slice(1)
      const data: ChatSessionStartInfo = {}
      setCollectMessages([{ role: 'assistant', content: getPrompt(firstField, data) }])
      setCollectState({ phase: 'collecting', pendingFields: remaining, currentField: firstField, collectedData: data })
    }
  }, [config, slug])

  // When phase becomes 'starting', call start-session
  useEffect(() => {
    if (collectState.phase !== 'starting') return

    console.log('[ChatWidget] Iniciando sessao para', slug, collectState.collectedData)
    AgentService.startSession(slug, collectState.collectedData).then(result => {
      if (result.sucesso && result.dados) {
        const session = result.dados
        const sessionId = session.chatSessionId
        const url = `${WS_URL_FALLBACK()}/ws/chat/${slug}?sessionId=${sessionId}`
        console.log('[ChatWidget] Sessao iniciada, conectando WebSocket:', url)

        // Save cookies for future visits (T004)
        if (session.resumeToken) {
          CookieService.setCookies(slug, {
            resumeToken: session.resumeToken,
            userName: session.userName ?? collectState.collectedData.userName ?? '',
            userEmail: session.userEmail ?? collectState.collectedData.userEmail,
            userPhone: session.userPhone ?? collectState.collectedData.userPhone,
          })
        }

        setCollectMessages(prev => [
          ...prev,
          { role: 'assistant', content: 'Muito obrigado pelas informacoes, agora em que posso ajudar?' },
        ])
        setWsUrl(url)
        setCollectState(prev => ({ ...prev, phase: 'ready' }))
      } else {
        const errorMsg = result.mensagem || 'Erro ao iniciar sessao'
        console.error('[ChatWidget] Erro ao iniciar sessao:', errorMsg)
        setStartError(errorMsg)
        setCollectMessages(prev => [
          ...prev,
          { role: 'assistant', content: `Desculpe, ocorreu um erro: ${errorMsg}` },
        ])
      }
    }).catch((err) => {
      console.error('[ChatWidget] Falha na requisicao de sessao:', err)
      setStartError('Erro ao conectar com o servidor')
      setCollectMessages(prev => [
        ...prev,
        { role: 'assistant', content: 'Desculpe, nao foi possivel conectar ao servidor. Tente novamente mais tarde.' },
      ])
    })
  }, [collectState.phase, collectState.collectedData, slug])

  // Handle action button selection (returning user)
  const handleActionSelect = useCallback((optionText: string) => {
    choiceMadeRef.current = true
    setShowActionButtons(false)
    setInputDisabled(false)

    // Show selected option as user message
    setCollectMessages(prev => [...prev, { role: 'user', content: optionText }])

    if (optionText === 'Desejo retomar nossa última conversa') {
      // Resume previous session (US1 - T007)
      const cookies = CookieService.getCookies(slug)
      if (!cookies) {
        startFreshFlow()
        return
      }

      AgentService.resumeSession(slug, cookies.resumeToken).then(result => {
        if (result.sucesso && result.dados) {
          const session = result.dados
          const sessionId = session.chatSessionId

          // Load historical messages (T008)
          const historical: ChatMessage[] = session.messages.map(m => ({
            role: m.senderType === 0 ? 'user' as const : 'assistant' as const,
            content: m.content,
          }))
          chat.loadHistoricalMessages(historical)

          // Connect WebSocket
          const url = `${WS_URL_FALLBACK()}/ws/chat/${slug}?sessionId=${sessionId}`
          console.log('[ChatWidget] Sessao retomada, conectando WebSocket:', url)
          setWsUrl(url)
          setCollectState(prev => ({ ...prev, phase: 'ready' }))
        } else {
          console.error('[ChatWidget] Falha ao retomar sessao:', result.mensagem)
          CookieService.clearCookies(slug)
          setCollectMessages(prev => [
            ...prev,
            { role: 'assistant', content: 'Nao foi possivel retomar a conversa anterior. Vamos comecar uma nova.' },
          ])
          startFreshFlow()
        }
      }).catch(err => {
        console.error('[ChatWidget] Erro ao retomar sessao:', err)
        CookieService.clearCookies(slug)
        setCollectMessages(prev => [
          ...prev,
          { role: 'assistant', content: 'Erro ao conectar com o servidor. Vamos comecar uma nova conversa.' },
        ])
        startFreshFlow()
      })
    } else if (optionText === 'Iniciar uma nova conversa') {
      // Start new session with cookie data (US2 - T011)
      const cookies = CookieService.getCookies(slug)
      if (!cookies) {
        startFreshFlow()
        return
      }

      const sessionData: ChatSessionStartInfo = {
        userName: cookies.userName,
        userEmail: cookies.userEmail,
        userPhone: cookies.userPhone,
      }

      AgentService.startSession(slug, sessionData).then(result => {
        if (result.sucesso && result.dados) {
          const session = result.dados
          const sessionId = session.chatSessionId

          // Update cookie with new resumeToken (T011)
          if (session.resumeToken) {
            CookieService.setCookies(slug, {
              resumeToken: session.resumeToken,
              userName: cookies.userName,
              userEmail: cookies.userEmail,
              userPhone: cookies.userPhone,
            })
          }

          const url = `${WS_URL_FALLBACK()}/ws/chat/${slug}?sessionId=${sessionId}`
          console.log('[ChatWidget] Nova sessao criada, conectando WebSocket:', url)
          setCollectMessages(prev => [
            ...prev,
            { role: 'assistant', content: 'Em que posso ajudar?' },
          ])
          setWsUrl(url)
          setCollectState(prev => ({ ...prev, phase: 'ready' }))
        } else {
          // T012 - Error handling
          console.error('[ChatWidget] Falha ao criar nova sessao:', result.mensagem)
          CookieService.clearCookies(slug)
          setCollectMessages(prev => [
            ...prev,
            { role: 'assistant', content: 'Nao foi possivel iniciar uma nova conversa. Vamos nos identificar novamente.' },
          ])
          startFreshFlow()
        }
      }).catch(err => {
        console.error('[ChatWidget] Erro ao criar nova sessao:', err)
        CookieService.clearCookies(slug)
        setCollectMessages(prev => [
          ...prev,
          { role: 'assistant', content: 'Erro ao conectar com o servidor. Vamos nos identificar novamente.' },
        ])
        startFreshFlow()
      })
    }
  }, [slug, chat])

  // Start fresh collection flow (when cookies are invalid)
  const startFreshFlow = useCallback(() => {
    if (!config) return
    const fields: string[] = []
    if (config.collectName) fields.push('name')
    if (config.collectEmail) fields.push('email')
    if (config.collectPhone) fields.push('phone')

    if (fields.length === 0) {
      setCollectState({ phase: 'starting', pendingFields: [], currentField: null, collectedData: {} })
    } else {
      const firstField = fields[0]
      const remaining = fields.slice(1)
      const data: ChatSessionStartInfo = {}
      setCollectMessages(prev => [
        ...prev,
        { role: 'assistant', content: getPrompt(firstField, data) },
      ])
      setCollectState({ phase: 'collecting', pendingFields: remaining, currentField: firstField, collectedData: data })
    }
  }, [config])

  const handleCollectResponse = useCallback((content: string) => {
    const { currentField, pendingFields, collectedData } = collectState
    if (!currentField) return

    if (!validateField(currentField, content)) {
      const errorMsg = FIELD_VALIDATION_ERRORS[currentField] ?? 'Resposta invalida. Tente novamente.'
      setCollectMessages(prev => [
        ...prev,
        { role: 'user', content },
        { role: 'assistant', content: errorMsg },
      ])
      return
    }

    const dataKey = FIELD_MAP[currentField] ?? currentField
    const newData = { ...collectedData, [dataKey]: content.trim() }
    const remaining = [...pendingFields]
    const nextField = remaining.shift() ?? null

    const newMessages: ChatMessage[] = [{ role: 'user', content }]

    if (nextField) {
      newMessages.push({ role: 'assistant', content: getPrompt(nextField, newData) })
      setCollectState({
        phase: 'collecting',
        pendingFields: remaining,
        currentField: nextField,
        collectedData: newData,
      })
    } else {
      setCollectState({
        phase: 'starting',
        pendingFields: [],
        currentField: null,
        collectedData: newData,
      })
    }

    setCollectMessages(prev => [...prev, ...newMessages])
  }, [collectState])

  const sendMessage = useCallback((content: string) => {
    if (inputDisabled) return
    if (collectState.phase === 'collecting') {
      handleCollectResponse(content)
    } else if (collectState.phase === 'ready') {
      chat.sendMessage(content)
    }
  }, [collectState.phase, handleCollectResponse, chat, inputDisabled])

  const greetingMsg: ChatMessage = { role: 'assistant', content: greeting }
  const allMessages = collectState.phase === 'returning'
    ? collectMessages
    : [...[greetingMsg], ...collectMessages, ...chat.messages]

  const isCollecting = collectState.phase === 'collecting' || collectState.phase === 'starting'

  return {
    messages: allMessages,
    streaming: chat.streaming,
    ready: collectState.phase === 'ready' && chat.ready,
    error: startError || chat.error,
    isCollecting,
    showActionButtons,
    inputDisabled,
    sendMessage,
    handleActionSelect,
  }
}

export default useChatWidget
