import type { HTMLAttributes, ReactNode } from 'react'
import AdminIcon from '../AdminIcon'
import type { AdminIconName } from '../AdminIcon'
import { cx } from './cx'

type CardTone = 'default' | 'danger'

interface CardProps extends HTMLAttributes<HTMLElement> {
  /** Elemento semântico (article por padrão; section para âncoras, form para formulários). */
  as?: 'article' | 'section' | 'div' | 'form'
  tone?: CardTone
  /** Realça a borda e a sombra no hover (cartões navegáveis). Desligado por padrão. */
  interactive?: boolean
  children: ReactNode
}

/** Cartão branco do painel (rounded-2xl, borda gray-200, sombra leve). */
export const Card = ({ as = 'article', tone = 'default', interactive = false, className, children, ...rest }: CardProps) => {
  // Todos os elementos aceitam os mesmos atributos HTML usados aqui (onSubmit, role, aria-*).
  const Tag = as as 'div'
  return (
  <Tag
    className={cx(
      'min-w-0 rounded-2xl border bg-white p-5 shadow-card',
      tone === 'danger' ? 'border-err-line' : 'border-gray-200',
      interactive &&
        'transition-[box-shadow,border-color] duration-200 hover:border-ava-100 hover:shadow-card-hover motion-reduce:transition-none',
      className,
    )}
    {...rest}
  >
    {children}
  </Tag>
  )
}

interface CardHeaderProps {
  title: ReactNode
  subtitle?: ReactNode
  icon?: AdminIconName
  /** Cor do ícone (padrão ava; "ok" para WhatsApp, "warn" para Power BI, "danger" para exclusão). */
  iconTone?: 'ava' | 'ok' | 'warn' | 'danger'
  /** Conteúdo à direita do título (selo, botão). */
  actions?: ReactNode
  /** id do h2, para aria-labelledby no cartão. */
  titleId?: string
  /** Sem margem inferior e centralizado (cartões que só têm o cabeçalho, como a zona de perigo). */
  flush?: boolean
  className?: string
}

const ICON_TONES = {
  ava: 'bg-ava-50 text-ava-700',
  ok: 'bg-ok-bg text-ok',
  warn: 'bg-warn-bg text-warn',
  danger: 'bg-err-bg text-err',
}

export const CardHeader = ({ title, subtitle, icon, iconTone = 'ava', actions, titleId, flush = false, className }: CardHeaderProps) => (
  <div className={cx('flex flex-wrap justify-between gap-3', flush ? 'items-center' : 'mb-4 items-start', className)}>
    <div className="flex min-w-0 flex-[1_1_240px] items-center gap-3">
      {icon && (
        <span className={cx('grid h-[34px] w-[34px] flex-none place-items-center rounded-[10px]', ICON_TONES[iconTone])}>
          <AdminIcon name={icon} className="h-[18px] w-[18px]" />
        </span>
      )}
      <div className="min-w-0">
        <h2 id={titleId} className="font-brand text-[17px] leading-[1.3] font-semibold tracking-[-0.01em] [overflow-wrap:anywhere] text-ink">
          {title}
        </h2>
        {subtitle && <p className="mt-[3px] text-[12.5px] [overflow-wrap:anywhere] text-ink-3">{subtitle}</p>}
      </div>
    </div>
    {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
  </div>
)

interface CardFooterProps {
  /** Texto auxiliar à esquerda. */
  note?: ReactNode
  children?: ReactNode
  className?: string
}

/** Rodapé cinza que encosta nas bordas do cartão; empilha os botões no celular. */
export const CardFooter = ({ note, children, className }: CardFooterProps) => (
  <div
    className={cx(
      '-mx-5 mt-5 -mb-5 flex flex-wrap items-center justify-end gap-2.5 rounded-b-2xl border-t border-gray-100 bg-gray-50 px-5 py-3.5',
      'max-md:flex-col-reverse max-md:items-stretch max-md:[&>button]:w-full max-md:[&>a]:w-full',
      className,
    )}
  >
    {note && <span className="mr-auto text-[12.5px] text-ink-3 max-md:m-0 max-md:text-center">{note}</span>}
    {children}
  </div>
)

/** Bloco interno separado por um filete (subseção de um cartão). */
export const CardSection = ({ className, children }: { className?: string; children: ReactNode }) => (
  <div className={cx('mt-[18px] border-t border-gray-100 pt-[18px]', className)}>{children}</div>
)
