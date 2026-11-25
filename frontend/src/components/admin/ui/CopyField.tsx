import type { ReactNode } from 'react'
import Button from './Button'
import { cx } from './cx'

interface CopyFieldProps {
  value: string
  /** Rótulo do botão (padrão "Copiar"). */
  buttonLabel?: string
  onCopy: () => void
  /** Rótulo acessível do botão quando há vários na tela (ex.: "Copiar chave"). */
  ariaLabel?: string
  className?: string
  children?: ReactNode
}

/** Valor em fonte mono com botão de copiar ao lado; o texto quebra em qualquer ponto. */
const CopyField = ({ value, buttonLabel = 'Copiar', onCopy, ariaLabel, className, children }: CopyFieldProps) => (
  <div
    className={cx(
      'flex min-w-0 items-center gap-2 rounded-xl border border-gray-200 bg-gray-50 py-1.5 pr-1.5 pl-3',
      className,
    )}
  >
    <code className="min-w-0 flex-1 font-ui-mono text-[12.5px] [overflow-wrap:anywhere] text-ink select-all">
      {children ?? value}
    </code>
    <Button size="sm" icon="copy" onClick={onCopy} aria-label={ariaLabel}>
      {buttonLabel}
    </Button>
  </div>
)

export default CopyField
