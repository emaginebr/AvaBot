import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import AdminIcon from '../AdminIcon'

interface DashboardHeroProps {
  greeting: string
  /** Frase destacada em ava-600 com sublinhado curvo (ex.: "Tudo em ordem."). */
  highlight?: string
  /** Texto do selo com ponto pulsante. */
  pill?: string
  dateLabel: string
  /** Controles à direita (filtro de período). */
  actions?: ReactNode
}

/**
 * Cabeçalho de boas-vindas com a decoração do hero de avabot.net: blobs azuis desfocados,
 * malha de pontos, filete em degradê, selo pulsante e destaque com sublinhado curvo.
 * Ocupa a largura toda do conteúdo (margens negativas compensam o padding do layout).
 */
const DashboardHero = ({ greeting, highlight, pill, dateLabel, actions }: DashboardHeroProps) => (
  <section aria-labelledby="dashboard-titulo" className="relative -mx-4 -mt-6 overflow-hidden bg-white sm:-mx-8 lg:-mt-8">
    <div aria-hidden="true" className="pointer-events-none absolute inset-0 overflow-hidden">
      <div className="absolute -top-[260px] -right-40 h-[600px] w-[600px] rounded-full bg-ava-100/70 blur-[64px]" />
      <div className="absolute top-10 -left-32 h-[400px] w-[400px] rounded-full bg-ava-200/50 blur-[64px]" />
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_1px_1px,#1f45f1_1px,transparent_0)] bg-[length:32px_32px] opacity-[0.035]" />
      <div className="absolute bottom-0 left-1/2 h-px w-4/5 -translate-x-1/2 bg-linear-to-r from-transparent via-gray-200 to-transparent" />
    </div>

    <div className="relative flex max-w-[1440px] flex-wrap items-end justify-between gap-4 px-4 pt-7 pb-6 sm:px-8 lg:pt-9 lg:pb-8">
      <div className="min-w-0">
        {pill && (
          <div className="mb-3.5 inline-flex items-center gap-2 rounded-full border border-ava-200 bg-ava-50 px-3.5 py-[5px] text-[12.5px] font-medium text-ava-700">
            <span aria-hidden="true" className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-ava-400 opacity-75 motion-reduce:animate-none" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-ava-500" />
            </span>
            {pill}
          </div>
        )}
        <h1
          id="dashboard-titulo"
          className="font-brand text-[26px] leading-[1.1] font-semibold tracking-[-0.025em] text-ink lg:text-[32px]"
        >
          {greeting}{' '}
          {highlight && (
            <em className="relative inline-block text-ava-600 not-italic">
              {highlight}
              <svg
                viewBox="0 0 200 12"
                fill="none"
                preserveAspectRatio="none"
                aria-hidden="true"
                className="absolute -bottom-1.5 left-0 h-2.5 w-full overflow-visible"
              >
                <path
                  d="M2 8.5C50 2.5 150 2.5 198 8.5"
                  stroke="currentColor"
                  strokeWidth="3"
                  strokeLinecap="round"
                  vectorEffect="non-scaling-stroke"
                />
              </svg>
            </em>
          )}
        </h1>
        <p className="mt-3 text-[13.5px] text-ink-3 first-letter:uppercase">{dateLabel}</p>
      </div>

      <div className="flex flex-wrap items-center gap-2.5">
        {actions}
        <Link
          to="/admin/agents/new"
          className="inline-flex min-h-[42px] items-center gap-1.5 rounded-xl border border-ava-600 bg-ava-600 px-[18px] text-[13px] font-semibold text-white shadow-ava transition-colors hover:border-ava-700 hover:bg-ava-700 hover:shadow-ava-lg motion-reduce:transition-none"
        >
          Novo agente
          <AdminIcon name="arrow" className="h-3.5 w-3.5" />
        </Link>
      </div>
    </div>
  </section>
)

export default DashboardHero
