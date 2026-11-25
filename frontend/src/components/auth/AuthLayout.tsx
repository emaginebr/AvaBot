import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import BrandLogo from '../common/BrandLogo'
import { ArrowRightIcon } from './AuthIcons'

interface AuthLayoutProps {
  /** Conteúdo do cartão branco (título, avisos, formulário, links). */
  children: ReactNode
}

// Pontos brancos a 7% com máscara radial, como na proposta.
const dotsStyle = {
  backgroundImage: 'radial-gradient(circle at 1px 1px, #fff 1px, transparent 0)',
  backgroundSize: '32px 32px',
  maskImage: 'radial-gradient(ellipse at 50% 35%, #000 15%, transparent 70%)',
  WebkitMaskImage: 'radial-gradient(ellipse at 50% 35%, #000 15%, transparent 70%)',
}

/**
 * Layout das telas de autenticação: fundo azul-escuro da sidebar com decoração sutil,
 * logomarca acima e um cartão branco centralizado com o conteúdo.
 */
const AuthLayout = ({ children }: AuthLayoutProps) => (
  <div className="relative min-h-dvh bg-ava-950 bg-linear-to-b from-ava-950 via-[#161f66] via-60% to-ava-900 bg-fixed font-ui text-[14px] leading-normal text-white/78">
    <div aria-hidden="true" className="pointer-events-none fixed inset-0 z-0 overflow-hidden">
      <div className="absolute -top-[220px] -right-[180px] h-[560px] w-[560px] rounded-full bg-ava-600/45 blur-[90px]" />
      <div className="absolute -bottom-[240px] -left-[200px] h-[520px] w-[520px] rounded-full bg-ava-500/25 blur-[100px]" />
      <div className="absolute inset-0 opacity-[0.07]" style={dotsStyle} />
    </div>

    <div className="relative z-10 flex min-h-dvh flex-col">
      <header className="flex justify-start px-7 py-5 max-[620px]:px-2 max-[620px]:py-3">
        <Link
          to="/"
          className="inline-flex min-h-11 items-center gap-1.5 rounded-[10px] px-2.5 py-2 text-[13px] text-white/78 transition-colors hover:bg-white/8 hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-300"
        >
          <ArrowRightIcon className="h-3.5 w-3.5 rotate-180" />
          <span>Voltar ao site</span>
        </Link>
      </header>

      <main className="flex flex-1 flex-col items-center justify-center gap-6 px-5 pt-2 pb-8 max-[620px]:justify-start max-[620px]:gap-[18px] max-[620px]:px-3 max-[620px]:pt-0 max-[620px]:pb-6 [@media(max-height:720px)]:justify-start">
        <Link
          to="/"
          aria-label="AvaBot — página inicial"
          className="rounded-xl p-0.5 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-300"
        >
          <BrandLogo tone="light" />
        </Link>

        <div className="w-full max-w-[440px] rounded-[20px] bg-white px-9 pt-9 pb-7 text-[#0f1430] shadow-[0_24px_60px_-20px_rgb(5_8_40/0.65),0_8px_24px_-12px_rgb(31_69_241/0.35)] max-[620px]:rounded-[18px] max-[620px]:px-5 max-[620px]:pt-[26px] max-[620px]:pb-[22px]">
          {children}
        </div>
      </main>

      <footer className="px-5 pb-6 text-center text-xs text-white/62">
        © {new Date().getFullYear()} AvaBot · avabot.net
      </footer>
    </div>
  </div>
)

export default AuthLayout
