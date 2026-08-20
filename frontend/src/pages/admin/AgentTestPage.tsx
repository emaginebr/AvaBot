import { useState } from 'react'
import { toast } from 'sonner'
import ReactMarkdown from 'react-markdown'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'
import type { AgentTestResult } from '../../types/agent'

const AgentTestPage = () => {
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<AgentTestResult | null>(null)
  const [loading, setLoading] = useState(false)

  if (!selectedAgent) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-400 mb-2">Nenhum agente selecionado</p>
          <p className="text-sm text-gray-300">Selecione um agente na navbar para testar.</p>
        </div>
      </div>
    )
  }

  const handleTest = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!query.trim()) return

    setLoading(true)
    setResult(null)

    try {
      const res = await AgentService.test(selectedAgent.agentId, query.trim())
      if (res.sucesso) {
        setResult(res.dados)
        toast.success('Teste executado com sucesso')
      } else {
        toast.error(res.mensagem || 'Erro ao executar teste')
      }
    } catch {
      toast.error('Erro de rede ao executar teste')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Teste do Agente</h1>
        <p className="text-sm text-gray-500 mt-1">
          Envie uma pergunta para testar a resposta do agente {selectedAgent.name}
        </p>
      </div>

      <form onSubmit={handleTest} className="mb-6">
        <div className="flex gap-3">
          <div className="flex-1">
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Ex: Qual o horário de funcionamento?"
              className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow"
            />
          </div>
          <button
            type="submit"
            disabled={loading || !query.trim()}
            className="px-6 py-2.5 bg-ava-600 text-white rounded-lg font-medium hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-sm"
          >
            {loading ? 'Testando...' : 'Testar'}
          </button>
        </div>
      </form>

      {loading && (
        <div className="flex items-center gap-2 text-gray-500">
          <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
          Aguardando resposta do agente...
        </div>
      )}

      {result && (
        <div className="space-y-4">
          {/* Resposta formatada */}
          <div className="bg-white rounded-xl border border-gray-200 p-5">
            <h2 className="text-sm font-semibold text-gray-900 mb-3">Resposta do Agente (Formatada)</h2>
            <div className="prose prose-sm max-w-none bg-ava-50 rounded-lg p-4">
              <ReactMarkdown>{result.assistantResponse}</ReactMarkdown>
            </div>
          </div>

          {/* Resposta raw */}
          <div className="bg-white rounded-xl border border-gray-200 p-5">
            <h2 className="text-sm font-semibold text-gray-900 mb-3">Resposta do Agente (Raw)</h2>
            <pre className="text-sm text-gray-700 bg-gray-50 rounded-lg p-4 whitespace-pre-wrap">{result.assistantResponse}</pre>
          </div>

          {/* Resultados da busca */}
          <div className="bg-white rounded-xl border border-gray-200 p-5">
            <h2 className="text-sm font-semibold text-gray-900 mb-1">Resultados da Busca na Base</h2>
            <p className="text-xs text-gray-400 mb-3">
              Query: "{result.searchQuery}" — {result.searchResults.length} chunk(s) encontrado(s)
            </p>
            {result.searchResults.length === 0 ? (
              <p className="text-sm text-gray-400">Nenhum resultado encontrado na base de conhecimento.</p>
            ) : (
              <div className="space-y-2">
                {result.searchResults.map((chunk, i) => (
                  <div key={i} className="bg-gray-50 rounded-lg p-3">
                    <span className="text-xs font-medium text-ava-600 bg-ava-50 px-1.5 py-0.5 rounded mr-2">#{i + 1}</span>
                    <span className="text-sm text-gray-700 whitespace-pre-wrap">{chunk}</span>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Messages enviadas à LLM */}
          <details className="bg-white rounded-xl border border-gray-200">
            <summary className="px-5 py-3 cursor-pointer text-sm font-semibold text-gray-900 hover:bg-gray-50 rounded-xl">
              Messages enviadas ({result.messages.length})
            </summary>
            <div className="px-5 pb-4 space-y-2">
              {result.messages.map((msg, i) => (
                <div key={i} className="bg-gray-50 rounded-lg p-3">
                  <span className={`text-xs font-medium px-1.5 py-0.5 rounded mr-2 ${
                    msg.role === 'user' ? 'bg-blue-100 text-blue-700' : 'bg-green-100 text-green-700'
                  }`}>
                    {msg.role}
                  </span>
                  <pre className="text-xs text-gray-600 mt-2 whitespace-pre-wrap">{msg.content}</pre>
                </div>
              ))}
            </div>
          </details>

          {/* System Prompt utilizado */}
          <details className="bg-white rounded-xl border border-gray-200">
            <summary className="px-5 py-3 cursor-pointer text-sm font-semibold text-gray-900 hover:bg-gray-50 rounded-xl">
              System Prompt utilizado
            </summary>
            <div className="px-5 pb-4">
              <pre className="text-xs text-gray-600 bg-gray-50 rounded-lg p-3 overflow-x-auto whitespace-pre-wrap">{result.systemPrompt}</pre>
            </div>
          </details>
        </div>
      )}
    </div>
  )
}

export default AgentTestPage
