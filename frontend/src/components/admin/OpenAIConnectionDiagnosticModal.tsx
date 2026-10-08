import { useEffect, useRef, useState } from 'react'
import { AgentService } from '../../Services/AgentService'
import type { AgentOpenAIDiagnoseResult } from '../../types/agent'

interface OpenAIConnectionDiagnosticModalProps {
  agentId: number
  apiKey: string | null
  hasSavedKey: boolean
  onClose: () => void
}

const FOCUSABLE = 'button:not(:disabled), input:not(:disabled), select, textarea, [href]'

const OpenAIConnectionDiagnosticModal = ({ agentId, apiKey, hasSavedKey, onClose }: OpenAIConnectionDiagnosticModalProps) => {
  // A chave e congelada na abertura: editar o campo atras do modal nao re-tenta o diagnostico.
  const [keyUnderTest] = useState(apiKey)
  const [loading, setLoading] = useState(true)
  const [result, setResult] = useState<AgentOpenAIDiagnoseResult | null>(null)
  const [transportError, setTransportError] = useState<string | null>(null)
  const [attempt, setAttempt] = useState(0)
  const panelRef = useRef<HTMLDivElement>(null)
  const openerRef = useRef<Element | null>(null)

  useEffect(() => {
    let cancelled = false

    AgentService.diagnoseOpenAI(agentId, keyUnderTest)
      .then((res) => {
        if (cancelled) return
        if (res.sucesso && res.dados) {
          setResult(res.dados)
          setTransportError(null)
        } else {
          setResult(null)
          setTransportError(res.mensagem || 'Não foi possível concluir o diagnóstico.')
        }
      })
      .catch(() => {
        if (cancelled) return
        setResult(null)
        setTransportError('Erro de rede ao falar com o servidor.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => { cancelled = true }
  }, [agentId, keyUnderTest, attempt])

  useEffect(() => {
    openerRef.current = document.activeElement
    panelRef.current?.focus()

    return () => {
      const opener = openerRef.current as HTMLElement | null
      opener?.focus?.()
    }
  }, [])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
        return
      }

      if (event.key !== 'Tab' || !panelRef.current) return

      const focusable = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE))
      if (focusable.length === 0) return

      const first = focusable[0]
      const last = focusable[focusable.length - 1]

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  const heading = keyUnderTest?.trim()
    ? 'Testando a chave informada no formulário (não salva).'
    : hasSavedKey
      ? 'Testando a chave salva deste agente.'
      : 'Nenhuma chave disponível para testar.'

  const succeeded = result?.success === true

  const handleRetry = () => {
    setLoading(true)
    setAttempt((a) => a + 1)
  }

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />

      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="openai-diagnose-title"
        tabIndex={-1}
        className="relative bg-white rounded-xl border border-gray-200 shadow-lg max-w-md w-full p-6 focus:outline-none"
      >
        <h2 id="openai-diagnose-title" className="text-lg font-semibold text-gray-900">
          Diagnóstico da conexão OpenAI
        </h2>
        <p className="text-sm text-gray-500 mt-1">{heading}</p>
        <p className="text-xs text-gray-400 mt-1">
          A verificação só confirma a autenticação. Nenhuma resposta é gerada e não há custo de tokens.
        </p>

        <div className="mt-5 min-h-[64px]">
          {loading ? (
            <div className="flex items-center gap-2 text-gray-500">
              <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
              Verificando a chave...
            </div>
          ) : succeeded ? (
            <div className="p-3 bg-green-50 border border-green-200 rounded-lg">
              <p className="text-sm font-medium text-green-700">Conexão confirmada</p>
              <p className="text-xs text-green-700 mt-1">{result?.message}</p>
            </div>
          ) : (
            <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm font-medium text-red-700">Não foi possível confirmar a chave</p>
              <p className="text-xs text-red-700 mt-1 whitespace-pre-wrap">
                {transportError ?? result?.message ?? 'Nenhuma chave informada. Digite a chave ou salve uma credencial para este agente.'}
              </p>
            </div>
          )}
        </div>

        <div className="flex justify-end gap-2 mt-6">
          <button
            onClick={handleRetry}
            disabled={loading}
            className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Tentar novamente
          </button>
          <button
            onClick={onClose}
            className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 transition-colors"
          >
            Fechar
          </button>
        </div>
      </div>
    </div>
  )
}

export default OpenAIConnectionDiagnosticModal
