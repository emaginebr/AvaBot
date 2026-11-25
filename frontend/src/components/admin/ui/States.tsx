import type { ReactNode } from 'react'
import AdminIcon from '../AdminIcon'
import type { AdminIconName } from '../AdminIcon'
import Button from './Button'
import { SkeletonRows } from './Skeleton'
import Spinner from './Spinner'
import { cx } from './cx'

interface EmptyStateProps {
  title: ReactNode
  description?: ReactNode
  icon?: AdminIconName
  /** Próximo passo (botão ou link). */
  action?: ReactNode
  /** Variante destacada para páginas inteiras (ex.: sem agente). */
  big?: boolean
  className?: string
}

/** Diz o que falta e qual é o próximo passo. */
export const EmptyState = ({ title, description, icon = 'file', action, big = false, className }: EmptyStateProps) => (
  <div
    className={cx(
      'flex flex-col items-center gap-2 rounded-xl border px-4 text-center text-ink-3',
      big
        ? 'border-ava-100 bg-[linear-gradient(180deg,var(--color-ava-50),#fff_60%)] py-14'
        : 'border-dashed border-gray-200 py-9',
      className,
    )}
  >
    <span aria-hidden="true" className="mb-1 grid h-11 w-11 place-items-center rounded-[14px] bg-ava-50 text-ava-700">
      <AdminIcon name={icon} className="h-[22px] w-[22px]" />
    </span>
    <b className="font-brand text-[14.5px] font-semibold text-ink">{title}</b>
    {description && <p className="m-0 max-w-[440px] text-[13px]">{description}</p>}
    {action && <div className="mt-2 flex flex-wrap justify-center gap-2">{action}</div>}
  </div>
)

interface LoadingStateProps {
  /** Texto lido pelos leitores de tela (e exibido na variante inline). */
  label?: string
  /** Linhas de esqueleto; 0 mostra spinner com texto. */
  rows?: number
  rowClassName?: string
}

/** Carregamento padrão: esqueleto de linhas ou spinner com texto. */
export const LoadingState = ({ label = 'Carregando…', rows = 3, rowClassName = 'h-11' }: LoadingStateProps) =>
  rows > 0 ? (
    <SkeletonRows rows={rows} className={rowClassName} label={label} />
  ) : (
    <div role="status" className="flex items-center gap-2.5 py-2 text-[13px] text-ink-2">
      <Spinner className="h-4 w-4 text-ava-600" />
      {label}
    </div>
  )

interface ErrorStateProps {
  title?: ReactNode
  description?: ReactNode
  onRetry?: () => void
  retryLabel?: string
  className?: string
}

/** Erro de carregamento no próprio conteúdo, com nova tentativa. */
export const ErrorState = ({
  title = 'Não foi possível carregar',
  description,
  onRetry,
  retryLabel = 'Tentar de novo',
  className,
}: ErrorStateProps) => (
  <div
    role="alert"
    className={cx(
      'flex flex-col items-center gap-2 rounded-xl border border-err-line bg-[#fffafa] px-4 py-[22px] text-center text-ink-3',
      className,
    )}
  >
    <span aria-hidden="true" className="mb-1 grid h-11 w-11 place-items-center rounded-[14px] bg-err-bg text-err">
      <AdminIcon name="alert" className="h-[22px] w-[22px]" />
    </span>
    <b className="font-brand text-[14.5px] font-semibold text-ink">{title}</b>
    {description && <p className="m-0 max-w-[440px] text-[13px] [overflow-wrap:anywhere]">{description}</p>}
    {onRetry && (
      <Button icon="refresh" onClick={onRetry} className="mt-2">
        {retryLabel}
      </Button>
    )}
  </div>
)

interface NoAgentStateProps {
  /** Complemento da frase: "Selecione um agente no menu lateral para {purpose}." */
  purpose: string
}

/** Páginas que dependem de um agente, quando nenhum está selecionado. */
export const NoAgentState = ({ purpose }: NoAgentStateProps) => (
  <EmptyState
    big
    icon="bot"
    title="Nenhum agente selecionado"
    description={`Selecione um agente no menu lateral para ${purpose}.`}
  />
)
