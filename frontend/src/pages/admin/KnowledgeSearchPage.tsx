import { useState } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'

const KnowledgeSearchPage = () => {
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const [query, setQuery] = useState('')
  const [topK, setTopK] = useState(5)
  const [results, setResults] = useState<string[]>([])
  const [loading, setLoading] = useState(false)
  const [searched, setSearched] = useState(false)

  if (!selectedAgent) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-400 mb-2">Nenhum agente selecionado</p>
          <p className="text-sm text-gray-300">Selecione um agente na navbar para buscar na base de conhecimento.</p>
        </div>
      </div>
    )
  }

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!query.trim()) return

    setLoading(true)
    setSearched(true)

    try {
      const result = await AgentService.search(selectedAgent.agentId, query.trim(), topK)
      if (result.sucesso) {
        setResults(result.dados)
        if (result.dados.length === 0) {
          toast.info('Nenhum resultado encontrado')
        } else {
          toast.success(`${result.dados.length} resultado(s) encontrado(s)`)
        }
      } else {
        toast.error(result.mensagem || 'Erro ao buscar na base de conhecimento')
        setResults([])
      }
    } catch {
      toast.error('Erro de rede ao buscar na base de conhecimento')
      setResults([])
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Busca na Base de Conhecimento</h1>
        <p className="text-sm text-gray-500 mt-1">Agente: {selectedAgent.name}</p>
      </div>

      <form onSubmit={handleSearch} className="mb-6">
        <div className="flex gap-3">
          <div className="flex-1">
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Digite o termo de busca..."
              className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow"
            />
          </div>
          <div className="w-24">
            <input
              type="number"
              value={topK}
              onChange={(e) => setTopK(Math.max(1, Math.min(20, Number(e.target.value))))}
              min={1}
              max={20}
              className="w-full px-3 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow text-center"
              title="Número de resultados"
            />
          </div>
          <button
            type="submit"
            disabled={loading || !query.trim()}
            className="px-6 py-2.5 bg-ava-600 text-white rounded-lg font-medium hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-sm"
          >
            {loading ? 'Buscando...' : 'Buscar'}
          </button>
        </div>
        <p className="text-xs text-gray-400 mt-1.5">Máximo de resultados: {topK}</p>
      </form>

      {loading ? (
        <div className="flex items-center gap-2 text-gray-500">
          <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
          Buscando na base de conhecimento...
        </div>
      ) : searched && results.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-xl border border-gray-200">
          <p className="text-gray-400">Nenhum resultado encontrado para "{query}".</p>
        </div>
      ) : results.length > 0 ? (
        <div className="space-y-3">
          <p className="text-sm text-gray-500">{results.length} resultado(s) encontrado(s)</p>
          {results.map((chunk, index) => (
            <div key={index} className="bg-white rounded-xl border border-gray-200 p-5">
              <div className="flex items-center gap-2 mb-2">
                <span className="text-xs font-medium text-ava-600 bg-ava-50 px-2 py-0.5 rounded">
                  #{index + 1}
                </span>
              </div>
              <p className="text-sm text-gray-700 whitespace-pre-wrap leading-relaxed">{chunk}</p>
            </div>
          ))}
        </div>
      ) : null}
    </div>
  )
}

export default KnowledgeSearchPage
