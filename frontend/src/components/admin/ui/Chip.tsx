import type { ReactNode } from 'react'
import { cx } from './cx'

/** Etiqueta neutra em pílula (ex.: campos coletados, origem do contato). */
export const Chip = ({ children, className }: { children: ReactNode; className?: string }) => (
  <span
    className={cx(
      'inline-flex items-center gap-[5px] rounded-full border border-gray-200 px-[9px] py-px text-xs whitespace-nowrap text-ink-2',
      className,
    )}
  >
    {children}
  </span>
)

/** Código curto em linha (slug, chave da ferramenta, prefixo da chave). Quebra em qualquer ponto. */
export const CodeChip = ({ children, className }: { children: ReactNode; className?: string }) => (
  <code
    className={cx(
      'rounded-md border border-gray-200 bg-gray-50 px-1.5 py-px font-ui-mono text-xs [overflow-wrap:anywhere] text-ink-2',
      className,
    )}
  >
    {children}
  </code>
)
