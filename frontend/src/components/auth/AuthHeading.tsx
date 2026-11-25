import type { ReactNode } from 'react'
import { ShieldCheckIcon } from './AuthIcons'

interface AuthHeadingProps {
  /** Texto do selo acima do título. */
  badge?: string
  /** Parte do título antes do destaque. */
  title: string
  /** Palavra destacada em azul com o traço sublinhado. */
  highlight: string
  lead: ReactNode
}

/** Cabeçalho do cartão de autenticação: selo, título com destaque sublinhado e subtítulo. */
const AuthHeading = ({ badge = 'Acesso seguro', title, highlight, lead }: AuthHeadingProps) => (
  <>
    <span className="mb-4 inline-flex items-center gap-2 rounded-full border border-ava-200 bg-ava-50 px-3.5 py-[5px] text-[12.5px] font-medium text-ava-700">
      <ShieldCheckIcon className="h-3.5 w-3.5" />
      {badge}
    </span>
    <h1 className="m-0 font-brand text-[30px] leading-[1.1] font-semibold tracking-[-0.025em] max-[620px]:text-[27px]">
      {title}{' '}
      <em className="relative inline-block text-ava-600 not-italic">
        {highlight}
        <svg
          viewBox="0 0 100 10"
          preserveAspectRatio="none"
          aria-hidden="true"
          className="absolute -bottom-[7px] left-0 h-2.5 w-full overflow-visible"
        >
          <path d="M2 7 Q 50 1 98 6" fill="none" stroke="#90b8ff" strokeWidth="3" strokeLinecap="round" />
        </svg>
      </em>
    </h1>
    <p className="mt-2.5 mb-6 text-[14.5px] text-[#475069]">{lead}</p>
  </>
)

export default AuthHeading
