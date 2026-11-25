import type { ReactNode } from 'react'
import { AlertCircleIcon } from './AuthIcons'

interface FormFieldProps {
  /** id do input; o erro recebe o id `${id}-error` e a dica `${id}-hint` (use em aria-describedby). */
  id: string
  label: string
  error?: string
  /** Texto de apoio exibido abaixo do campo. */
  hint?: ReactNode
  /** Classe extra da dica (ex.: destacar quando a regra ainda não foi atendida). */
  hintClassName?: string
  children: ReactNode
}

/** Campo de formulário: label, controle, dica opcional e mensagem de erro inline. */
const FormField = ({ id, label, error, hint, hintClassName = 'text-[#5d6683]', children }: FormFieldProps) => (
  <div>
    <label htmlFor={id} className="mb-1.5 block text-[13.5px] font-medium text-gray-700">
      {label}
    </label>
    {children}
    <p
      id={`${id}-error`}
      className={error ? 'mt-1.5 flex items-center gap-1.5 text-[12.5px] font-medium text-[#b42318]' : 'hidden'}
    >
      {error && (
        <>
          <AlertCircleIcon className="h-3.5 w-3.5 flex-none" />
          <span>{error}</span>
        </>
      )}
    </p>
    {hint && (
      <p id={`${id}-hint`} className={`mt-1.5 text-[12.5px] ${hintClassName}`}>
        {hint}
      </p>
    )}
  </div>
)

export default FormField
