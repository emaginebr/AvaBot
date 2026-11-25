import { useCallback, useState } from 'react'
import { toast } from 'sonner'
import { useWhatsappAccessStore } from '../../../stores/useWhatsappAccessStore'
import { WHATSAPP_REQUEST_STATUS } from '../../../types/whatsappAccess'
import type { WhatsappAccessRequestInfo } from '../../../types/whatsappAccess'
import Button from '../ui/Button'
import DataTable from '../ui/DataTable'
import type { DataColumn } from '../ui/DataTable'
import Field from '../ui/Field'
import { Input } from '../ui/Input'
import KeyValueList from '../ui/KeyValueList'
import Modal from '../ui/Modal'
import SegmentedControl from '../ui/SegmentedControl'
import { EmptyState, LoadingState } from '../ui/States'
import { useConfirm } from '../ui/useConfirm'

interface WhatsappAccessRequestListProps {
  slug: string
}

const TABS = [
  { status: WHATSAPP_REQUEST_STATUS.PENDING, label: 'Pendentes' },
  { status: WHATSAPP_REQUEST_STATUS.APPROVED, label: 'Aprovadas' },
  { status: WHATSAPP_REQUEST_STATUS.REJECTED, label: 'Rejeitadas' },
]

const formatDate = (value: string | null) =>
  value ? new Date(value.endsWith('Z') ? value : `${value}Z`).toLocaleString('pt-BR') : ''

interface ApproveModalProps {
  slug: string
  request: WhatsappAccessRequestInfo
  onClose: () => void
}

const ApproveModal = ({ slug, request, onClose }: ApproveModalProps) => {
  const { approveRequest } = useWhatsappAccessStore()
  const [name, setName] = useState(request.profileName ?? request.phone ?? '')
  const [saving, setSaving] = useState(false)

  const handleApprove = async () => {
    setSaving(true)
    const result = await approveRequest(slug, request.whatsappAccessRequestId, name)
    setSaving(false)
    if (result.sucesso) {
      toast.success('Acesso aprovado. A pessoa já pode conversar com o agente.')
      onClose()
    } else {
      toast.error(result.mensagem || 'Não foi possível aprovar')
    }
  }

  return (
    <Modal
      title="Aprovar acesso"
      description="A pessoa vira um contato autorizado e passa a ser atendida pela IA na próxima mensagem."
      icon="check"
      iconTone="ok"
      onClose={onClose}
      footer={
        <>
          <Button onClick={onClose}>Cancelar</Button>
          <Button variant="primary" onClick={handleApprove} loading={saving}>
            {saving ? 'Aprovando...' : 'Aprovar'}
          </Button>
        </>
      }
    >
      <Field id="whatsapp-aprovar-nome" label="Nome do contato">
        <Input
          data-autofocus
          value={name}
          onChange={(e) => setName(e.target.value)}
          maxLength={120}
          placeholder="Nome de perfil do WhatsApp"
        />
      </Field>
      <KeyValueList items={[{ label: 'Telefone', value: request.phone ?? 'Não identificado', numeric: Boolean(request.phone) }]} />
    </Modal>
  )
}

const WhatsappAccessRequestList = ({ slug }: WhatsappAccessRequestListProps) => {
  const { config, requests, requestFilter, loadingRequests, setRequestFilter, rejectRequest } = useWhatsappAccessStore()
  const [approving, setApproving] = useState<WhatsappAccessRequestInfo | null>(null)
  const closeApprove = useCallback(() => setApproving(null), [])
  const [confirm, confirmDialog] = useConfirm()

  const handleReject = async (request: WhatsappAccessRequestInfo) => {
    const who = request.profileName ?? request.phone ?? 'esta pessoa'
    const ok = await confirm({
      title: 'Rejeitar pedido de acesso?',
      message: `Rejeitar o pedido de ${who}? As próximas mensagens serão ignoradas, sem aviso.`,
      confirmLabel: 'Rejeitar',
    })
    if (!ok) return
    const result = await rejectRequest(slug, request.whatsappAccessRequestId)
    if (result.sucesso) toast.success('Solicitação rejeitada')
    else toast.error(result.mensagem || 'Não foi possível rejeitar')
  }

  const pending = config?.pendingRequestCount ?? 0

  const columns: DataColumn<WhatsappAccessRequestInfo>[] = [
    {
      key: 'name',
      header: 'Nome de perfil',
      kind: 'main',
      cell: (r) => (
        <span className="block font-medium [overflow-wrap:anywhere] text-ink">
          {r.profileName ?? <span className="font-normal text-ink-3">Sem nome</span>}
        </span>
      ),
    },
    {
      key: 'phone',
      header: 'Telefone',
      nowrap: true,
      cell: (r) =>
        r.phone ? <span className="font-ui-mono tabular-nums">{r.phone}</span> : <span className="text-ink-3">Não identificado</span>,
    },
    {
      key: 'date',
      header: 'Data',
      nowrap: true,
      cell: (r) => <span className="font-ui-mono tabular-nums text-ink-2">{formatDate(r.requestedAt)}</span>,
    },
    {
      key: 'actions',
      header: 'Ações',
      hideHeader: true,
      kind: 'actions',
      cell: (r) => (
        <>
          {r.status !== WHATSAPP_REQUEST_STATUS.APPROVED && (
            <Button size="sm" variant="primary" icon="check" onClick={() => setApproving(r)}>
              Aprovar
            </Button>
          )}
          {r.status === WHATSAPP_REQUEST_STATUS.PENDING && (
            <Button size="sm" variant="dangerGhost" onClick={() => handleReject(r)}>
              Rejeitar
            </Button>
          )}
          {r.status === WHATSAPP_REQUEST_STATUS.APPROVED && (
            <span className="text-xs text-ink-3">Aprovada em {formatDate(r.decidedAt)}</span>
          )}
        </>
      ),
    },
  ]

  return (
    <div>
      <div className="mb-3.5 flex flex-wrap items-center justify-between gap-2.5">
        <p className="m-0 text-[12.5px] text-ink-3">Pedidos de quem respondeu "sim" à oferta de acesso pelo WhatsApp.</p>
        <SegmentedControl
          label="Filtrar pedidos"
          value={requestFilter}
          onChange={(status) => setRequestFilter(slug, status)}
          options={TABS.map((tab) => ({
            value: tab.status as number,
            label: `${tab.label}${tab.status === WHATSAPP_REQUEST_STATUS.PENDING && pending > 0 ? ` (${pending})` : ''}`,
          }))}
        />
      </div>

      {loadingRequests && requests.length === 0 ? (
        <LoadingState label="Carregando solicitações…" rows={3} />
      ) : requests.length === 0 ? (
        <EmptyState
          icon="user"
          title="Nenhuma solicitação"
          description="Quando alguém pedir acesso pelo WhatsApp, o pedido aparece aqui."
        />
      ) : (
        <DataTable columns={columns} rows={requests} rowKey={(r) => r.whatsappAccessRequestId} caption="Pedidos de acesso" />
      )}

      {approving && (
        <ApproveModal key={approving.whatsappAccessRequestId} slug={slug} request={approving} onClose={closeApprove} />
      )}
      {confirmDialog}
    </div>
  )
}

export default WhatsappAccessRequestList
