import type { InputHTMLAttributes, ReactNode } from 'react'
import { cx } from './cx'

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: ReactNode
}

/** Caixa de seleção em "pílula" com o rótulo clicável; destaca quando marcada. */
const Checkbox = ({ label, className, ...rest }: CheckboxProps) => (
  <label
    className={cx(
      'flex min-h-11 cursor-pointer items-center gap-2.5 rounded-xl border border-gray-200 px-3.5 text-[13.5px] transition-colors motion-reduce:transition-none',
      'hover:border-ava-200 has-checked:border-ava-300 has-checked:bg-ava-50',
      'has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-ava-600',
      className,
    )}
  >
    <input type="checkbox" {...rest} className="m-0 h-[18px] w-[18px] accent-ava-600 focus:outline-none" />
    {label}
  </label>
)

export default Checkbox
