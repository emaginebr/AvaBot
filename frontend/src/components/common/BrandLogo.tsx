interface BrandLogoProps {
  /** `light` para fundos escuros (nome em branco); `dark` para fundos claros. */
  tone?: 'light' | 'dark'
  /** `md` (padrão) nas telas de autenticação; `sm` na sidebar compacta do painel. */
  size?: 'sm' | 'md'
  className?: string
}

const SIZES = {
  md: {
    mark: 'h-10 w-10 rounded-xl shadow-[0_6px_18px_-4px_rgb(31_69_241/0.8),inset_0_0_0_1px_rgb(255_255_255/0.12)]',
    icon: 'h-[22px] w-[22px]',
    name: 'text-[22px] leading-7',
    gap: 'gap-2.5',
  },
  sm: {
    mark: 'h-8 w-8 rounded-[10px] shadow-[0_4px_12px_-2px_rgb(31_69_241/0.55)]',
    icon: 'h-[18px] w-[18px]',
    name: 'text-lg leading-[26px]',
    gap: 'gap-2.5',
  },
} as const

/**
 * Logomarca do AvaBot: quadrado ava-600 com os dois balões de chat + nome.
 * Mesmos paths SVG usados na LandingPage. Não é um link; quem usa decide se embrulha em <Link>.
 */
const BrandLogo = ({ tone = 'dark', size = 'md', className = '' }: BrandLogoProps) => {
  const s = SIZES[size]
  return (
    <span className={`inline-flex items-center ${s.gap} ${className}`}>
      <span
        aria-hidden="true"
        className={`flex flex-none items-center justify-center bg-ava-600 text-white ${s.mark}`}
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className={s.icon}>
          <path d="M4.913 2.658c2.075-.27 4.19-.408 6.337-.408 2.147 0 4.262.139 6.337.408 1.922.25 3.291 1.861 3.405 3.727a4.403 4.403 0 00-1.032-.211 50.89 50.89 0 00-8.42 0c-2.358.196-4.04 2.19-4.04 4.434v4.286a4.47 4.47 0 002.433 3.984L7.28 21.53A.75.75 0 016 21v-4.03a48.527 48.527 0 01-1.087-.128C2.905 16.58 1.5 14.833 1.5 12.862V6.638c0-1.97 1.405-3.718 3.413-3.979z" />
          <path d="M15.75 7.5c-1.376 0-2.739.057-4.086.169C10.124 7.797 9 9.103 9 10.609v4.285c0 1.507 1.128 2.814 2.67 2.94 1.243.102 2.5.157 3.768.165l2.782 2.781a.75.75 0 001.28-.53v-2.39l.33-.026c1.542-.125 2.67-1.433 2.67-2.94v-4.286c0-1.505-1.125-2.811-2.664-2.94A49.392 49.392 0 0015.75 7.5z" />
        </svg>
      </span>
      <span
        className={`${s.name} font-bold tracking-[-0.025em] ${tone === 'light' ? 'text-white' : 'text-gray-900'}`}
      >
        AvaBot
      </span>
    </span>
  )
}

export default BrandLogo
