import { DASHBOARD_PERIODS } from './period'
import type { DashboardPeriod } from './period'

interface PeriodToggleProps {
  value: DashboardPeriod
  onChange: (period: DashboardPeriod) => void
}

/** Controle segmentado 7/14/30 dias. */
const PeriodToggle = ({ value, onChange }: PeriodToggleProps) => (
  <div role="group" aria-label="Período" className="inline-flex rounded-xl border border-gray-200 bg-white p-[3px]">
    {DASHBOARD_PERIODS.map((period) => {
      const active = period === value
      return (
        <button
          key={period}
          type="button"
          aria-pressed={active}
          onClick={() => onChange(period)}
          className={`min-h-[34px] rounded-lg px-3.5 text-[13px] transition-colors motion-reduce:transition-none ${
            active ? 'bg-ava-600 text-white shadow-ava' : 'text-ink-2 hover:text-ink'
          }`}
        >
          {period} dias
        </button>
      )
    })}
  </div>
)

export default PeriodToggle
