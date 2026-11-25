import type { ComponentPropsWithRef, ReactNode } from 'react'

type IconInputProps = Omit<ComponentPropsWithRef<'input'>, 'className'> & {
  /** Ícone à esquerda (decorativo). */
  icon: ReactNode
  invalid?: boolean
  /** Elemento à direita dentro do campo (ex.: botão de mostrar senha). */
  trailing?: ReactNode
}

const baseInput =
  'h-[46px] w-full rounded-xl border bg-white pl-10 text-[14.5px] text-[#0f1430] outline-none transition-[border-color,box-shadow] duration-150 placeholder:text-[#8a91a8]'
const validInput =
  'border-[#d1d5db] hover:border-[#b6bcc8] focus:border-ava-500 focus:shadow-[0_0_0_4px_var(--color-ava-100)]'
const invalidInput = 'border-[#b42318] focus:shadow-[0_0_0_4px_#fdeceb]'

/** Input com ícone à esquerda, estado inválido e slot opcional à direita. */
const IconInput = ({ icon, invalid = false, trailing, ...props }: IconInputProps) => (
  <div className="group relative flex items-center">
    <span
      aria-hidden="true"
      className={`pointer-events-none absolute left-[13px] flex h-[18px] w-[18px] transition-colors ${
        invalid ? 'text-[#b42318]' : 'text-[#5d6683] group-focus-within:text-ava-600'
      }`}
    >
      {icon}
    </span>
    <input
      {...props}
      aria-invalid={invalid || undefined}
      className={`${baseInput} ${trailing ? 'pr-[52px]' : 'pr-3.5'} ${invalid ? invalidInput : validInput}`}
    />
    {trailing}
  </div>
)

export default IconInput
