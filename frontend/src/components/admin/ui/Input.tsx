import type { InputHTMLAttributes, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'
import { controlClasses } from './controlStyles'
import { cx } from './cx'
import { useFieldControl } from './fieldContext'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  /** Fonte mono (IDs, tokens, chaves). */
  mono?: boolean
}

/** Campo de texto do kit; dentro de um Field herda id, aria-describedby e aria-invalid. */
export const Input = ({ mono, className, ...rest }: InputProps) => {
  const a11y = useFieldControl(rest)
  return <input {...rest} {...a11y} className={controlClasses({ mono, className })} />
}

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  mono?: boolean
}

export const Textarea = ({ mono, className, ...rest }: TextareaProps) => {
  const a11y = useFieldControl(rest)
  return <textarea {...rest} {...a11y} className={controlClasses({ mono, className: cx('resize-y leading-[1.55]', className) })} />
}

const SELECT_ARROW =
  "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%235d6683' stroke-width='2' stroke-linecap='round'%3E%3Cpath d='m7 10 5 5 5-5'/%3E%3C/svg%3E\")"

export const Select = ({ className, style, children, ...rest }: SelectHTMLAttributes<HTMLSelectElement>) => {
  const a11y = useFieldControl(rest)
  return (
    <select
      {...rest}
      {...a11y}
      style={{ backgroundImage: SELECT_ARROW, ...style }}
      className={controlClasses({ className: cx('appearance-none bg-[length:18px] bg-[position:right_10px_center] bg-no-repeat pr-9', className) })}
    >
      {children}
    </select>
  )
}
