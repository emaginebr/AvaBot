import { useId } from 'react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import AdminIcon from '../AdminIcon'

interface DashboardCardProps {
  title: string
  subtitle?: ReactNode
  /** Link "ver mais" no canto do cabeçalho. */
  action?: { to: string; label: string }
  className?: string
  children: ReactNode
}

/** Cartão branco do dashboard (rounded-2xl, borda gray-200, sombra leve no hover). */
const DashboardCard = ({ title, subtitle, action, className = '', children }: DashboardCardProps) => {
  const titleId = useId()
  return (
    <article
      aria-labelledby={titleId}
      className={`min-w-0 rounded-2xl border border-gray-200 bg-white p-5 shadow-[0_1px_2px_rgb(17_24_39/0.04)] transition-[box-shadow,border-color] duration-200 hover:border-ava-100 hover:shadow-card-hover motion-reduce:transition-none ${className}`}
    >
      <div className="mb-4 flex items-center justify-between gap-3">
        <div className="min-w-0">
          <h2 id={titleId} className="font-brand text-[17px] font-semibold tracking-[-0.01em] text-ink">
            {title}
          </h2>
          {subtitle && <p className="mt-0.5 text-[12.5px] text-ink-3">{subtitle}</p>}
        </div>
        {action && (
          <Link
            to={action.to}
            className="inline-flex flex-none items-center gap-1 rounded-md px-0.5 py-1 text-[13px] font-medium text-ava-700 hover:text-ava-900 hover:underline hover:underline-offset-[3px]"
          >
            {action.label}
            <AdminIcon name="arrow" className="h-3.5 w-3.5" />
          </Link>
        )}
      </div>
      {children}
    </article>
  )
}

export default DashboardCard
