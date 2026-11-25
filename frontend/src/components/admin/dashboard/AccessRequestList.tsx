import type { WhatsappAccessRequestInfo } from '../../../types/whatsappAccess'
import AdminIcon from '../AdminIcon'
import { initialsOf } from '../layout/initials'
import { fmtRelative, parseApiDate } from './format'

interface AccessRequestListProps {
  requests: WhatsappAccessRequestInfo[]
  now: number
  /** Pedido com ação em andamento (botões desabilitados). */
  busyId: number | null
  onApprove: (request: WhatsappAccessRequestInfo) => void
  onReject: (request: WhatsappAccessRequestInfo) => void
}

/** Pedidos de acesso pendentes do WhatsApp com aprovação/recusa direta. */
const AccessRequestList = ({ requests, now, busyId, onApprove, onReject }: AccessRequestListProps) => (
  <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
    {requests.map((request) => {
      const name = request.profileName?.trim() || 'Contato sem nome'
      const requestedAt = parseApiDate(request.requestedAt)
      const busy = busyId === request.whatsappAccessRequestId
      return (
        <li
          key={request.whatsappAccessRequestId}
          className="flex flex-col gap-2.5 rounded-xl border border-gray-100 p-3 transition-colors hover:border-ava-200 motion-reduce:transition-none"
        >
          <div className="flex items-center gap-2.5">
            <span
              aria-hidden="true"
              className={`grid h-[30px] w-[30px] flex-none place-items-center rounded-full text-[11.5px] font-semibold ${
                request.profileName ? 'bg-ok-bg text-[#0b5a40]' : 'bg-warn-bg text-[#6b3f00]'
              }`}
            >
              {request.profileName ? initialsOf(request.profileName) : '?'}
            </span>
            <div className="min-w-0">
              <b className="block truncate leading-tight font-medium text-ink">{name}</b>
              <small className="font-ui-mono text-xs text-ink-3">{request.phone ?? 'Telefone não identificado'}</small>
            </div>
            <time
              dateTime={requestedAt.toISOString()}
              title={requestedAt.toLocaleString('pt-BR')}
              className="ml-auto text-xs whitespace-nowrap text-ink-3"
            >
              {fmtRelative(requestedAt, now)}
            </time>
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={busyId !== null}
              onClick={() => onApprove(request)}
              aria-label={`Aprovar acesso de ${name}`}
              className="inline-flex min-h-9 items-center gap-1.5 rounded-xl border border-ava-600 bg-ava-600 px-3.5 text-[13px] font-medium text-white shadow-ava transition-colors hover:border-ava-700 hover:bg-ava-700 hover:shadow-ava-lg disabled:cursor-not-allowed disabled:opacity-60 motion-reduce:transition-none"
            >
              <AdminIcon name="check" className="h-3.5 w-3.5" />
              {busy ? 'Salvando…' : 'Aprovar'}
            </button>
            <button
              type="button"
              disabled={busyId !== null}
              onClick={() => onReject(request)}
              aria-label={`Recusar acesso de ${name}`}
              className="inline-flex min-h-9 items-center gap-1.5 rounded-xl border border-gray-200 bg-white px-3.5 text-[13px] font-medium text-err transition-colors hover:border-[#f6c9c5] hover:bg-err-bg disabled:cursor-not-allowed disabled:opacity-60 motion-reduce:transition-none"
            >
              <AdminIcon name="x" className="h-3.5 w-3.5" />
              Recusar
            </button>
          </div>
        </li>
      )
    })}
  </ul>
)

export default AccessRequestList
