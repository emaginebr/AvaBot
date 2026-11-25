import { useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import PowerBISchemaViewer from './PowerBISchemaViewer'
import { PowerBISchemaStatus } from '../../../types/powerbi'
import type { PowerBIDatasetInfo } from '../../../types/powerbi'
import Alert from '../ui/Alert'
import Button from '../ui/Button'
import { CodeChip } from '../ui/Chip'
import { EmptyState } from '../ui/States'
import StatusBadge from '../ui/StatusBadge'
import type { StatusTone } from '../ui/StatusBadge'
import { useConfirm } from '../ui/useConfirm'

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
  const [confirm, confirmDialog] = useConfirm()

  const getStatusBadge = (dataset: PowerBIDatasetInfo): { label: string; tone: StatusTone } => {
    if (dataset.schemaStatus === PowerBISchemaStatus.Generated) {
      const date = dataset.schemaGeneratedAt
        ? new Date(dataset.schemaGeneratedAt).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
        : ''
      return { label: `Gerado${date ? ` em ${date}` : ''}`, tone: 'ok' }
    }
    if (dataset.schemaStatus === PowerBISchemaStatus.Partial) {
      return { label: 'Parcial', tone: 'warn' }
    }
    if (dataset.schemaStatus === PowerBISchemaStatus.Error) {
      return { label: 'Erro', tone: 'err' }
    }
    return { label: 'Não gerado', tone: 'idle' }
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

  const askDelete = async (dataset: PowerBIDatasetInfo) => {
    const ok = await confirm({
      title: 'Remover dataset?',
      message: (
        <>
          Remover <strong>{dataset.name}</strong> deste agente? O schema gerado também será descartado.
        </>
      ),
      confirmLabel: 'Sim, remover',
    })
    if (!ok) return
    setDeletingId(dataset.powerBIDatasetId)
    await handleDelete(dataset)
  }

  if (datasets.length === 0) {
    return (
      <EmptyState
        icon="database"
        title="Nenhum dataset cadastrado"
        description="Adicione um dataset para que o agente possa consultá-lo."
      />
    )
  }

  return (
    <div className="flex flex-col gap-3">
      {datasets.map((dataset) => {
        const badge = getStatusBadge(dataset)

        return (
          <article
            key={dataset.powerBIDatasetId}
            aria-labelledby={`powerbi-ds-${dataset.powerBIDatasetId}`}
            className="flex min-w-0 flex-col gap-2.5 rounded-xl border border-gray-200 p-4"
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="min-w-0 flex-[1_1_260px]">
                <h3 id={`powerbi-ds-${dataset.powerBIDatasetId}`} className="m-0 text-[15px] font-semibold [overflow-wrap:anywhere] text-ink">
                  {dataset.name}
                </h3>
                <p className="m-0 mt-0.5 text-[13px] [overflow-wrap:anywhere] text-ink-2">
                  {dataset.description || 'Sem descrição de negócio.'}
                </p>
                <CodeChip className="mt-1.5 inline-block">{dataset.toolKey}</CodeChip>
              </div>
              <StatusBadge tone={badge.tone} label={badge.label} />
            </div>

            <div className="flex flex-wrap gap-x-4 gap-y-1.5 text-[12.5px] text-ink-3">
              <span>
                <b className="font-ui-mono font-medium text-ink">{dataset.tableCount}</b> tabela(s)
              </span>
              <span>
                <b className="font-ui-mono font-medium text-ink">{dataset.columnCount}</b> coluna(s)
              </span>
              <span>
                <b className="font-ui-mono font-medium text-ink">{dataset.measureCount}</b> medida(s)
              </span>
            </div>

            {dataset.schemaError && (
              <Alert tone="err">
                <span className="whitespace-pre-wrap">{dataset.schemaError}</span>
              </Alert>
            )}

            <div className="flex flex-wrap gap-2">
              <Button
                size="sm"
                variant="primary"
                icon="refresh"
                onClick={() => handleGenerateSchema(dataset)}
                loading={generatingId === dataset.powerBIDatasetId}
              >
                {generatingId === dataset.powerBIDatasetId ? 'Gerando...' : 'Gerar schema'}
              </Button>
              <Button
                size="sm"
                icon="eye"
                onClick={() => setViewerDataset(dataset)}
                disabled={dataset.schemaStatus === PowerBISchemaStatus.NotGenerated && !dataset.tableCount}
              >
                Ver schema
              </Button>
              <Button size="sm" variant="ghost" icon="edit" onClick={() => onEdit(dataset)}>
                Editar
              </Button>
              <Button
                size="sm"
                variant="dangerGhost"
                icon="trash"
                onClick={() => askDelete(dataset)}
                loading={deletingId === dataset.powerBIDatasetId}
                aria-label={`Remover ${dataset.name}`}
              >
                Remover
              </Button>
            </div>
          </article>
        )
      })}

      {viewerDataset && (
        <PowerBISchemaViewer
          slug={slug}
          dataset={viewerDataset}
          onClose={() => setViewerDataset(null)}
        />
      )}
      {confirmDialog}
    </div>
  )
}

export default PowerBIDatasetList
