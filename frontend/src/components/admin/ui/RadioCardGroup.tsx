import type { ReactNode } from 'react'
import { cx } from './cx'

interface RadioCardOption<T extends string | number> {
  value: T
  title: ReactNode
  description?: ReactNode
}

interface RadioCardGroupProps<T extends string | number> {
  name: string
  legend: ReactNode
  /** Esconde a legenda visualmente (continua para leitores de tela). */
  hideLegend?: boolean
  value: T
  options: RadioCardOption<T>[]
  onChange: (value: T) => void
  className?: string
}

/** Opções exclusivas em cartões (rádio nativo, setas do teclado funcionam). */
const RadioCardGroup = <T extends string | number>({
  name,
  legend,
  hideLegend = false,
  value,
  options,
  onChange,
  className,
}: RadioCardGroupProps<T>) => (
  <fieldset className={cx('m-0 min-w-0 border-0 p-0', className)}>
    <legend className={hideLegend ? 'sr-only' : 'mb-2 p-0 text-[13px] font-medium text-ink'}>{legend}</legend>
    <div className="grid grid-cols-1 gap-2.5 md:grid-cols-2">
      {options.map((option) => (
        <label
          key={String(option.value)}
          className={cx(
            'flex cursor-pointer items-start gap-3 rounded-xl border border-gray-200 p-3.5 transition-colors motion-reduce:transition-none',
            'hover:border-ava-200 has-checked:border-ava-500 has-checked:bg-ava-50 has-checked:shadow-[inset_0_0_0_1px_var(--color-ava-500)]',
            'has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-ava-600',
          )}
        >
          <input
            type="radio"
            name={name}
            checked={value === option.value}
            onChange={() => onChange(option.value)}
            className="m-0 mt-0.5 h-[18px] w-[18px] flex-none accent-ava-600 focus:outline-none"
          />
          <span className="min-w-0">
            <b className="block text-[13.5px] font-semibold text-ink">{option.title}</b>
            {option.description && <span className="mt-0.5 block text-[12.5px] text-ink-2">{option.description}</span>}
          </span>
        </label>
      ))}
    </div>
  </fieldset>
)

export default RadioCardGroup
