import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

/** Rodapé do cartão de autenticação (links alternativos), separado por uma linha. */
export const AuthCardFooter = ({ children }: { children: ReactNode }) => (
  <div className="mt-6 flex flex-col gap-1.5 border-t border-gray-100 pt-5 text-center text-[13.5px] text-[#475069]">
    {children}
  </div>
)

/** Link de texto destacado usado no rodapé do cartão. */
export const AuthTextLink = ({ to, children }: { to: string; children: ReactNode }) => (
  <Link
    to={to}
    className="rounded-md p-0.5 font-semibold text-ava-700 hover:text-ava-900 hover:underline hover:underline-offset-3 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600"
  >
    {children}
  </Link>
)
