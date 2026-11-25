import { useState } from 'react'
import type { ReactNode } from 'react'
import { toast } from 'sonner'
import ReactMarkdown from 'react-markdown'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'
import type { AgentTestResult } from '../../types/agent'
import AdminIcon from '../../components/admin/AdminIcon'
import Button from '../../components/admin/ui/Button'
import { Card, CardHeader } from '../../components/admin/ui/Card'
import { CodeChip } from '../../components/admin/ui/Chip'
import ChunkList from '../../components/admin/ui/ChunkList'
import CodeBlock from '../../components/admin/ui/CodeBlock'
import Field from '../../components/admin/ui/Field'
import { Input } from '../../components/admin/ui/Input'
import PageHeader from '../../components/admin/ui/PageHeader'
import { EmptyState, LoadingState, NoAgentState } from '../../components/admin/ui/States'
import StatusBadge from '../../components/admin/ui/StatusBadge'
import Tabs, { TabPanel } from '../../components/admin/ui/Tabs'
import { cx } from '../../components/admin/ui/cx'

type ResponseTab = 'formatada' | 'bruta'

const ROLE_STYLES: Record<string, string> = {
  system: 'bg-gray-100 text-ink-2',
  user: 'bg-ava-50 text-ava-700',
  assistant: 'bg-ok-bg text-ok',
}

/** Seção recolhível (details/summary nativos) no estilo dos cartões. */
const Disclosure = ({ title, children }: { title: ReactNode; children: ReactNode }) => (
  <details className="group rounded-2xl border border-gray-200 bg-white shadow-card">
    <summary className="flex min-h-[52px] cursor-pointer list-none items-center gap-2.5 rounded-2xl px-5 py-4 font-brand text-[15px] font-semibold text-ink focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600 [&::-webkit-details-marker]:hidden">
      <AdminIcon
        name="chevron"
        className="h-4 w-4 -rotate-90 text-ink-3 transition-transform group-open:rotate-0 motion-reduce:transition-none"
      />
      {title}
    </summary>
    <div className="flex flex-col gap-2.5 px-5 pb-5">{children}</div>
  </details>
)

