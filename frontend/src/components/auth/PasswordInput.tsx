import { useState } from 'react'
import type { ComponentPropsWithRef, KeyboardEvent, FocusEvent } from 'react'
import IconInput from './IconInput'
import { EyeIcon, EyeOffIcon, LockIcon } from './AuthIcons'

type PasswordInputProps = Omit<ComponentPropsWithRef<'input'>, 'className' | 'type'> & {
  id: string
  invalid?: boolean
}

/**
 * Campo de senha com mostrar/ocultar (aria-pressed) e aviso de Caps Lock.
 * O aviso usa o id `${id}-caps`; inclua-o no aria-describedby.
 */
const PasswordInput = ({ id, invalid, onKeyDown, onKeyUp, onBlur, ...props }: PasswordInputProps) => {
  const [visible, setVisible] = useState(false)
  const [capsLock, setCapsLock] = useState(false)

  const detectCaps = (e: KeyboardEvent<HTMLInputElement>) => setCapsLock(e.getModifierState('CapsLock'))

  return (
    <>
      <IconInput
        {...props}
        id={id}
        type={visible ? 'text' : 'password'}
        invalid={invalid}
        icon={<LockIcon className="h-full w-full" />}
        onKeyDown={(e) => {
          detectCaps(e)
          onKeyDown?.(e)
        }}
        onKeyUp={(e) => {
          detectCaps(e)
          onKeyUp?.(e)
        }}
        onBlur={(e: FocusEvent<HTMLInputElement>) => {
          setCapsLock(false)
          onBlur?.(e)
        }}
        trailing={
          <button
            type="button"
            aria-pressed={visible}
            aria-controls={id}
            aria-label={visible ? 'Ocultar senha' : 'Mostrar senha'}
            onClick={(e) => {
              setVisible((v) => !v)
              // Devolve o foco ao campo para a pessoa continuar digitando.
              e.currentTarget.parentElement?.querySelector('input')?.focus({ preventScroll: true })
            }}
            className="absolute right-1 grid h-[38px] w-11 cursor-pointer place-items-center rounded-[9px] text-[#5d6683] transition-colors hover:bg-ava-50 hover:text-ava-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600"
          >
            {visible ? <EyeOffIcon className="h-[18px] w-[18px]" /> : <EyeIcon className="h-[18px] w-[18px]" />}
          </button>
        }
      />
      <p id={`${id}-caps`} aria-live="polite" className={capsLock ? 'mt-1.5 text-[12.5px] text-[#9a5b00]' : 'sr-only'}>
        {capsLock ? 'Caps Lock está ativado.' : ''}
      </p>
    </>
  )
}

export default PasswordInput
