import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import type { PowerBIDatasetInfo, PowerBIWorkspaceInfo } from '../../../types/powerbi'
import Button from '../ui/Button'
import Field from '../ui/Field'
import { Input, Select, Textarea } from '../ui/Input'

interface PowerBIDatasetFormProps {
  slug: string
  dataset: PowerBIDatasetInfo | null
  onSaved: () => void
  onCancel: () => void
}

type DatasetErrors = Partial<Record<'ids' | 'name', string>>

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
  const [errors, setErrors] = useState<DatasetErrors>({})

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

  const clearIdsError = () => setErrors((prev) => (prev.ids ? { ...prev, ids: undefined } : prev))

  const handleWorkspaceChange = (value: string) => {
    setWorkspaceId(value)
    setDatasetId('')
    clearIdsError()
  }

  const handleDatasetChange = (value: string) => {
    setDatasetId(value)
    clearIdsError()

    if (nameEdited) return
    const option = selectedWorkspace?.datasets.find((ds) => ds.datasetId === value)
    if (option) setName(option.name)
  }

  const handleSubmit = async () => {
    if (!workspaceId.trim() || !datasetId.trim()) {
      setErrors({ ids: 'Selecione o workspace e o dataset.' })
      toast.error('Selecione o workspace e o dataset')
      return
    }
    if (!name.trim()) {
      setErrors({ name: 'Informe o nome do dataset.' })
      toast.error('Informe o nome do dataset')
      return
    }
    setErrors({})

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

  const idInputs = (hint?: string, placeholders?: { workspace: string; dataset: string }) => (
    <>
      <Field id="powerbi-ds-workspace-id" label="Workspace ID" hint={hint} error={errors.ids}>
        <Input
          mono
          type="text"
          value={workspaceId}
          onChange={(e) => {
            setWorkspaceId(e.target.value)
            clearIdsError()
          }}
          placeholder={placeholders?.workspace}
          spellCheck={false}
        />
      </Field>
      <Field id="powerbi-ds-dataset-id" label="Dataset ID" hint={hint} error={errors.ids}>
        <Input
          mono
          type="text"
          value={datasetId}
          onChange={(e) => {
            setDatasetId(e.target.value)
            clearIdsError()
          }}
          placeholder={placeholders?.dataset}
          spellCheck={false}
        />
      </Field>
    </>
  )

  return (
    <section
      aria-labelledby="powerbi-ds-form-titulo"
      className="rounded-2xl border border-gray-200 bg-gray-50 p-5"
    >
      <h2
        id="powerbi-ds-form-titulo"
        className="mb-3.5 font-brand text-[17px] leading-[1.3] font-semibold tracking-[-0.01em] [overflow-wrap:anywhere] text-ink"
      >
        {isEdit ? `Editar dataset: ${dataset.name}` : 'Adicionar dataset'}
      </h2>

      <div className="grid grid-cols-1 gap-x-[18px] gap-y-4 md:grid-cols-2">
        {isEdit ? (
          idInputs('Alterar este ID reseta o schema gerado.')
        ) : manualMode ? (
          <>
            {idInputs(undefined, {
              workspace: 'a9b9d4ae-0000-0000-0000-000000000000',
              dataset: 'eab95e03-0000-0000-0000-000000000000',
            })}
            <p className="m-0 text-[12.5px] text-ink-3 md:col-span-2">
              A listagem automática falhou. Os IDs aparecem na URL do relatório no Power BI.
            </p>
          </>
        ) : (
          <>
            <Field id="powerbi-ds-workspace" label="Workspace">
              <Select value={workspaceId} onChange={(e) => handleWorkspaceChange(e.target.value)} disabled={loadingWorkspaces}>
                <option value="">{loadingWorkspaces ? 'Carregando workspaces...' : 'Selecione o workspace'}</option>
                {workspaces.map((ws) => (
                  <option key={ws.workspaceId} value={ws.workspaceId}>
                    {ws.name}
                  </option>
                ))}
              </Select>
            </Field>
            <Field id="powerbi-ds-dataset" label="Dataset" error={errors.ids}>
              <Select value={datasetId} onChange={(e) => handleDatasetChange(e.target.value)} disabled={!selectedWorkspace}>
                <option value="">Selecione o dataset</option>
                {selectedWorkspace?.datasets.map((ds) => (
                  <option key={ds.datasetId} value={ds.datasetId}>
                    {ds.name}
                  </option>
                ))}
              </Select>
            </Field>
            <div className="md:col-span-2">
              <Button size="sm" variant="ghost" onClick={() => setManualMode(true)}>
                Digitar os IDs manualmente
              </Button>
            </div>
          </>
        )}

        <Field
          id="powerbi-ds-name"
          label="Nome"
          required
          hint="Usado para gerar a chave da ferramenta (ex.: comercio_internacional)."
          error={errors.name}
        >
          <Input
            type="text"
            value={name}
            onChange={(e) => {
              setName(e.target.value)
              setNameEdited(true)
              setErrors((prev) => (prev.name ? { ...prev, name: undefined } : prev))
            }}
            placeholder="Comércio Internacional"
            maxLength={120}
          />
        </Field>

        <Field
          id="powerbi-ds-description"
          label="Descrição de negócio"
          hint="Descreva o que o dataset contém; o agente usa isso para escolher o dataset."
        >
          <Textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
            maxLength={1000}
            placeholder="Exportações e importações de pescado por país, produto e mês"
          />
        </Field>
      </div>

      <div className="mt-4 flex flex-wrap justify-end gap-2.5 max-md:flex-col-reverse max-md:[&>button]:w-full">
        <Button onClick={onCancel} disabled={saving}>
          Cancelar
        </Button>
        <Button variant="primary" onClick={handleSubmit} loading={saving}>
          {saving ? 'Salvando...' : isEdit ? 'Salvar alterações' : 'Adicionar dataset'}
        </Button>
      </div>
    </section>
  )
}

export default PowerBIDatasetForm
