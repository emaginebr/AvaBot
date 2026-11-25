import { useState } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'
import Button from '../../components/admin/ui/Button'
import { Card, CardHeader } from '../../components/admin/ui/Card'
import ChunkList from '../../components/admin/ui/ChunkList'
import Field from '../../components/admin/ui/Field'
import { Input } from '../../components/admin/ui/Input'
import PageHeader from '../../components/admin/ui/PageHeader'
import { EmptyState, LoadingState, NoAgentState } from '../../components/admin/ui/States'

const PAGE_TITLE = 'Busca na base de conhecimento'
const PAGE_DESCRIPTION = 'Veja quais trechos dos arquivos a busca semântica devolve para um termo.'

const KnowledgeSearchPage = () => {
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const [query, setQuery] = useState('')
  const [topK, setTopK] = useState(5)
  const [results, setResults] = useState<string[]>([])
  const [loading, setLoading] = useState(false)
  const [searched, setSearched] = useState(false)

  if (!selectedAgent) {
    return (
      <div className="flex flex-col gap-5">
        <PageHeader title={PAGE_TITLE} description={PAGE_DESCRIPTION} />
        <NoAgentState purpose="buscar na base de conhecimento" />
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
    <div className="flex flex-col gap-5">
      <PageHeader agentName={selectedAgent.name} title={PAGE_TITLE} description={PAGE_DESCRIPTION} />

      <Card as="form" role="search" aria-label="Busca na base" onSubmit={handleSearch}>
        <div className="flex flex-col gap-3 md:flex-row md:items-start">
          <Field id="kb-search-query" label="Termo de busca" className="flex-1">
            <Input
              type="search"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Digite o termo de busca..."
            />
          </Field>
          <Field
            id="kb-search-topk"
            label="Máx. de resultados"
            hint={`Máximo de resultados: ${topK} (de 1 a 20).`}
            className="md:w-[180px]"
          >
            <Input
              type="number"
              value={topK}
              onChange={(e) => setTopK(Math.max(1, Math.min(20, Number(e.target.value))))}
              min={1}
              max={20}
              className="font-ui-mono tabular-nums"
            />
          </Field>
          <Button
            type="submit"
            variant="primary"
            size="lg"
            icon="search"
            loading={loading}
            disabled={!query.trim()}
            className="md:mt-[25px]"
          >
            {loading ? 'Buscando...' : 'Buscar'}
          </Button>
        </div>
      </Card>

      <div aria-live="polite">
        {loading ? (
          <Card>
            <LoadingState label="Buscando na base de conhecimento..." rows={3} rowClassName="h-16" />
          </Card>
        ) : searched && results.length === 0 ? (
          <EmptyState icon="search" title={`Nenhum resultado encontrado para "${query}".`} className="bg-white" />
        ) : results.length > 0 ? (
          <Card aria-labelledby="kb-search-resultados">
            <CardHeader
              titleId="kb-search-resultados"
              icon="search"
              title="Trechos encontrados"
              subtitle={`${results.length} resultado(s) encontrado(s)`}
            />
            <ChunkList chunks={results} />
          </Card>
        ) : null}
      </div>
    </div>
  )
}

export default KnowledgeSearchPage
