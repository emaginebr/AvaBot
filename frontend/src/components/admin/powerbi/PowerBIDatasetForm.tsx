import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import type { PowerBIDatasetInfo, PowerBIWorkspaceInfo } from '../../../types/powerbi'

interface PowerBIDatasetFormProps {
  slug: string
  dataset: PowerBIDatasetInfo | null
  onSaved: () => void
  onCancel: () => void
}

const PowerBIDatasetForm = ({ slug, dataset, onSaved, onCancel }: PowerBIDatasetFormProps) => {
  const isEdit = dataset !== null

  const [workspaces, setWorkspaces] = useState<PowerBIWorkspaceInfo[]>([])
  const [loadingWorkspaces, setLoadingWorkspaces] = useState(false)
  const [manualMode, setManualMode] = useState(false)
  const [workspaceId, setWorkspaceId] = useState(dataset?.workspaceId || '')
  const [datasetId, setDatasetId] = useState(dataset?.datasetId || '')
  const [name, setName] = useState(dataset?.name || '')
  const [description, setDescription] = useState(dataset?.description || '')
  const [nameEdited, setNameEdited] = useState(isEdit)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (isEdit) return

    const loadWorkspaces = async () => {
      setLoadingWorkspaces(true)
      try {
        const result = await PowerBIService.getWorkspaces(slug)
        if (result.sucesso && result.dados && result.dados.length > 0) {
          setWorkspaces(result.dados)
          setManualMode(false)
        } else {
          setManualMode(true)
          toast.error(result.mensagem || 'Não foi possível listar os workspaces. Digite os IDs manualmente.')
          console.error('[PowerBIDatasetForm] loadWorkspaces — erro:', result.mensagem, result.erros)
        }
      } catch (err) {
        console.error('[PowerBIDatasetForm] loadWorkspaces — exceção:', err)
        setManualMode(true)
        toast.error('Erro de rede ao listar os workspaces. Digite os IDs manualmente.')
      } finally {
        setLoadingWorkspaces(false)
      }
    }

    loadWorkspaces()
  }, [slug, isEdit])

  const selectedWorkspace = workspaces.find((ws) => ws.workspaceId === workspaceId)

  const handleWorkspaceChange = (value: string) => {
    setWorkspaceId(value)
    setDatasetId('')
  }

  const handleDatasetChange = (value: string) => {
    setDatasetId(value)

    if (nameEdited) return
    const option = selectedWorkspace?.datasets.find((ds) => ds.datasetId === value)
    if (option) setName(option.name)
  }

  const handleSubmit = async () => {
    if (!workspaceId.trim() || !datasetId.trim()) {
      toast.error('Selecione o workspace e o dataset')
      return
    }
    if (!name.trim()) {
      toast.error('Informe o nome do dataset')
      return
    }

    const payload = {
      workspaceId: workspaceId.trim(),
      datasetId: datasetId.trim(),
      name: name.trim(),
      description: description.trim() || null,
    }

    setSaving(true)
    try {
      const result = isEdit
        ? await PowerBIService.updateDataset(slug, dataset.powerBIDatasetId, payload)
        : await PowerBIService.createDataset(slug, payload)

      if (result.sucesso) {
        toast.success(isEdit ? 'Dataset atualizado' : 'Dataset vinculado ao agente')
        onSaved()
      } else {
        toast.error(result.mensagem || 'Erro ao salvar o dataset')
        console.error('[PowerBIDatasetForm] handleSubmit — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[PowerBIDatasetForm] handleSubmit — exceção:', err)
      toast.error('Erro de rede ao salvar o dataset')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 p-6">
      <h2 className="text-lg font-semibold text-gray-900 mb-4">
        {isEdit ? `Editar dataset: ${dataset.name}` : 'Adicionar dataset'}
      </h2>

      {isEdit ? (
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Workspace ID</label>
            <input
              type="text"
              value={workspaceId}
              onChange={(e) => setWorkspaceId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm font-mono focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent"
            />
            <p className="text-xs text-gray-400 mt-1">Alterar este ID reseta o schema gerado.</p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Dataset ID</label>
            <input
              type="text"
              value={datasetId}
              onChange={(e) => setDatasetId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm font-mono focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent"
            />
            <p className="text-xs text-gray-400 mt-1">Alterar este ID reseta o schema gerado.</p>
          </div>
        </div>
      ) : manualMode ? (
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Workspace ID</label>
            <input
              type="text"
              value={workspaceId}
              onChange={(e) => setWorkspaceId(e.target.value)}
              placeholder="a9b9d4ae-0000-0000-0000-000000000000"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm font-mono focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Dataset ID</label>
            <input
              type="text"
              value={datasetId}
              onChange={(e) => setDatasetId(e.target.value)}
              placeholder="eab95e03-0000-0000-0000-000000000000"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm font-mono focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent"
            />
          </div>

          <p className="text-xs text-gray-400">
            A listagem automática falhou. Os IDs aparecem na URL do relatório no Power BI.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Workspace</label>
            <select
              value={workspaceId}
              onChange={(e) => handleWorkspaceChange(e.target.value)}
              disabled={loadingWorkspaces}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm bg-white focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent disabled:opacity-50"
            >
              <option value="">
                {loadingWorkspaces ? 'Carregando workspaces...' : 'Selecione o workspace'}
              </option>
              {workspaces.map((ws) => (
                <option key={ws.workspaceId} value={ws.workspaceId}>{ws.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Dataset</label>
            <select
              value={datasetId}
              onChange={(e) => handleDatasetChange(e.target.value)}
              disabled={!selectedWorkspace}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm bg-white focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent disabled:opacity-50"
            >
              <option value="">Selecione o dataset</option>
              {selectedWorkspace?.datasets.map((ds) => (
                <option key={ds.datasetId} value={ds.datasetId}>{ds.name}</option>
              ))}
            </select>
          </div>

          <button
            type="button"
            onClick={() => setManualMode(true)}
            className="text-xs text-ava-600 hover:text-ava-700 font-medium"
          >
            Digitar os IDs manualmente
          </button>
        </div>
      )}

      <div className="mt-4 space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Nome</label>
          <input
            type="text"
            value={name}
            onChange={(e) => {
              setName(e.target.value)
              setNameEdited(true)
            }}
            placeholder="Comércio Internacional"
            maxLength={120}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent"
          />
          <p className="text-xs text-gray-400 mt-1">
            Usado para gerar a chave da ferramenta (ex.: comercio_internacional).
          </p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Descrição de negócio</label>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
            maxLength={1000}
            placeholder="Exportações e importações de pescado por país, produto e mês"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ava-600 focus:border-transparent resize-y"
          />
          <p className="text-xs text-gray-400 mt-1">
            Descreva o que o dataset contém; o agente usa isso para escolher o dataset.
          </p>
        </div>
      </div>

      <div className="mt-6 pt-4 border-t border-gray-100 flex gap-3">
        <button
          onClick={handleSubmit}
          disabled={saving}
          className="px-5 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {saving ? 'Salvando...' : isEdit ? 'Salvar alterações' : 'Adicionar dataset'}
        </button>
        <button
          onClick={onCancel}
          disabled={saving}
          className="px-5 py-2 bg-white text-gray-700 text-sm rounded-lg border border-gray-300 hover:bg-gray-50 font-medium"
        >
          Cancelar
        </button>
      </div>
    </div>
  )
}

export default PowerBIDatasetForm
