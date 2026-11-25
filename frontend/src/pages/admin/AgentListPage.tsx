import { useEffect } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { AgentService } from '../../Services/AgentService'
import type { AgentInfo } from '../../types/agent'
import Button, { ButtonLink } from '../../components/admin/ui/Button'
import { Card, CardHeader } from '../../components/admin/ui/Card'
import { Chip, CodeChip } from '../../components/admin/ui/Chip'
import DataTable from '../../components/admin/ui/DataTable'
import type { DataColumn } from '../../components/admin/ui/DataTable'
import PageHeader from '../../components/admin/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/admin/ui/States'
import StatusBadge from '../../components/admin/ui/StatusBadge'
import { useConfirm } from '../../components/admin/ui/useConfirm'

const plural = (n: number, one: string, many: string) => `${n} ${n === 1 ? one : many}`

const AgentListPage = () => {
  const { agents, loading, error, loadAgents } = useAgentStore()
  const [confirm, confirmDialog] = useConfirm()

  useEffect(() => {
    loadAgents()
  }, [loadAgents])

  useEffect(() => {
    if (error) toast.error(error)
  }, [error])

  const handleToggleStatus = async (id: number) => {
    try {
      const result = await AgentService.toggleStatus(id)
      if (!result.sucesso) {
        toast.error(result.mensagem || 'Erro ao alterar status do agente')
        return
      }
      toast.success('Status do agente alterado com sucesso')
      await loadAgents()
    } catch (err) {
      console.error('[AgentListPage] handleToggleStatus error:', err)
      toast.error('Erro de rede ao alterar status do agente')
    }
  }

  const handleDelete = async (id: number) => {
    try {
      const result = await AgentService.delete(id)
      if (!result.sucesso) {
        toast.error(result.mensagem || 'Erro ao remover agente')
        return
      }
      toast.success('Agente removido com sucesso')
      await loadAgents()
    } catch (err) {
      console.error('[AgentListPage] handleDelete error:', err)
      toast.error('Erro de rede ao remover agente')
    }
  }

  const askDelete = async (agent: AgentInfo) => {
    const ok = await confirm({
      title: 'Remover agente?',
      message: (
        <>
          Tem certeza que deseja remover o agente <strong>{agent.name}</strong>? Esta ação não pode ser desfeita.
        </>
      ),
      confirmLabel: 'Sim, remover',
    })
    if (ok) await handleDelete(agent.agentId)
  }

  const columns: DataColumn<AgentInfo>[] = [
    {
      key: 'name',
      header: 'Nome',
      kind: 'main',
      cell: (agent) => (
        <>
          <span className="block font-medium [overflow-wrap:anywhere] text-ink">{agent.name}</span>
          {agent.description && (
            <span className="mt-px line-clamp-2 block max-w-[420px] text-[12.5px] [overflow-wrap:anywhere] text-ink-3 max-md:max-w-none">
              {agent.description}
            </span>
          )}
        </>
      ),
    },
    { key: 'slug', header: 'Slug', cell: (agent) => <CodeChip>{agent.slug}</CodeChip> },
    {
      key: 'status',
      header: 'Situação',
      cell: (agent) =>
        agent.status === 1 ? (
          <StatusBadge tone="ok" label="Ativo" />
        ) : (
          <StatusBadge tone="idle" icon="power" label="Inativo" />
        ),
    },
    {
      key: 'collect',
      header: 'Coleta',
      cell: (agent) =>
        !agent.collectName && !agent.collectEmail && !agent.collectPhone ? (
          <span className="text-[12.5px] text-ink-3">Nenhum</span>
        ) : (
          <span className="inline-flex flex-wrap justify-end gap-1 md:justify-start">
            {agent.collectName && <Chip>Nome</Chip>}
            {agent.collectEmail && <Chip>E-mail</Chip>}
            {agent.collectPhone && <Chip>Telefone</Chip>}
          </span>
        ),
    },
    {
      key: 'actions',
      header: 'Ações',
      hideHeader: true,
      kind: 'actions',
      cell: (agent) => (
        <>
          <ButtonLink size="sm" icon="edit" to={`/admin/agents/${agent.agentId}/edit`} aria-label={`Editar ${agent.name}`}>
            Editar
          </ButtonLink>
          <Button size="sm" variant="ghost" onClick={() => handleToggleStatus(agent.agentId)}>
            {agent.status === 1 ? 'Desativar' : 'Ativar'}
          </Button>
          <Button
            size="sm"
            variant="dangerGhost"
            icon="trash"
            onClick={() => askDelete(agent)}
            aria-label={`Remover ${agent.name}`}
          >
            Remover
          </Button>
        </>
      ),
    },
  ]

  const activeCount = agents.filter((a) => a.status === 1).length

  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        title="Meus agentes"
        description="Gerencie seus agentes de atendimento: crie, edite, ative ou desative e remova."
        actions={
          <ButtonLink to="/admin/agents/new" variant="primary" size="lg" icon="plus">
            Novo agente
          </ButtonLink>
        }
      />

      <Card aria-labelledby="agentes-lista-titulo">
        <CardHeader
          titleId="agentes-lista-titulo"
          title="Agentes cadastrados"
          subtitle={
            agents.length > 0
              ? `${plural(agents.length, 'agente', 'agentes')} · ${plural(activeCount, 'ativo', 'ativos')}`
              : undefined
          }
        />
        {loading ? (
          <LoadingState label="Carregando agentes…" rows={4} />
        ) : error && agents.length === 0 ? (
          <ErrorState title="Não foi possível carregar os agentes." description={error} onRetry={() => loadAgents()} />
        ) : agents.length === 0 ? (
          <EmptyState
            icon="bot"
            title="Nenhum agente cadastrado"
            description="Crie o primeiro agente para começar a atender pelo site, Telegram ou WhatsApp."
            action={
              <ButtonLink to="/admin/agents/new" variant="primary" icon="plus">
                Criar primeiro agente
              </ButtonLink>
            }
          />
        ) : (
          <DataTable columns={columns} rows={agents} rowKey={(agent) => agent.agentId} caption="Agentes cadastrados" />
        )}
      </Card>
      {confirmDialog}
    </div>
  )
}

export default AgentListPage