const AgentTestPage = () => {
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<AgentTestResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [responseTab, setResponseTab] = useState<ResponseTab>('formatada')

  if (!selectedAgent) {
    return (
      <div className="flex flex-col gap-5">
        <PageHeader title="Teste do agente" description="Envie uma pergunta para testar a resposta do agente." />
        <NoAgentState purpose="testar" />
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
    <div className="flex flex-col gap-5">
      <PageHeader
        agentName={selectedAgent.name}
        title="Teste do agente"
        description={`Envie uma pergunta para testar a resposta do agente ${selectedAgent.name} e ver o que foi usado para responder.`}
      />

      <Card as="form" onSubmit={handleTest} aria-label="Teste do agente">
        <div className="flex flex-col gap-3 md:flex-row md:items-end">
          <Field id="agent-test-query" label="Pergunta" className="flex-1">
            <Input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Ex.: Qual o horário de funcionamento?"
            />
          </Field>
          <Button type="submit" variant="primary" size="lg" icon="flask" loading={loading} disabled={!query.trim()}>
            {loading ? 'Testando...' : 'Testar'}
          </Button>
        </div>
      </Card>

      <div aria-live="polite" className="flex flex-col gap-5">
        {loading && (
          <Card>
            <LoadingState label="Aguardando resposta do agente..." rows={0} />
          </Card>
        )}

        {result && (
          <>
            <Card aria-labelledby="teste-resposta-titulo">
              <CardHeader titleId="teste-resposta-titulo" icon="bot" title="Resposta do agente" />
              <Tabs
                idPrefix="teste-resposta"
                label="Formato da resposta"
                value={responseTab}
                onChange={setResponseTab}
                items={[
                  { id: 'formatada', label: 'Formatada' },
                  { id: 'bruta', label: 'Bruta (raw)' },
                ]}
              />
              <TabPanel idPrefix="teste-resposta" tabId={responseTab}>
                {responseTab === 'formatada' ? (
                  <div className="prose prose-sm max-w-none rounded-xl bg-ava-50 p-4 [overflow-wrap:anywhere]">
                    <ReactMarkdown>{result.assistantResponse}</ReactMarkdown>
                  </div>
                ) : (
                  <CodeBlock variant="plain" limitHeight={false}>
                    {result.assistantResponse}
                  </CodeBlock>
                )}
              </TabPanel>
            </Card>

            <Card aria-labelledby="teste-busca-titulo">
              <CardHeader
                titleId="teste-busca-titulo"
                icon="search"
                title="Resultados da busca na base"
                subtitle={`Query: "${result.searchQuery}" — ${result.searchResults.length} chunk(s) encontrado(s)`}
              />
              {result.searchResults.length === 0 ? (
                <EmptyState icon="search" title="Nenhum resultado encontrado na base de conhecimento." />
              ) : (
                <ChunkList chunks={result.searchResults} />
              )}
            </Card>

            {result.powerBIQueries.length > 0 && (
              <Card aria-labelledby="teste-powerbi-titulo">
                <CardHeader
                  titleId="teste-powerbi-titulo"
                  icon="chart"
                  iconTone="warn"
                  title="Consultas Power BI"
                  subtitle={`${result.powerBIQueries.length} chamada(s) de ferramenta durante a resposta`}
                />
                <ol className="m-0 flex list-none flex-col gap-3 p-0">
                  {result.powerBIQueries.map((call, i) => (
                    <li key={i} className="flex min-w-0 flex-col gap-2.5 rounded-xl border border-gray-200 p-3.5">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="grid h-[22px] min-w-[30px] place-items-center rounded-md bg-ava-50 font-ui-mono text-xs font-medium text-ava-700">
                          #{i + 1}
                        </span>
                        <CodeChip>{call.toolName}</CodeChip>
                        {call.datasetName && <span className="text-[13px] [overflow-wrap:anywhere] text-ink">{call.datasetName}</span>}
                        <span className="ml-auto">
                          {call.success ? <StatusBadge tone="ok" label="Sucesso" /> : <StatusBadge tone="err" label="Erro" />}
                        </span>
                      </div>

                      {call.query && <CodeBlock variant="plain">{call.query}</CodeBlock>}

                      {call.error && (
                        <p className="m-0 text-xs whitespace-pre-wrap [overflow-wrap:anywhere] text-err">{call.error}</p>
                      )}

                      {call.resultPreview && <CodeBlock variant="plain">{call.resultPreview}</CodeBlock>}

                      <p className="m-0 font-ui-mono text-xs tabular-nums text-ink-3">
                        {call.durationMs} ms · {call.rowCount ?? 0} linha(s)
                        {call.truncated && ' · truncado'}
                      </p>
                    </li>
                  ))}
                </ol>
              </Card>
            )}

            <Disclosure title={`Mensagens enviadas (${result.messages.length})`}>
              {result.messages.map((msg, i) => (
                <div key={i} className="min-w-0 rounded-xl border border-gray-200 p-3.5">
                  <span
                    className={cx(
                      'mb-1.5 inline-flex rounded-md px-2 py-px font-ui-mono text-[11.5px]',
                      ROLE_STYLES[msg.role] ?? ROLE_STYLES.system,
                    )}
                  >
                    {msg.role}
                  </span>
                  <CodeBlock variant="bare">
                    {msg.content}
                  </CodeBlock>
                </div>
              ))}
            </Disclosure>

            <Disclosure title="Prompt do sistema utilizado">
              <CodeBlock variant="plain">{result.systemPrompt}</CodeBlock>
            </Disclosure>
          </>
        )}
      </div>
    </div>
  )
}

export default AgentTestPage
