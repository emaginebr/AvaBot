import { useEffect, useMemo, useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import { PowerBISchemaStatus } from '../../../types/powerbi'
import type { PowerBIDatasetInfo, PowerBIDatasetSchemaInfo, PowerBISchemaDescriptionItemInfo } from '../../../types/powerbi'

const tableKey = (table: string) => `t:${table.toLowerCase()}`
const columnKey = (table: string, name: string) => `c:${table.toLowerCase()}|${name.toLowerCase()}`
const measureKey = (table: string, name: string) => `m:${table.toLowerCase()}|${name.toLowerCase()}`

interface PowerBISchemaViewerProps {
  slug: string
  dataset: PowerBIDatasetInfo
  onClose: () => void
}

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
    modelDescription: string | null,
    userDescription: string | null,
  ) => {
    const isEditing = editingKey === key

    return (
      <div className="mt-1">
        {modelDescription && (
          <p className="text-xs text-gray-400 italic whitespace-pre-wrap">{modelDescription}</p>
        )}

        {isEditing ? (
          <div className="mt-1">
            <textarea
              value={currentDescription(key, userDescription)}
              onChange={(e) => handleDraftChange(key, e.target.value)}
              rows={2}
              maxLength={1000}
              placeholder="Explique o que este item significa para o negócio"
              className="w-full px-2 py-1.5 border border-gray-300 rounded-lg text-xs focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent resize-y"
            />
            <button
              onClick={() => handleStopEditing(key, userDescription)}
              className="mt-1 text-xs text-gray-500 hover:text-gray-700 font-medium"
            >
              Cancelar edição
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-2">
            {userDescription && (
              <p className="text-xs text-gray-700 whitespace-pre-wrap">{userDescription}</p>
            )}
            <button
              onClick={() => handleDescribe(key, userDescription)}
              className={`text-xs font-medium whitespace-nowrap ${
                key in drafts ? 'text-ava-700' : 'text-gray-400 hover:text-gray-600'
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
    <div className="fixed inset-0 z-40 flex items-start justify-center p-4 overflow-y-auto">
      <div className="absolute inset-0 bg-black/30" onClick={handleClose} />

      <div className="relative bg-white rounded-xl border border-gray-200 shadow-lg max-w-3xl w-full my-8">
        <div className="px-5 py-4 border-b border-gray-200 flex items-start justify-between gap-4">
          <div>
            <h2 className="font-semibold text-gray-900">Schema — {dataset.name}</h2>
            <p className="text-xs text-gray-400 mt-0.5">
              {statusLabel}
              {schema?.schemaGeneratedAt && ` em ${new Date(schema.schemaGeneratedAt).toLocaleString('pt-BR')}`}
              {schema && ` — ${schema.tables.length} tabela(s)`}
            </p>
          </div>
          <button
            onClick={handleClose}
            className="text-sm text-gray-400 hover:text-gray-600 font-medium shrink-0"
          >
            Fechar
          </button>
        </div>

        {confirmClose && (
          <div className="px-5 py-4 bg-red-50 border-b border-red-200">
            <p className="text-sm text-red-700 mb-3">
              Há descrições não salvas. Fechar sem salvar descarta essas alterações.
            </p>
            <div className="flex gap-2">
              <button
                onClick={onClose}
                className="px-4 py-2 bg-red-600 text-white text-sm rounded-lg hover:bg-red-700 font-medium"
              >
                Fechar sem salvar
              </button>
              <button
                onClick={() => setConfirmClose(false)}
                className="px-4 py-2 bg-white text-gray-700 text-sm rounded-lg border border-gray-300 hover:bg-gray-50 font-medium"
              >
                Continuar editando
              </button>
            </div>
          </div>
        )}

        <div className="px-5 py-4">
          {schema?.schemaStatus === PowerBISchemaStatus.Partial && (
            <div className="mb-4 p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
              <p className="text-sm text-yellow-800">
                schema parcial: medidas e tipos indisponíveis
              </p>
            </div>
          )}

          {schema?.schemaError && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm text-red-700 whitespace-pre-wrap">{schema.schemaError}</p>
            </div>
          )}

          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar tabela, coluna ou medida"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent mb-4"
          />

          {loading ? (
            <div className="flex items-center gap-2 text-gray-500">
              <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
              Carregando schema...
            </div>
          ) : !schema || schema.tables.length === 0 ? (
            <p className="text-sm text-gray-400">Nenhuma tabela no schema gerado.</p>
          ) : filteredTables.length === 0 ? (
            <p className="text-sm text-gray-400">Nenhum item corresponde à busca.</p>
          ) : (
            <div className="space-y-2">
              {filteredTables.map((table) => (
                <details
                  key={table.name}
                  className="bg-gray-50 rounded-lg border border-gray-200"
                >
                  <summary className="px-4 py-2.5 cursor-pointer text-sm font-medium text-gray-800 hover:bg-gray-100 rounded-lg">
                    {table.name}
                    <span className="text-xs text-gray-400 font-normal ml-2">
                      {table.columns.length} coluna(s) · {table.measures.length} medida(s)
                    </span>
                  </summary>

                  <div className="px-4 pb-3">
                    {renderDescriptionBlock(tableKey(table.name), table.description, table.userDescription)}

                    <div className="mt-3">
                      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Colunas</p>
                      <div className="space-y-1">
                        {table.columns.map((column) => (
                          <div key={column.name} className="pl-3 border-l-2 border-gray-200">
                            <p className="text-sm text-gray-700">
                              {column.name}
                              {column.dataType && (
                                <span className="text-xs text-gray-400 ml-1.5">{column.dataType}</span>
                              )}
                            </p>
                            {renderDescriptionBlock(
                              columnKey(table.name, column.name),
                              column.description,
                              column.userDescription,
                            )}
                          </div>
                        ))}
                      </div>
                    </div>

                    {table.measures.length > 0 && (
                      <div className="mt-3">
                        <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Medidas</p>
                        <div className="space-y-1">
                          {table.measures.map((measure) => (
                            <div key={measure.name} className="pl-3 border-l-2 border-gray-200">
                              <p className="text-sm text-gray-700">{measure.name}</p>
                              {renderDescriptionBlock(
                                measureKey(table.name, measure.name),
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
        </div>

        <div className="px-5 py-4 border-t border-gray-200 flex items-center justify-between gap-3">
          <p className="text-xs text-gray-400">
            {dirtyKeys.length > 0
              ? `${dirtyKeys.length} alteração(ões) não salva(s)`
              : 'As descrições vão para o modelo junto com o schema.'}
          </p>
          <button
            onClick={handleSave}
            disabled={saving || dirtyKeys.length === 0}
            className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {saving ? 'Salvando...' : 'Salvar descrições'}
          </button>
        </div>
      </div>
    </div>
  )
}

export default PowerBISchemaViewer
