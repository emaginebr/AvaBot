import { useEffect, useState } from 'react'
import { AgentService } from '../../Services/AgentService'
import type { AgentOpenAIDiagnoseResult } from '../../types/agent'
import Alert from './ui/Alert'
import Button from './ui/Button'
import Modal from './ui/Modal'
import { LoadingState } from './ui/States'

interface OpenAIConnectionDiagnosticModalProps {
  agentId: number
  apiKey: string | null
  hasSavedKey: boolean
  onClose: () => void
}

const OpenAIConnectionDiagnosticModal = ({ agentId, apiKey, hasSavedKey, onClose }: OpenAIConnectionDiagnosticModalProps) => {
  // A chave é congelada na abertura: editar o campo atrás do modal não re-tenta o diagnóstico.
  const [keyUnderTest] = useState(apiKey)
  const [loading, setLoading] = useState(true)
  const [result, setResult] = useState<AgentOpenAIDiagnoseResult | null>(null)
  const [transportError, setTransportError] = useState<string | null>(null)
  const [attempt, setAttempt] = useState(0)

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
    <Modal
      icon="shield"
      title="Diagnóstico da conexão OpenAI"
      description={heading}
      onClose={onClose}
      footer={
        // type="button": o modal é aberto de dentro do formulário do agente e não deve enviá-lo.
        <>
          <Button onClick={handleRetry} disabled={loading}>
            Tentar novamente
          </Button>
          <Button variant="primary" onClick={onClose}>
            Fechar
          </Button>
        </>
      }
    >
      <p className="m-0 text-[12.5px] text-ink-3">
        A verificação só confirma a autenticação. Nenhuma resposta é gerada e não há custo de tokens.
      </p>

      <div aria-live="polite" className="min-h-16">
        {loading ? (
          <LoadingState label="Verificando a chave..." rows={0} />
        ) : succeeded ? (
          <Alert tone="ok">
            <p className="m-0 font-semibold">Conexão confirmada</p>
            <p className="m-0 mt-1">{result?.message}</p>
          </Alert>
        ) : (
          <Alert tone="err">
            <p className="m-0 font-semibold">Não foi possível confirmar a chave</p>
            <p className="m-0 mt-1 whitespace-pre-wrap">
              {transportError ?? result?.message ?? 'Nenhuma chave informada. Digite a chave ou salve uma credencial para este agente.'}
            </p>
          </Alert>
        )}
      </div>
    </Modal>
  )
}

export default OpenAIConnectionDiagnosticModal
