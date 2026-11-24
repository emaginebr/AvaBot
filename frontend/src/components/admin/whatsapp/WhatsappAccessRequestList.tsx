import { useCallback, useState } from 'react'
import { toast } from 'sonner'
import { useWhatsappAccessStore } from '../../../stores/useWhatsappAccessStore'
import { WHATSAPP_REQUEST_STATUS } from '../../../types/whatsappAccess'
import type { WhatsappAccessRequestInfo } from '../../../types/whatsappAccess'
import WhatsappModal from './WhatsappModal'

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
    <WhatsappModal
      title="Aprovar acesso"
      onClose={onClose}
      footer={
        <>
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 rounded-lg hover:bg-gray-100">
            Cancelar
          </button>
          <button
            onClick={handleApprove}
            disabled={saving}
            className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50"
          >
            {saving ? 'Aprovando...' : 'Aprovar'}
          </button>
        </>
      }
    >
      <p className="text-sm text-gray-500 mb-3">
        A pessoa vira um contato autorizado e passa a ser atendida pela IA na próxima mensagem.
      </p>
      <label className="block">
        <span className="text-sm font-medium text-gray-700">Nome do contato</span>
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          maxLength={120}
          placeholder="Nome de perfil do WhatsApp"
          className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ava-500"
        />
      </label>
      <p className="text-xs text-gray-400 mt-2">Telefone: {request.phone ?? 'Não identificado'}</p>
    </WhatsappModal>
  )
}

const WhatsappAccessRequestList = ({ slug }: WhatsappAccessRequestListProps) => {
  const { config, requests, requestFilter, loadingRequests, setRequestFilter, rejectRequest } = useWhatsappAccessStore()
  const [approving, setApproving] = useState<WhatsappAccessRequestInfo | null>(null)
  const closeApprove = useCallback(() => setApproving(null), [])

  const handleReject = async (request: WhatsappAccessRequestInfo) => {
    const who = request.profileName ?? request.phone ?? 'esta pessoa'
    if (!confirm(`Rejeitar o pedido de ${who}? As próximas mensagens serão ignoradas, sem aviso.`)) return
    const result = await rejectRequest(slug, request.whatsappAccessRequestId)
    if (result.sucesso) toast.success('Solicitação rejeitada')
    else toast.error(result.mensagem || 'Não foi possível rejeitar')
  }

  const pending = config?.pendingRequestCount ?? 0

  return (
    <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
      <div className="flex items-center gap-2 mb-1">
        <h2 className="text-lg font-semibold text-gray-900">Solicitações de acesso</h2>
        {pending > 0 && (
          <span className="px-2 py-0.5 rounded-full bg-yellow-100 text-yellow-700 text-xs font-medium">{pending} pendente{pending > 1 ? 's' : ''}</span>
        )}
      </div>
      <p className="text-sm text-gray-500 mb-4">Pedidos de quem respondeu "sim" à oferta de acesso pelo WhatsApp.</p>

      <div className="flex gap-1 border-b border-gray-100 mb-3">
        {TABS.map((tab) => (
          <button
            key={tab.status}
            onClick={() => setRequestFilter(slug, tab.status)}
            className={`px-3 py-2 text-sm -mb-px border-b-2 ${
              requestFilter === tab.status ? 'border-ava-600 text-ava-700 font-medium' : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            {tab.label}
            {tab.status === WHATSAPP_REQUEST_STATUS.PENDING && pending > 0 ? ` (${pending})` : ''}
          </button>
        ))}
      </div>

      {loadingRequests && requests.length === 0 ? (
        <p className="text-sm text-gray-400">Carregando...</p>
      ) : requests.length === 0 ? (
        <p className="text-sm text-gray-400">Nenhuma solicitação.</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-gray-500 border-b border-gray-100">
              <th className="py-2 font-medium">Nome de perfil</th>
              <th className="py-2 font-medium">Telefone</th>
              <th className="py-2 font-medium">Data</th>
              <th className="py-2" />
            </tr>
          </thead>
          <tbody>
            {requests.map((r) => (
              <tr key={r.whatsappAccessRequestId} className="border-b border-gray-50 last:border-0">
                <td className="py-2 text-gray-900">{r.profileName ?? <span className="text-gray-400">Sem nome</span>}</td>
                <td className="py-2 text-gray-600">{r.phone ?? <span className="text-gray-400">Não identificado</span>}</td>
                <td className="py-2 text-gray-500">{formatDate(r.requestedAt)}</td>
                <td className="py-2 text-right whitespace-nowrap">
                  {r.status !== WHATSAPP_REQUEST_STATUS.APPROVED && (
                    <button onClick={() => setApproving(r)} className="text-ava-600 hover:underline mr-3">Aprovar</button>
                  )}
                  {r.status === WHATSAPP_REQUEST_STATUS.PENDING && (
                    <button onClick={() => handleReject(r)} className="text-red-600 hover:underline">Rejeitar</button>
                  )}
                  {r.status === WHATSAPP_REQUEST_STATUS.APPROVED && (
                    <span className="text-xs text-gray-400">Aprovada em {formatDate(r.decidedAt)}</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {approving && (
        <ApproveModal key={approving.whatsappAccessRequestId} slug={slug} request={approving} onClose={closeApprove} />
      )}
    </div>
  )
}

export default WhatsappAccessRequestList
