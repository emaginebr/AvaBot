import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import AdminIcon from '../AdminIcon'
import { initialsOf } from '../layout/initials'

interface PageHeaderProps {
  title: ReactNode
  description?: ReactNode
  /** Nome do agente ativo: aparece no selo "Agente ativo · nome" acima do título. */
  agentName?: string
  /** Link de volta acima do título (páginas de detalhe/formulário). */
  backTo?: { to: string; label: string }
  /** Ações à direita; no máximo uma primária. */
  actions?: ReactNode
}

/**
 * Faixa de título das páginas admin com a decoração do avabot.net (blobs azuis, malha de
 * pontos e filete). Generaliza o DashboardHero: ocupa a largura toda do conteúdo, compensando
 * o padding do layout com margens negativas.
 */
const PageHeader = ({ title, description, agentName, backTo, actions }: PageHeaderProps) => (
  <header className="relative -mx-4 -mt-6 overflow-hidden bg-white sm:-mx-8 lg:-mt-8">
    <div aria-hidden="true" className="pointer-events-none absolute inset-0 overflow-hidden">
      <div className="absolute -top-[220px] -right-[140px] h-[460px] w-[460px] rounded-full bg-ava-100/70 blur-[56px]" />
      <div className="absolute top-[30px] -left-[120px] h-[300px] w-[300px] rounded-full bg-ava-200/45 blur-[56px]" />
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_1px_1px,#1f45f1_1px,transparent_0)] bg-[length:32px_32px] opacity-[0.035]" />
      <div className="absolute bottom-0 left-1/2 h-px w-4/5 -translate-x-1/2 bg-linear-to-r from-transparent via-gray-200 to-transparent" />
    </div>

    <div className="relative flex max-w-[1440px] flex-wrap items-end justify-between gap-4 px-4 pt-[22px] pb-5 sm:px-8 md:pt-[26px] md:pb-6">
      <div className="min-w-0 flex-[1_1_360px]">
        {backTo && (
          <Link
            to={backTo.to}
            className="mb-2.5 inline-flex min-h-8 items-center gap-1.5 rounded-lg pr-1 text-[13px] font-medium text-ava-700 hover:text-ava-900 hover:underline hover:underline-offset-[3px] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600"
          >
            <AdminIcon name="back" className="h-4 w-4" />
            {backTo.label}
          </Link>
        )}
        {agentName && (
          <span className="mb-2.5 inline-flex max-w-full items-center gap-2 rounded-full border border-ava-200 bg-ava-50 py-[3px] pr-3 pl-1 text-[12.5px] font-medium text-ava-700">
            <span
              aria-hidden="true"
              className="grid h-5 w-5 flex-none place-items-center rounded-full bg-ava-600 text-[9px] font-semibold text-white"
            >
              {initialsOf(agentName)}
            </span>
            <span className="truncate">Agente ativo · {agentName}</span>
          </span>
        )}
        <h1 className="font-brand text-[26px] leading-[1.12] font-semibold tracking-[-0.025em] [overflow-wrap:anywhere] text-ink md:text-[30px]">
          {title}
        </h1>
        {description && <p className="mt-2 max-w-[720px] text-sm text-ink-2">{description}</p>}
      </div>

      {actions && (
        <div className="flex flex-wrap items-center gap-2.5 max-md:w-full max-md:[&>*]:flex-auto">{actions}</div>
      )}
    </div>
  </header>
)

export default PageHeader
