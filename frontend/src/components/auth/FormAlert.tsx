import type { ReactNode, Ref } from 'react'
import { AlertCircleIcon, ClockIcon } from './AuthIcons'

interface FormAlertProps {
  /** `warning` para avisos informativos (ex.: sessão expirada); `error` para falhas (ex.: erro da API). */
  tone: 'warning' | 'error'
  children: ReactNode
  ref?: Ref<HTMLDivElement>
}

const toneStyles = {
  warning: 'border-[#f5d9a6] bg-[#fff4de] text-[#9a5b00]',
  error: 'border-[#f6c9c5] bg-[#fdeceb] text-[#b42318] motion-safe:animate-auth-shake',
} as const

/**
 * Aviso persistente acima do formulário. Erros usam role="alert" (anunciado na hora);
 * avisos usam role="status". Para repetir o "tremor" do erro, troque a `key` do componente.
 */
const FormAlert = ({ tone, children, ref }: FormAlertProps) => {
  const Icon = tone === 'error' ? AlertCircleIcon : ClockIcon
  return (
    <div
      ref={ref}
      role={tone === 'error' ? 'alert' : 'status'}
      tabIndex={tone === 'error' ? -1 : undefined}
      className={`mb-5 flex items-start gap-2.5 rounded-xl border px-3.5 py-3 text-[13.5px] outline-none ${toneStyles[tone]}`}
    >
      <Icon className="mt-px h-[18px] w-[18px] flex-none" />
      <p className="m-0">{children}</p>
    </div>
  )
}

export default FormAlert
