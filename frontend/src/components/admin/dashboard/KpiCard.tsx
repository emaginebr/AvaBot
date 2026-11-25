import type { ReactNode } from 'react'
import AdminIcon from '../AdminIcon'
import type { AdminIconName } from '../AdminIcon'
import Skeleton from '../ui/Skeleton'
import Sparkline from './Sparkline'
import { fmtPercent } from './format'

interface KpiCardProps {
  icon: AdminIconName
  label: string
  value: ReactNode
  /** Texto menor ao lado do valor (ex.: "de 8"). */
  suffix?: string
  /** Variação relativa ao período anterior (0,12 = +12%); null oculta o selo. */
  delta?: number | null
  meta?: ReactNode
  spark?: number[]
  /** Usa a fonte mono tabular no valor (contagens grandes). */
  numeric?: boolean
  loading?: boolean
}

/** Indicador do topo do dashboard: rótulo com ícone, valor grande, variação e minigráfico. */
const KpiCard = ({ icon, label, value, suffix, delta, meta, spark, numeric = false, loading = false }: KpiCardProps) => (
  <article className="relative flex min-w-0 flex-col gap-2.5 overflow-hidden rounded-2xl border border-gray-200 bg-white p-5 shadow-[0_1px_2px_rgb(17_24_39/0.04)] transition-[box-shadow,border-color] duration-200 hover:border-ava-100 hover:shadow-card-hover motion-reduce:transition-none">
    <div className="flex items-center justify-between gap-2">
      <h3 className="flex min-w-0 items-center gap-2 text-[13px] font-normal text-ink-2">
        <span className="grid h-[30px] w-[30px] flex-none place-items-center rounded-[9px] bg-ava-50 text-ava-700">
          <AdminIcon name={icon} className="h-4 w-4" />
        </span>
        <span className="truncate">{label}</span>
      </h3>
      {!loading && spark && <Sparkline values={spark} />}
    </div>

    {loading ? (
      <div role="status" className="flex flex-col gap-2.5">
        <span className="sr-only">Carregando {label}…</span>
        <Skeleton className="h-[34px] w-24" />
        <Skeleton className="h-4 w-36" />
      </div>
    ) : (
      <>
        <p
          className={`text-[34px] leading-none font-semibold tracking-[-0.03em] text-ink tabular-nums max-sm:text-[30px] ${
            numeric ? 'font-ui-mono tracking-[-0.01em]' : 'font-brand'
          }`}
        >
          {value}
          {suffix && <small className="ml-1 font-ui text-sm font-normal tracking-normal text-ink-3">{suffix}</small>}
        </p>
        <div className="flex min-h-5 flex-wrap items-center justify-between gap-2">
          {delta !== undefined && delta !== null && (
            <span
              className={`inline-flex items-center gap-[3px] rounded-full px-2 py-0.5 text-xs font-medium ${
                delta >= 0 ? 'bg-ok-bg text-ok' : 'bg-err-bg text-err'
              }`}
            >
              <AdminIcon name={delta >= 0 ? 'up' : 'down'} className="h-3 w-3" />
              <span className="sr-only">{delta >= 0 ? 'Alta de' : 'Queda de'}</span>
              {fmtPercent(Math.abs(delta))}
            </span>
          )}
          {meta && <span className="text-xs text-ink-3">{meta}</span>}
        </div>
      </>
    )}
  </article>
)

export default KpiCard
