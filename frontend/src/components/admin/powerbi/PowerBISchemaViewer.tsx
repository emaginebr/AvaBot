import { useEffect, useMemo, useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import { PowerBISchemaStatus } from '../../../types/powerbi'
import type { PowerBIDatasetInfo, PowerBIDatasetSchemaInfo, PowerBISchemaDescriptionItemInfo } from '../../../types/powerbi'
import AdminIcon from '../AdminIcon'
import Alert from '../ui/Alert'
import Button from '../ui/Button'
import { Input, Textarea } from '../ui/Input'
import Modal from '../ui/Modal'
import { EmptyState, LoadingState } from '../ui/States'

const tableKey = (table: string) => `t:${table.toLowerCase()}`
const columnKey = (table: string, name: string) => `c:${table.toLowerCase()}|${name.toLowerCase()}`
const measureKey = (table: string, name: string) => `m:${table.toLowerCase()}|${name.toLowerCase()}`

interface PowerBISchemaViewerProps {
  slug: string
  dataset: PowerBIDatasetInfo
  onClose: () => void
}

const SectionLabel = ({ children }: { children: string }) => (
  <p className="m-0 mb-1.5 text-[11px] font-semibold tracking-[0.08em] text-ink-3 uppercase">{children}</p>
)

const PowerBISchemaViewer = ({ slug, dataset, onClose }: PowerBISchemaViewerProps) => {
  const [schema, setSchema] = useState<PowerBIDatasetSchemaInfo | null>(null)
  const [loading, setLoading] = useState(false)
  const [search, setSearch] = useState('')
  const [drafts, setDrafts] = useState<Record<string, string>>({})
  const [editingKey, setEditingKey] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [confirmClose, setConfirmClose] = useState(false)

  useEffect(() => {
    const loadSchema = async () => {
      setLoading(true)
      try {
        const result = await PowerBIService.getSchema(slug, dataset.powerBIDatasetId)
        if (result.sucesso && result.dados) {
          setSchema(result.dados)
        } else {
          toast.error(result.mensagem || 'Erro ao carregar o schema')
          console.error('[PowerBISchemaViewer] loadSchema — erro:', result.mensagem, result.erros)
        }
      } catch (err) {
        console.error('[PowerBISchemaViewer] loadSchema — exceção:', err)
        toast.error('Erro de rede ao carregar o schema')
      } finally {
        setLoading(false)
      }
    }

    loadSchema()
  }, [slug, dataset.powerBIDatasetId])

  const dirtyKeys = useMemo(() => Object.keys(drafts), [drafts])

  const currentDescription = (key: string, saved: string | null) =>
    key in drafts ? drafts[key] : (saved || '')

  const handleDescribe = (key: string, saved: string | null) => {
    setEditingKey(key)
    setDrafts((prev) => ({ ...prev, [key]: saved || '' }))
  }

  const handleDraftChange = (key: string, value: string) => {
    setDrafts((prev) => ({ ...prev, [key]: value }))
  }

  const handleStopEditing = (key: string, saved: string | null) => {
    setEditingKey(null)
    setDrafts((prev) => {
      const next = { ...prev }
      if ((saved || '') === (next[key] || '')) delete next[key]
      return next
    })
  }

  const handleSave = async () => {
    if (!schema) return

    const items: PowerBISchemaDescriptionItemInfo[] = []

    schema.tables.forEach((table) => {
      const tKey = tableKey(table.name)
      if (tKey in drafts) {
        items.push({ kind: 'table', table: table.name, name: null, userDescription: drafts[tKey] || null })
      }

      table.columns.forEach((column) => {
        const cKey = columnKey(table.name, column.name)
        if (cKey in drafts) {
          items.push({ kind: 'column', table: table.name, name: column.name, userDescription: drafts[cKey] || null })
        }
      })

      table.measures.forEach((measure) => {
        const mKey = measureKey(table.name, measure.name)
        if (mKey in drafts) {
          items.push({ kind: 'measure', table: table.name, name: measure.name, userDescription: drafts[mKey] || null })
        }
      })
    })

    if (items.length === 0) return

    setSaving(true)
    try {
      const result = await PowerBIService.updateSchemaDescriptions(slug, dataset.powerBIDatasetId, { items })
      if (result.sucesso && result.dados) {
        setSchema(result.dados)
        setDrafts({})
        setEditingKey(null)
        toast.success('Descrições salvas')
      } else {
        toast.error(result.mensagem || 'Erro ao salvar as descrições')
        console.error('[PowerBISchemaViewer] handleSave — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[PowerBISchemaViewer] handleSave — exceção:', err)
      toast.error('Erro de rede ao salvar as descrições')
    } finally {
      setSaving(false)
    }
  }

  const handleClose = () => {
    if (dirtyKeys.length > 0) {
      setConfirmClose(true)
      return
    }
    onClose()
  }

  const query = search.trim().toLowerCase()

  const filteredTables = (schema?.tables || []).filter((table) => {
    if (!query) return true
    if (table.name.toLowerCase().includes(query)) return true
    return table.columns.some((c) => c.name.toLowerCase().includes(query))
      || table.measures.some((m) => m.name.toLowerCase().includes(query))
  })

  const statusLabel = (() => {
    if (!schema) return ''
    if (schema.schemaStatus === PowerBISchemaStatus.Generated) return 'Gerado'
    if (schema.schemaStatus === PowerBISchemaStatus.Partial) return 'Parcial'
    if (schema.schemaStatus === PowerBISchemaStatus.Error) return 'Erro'
    return 'Não gerado'
  })()

  const renderDescriptionBlock = (
    key: string,
    label: string,
    modelDescription: string | null,
    userDescription: string | null,
  ) => {
    const isEditing = editingKey === key

    return (
      <div className="mt-1">
        {modelDescription && (
          <p className="m-0 text-[12.5px] whitespace-pre-wrap text-ink-3 italic">{modelDescription}</p>
        )}

        {isEditing ? (
          <div className="mt-1 flex flex-col items-start gap-1">
            <Textarea
              aria-label={`Descrição de negócio: ${label}`}
              value={currentDescription(key, userDescription)}
              onChange={(e) => handleDraftChange(key, e.target.value)}
              rows={2}
              maxLength={1000}
              placeholder="Explique o que este item significa para o negócio"
            />
            <Button size="xs" variant="ghost" onClick={() => handleStopEditing(key, userDescription)}>
              Cancelar edição
            </Button>
          </div>
        ) : (
          <div className="flex flex-wrap items-center gap-2">
            {userDescription && (
              <p className="m-0 text-[12.5px] whitespace-pre-wrap [overflow-wrap:anywhere] text-ink">{userDescription}</p>
            )}
            <button
              type="button"
              onClick={() => handleDescribe(key, userDescription)}
              aria-label={`${userDescription ? 'Editar descrição' : 'Descrever'}: ${label}`}
              className={`cursor-pointer rounded-md text-xs font-medium whitespace-nowrap focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600 ${
                key in drafts ? 'text-ava-700' : 'text-ink-3 hover:text-ava-700'
              }`}
            >
              {userDescription ? 'Editar descrição' : 'Descrever'}
            </button>
          </div>
        )}
      </div>
    )
  }

  return (
    <Modal
      size="lg"
      icon="database"
      title={`Schema — ${dataset.name}`}
      description={
        <>
          {statusLabel}
          {schema?.schemaGeneratedAt && ` em ${new Date(schema.schemaGeneratedAt).toLocaleString('pt-BR')}`}
          {schema && ` — ${schema.tables.length} tabela(s)`}
        </>
      }
      onClose={handleClose}
      footer={
        <>
          <p className="m-0 mr-auto self-center text-[12.5px] text-ink-3 max-md:mr-0 max-md:text-center">
            {dirtyKeys.length > 0
              ? `${dirtyKeys.length} alteração(ões) não salva(s)`
              : 'As descrições vão para o modelo junto com o schema.'}
          </p>
          <Button variant="primary" onClick={handleSave} loading={saving} disabled={dirtyKeys.length === 0}>
            {saving ? 'Salvando...' : 'Salvar descrições'}
          </Button>
        </>
      }
    >
      {confirmClose && (
        <Alert
          tone="err"
          live
          action={
            <div className="flex flex-wrap gap-2">
              <Button size="sm" variant="danger" onClick={onClose}>
                Fechar sem salvar
              </Button>
              <Button size="sm" onClick={() => setConfirmClose(false)}>
                Continuar editando
              </Button>
            </div>
          }
          className="flex-wrap"
        >
          Há descrições não salvas. Fechar sem salvar descarta essas alterações.
        </Alert>
      )}

      {schema?.schemaStatus === PowerBISchemaStatus.Partial && (
        <Alert tone="warn">schema parcial: medidas e tipos indisponíveis</Alert>
      )}

      {schema?.schemaError && (
        <Alert tone="err">
          <span className="whitespace-pre-wrap">{schema.schemaError}</span>
        </Alert>
      )}

      <div className="relative">
        <AdminIcon name="search" className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-ink-3" />
        <Input
          type="search"
          aria-label="Buscar tabela, coluna ou medida"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar tabela, coluna ou medida"
          className="pl-9"
        />
      </div>

      {loading ? (
        <LoadingState label="Carregando schema..." rows={4} />
      ) : !schema || schema.tables.length === 0 ? (
        <EmptyState icon="database" title="Nenhuma tabela no schema gerado." />
      ) : filteredTables.length === 0 ? (
        <EmptyState icon="search" title="Nenhum item corresponde à busca." />
      ) : (
        <div className="flex flex-col gap-2">
          {filteredTables.map((table) => (
            <details key={table.name} className="group overflow-hidden rounded-xl border border-gray-200">
              <summary className="flex min-h-12 cursor-pointer list-none items-center gap-2.5 bg-gray-50 px-3.5 py-2.5 text-left focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ava-600 [&::-webkit-details-marker]:hidden">
                <AdminIcon
                  name="chevron"
                  className="h-4 w-4 -rotate-90 text-ink-3 transition-transform group-open:rotate-0 motion-reduce:transition-none"
                />
                <b className="font-ui-mono text-[13px] font-medium [overflow-wrap:anywhere] text-ink">{table.name}</b>
                <small className="ml-auto text-xs whitespace-nowrap text-ink-3">
                  {table.columns.length} coluna(s) · {table.measures.length} medida(s)
                </small>
              </summary>

              <div className="flex flex-col gap-2 px-3.5 pt-2 pb-3.5">
                {renderDescriptionBlock(tableKey(table.name), table.name, table.description, table.userDescription)}

                <div className="mt-2">
                  <SectionLabel>Colunas</SectionLabel>
                  <div className="flex flex-col gap-2">
                    {table.columns.map((column) => (
                      <div key={column.name} className="border-l-2 border-gray-200 py-1 pl-3">
                        <p className="m-0 text-[13px] text-ink">
                          <code className="font-ui-mono text-[12.5px] [overflow-wrap:anywhere]">{column.name}</code>
                          {column.dataType && <span className="ml-1.5 text-xs text-ink-3">{column.dataType}</span>}
                        </p>
                        {renderDescriptionBlock(
                          columnKey(table.name, column.name),
                          `${table.name}.${column.name}`,
                          column.description,
                          column.userDescription,
                        )}
                      </div>
                    ))}
                  </div>
                </div>

                {table.measures.length > 0 && (
                  <div className="mt-2">
                    <SectionLabel>Medidas</SectionLabel>
                    <div className="flex flex-col gap-2">
                      {table.measures.map((measure) => (
                        <div key={measure.name} className="border-l-2 border-gray-200 py-1 pl-3">
                          <p className="m-0 text-[13px] text-ink">
                            <code className="font-ui-mono text-[12.5px] [overflow-wrap:anywhere]">{measure.name}</code>
                          </p>
                          {renderDescriptionBlock(
                            measureKey(table.name, measure.name),
                            `${table.name}.${measure.name}`,
                            measure.description,
                            measure.userDescription,
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </details>
          ))}
        </div>
      )}
    </Modal>
  )
}

export default PowerBISchemaViewer
