import type { ReactNode } from 'react'
import AdminIcon from '../AdminIcon'
import type { AdminIconName } from '../AdminIcon'
import { cx } from './cx'

export type StatusTone = 'ok' | 'warn' | 'err' | 'idle' | 'info'

const TONES: Record<StatusTone, { className: string; icon: AdminIconName }> = {
  ok: { className: 'bg-ok-bg text-ok', icon: 'check' },
  warn: { className: 'bg-warn-bg text-warn', icon: 'alert' },
  err: { className: 'bg-err-bg text-err', icon: 'x' },
  idle: { className: 'bg-gray-100 text-ink-2', icon: 'clock' },
  info: { className: 'bg-ava-50 text-ava-700', icon: 'info' },
}

interface StatusBadgeProps {
  tone: StatusTone
  label: ReactNode
  /** Ícone diferente do padrão do tom. */
  icon?: AdminIconName
  /** Ponto piscando no lugar do ícone (processando, conectando). */
  pulse?: boolean
  className?: string
}

/** Selo de status: sempre ícone + texto, nunca só cor. */
const StatusBadge = ({ tone, label, icon, pulse = false, className }: StatusBadgeProps) => (
  <span
    className={cx(
      'inline-flex items-center gap-1.5 rounded-full px-2.5 py-[3px] text-xs leading-[1.4] font-medium whitespace-nowrap',
      TONES[tone].className,
      className,
    )}
  >
    {pulse ? (
      <span aria-hidden="true" className="h-[7px] w-[7px] flex-none animate-blink rounded-full bg-current motion-reduce:animate-none" />
    ) : (
      <AdminIcon name={icon ?? TONES[tone].icon} className="h-3 w-3" />
    )}
    {label}
  </span>
)

export default StatusBadge
