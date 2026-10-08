import { useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import PowerBISchemaViewer from './PowerBISchemaViewer'
import { PowerBISchemaStatus } from '../../../types/powerbi'
import type { PowerBIDatasetInfo } from '../../../types/powerbi'

interface PowerBIDatasetListProps {
  slug: string
  datasets: PowerBIDatasetInfo[]
  onChange: () => void
  onEdit: (dataset: PowerBIDatasetInfo) => void
}

const PowerBIDatasetList = ({ slug, datasets, onChange, onEdit }: PowerBIDatasetListProps) => {
  const [generatingId, setGeneratingId] = useState<number | null>(null)
  const [deletingId, setDeletingId] = useState<number | null>(null)
  const [viewerDataset, setViewerDataset] = useState<PowerBIDatasetInfo | null>(null)

  const getStatusBadge = (dataset: PowerBIDatasetInfo) => {
    if (dataset.schemaStatus === PowerBISchemaStatus.Generated) {
      const date = dataset.schemaGeneratedAt
        ? new Date(dataset.schemaGeneratedAt).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
        : ''
      return { label: `Gerado${date ? ` em ${date}` : ''}`, className: 'bg-green-100 text-green-700' }
    }
    if (dataset.schemaStatus === PowerBISchemaStatus.Partial) {
      return { label: 'Parcial', className: 'bg-yellow-100 text-yellow-700' }
    }
    if (dataset.schemaStatus === PowerBISchemaStatus.Error) {
      return { label: 'Erro', className: 'bg-red-100 text-red-700' }
    }
    return { label: 'Não gerado', className: 'bg-gray-100 text-gray-500' }
  }

  const handleGenerateSchema = async (dataset: PowerBIDatasetInfo) => {
    setGeneratingId(dataset.powerBIDatasetId)
    try {
      const result = await PowerBIService.generateSchema(slug, dataset.powerBIDatasetId)
      if (result.sucesso) {
        toast.success(`Schema gerado: ${result.dados.tables.length} tabela(s)`)
        onChange()
      } else {
        toast.error(result.mensagem || 'Erro ao gerar o schema')
        console.error('[PowerBIDatasetList] handleGenerateSchema — erro:', result.mensagem, result.erros)
        onChange()
      }
    } catch (err) {
      console.error('[PowerBIDatasetList] handleGenerateSchema — exceção:', err)
      toast.error('Erro de rede ao gerar o schema')
    } finally {
      setGeneratingId(null)
    }
  }

  const handleDelete = async (dataset: PowerBIDatasetInfo) => {
    try {
      const result = await PowerBIService.deleteDataset(slug, dataset.powerBIDatasetId)
      if (result.sucesso) {
        toast.success(result.mensagem || 'Dataset removido')
        onChange()
      } else {
        toast.error(result.mensagem || 'Erro ao remover o dataset')
        console.error('[PowerBIDatasetList] handleDelete — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[PowerBIDatasetList] handleDelete — exceção:', err)
      toast.error('Erro de rede ao remover o dataset')
    } finally {
      setDeletingId(null)
    }
  }

  if (datasets.length === 0) {
    return (
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <p className="text-sm text-gray-400">Nenhum dataset cadastrado.</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {datasets.map((dataset) => {
        const badge = getStatusBadge(dataset)

        return (
          <div key={dataset.powerBIDatasetId} className="bg-white rounded-xl border border-gray-200 p-6">
            <div className="flex items-start justify-between gap-4">
              <div className="min-w-0">
                <h3 className="text-base font-semibold text-gray-900">{dataset.name}</h3>
                <p className="text-sm text-gray-500 mt-0.5">{dataset.description || 'Sem descrição de negócio.'}</p>
                <p className="text-xs text-gray-400 mt-1 font-mono">{dataset.toolKey}</p>
              </div>
              <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium shrink-0 ${badge.className}`}>
                {badge.label}
              </span>
            </div>

            <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-gray-500 mt-3">
              <span>{dataset.tableCount} tabela(s)</span>
              <span>{dataset.columnCount} coluna(s)</span>
              <span>{dataset.measureCount} medida(s)</span>
            </div>

            {dataset.schemaError && (
              <p className="text-xs text-red-600 mt-3 whitespace-pre-wrap">{dataset.schemaError}</p>
            )}

            {deletingId === dataset.powerBIDatasetId ? (
              <div className="mt-4 px-4 py-3 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-700 mb-3">
                  Remover <strong>{dataset.name}</strong> deste agente? O schema gerado também será descartado.
                </p>
                <div className="flex gap-2">
                  <button
                    onClick={() => handleDelete(dataset)}
                    className="px-4 py-2 bg-red-600 text-white text-sm rounded-lg hover:bg-red-700 font-medium"
                  >
                    Sim, remover
                  </button>
                  <button
                    onClick={() => setDeletingId(null)}
                    className="px-4 py-2 bg-white text-gray-700 text-sm rounded-lg border border-gray-300 hover:bg-gray-50 font-medium"
                  >
                    Cancelar
                  </button>
                </div>
              </div>
            ) : (
              <div className="flex flex-wrap gap-2 mt-4 pt-4 border-t border-gray-100">
                <button
                  onClick={() => handleGenerateSchema(dataset)}
                  disabled={generatingId === dataset.powerBIDatasetId}
                  className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {generatingId === dataset.powerBIDatasetId ? 'Gerando...' : 'Gerar schema'}
                </button>
                <button
                  onClick={() => setViewerDataset(dataset)}
                  disabled={dataset.schemaStatus === PowerBISchemaStatus.NotGenerated && !dataset.tableCount}
                  className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  Ver schema
                </button>
                <button
                  onClick={() => onEdit(dataset)}
                  className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Editar
                </button>
                <button
                  onClick={() => setDeletingId(dataset.powerBIDatasetId)}
                  className="px-4 py-2 text-sm border border-red-200 text-red-600 rounded-lg hover:bg-red-50 transition-colors"
                >
                  Remover
                </button>
              </div>
            )}
          </div>
        )
      })}

      {viewerDataset && (
        <PowerBISchemaViewer
          slug={slug}
          dataset={viewerDataset}
          onClose={() => setViewerDataset(null)}
        />
      )}
    </div>
  )
}

export default PowerBIDatasetList
