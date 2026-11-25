import type { ReactNode } from 'react'
import { cx } from './cx'

interface CodeBlockProps {
  children: ReactNode
  /**
   * dark = trecho de configuração (fundo ava-950);
   * plain = texto técnico claro (consultas, prompts);
   * bare = só a tipografia mono, sem caixa (dentro de outro bloco).
   */
  variant?: 'dark' | 'plain' | 'bare'
  /** Quebra linhas longas em vez de rolar na horizontal (sempre ligado em plain/bare). */
  wrap?: boolean
  /** Limita a altura a 280 px com rolagem interna (padrão em plain). */
  limitHeight?: boolean
  className?: string
}

const VARIANTS = {
  dark: 'rounded-xl bg-ava-950 p-3.5 text-[#e6ebff]',
  plain: 'rounded-[10px] border border-gray-100 bg-gray-50 p-3 text-ink-2',
  bare: 'text-ink-2',
}

/** Bloco de código que rola dentro de si mesmo, sem estourar o cartão. */
const CodeBlock = ({ children, variant = 'dark', wrap = false, limitHeight, className }: CodeBlockProps) => {
  const limited = limitHeight ?? variant === 'plain'
  return (
    <pre
      tabIndex={variant === 'bare' ? undefined : 0}
      className={cx(
        'm-0 max-w-full overflow-auto font-ui-mono text-[12.5px] leading-[1.6]',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600',
        VARIANTS[variant],
        limited && 'max-h-[280px]',
        wrap || variant !== 'dark' ? 'whitespace-pre-wrap [overflow-wrap:anywhere]' : 'whitespace-pre',
        className,
      )}
    >
      {children}
    </pre>
  )
}

export default CodeBlock
