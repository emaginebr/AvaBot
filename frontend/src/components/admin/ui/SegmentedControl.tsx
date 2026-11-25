import type { ReactNode } from 'react'
import { cx } from './cx'

interface SegmentedOption<T extends string | number> {
  value: T
  label: ReactNode
}

interface SegmentedControlProps<T extends string | number> {
  label: string
  value: T
  options: SegmentedOption<T>[]
  onChange: (value: T) => void
  className?: string
}

/** Grupo de botões alternados (aria-pressed), para filtros curtos. */
const SegmentedControl = <T extends string | number>({ label, value, options, onChange, className }: SegmentedControlProps<T>) => (
  <div
    role="group"
    aria-label={label}
    className={cx('inline-flex flex-wrap rounded-xl border border-gray-200 bg-white p-[3px]', className)}
  >
    {options.map((option) => {
      const active = option.value === value
      return (
        <button
          key={String(option.value)}
          type="button"
          aria-pressed={active}
          onClick={() => onChange(option.value)}
          className={cx(
            'min-h-[34px] cursor-pointer rounded-lg px-3 text-[13px] pointer-coarse:min-h-11',
            'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600',
            active ? 'bg-ava-600 text-white shadow-ava' : 'text-ink-2 hover:text-ink',
          )}
        >
          {option.label}
        </button>
      )
    })}
  </div>
)

export default SegmentedControl
