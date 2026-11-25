import { fmtInt } from './format'

export interface AgentRankingItem {
  id: number
  name: string
  detail: string
  value: number
}

/** Ranking de agentes por sessões no período, com barras proporcionais ao primeiro colocado. */
const AgentRanking = ({ items }: { items: AgentRankingItem[] }) => {
  const top = Math.max(1, ...items.map((i) => i.value))
  return (
    <ol className="m-0 flex list-none flex-col gap-3.5 p-0">
      {items.map((item) => (
        <li key={item.id}>
          <div className="mb-1.5 flex items-baseline justify-between gap-2">
            <div className="min-w-0 truncate">
              <b className="text-[13.5px] font-medium text-ink">{item.name}</b>{' '}
              <span className="text-xs text-ink-3">· {item.detail}</span>
            </div>
            <span className="font-ui-mono text-[13px] text-ink">{fmtInt(item.value)}</span>
          </div>
          <div className="h-2 overflow-hidden rounded-full bg-gray-100" role="presentation">
            <div
              className="h-full origin-left animate-bar-grow rounded-full bg-ava-600 motion-reduce:animate-none"
              style={{ width: `${(item.value / top) * 100}%` }}
            />
          </div>
        </li>
      ))}
    </ol>
  )
}

export default AgentRanking
