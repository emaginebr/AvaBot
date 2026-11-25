import type { ReactNode } from 'react'
import { ArrowRightIcon } from './AuthIcons'

interface AuthSubmitButtonProps {
  loading: boolean
  /** Texto durante o envio (ex.: "Entrando..."). */
  loadingLabel: string
  children: ReactNode
}

/**
 * Botão de envio dos formulários de autenticação. Durante o envio usa aria-disabled
 * (em vez de disabled) para não perder o foco; o formulário deve ignorar envios repetidos.
 */
const AuthSubmitButton = ({ loading, loadingLabel, children }: AuthSubmitButtonProps) => (
  <button
    type="submit"
    aria-disabled={loading}
    aria-busy={loading}
    className={`group mt-1 inline-flex min-h-[46px] w-full items-center justify-center gap-2 rounded-xl border text-[14.5px] font-semibold text-white transition-[background-color,border-color,box-shadow,transform] duration-150 focus-visible:outline-2 focus-visible:outline-offset-3 focus-visible:outline-ava-600 ${
      loading
        ? 'cursor-progress border-ava-500 bg-ava-500'
        : 'cursor-pointer border-ava-600 bg-ava-600 shadow-[0_4px_6px_-1px_rgb(31_69_241/0.25),0_2px_4px_-2px_rgb(31_69_241/0.25)] hover:border-ava-700 hover:bg-ava-700 hover:shadow-[0_10px_15px_-3px_rgb(31_69_241/0.25),0_4px_6px_-4px_rgb(31_69_241/0.25)] active:translate-y-px'
    }`}
  >
    {loading && (
      <span
        aria-hidden="true"
        className="h-4 w-4 animate-spin rounded-full border-2 border-white/40 border-t-white motion-reduce:animate-none"
      />
    )}
    <span>{loading ? loadingLabel : children}</span>
    {!loading && <ArrowRightIcon className="h-4 w-4 transition-transform duration-150 group-hover:translate-x-0.5" />}
  </button>
)

export default AuthSubmitButton
