import { useMemo } from 'react'
import type { ReactNode } from 'react'
import AdminIcon from '../AdminIcon'
import { cx } from './cx'
import { FieldContext } from './fieldContext'

interface FieldProps {
  /** id do controle; a ajuda e o erro usam `${id}-hint` e `${id}-err`. */
  id: string
  label: ReactNode
  hint?: ReactNode
  /** Mensagem de erro local; marca o controle com aria-invalid. */
  error?: ReactNode
  required?: boolean
  /** Etiqueta discreta ao lado do rótulo (ex.: "opcional", "(padrão)"). */
  tag?: ReactNode
  /** Conteúdo à direita do rótulo (ex.: botão "Restaurar padrão", selo). */
  labelAside?: ReactNode
  /** Conteúdo abaixo da ajuda (ex.: contador de caracteres, botões auxiliares). */
  footer?: ReactNode
  /** Rótulo como texto (sem <label>) quando o conteúdo não é um controle de formulário. */
  plainLabel?: boolean
  className?: string
  children: ReactNode
}

/** Campo do kit: rótulo real, ajuda e erro ligados ao controle por aria-describedby. */
const Field = ({
  id,
  label,
  hint,
  error,
  required = false,
  tag,
  labelAside,
  footer,
  plainLabel = false,
  className,
  children,
}: FieldProps) => {
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-err` : undefined
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined
  const invalid = Boolean(error)

  const context = useMemo(() => ({ id, describedBy, invalid, required }), [id, describedBy, invalid, required])

  const labelContent = (
    <>
      {label}
      {required && (
        <span aria-hidden="true" className="text-err">
          *
        </span>
      )}
      {tag && <span className="text-xs font-normal text-ink-3">{tag}</span>}
    </>
  )
  const labelClass = 'flex flex-wrap items-center gap-1.5 text-[13px] font-medium text-ink'

  return (
    <FieldContext.Provider value={context}>
      <div className={cx('flex min-w-0 flex-col gap-1.5', className)}>
        <div className="flex flex-wrap items-center justify-between gap-2">
          {plainLabel ? (
            <span className={labelClass}>{labelContent}</span>
          ) : (
            <label htmlFor={id} className={labelClass}>
              {labelContent}
            </label>
          )}
          {labelAside}
        </div>
        {children}
        {hint && (
          <p id={hintId} className="m-0 text-[12.5px] text-ink-3">
            {hint}
          </p>
        )}
        {error && (
          <p id={errorId} className="m-0 flex items-start gap-1.5 text-[12.5px] font-medium text-err">
            <AdminIcon name="alert" className="mt-0.5 h-3.5 w-3.5" />
            <span>{error}</span>
          </p>
        )}
        {footer}
      </div>
    </FieldContext.Provider>
  )
}

export default Field
