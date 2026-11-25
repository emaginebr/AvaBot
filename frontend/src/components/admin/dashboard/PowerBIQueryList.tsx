import { PowerBIQueryStatus } from '../../../types/powerbi'
import type { PowerBIQueryLogPageInfo } from '../../../types/powerbi'
import StatusPill from '../ui/StatusBadge'
import { fmtDuration, fmtInt, fmtPercent, fmtRelative, parseApiDate } from './format'

interface PowerBIQueryListProps {
  page: PowerBIQueryLogPageInfo
  now: number
  /** Quantas consultas listar. */
  visible?: number
}

/**
 * Resumo do registro de consultas Power BI do agente: total geral, taxa de sucesso e duração
 * média das últimas consultas carregadas, e as mais recentes.
 */
const PowerBIQueryList = ({ page, now, visible = 3 }: PowerBIQueryListProps) => {
  const sample = page.items
  const successes = sample.filter((q) => q.status === PowerBIQueryStatus.Success).length
  const avgMs = sample.length ? sample.reduce((sum, q) => sum + q.durationMs, 0) / sample.length : 0

  const stats = [
    { label: 'Consultas', value: fmtInt(page.total) },
    { label: `Sucesso (últ. ${sample.length})`, value: sample.length ? fmtPercent(successes / sample.length, 0) : '—' },
    { label: `Média (últ. ${sample.length})`, value: sample.length ? fmtDuration(avgMs) : '—' },
  ]

  return (
    <>
      <dl className="mb-3.5 grid grid-cols-3 gap-2">
        {stats.map((s) => (
          <div key={s.label} className="rounded-[10px] bg-gray-50 p-2.5">
            <dt className="truncate text-[11.5px] text-ink-3" title={s.label}>{s.label}</dt>
            <dd className="font-brand text-xl font-semibold tracking-[-0.02em] text-ink tabular-nums">{s.value}</dd>
          </div>
        ))}
      </dl>
      <ul className="m-0 list-none p-0">
        {sample.slice(0, visible).map((q) => {
          const at = parseApiDate(q.createdAt)
          return (
            <li
              key={q.powerBIQueryLogId}
              className="flex items-start gap-2.5 border-t border-gray-100 py-2.5 first:border-t-0 first:pt-0"
            >
              <div className="min-w-0 flex-1 text-[13px] text-ink">
                <span className="line-clamp-2">{q.userQuestion?.trim() || q.toolName}</span>
                <span className="mt-0.5 block text-xs text-ink-3">
                  {q.datasetName ?? q.toolName} · {fmtRelative(at, now)}
                </span>
              </div>
              {q.status === PowerBIQueryStatus.Success ? (
                <span className="font-ui-mono text-xs whitespace-nowrap text-ink-2">{fmtInt(q.durationMs)} ms</span>
              ) : (
                <StatusPill tone="err" label={q.status === PowerBIQueryStatus.Timeout ? 'Tempo esgotado' : 'Falhou'} />
              )}
            </li>
          )
        })}
      </ul>
    </>
  )
}

export default PowerBIQueryList
