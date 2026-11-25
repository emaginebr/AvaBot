import AdminIcon from '../AdminIcon'
import type { AdminIconName } from '../AdminIcon'
import StatusBadge from '../ui/StatusBadge'
import type { StatusTone } from '../ui/StatusBadge'

type ConnectionTone = Exclude<StatusTone, 'info'>

export interface ConnectionItem {
  key: string
  label: string
  description: string
  icon: AdminIconName
  tone: ConnectionTone
  status: string
}

const ICON_TONES: Record<ConnectionTone, string> = {
  ok: 'bg-ok-bg text-ok',
  warn: 'bg-warn-bg text-warn',
  err: 'bg-err-bg text-err',
  idle: 'bg-gray-100 text-ink-2',
}

/** Lista de conexões do agente ativo (WhatsApp, Telegram, Power BI, OpenAI, MCP). */
const ConnectionStatusList = ({ items }: { items: ConnectionItem[] }) => (
  <ul className="m-0 flex list-none flex-col p-0">
    {items.map((item) => (
      <li
        key={item.key}
        className="flex items-center gap-3 border-t border-gray-100 py-3 first:border-t-0 first:pt-0 last:pb-0"
      >
        <span className={`grid h-[38px] w-[38px] flex-none place-items-center rounded-[11px] ${ICON_TONES[item.tone]}`}>
          <AdminIcon name={item.icon} className="h-5 w-5" />
        </span>
        <span className="min-w-0 flex-1">
          <strong className="block text-[13.5px] font-medium text-ink">{item.label}</strong>
          <span className="block truncate text-[12.5px] text-ink-3" title={item.description}>
            {item.description}
          </span>
        </span>
        <StatusBadge tone={item.tone} label={item.status} />
      </li>
    ))}
  </ul>
)

export default ConnectionStatusList
