import { cx } from './cx'

interface SwitchProps {
  checked: boolean
  onChange: () => void
  disabled?: boolean
  /** id do texto que nomeia o interruptor. */
  labelledBy?: string
  /** Nome acessível quando não há rótulo visível. */
  label?: string
  describedBy?: string
  className?: string
}

/** Interruptor com role="switch"; área de toque de 44 px. */
const Switch = ({ checked, onChange, disabled, labelledBy, label, describedBy, className }: SwitchProps) => (
  <button
    type="button"
    role="switch"
    aria-checked={checked}
    aria-labelledby={labelledBy}
    aria-label={labelledBy ? undefined : label}
    aria-describedby={describedBy}
    disabled={disabled}
    onClick={onChange}
    className={cx(
      'relative h-7 w-12 flex-none cursor-pointer rounded-full p-0 transition-colors duration-200 motion-reduce:transition-none',
      'before:absolute before:-inset-2 before:content-[""]',
      'after:absolute after:top-[3px] after:left-[3px] after:h-[22px] after:w-[22px] after:rounded-full after:bg-white after:shadow-[0_1px_3px_rgb(15_20_48/0.3)] after:transition-transform after:duration-200 after:content-[""] motion-reduce:after:transition-none',
      'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600',
      'disabled:cursor-not-allowed disabled:opacity-50',
      checked ? 'bg-ava-600 after:translate-x-5' : 'bg-[#c3c8d4]',
      className,
    )}
  />
)

export default Switch
