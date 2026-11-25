import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'
import AdminIcon from '../../components/admin/AdminIcon'
import type { AdminIconName } from '../../components/admin/AdminIcon'
import Button, { ButtonLink } from '../../components/admin/ui/Button'
import { Card, CardHeader, CardSection } from '../../components/admin/ui/Card'
import { CodeChip } from '../../components/admin/ui/Chip'
import KeyValueList from '../../components/admin/ui/KeyValueList'
import PageHeader from '../../components/admin/ui/PageHeader'
import { NoAgentState } from '../../components/admin/ui/States'
import StatusBadge from '../../components/admin/ui/StatusBadge'
import Switch from '../../components/admin/ui/Switch'
import { useConfirm } from '../../components/admin/ui/useConfirm'

const ActionItem = ({ icon, title, description, to }: { icon: AdminIconName; title: string; description: string; to: string }) => (
  <li className="flex items-center gap-3.5 border-t border-gray-100 py-3.5 first:border-t-0 first:pt-0 last:pb-0">
    <span aria-hidden="true" className="grid h-[38px] w-[38px] flex-none place-items-center rounded-[11px] bg-ava-50 text-ava-700">
      <AdminIcon name={icon} className="h-[18px] w-[18px]" />
    </span>
    <span className="min-w-0 flex-1">
      <b className="block font-medium text-ink">{title}</b>
      <span className="block text-[12.5px] text-ink-3">{description}</span>
    </span>
    <ButtonLink size="sm" to={to} aria-label={`Abrir ${title.toLowerCase()}`}>
      Abrir
    </ButtonLink>
  </li>
)

const AgentSettingsPage = () => {
  const { selectedAgent, selectAgent, loadAgents } = useAgentStore()
  const [deleting, setDeleting] = useState(false)
  const navigate = useNavigate()
  const [confirm, confirmDialog] = useConfirm()

  if (!selectedAgent) {
    return (
      <div className="flex flex-col gap-5">
        <PageHeader title="Configurações do agente" description="Resumo do agente selecionado, situação e ações de manutenção." />
        <NoAgentState purpose="acessar as configurações" />
      </div>
    )
  }

  const handleDelete = async () => {
    setDeleting(true)
    try {
      const result = await AgentService.delete(selectedAgent.agentId)
      if (result.sucesso) {
        toast.success('Agente excluído com sucesso')
        selectAgent(null)
        await loadAgents()
        navigate('/admin/agents')
      } else {
        toast.error(result.mensagem || 'Erro ao excluir agente')
        console.error('[AgentSettingsPage] handleDelete — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[AgentSettingsPage] handleDelete — exceção:', err)
      toast.error('Erro de rede ao excluir agente')
    } finally {
      setDeleting(false)
    }
  }

  const askDelete = async () => {
    const ok = await confirm({
      title: 'Remover agente?',
      message: (
        <>
          Tem certeza que deseja remover o agente <strong>{selectedAgent.name}</strong>? Esta ação não pode ser desfeita.
        </>
      ),
      confirmLabel: 'Sim, remover',
    })
    if (ok) await handleDelete()
  }

  const handleToggleStatus = async () => {
    try {
      const result = await AgentService.toggleStatus(selectedAgent.agentId)
      if (!result.sucesso) {
        toast.error(result.mensagem || 'Erro ao alterar status do agente')
        console.error('[AgentSettingsPage] handleToggleStatus — erro:', result.mensagem, result.erros)
        return
      }
      toast.success('Status do agente alterado com sucesso')
      await loadAgents()
      const updated = useAgentStore.getState().agents.find((a) => a.agentId === selectedAgent.agentId)
      if (updated) selectAgent(updated)
    } catch (err) {
      console.error('[AgentSettingsPage] handleToggleStatus — exceção:', err)
      toast.error('Erro de rede ao alterar status do agente')
    }
  }

  const active = selectedAgent.status === 1
  const editPath = `/admin/agents/${selectedAgent.agentId}/edit`
  const collected =
    [
      selectedAgent.collectName && 'Nome',
      selectedAgent.collectEmail && 'E-mail',
      selectedAgent.collectPhone && 'Telefone',
    ]
      .filter(Boolean)
      .join(', ') || 'Nenhum campo'

  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        agentName={selectedAgent.name}
        title="Configurações do agente"
        description="Resumo do agente selecionado, situação e ações de manutenção."
        actions={
          <ButtonLink to={editPath} variant="primary" size="lg" icon="edit">
            Editar agente
          </ButtonLink>
        }
      />

      <div className="grid grid-cols-1 gap-5 xl:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)]">
        <Card aria-labelledby="config-resumo-titulo">
          <CardHeader
            titleId="config-resumo-titulo"
            title={selectedAgent.name}
            subtitle={selectedAgent.description || 'Sem descrição'}
            actions={
              active ? <StatusBadge tone="ok" label="Ativo" /> : <StatusBadge tone="idle" icon="power" label="Inativo" />
            }
          />
          <KeyValueList
            items={[
              { label: 'Slug', value: <CodeChip>{selectedAgent.slug}</CodeChip> },
              { label: 'Coleta', value: collected },
              { label: 'Criado em', value: new Date(selectedAgent.createdAt).toLocaleString('pt-BR'), numeric: true },
            ]}
          />
          <CardSection className="flex items-center justify-between gap-4">
            <div className="min-w-0">
              <h3 id="config-ativo-rotulo" className="m-0 text-sm font-semibold text-ink">
                Agente ativo
              </h3>
              <p id="config-ativo-descricao" className="m-0 mt-[3px] text-[13px] text-ink-2">
                {active
                  ? 'Desligue para que o agente não fique disponível para novos atendimentos.'
                  : 'Ligue para que o agente fique disponível para atendimentos.'}
              </p>
            </div>
            <Switch
              checked={active}
              onChange={handleToggleStatus}
              labelledBy="config-ativo-rotulo"
              describedBy="config-ativo-descricao"
            />
          </CardSection>
        </Card>

        <Card aria-labelledby="config-acoes-titulo">
          <CardHeader titleId="config-acoes-titulo" title="Ações" subtitle="Atalhos de manutenção do agente." />
          <ul className="m-0 list-none p-0">
            <ActionItem
              icon="edit"
              title="Editar agente"
              description="Alterar nome, descrição, prompt, modelo e coleta."
              to={editPath}
            />
            <ActionItem
              icon="flask"
              title="Teste do agente"
              description="Envie uma pergunta e veja a resposta e as fontes."
              to="/admin/knowledge/test"
            />
          </ul>
        </Card>
      </div>

      <Card tone="danger" aria-labelledby="config-perigo-titulo">
        <CardHeader
          titleId="config-perigo-titulo"
          flush
          icon="trash"
          iconTone="danger"
          title="Excluir agente"
          subtitle="Remove permanentemente este agente e todos os seus dados. Esta ação não pode ser desfeita."
          actions={
            <Button variant="danger" icon="trash" onClick={askDelete} loading={deleting}>
              {deleting ? 'Removendo...' : 'Excluir agente'}
            </Button>
          }
        />
      </Card>
      {confirmDialog}
    </div>
  )
}

export default AgentSettingsPage
