import { useState } from 'react'
import type { InputHTMLAttributes } from 'react'
import { buttonClasses } from './buttonStyles'
import { controlClasses } from './controlStyles'
import { useFieldControl } from './fieldContext'

interface PasswordInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  mono?: boolean
  /** Mostra o botão Mostrar/Ocultar (padrão: sempre). */
  revealable?: boolean
}

/** Campo de senha/segredo com botão Mostrar/Ocultar (aria-pressed) ao lado. */
const PasswordInput = ({ mono, className, revealable = true, ...rest }: PasswordInputProps) => {
  const a11y = useFieldControl(rest)
  const [visible, setVisible] = useState(false)

  return (
    <div className="flex min-w-0 gap-2">
      <input
        {...rest}
        {...a11y}
        type={visible && revealable ? 'text' : 'password'}
        className={controlClasses({ mono, className: `flex-1 ${className ?? ''}` })}
      />
      {revealable && (
        <button
          type="button"
          aria-pressed={visible}
          aria-controls={a11y.id}
          onClick={() => setVisible((v) => !v)}
          className={buttonClasses('secondary', 'md', 'w-[84px] flex-none')}
        >
          {visible ? 'Ocultar' : 'Mostrar'}
        </button>
      )}
    </div>
  )
}

export default PasswordInput
