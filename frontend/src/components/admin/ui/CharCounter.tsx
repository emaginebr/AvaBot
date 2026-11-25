import { cx } from './cx'

interface CharCounterProps {
  value: number
  max: number
  className?: string
}

/** Contador "n/max" em fonte mono; fica âmbar perto do limite. */
const CharCounter = ({ value, max, className }: CharCounterProps) => (
  <span
    aria-label={`${value} de ${max} caracteres`}
    className={cx(
      'ml-auto font-ui-mono text-[11.5px] tabular-nums',
      value >= max * 0.9 ? 'text-warn' : 'text-ink-3',
      className,
    )}
  >
    {value}/{max}
  </span>
)

export default CharCounter
