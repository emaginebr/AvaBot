import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { LinkProps } from 'react-router-dom'
import AdminIcon from '../AdminIcon'
import type { AdminIconName } from '../AdminIcon'
import Spinner from './Spinner'
import { buttonClasses } from './buttonStyles'
import type { ButtonSize, ButtonVariant } from './buttonStyles'

interface CommonProps {
  variant?: ButtonVariant
  size?: ButtonSize
  /** Ícone à esquerda do texto. */
  icon?: AdminIconName
  children?: ReactNode
}

interface ButtonProps extends CommonProps, ButtonHTMLAttributes<HTMLButtonElement> {
  /** Mostra o spinner, desabilita e marca aria-busy; o texto continua visível. */
  loading?: boolean
}

/** Botão do kit: uma ação primária por área; destrutivas sempre com confirmação. */
const Button = ({
  variant = 'secondary',
  size = 'md',
  icon,
  loading = false,
  disabled,
  className,
  type = 'button',
  children,
  ...rest
}: ButtonProps) => (
  <button
    type={type}
    disabled={disabled || loading}
    aria-busy={loading || undefined}
    className={buttonClasses(variant, size, className)}
    {...rest}
  >
    {loading ? <Spinner className="h-3.5 w-3.5" /> : icon && <AdminIcon name={icon} />}
    {children}
  </button>
)

type ButtonLinkProps = CommonProps & Omit<LinkProps, 'className'> & { className?: string }

/** Link do React Router com a aparência de botão (ações que navegam). */
export const ButtonLink = ({ variant = 'secondary', size = 'md', icon, className, children, ...rest }: ButtonLinkProps) => (
  <Link className={buttonClasses(variant, size, className)} {...rest}>
    {icon && <AdminIcon name={icon} />}
    {children}
  </Link>
)

export default Button
