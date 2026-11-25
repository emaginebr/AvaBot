import type { ReactNode } from 'react'
import AdminIcon from '../AdminIcon'
import type { AdminIconName } from '../AdminIcon'
import { cx } from './cx'

export type AlertTone = 'info' | 'warn' | 'err' | 'ok'

const TONES: Record<AlertTone, { className: string; icon: AdminIconName }> = {
  info: { className: 'border-ava-200 bg-ava-50 text-ava-900', icon: 'info' },
  warn: { className: 'border-[#f3d79b] bg-warn-bg text-[#6b3f00]', icon: 'alert' },
  err: { className: 'border-err-line bg-err-bg text-[#8a1a12]', icon: 'x' },
  ok: { className: 'border-[#b6e3cf] bg-ok-bg text-[#075a3f]', icon: 'check' },
}

interface AlertProps {
  tone?: AlertTone
  icon?: AdminIconName
  /** Ação à direita (ex.: "Tentar de novo"). */
  action?: ReactNode
  /** role="alert" para erros que surgem após uma ação; padrão: sem role (aviso estático). */
  live?: boolean
  className?: string
  children: ReactNode
}

/** Aviso persistente no próprio conteúdo. */
const Alert = ({ tone = 'info', icon, action, live = false, className, children }: AlertProps) => (
  <div
    role={live ? (tone === 'err' ? 'alert' : 'status') : undefined}
    className={cx('flex items-start gap-2.5 rounded-xl border px-3.5 py-3 text-[13px]', TONES[tone].className, className)}
  >
    <AdminIcon name={icon ?? TONES[tone].icon} className="mt-px h-[18px] w-[18px]" />
    <div className="min-w-0 flex-1 [overflow-wrap:anywhere]">{children}</div>
    {action && <div className="flex-none">{action}</div>}
  </div>
)

export default Alert
