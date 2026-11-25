import { useCallback, useEffect, useState } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { PowerBIService } from '../../Services/PowerBIService'
import PowerBICredentialsForm from '../../components/admin/powerbi/PowerBICredentialsForm'
import PowerBIDatasetForm from '../../components/admin/powerbi/PowerBIDatasetForm'
import PowerBIDatasetList from '../../components/admin/powerbi/PowerBIDatasetList'
import PowerBIEnableToggle from '../../components/admin/powerbi/PowerBIEnableToggle'
import PowerBIQueryLogTable from '../../components/admin/powerbi/PowerBIQueryLogTable'
import Button from '../../components/admin/ui/Button'
import { Card } from '../../components/admin/ui/Card'
import PageHeader from '../../components/admin/ui/PageHeader'
import { NoAgentState } from '../../components/admin/ui/States'
import Tabs, { TabPanel } from '../../components/admin/ui/Tabs'
import type { AgentInfo } from '../../types/agent'
import type { PowerBIConfigInfo, PowerBIDatasetInfo } from '../../types/powerbi'

const tabs = ['Credenciais', 'Datasets', 'Histórico'] as const

type PowerBITab = (typeof tabs)[number]

const TAB_IDS: Record<PowerBITab, string> = {
  Credenciais: 'credenciais',
  Datasets: 'datasets',
  Histórico: 'historico',
}

interface PowerBIContentProps {
  agent: AgentInfo
  onEnabledChange: (enabled: boolean) => void
}

// Renderizado com key={slug} a cada troca de agente: o estado da área é por agente,
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
    <div className="flex flex-col gap-5">
      <PageHeader
        agentName={agent.name}
        title="Power BI"
        description={`Credenciais, datasets e histórico de consultas do agente ${agent.name}.`}
      />

      <PowerBIEnableToggle slug={agent.slug} enabled={agent.powerBIEnabled} onChanged={onEnabledChange} />

      <Card>
        <Tabs
          idPrefix="powerbi"
          label="Power BI"
          items={tabs.map((tab) => ({ id: TAB_IDS[tab], label: tab }))}
          value={TAB_IDS[activeTab]}
          onChange={(id) => {
            const tab = tabs.find((t) => TAB_IDS[t] === id)
            if (tab) handleTabClick(tab)
          }}
        />

        <TabPanel idPrefix="powerbi" tabId={TAB_IDS[activeTab]}>
          {activeTab === 'Credenciais' && <PowerBICredentialsForm slug={agent.slug} />}

          {activeTab === 'Datasets' && (
            <div className="flex flex-col gap-3.5">
              <div className="flex flex-wrap items-center justify-between gap-2.5">
                <p className="m-0 text-[12.5px] text-ink-3">
                  Datasets que o agente pode consultar. Cada um vira uma opção na ferramenta do modelo.
                </p>
                <Button
                  variant="primary"
                  icon="plus"
                  onClick={() => {
                    setEditingDataset(null)
                    setShowDatasetForm(true)
                  }}
                  disabled={!credentialsReady || showDatasetForm}
                  title={!credentialsReady ? 'Configure as credenciais antes de adicionar datasets' : ''}
                >
                  Adicionar dataset
                </Button>
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

          {activeTab === 'Histórico' && <PowerBIQueryLogTable slug={agent.slug} />}
        </TabPanel>
      </Card>
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
      <div className="flex flex-col gap-5">
        <PageHeader title="Power BI" description="Credenciais, datasets e histórico de consultas do agente." />
        <NoAgentState purpose="configurar o Power BI" />
      </div>
    )
  }

  return <PowerBIContent key={selectedAgent.slug} agent={selectedAgent} onEnabledChange={handleEnabledChange} />
}

export default PowerBIPage
