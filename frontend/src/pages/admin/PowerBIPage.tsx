import { useCallback, useEffect, useState } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { PowerBIService } from '../../Services/PowerBIService'
import PowerBICredentialsForm from '../../components/admin/powerbi/PowerBICredentialsForm'
import PowerBIDatasetForm from '../../components/admin/powerbi/PowerBIDatasetForm'
import PowerBIDatasetList from '../../components/admin/powerbi/PowerBIDatasetList'
import PowerBIEnableToggle from '../../components/admin/powerbi/PowerBIEnableToggle'
import PowerBIQueryLogTable from '../../components/admin/powerbi/PowerBIQueryLogTable'
import type { AgentInfo } from '../../types/agent'
import type { PowerBIConfigInfo, PowerBIDatasetInfo } from '../../types/powerbi'

const tabs = ['Credenciais', 'Datasets', 'Histórico'] as const

type PowerBITab = (typeof tabs)[number]

interface PowerBIContentProps {
  agent: AgentInfo
  onEnabledChange: (enabled: boolean) => void
}

// Renderado com key={slug} a cada troca de agente: o estado da área é por agente,
// então desmontar/montar substitui o reset manual dentro de um effect.
const PowerBIContent = ({ agent, onEnabledChange }: PowerBIContentProps) => {
  const [activeTab, setActiveTab] = useState<PowerBITab>('Credenciais')
  const [config, setConfig] = useState<PowerBIConfigInfo | null>(null)
  const [datasets, setDatasets] = useState<PowerBIDatasetInfo[]>([])
  const [showDatasetForm, setShowDatasetForm] = useState(false)
  const [editingDataset, setEditingDataset] = useState<PowerBIDatasetInfo | null>(null)
  const [configKey, setConfigKey] = useState(0)

  const loadDatasets = useCallback(async () => {
    try {
      const result = await PowerBIService.getDatasets(agent.slug)
      if (result.sucesso && result.dados) {
        setDatasets(result.dados)
      } else {
        toast.error(result.mensagem || 'Erro ao carregar os datasets')
        console.error('[PowerBIPage] loadDatasets — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[PowerBIPage] loadDatasets — exceção:', err)
      toast.error('Erro de rede ao carregar os datasets')
    }
  }, [agent.slug])

  useEffect(() => {
    PowerBIService.getConfig(agent.slug)
      .then((result) => {
        if (result.sucesso && result.dados) setConfig(result.dados)
        else console.error('[PowerBIPage] loadConfig — erro:', result.mensagem, result.erros)
      })
      .catch((err) => console.error('[PowerBIPage] loadConfig — exceção:', err))

    PowerBIService.getDatasets(agent.slug)
      .then((result) => {
        if (result.sucesso && result.dados) {
          setDatasets(result.dados)
        } else {
          toast.error(result.mensagem || 'Erro ao carregar os datasets')
          console.error('[PowerBIPage] loadDatasets — erro:', result.mensagem, result.erros)
        }
      })
      .catch((err) => {
        console.error('[PowerBIPage] loadDatasets — exceção:', err)
        toast.error('Erro de rede ao carregar os datasets')
      })
  }, [agent.slug, configKey])

  // Ao abrir a aba de datasets, as credenciais podem ter acabado de ser salvas em outra aba.
  const handleTabClick = (tab: PowerBITab) => {
    setActiveTab(tab)
    if (tab === 'Datasets') setConfigKey((k) => k + 1)
  }

  const handleDatasetSaved = async () => {
    setShowDatasetForm(false)
    setEditingDataset(null)
    await loadDatasets()
  }

  const credentialsReady = Boolean(config?.isConfigured && config?.hasClientSecret)

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-gray-900">Power BI</h1>
      <p className="text-sm text-gray-500 mt-1 mb-6">
        Credenciais, datasets e histórico de consultas do agente {agent.name}.
      </p>

      <PowerBIEnableToggle
        slug={agent.slug}
        enabled={agent.powerBIEnabled}
        onChanged={onEnabledChange}
      />

      <div className="flex gap-1 border-b border-gray-200 mb-6">
        {tabs.map((tab) => (
          <button
            key={tab}
            onClick={() => handleTabClick(tab)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
              activeTab === tab
                ? 'text-ava-700 border-ava-600'
                : 'text-gray-500 border-transparent hover:text-gray-900'
            }`}
          >
            {tab}
          </button>
        ))}
      </div>

      {activeTab === 'Credenciais' && (
        <PowerBICredentialsForm slug={agent.slug} />
      )}

      {activeTab === 'Datasets' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between gap-4">
            <p className="text-sm text-gray-500">
              Datasets que o agente pode consultar. Cada um vira uma opção na ferramenta do modelo.
            </p>
            <button
              onClick={() => {
                setEditingDataset(null)
                setShowDatasetForm(true)
              }}
              disabled={!credentialsReady || showDatasetForm}
              className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors whitespace-nowrap"
              title={!credentialsReady ? 'Configure as credenciais antes de adicionar datasets' : ''}
            >
              Adicionar dataset
            </button>
          </div>

          {showDatasetForm && (
            <PowerBIDatasetForm
              slug={agent.slug}
              dataset={editingDataset}
              onSaved={handleDatasetSaved}
              onCancel={() => {
                setShowDatasetForm(false)
                setEditingDataset(null)
              }}
            />
          )}

          <PowerBIDatasetList
            slug={agent.slug}
            datasets={datasets}
            onChange={loadDatasets}
            onEdit={(dataset) => {
              setShowDatasetForm(true)
              setEditingDataset(dataset)
            }}
          />
        </div>
      )}

      {activeTab === 'Histórico' && (
        <PowerBIQueryLogTable slug={agent.slug} />
      )}
    </div>
  )
}

const PowerBIPage = () => {
  const { selectedAgent, selectAgent } = useAgentStore()

  const handleEnabledChange = (enabled: boolean) => {
    if (!selectedAgent) return
    selectAgent({ ...selectedAgent, powerBIEnabled: enabled })
  }

  if (!selectedAgent) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-400 mb-2">Nenhum agente selecionado</p>
          <p className="text-sm text-gray-300">Selecione um agente na navbar para configurar o Power BI.</p>
        </div>
      </div>
    )
  }

  return <PowerBIContent key={selectedAgent.slug} agent={selectedAgent} onEnabledChange={handleEnabledChange} />
}

export default PowerBIPage
