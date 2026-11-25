import { Link } from 'react-router-dom'
import type { ChatSessionInfo } from '../../../types/chatSession'
import { initialsOf } from '../layout/initials'
import { fmtInt, fmtRelative, parseApiDate } from './format'

export interface RecentSessionRow {
  session: ChatSessionInfo
  agentName: string
}

interface RecentSessionsTableProps {
  rows: RecentSessionRow[]
  /** Referência para os tempos relativos ("há 3 min"). */
  now: number
}

const AVATAR_TONES = [
  'bg-ava-100 text-ava-800',
  'bg-ok-bg text-[#0b5a40]',
  'bg-warn-bg text-[#6b3f00]',
  'bg-[#f1e9ff] text-[#4b2a8a]',
]

/** Últimas sessões de todos os agentes; cada linha abre a conversa. */
const RecentSessionsTable = ({ rows, now }: RecentSessionsTableProps) => (
  <div className="-mx-5 -mb-5 overflow-x-auto">
    <table className="w-full border-collapse text-[13px]">
      <thead>
        <tr>
          {['Usuário', 'Agente', 'Msgs', 'Início', 'Situação'].map((h) => (
            <th
              key={h}
              scope="col"
              className={`border-b border-gray-200 bg-gray-50 px-5 py-2.5 text-xs font-medium whitespace-nowrap text-ink-3 ${
                h === 'Msgs' ? 'text-right' : 'text-left'
              }`}
            >
              {h}
            </th>
          ))}
        </tr>
      </thead>
      <tbody className="[&>tr:last-child>td]:border-b-0">
        {rows.map(({ session, agentName }) => {
          const name = session.userName?.trim() || 'Anônimo'
          const contact = session.userEmail || session.userPhone || `Sessão #${session.chatSessionId}`
          const started = parseApiDate(session.startedAt)
          return (
            <tr key={session.chatSessionId} className="transition-colors hover:bg-ava-50 motion-reduce:transition-none">
              <td className="border-b border-gray-100 px-5 py-3 whitespace-nowrap">
                <Link
                  to={`/admin/sessions/${session.chatSessionId}`}
                  className="flex items-center gap-2.5 rounded-md"
                  title="Abrir conversa"
                >
                  <span
                    aria-hidden="true"
                    className={`grid h-[30px] w-[30px] flex-none place-items-center rounded-full text-[11.5px] font-semibold ${
                      AVATAR_TONES[session.chatSessionId % AVATAR_TONES.length]
                    }`}
                  >
                    {session.userName ? initialsOf(session.userName) : '?'}
                  </span>
                  <span className="min-w-0">
                    <b className="block leading-tight font-medium text-ink">{name}</b>
                    <small className="text-xs text-ink-3">{contact}</small>
                  </span>
                </Link>
              </td>
              <td className="border-b border-gray-100 px-5 py-3 whitespace-nowrap text-ink-2">{agentName}</td>
              <td className="border-b border-gray-100 px-5 py-3 text-right font-ui-mono whitespace-nowrap">
                {fmtInt(session.messageCount ?? 0)}
              </td>
              <td className="border-b border-gray-100 px-5 py-3 whitespace-nowrap text-ink-2">
                <time dateTime={started.toISOString()} title={started.toLocaleString('pt-BR')}>
                  {fmtRelative(started, now)}
                </time>
              </td>
              <td className="border-b border-gray-100 px-5 py-3 whitespace-nowrap">
                {session.endedAt ? (
                  <span className="text-xs text-ink-3">Encerrada</span>
                ) : (
                  <span className="inline-flex items-center gap-1.5 text-xs font-medium text-ok">
                    <i
                      aria-hidden="true"
                      className="h-[7px] w-[7px] rounded-full bg-ok shadow-[0_0_0_3px_var(--color-ok-bg)]"
                    />
                    Em andamento
                  </span>
                )}
              </td>
            </tr>
          )
        })}
      </tbody>
    </table>
  </div>
)

export default RecentSessionsTable
