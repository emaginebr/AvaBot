import { useEffect, useId, useRef } from 'react'
import type { KeyboardEvent, ReactNode } from 'react'
import { createPortal } from 'react-dom'
import AdminIcon from '../AdminIcon'
import type { AdminIconName } from '../AdminIcon'
import { cx } from './cx'

const FOCUSABLE =
  'a[href], button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]:not([tabindex="-1"])'

const ICON_TONES = {
  ava: 'bg-ava-50 text-ava-700',
  ok: 'bg-ok-bg text-ok',
  warn: 'bg-warn-bg text-warn',
  err: 'bg-err-bg text-err',
}

export type ModalIconTone = keyof typeof ICON_TONES

interface ModalProps {
  title: ReactNode
  description?: ReactNode
  icon?: AdminIconName
  iconTone?: ModalIconTone
  onClose: () => void
  /** Corpo do modal; omita para diálogos só com título, texto e botões. */
  children?: ReactNode
  footer?: ReactNode
  /** md = 480 px; lg = 720 px. */
  size?: 'md' | 'lg'
  /** alertdialog para confirmações. */
  role?: 'dialog' | 'alertdialog'
  /** Mostra o "X" de fechar no cabeçalho. */
  showClose?: boolean
  closeOnBackdrop?: boolean
}

/**
 * Modal do kit, renderizado em portal: foco preso no painel, Esc fecha, o foco volta ao
 * elemento que abriu e a página atrás não rola. Abaixo de md vira folha inferior.
 * Para focar um campo ao abrir, marque-o com data-autofocus.
 */
const Modal = ({
  title,
  description,
  icon,
  iconTone = 'ava',
  onClose,
  children,
  footer,
  size = 'md',
  role = 'dialog',
  showClose = true,
  closeOnBackdrop = true,
}: ModalProps) => {
  const panelRef = useRef<HTMLDivElement>(null)
  const titleId = useId()
  const descriptionId = useId()

  useEffect(() => {
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null
    const panel = panelRef.current
    const target = panel?.querySelector<HTMLElement>('[data-autofocus]') ?? panel
    target?.focus()

    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.body.style.overflow = previousOverflow
      opener?.focus()
    }
  }, [])

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === 'Escape') {
      // Não deixa o Esc chegar a modais por baixo nem ao drawer do layout.
      event.stopPropagation()
      event.preventDefault()
      onClose()
      return
    }
    if (event.key !== 'Tab' || !panelRef.current) return

    const focusable = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(
      (el) => el.getClientRects().length > 0,
    )
    if (focusable.length === 0) {
      event.preventDefault()
      return
    }
    const first = focusable[0]
    const last = focusable[focusable.length - 1]
    const active = document.activeElement
    if (event.shiftKey && (active === first || active === panelRef.current)) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && active === last) {
      event.preventDefault()
      first.focus()
    }
  }

  return createPortal(
    <div
      onKeyDown={handleKeyDown}
      className="fixed inset-0 z-[60] flex items-center justify-center p-4 font-ui text-sm text-ink antialiased max-md:items-end max-md:p-0"
    >
      <div
        aria-hidden="true"
        onClick={closeOnBackdrop ? onClose : undefined}
        className="absolute inset-0 bg-[rgb(15_20_48/0.5)] backdrop-blur-[2px] motion-safe:animate-modal-fade"
      />
      <div
        ref={panelRef}
        role={role}
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        tabIndex={-1}
        className={cx(
          'relative flex max-h-[calc(100dvh-32px)] w-full flex-col overflow-auto rounded-[20px] bg-white shadow-pop focus:outline-none motion-safe:animate-modal-pop',
          'max-md:max-h-[92dvh] max-md:rounded-b-none',
          size === 'lg' ? 'max-w-[720px]' : 'max-w-[480px]',
        )}
      >
        <div className="flex items-start gap-3.5 px-[22px] pt-[22px]">
          {icon && (
            <span aria-hidden="true" className={cx('grid h-10 w-10 flex-none place-items-center rounded-xl', ICON_TONES[iconTone])}>
              <AdminIcon name={icon} className="h-5 w-5" />
            </span>
          )}
          <div className="min-w-0 flex-1">
            <h2 id={titleId} className="font-brand text-[17px] leading-[1.3] font-semibold tracking-[-0.01em] [overflow-wrap:anywhere]">
              {title}
            </h2>
            {description && (
              <div id={descriptionId} className="mt-1.5 text-[13.5px] [overflow-wrap:anywhere] text-ink-2">
                {description}
              </div>
            )}
          </div>
          {showClose && (
            <button
              type="button"
              onClick={onClose}
              aria-label="Fechar"
              className="ml-auto grid h-9 w-9 flex-none cursor-pointer place-items-center rounded-[10px] text-ink-3 hover:bg-gray-100 hover:text-ink focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600"
            >
              <AdminIcon name="x" className="h-4 w-4" />
            </button>
          )}
        </div>

        {children ? (
          <div className="flex min-w-0 flex-col gap-3.5 px-[22px] py-[18px]">{children}</div>
        ) : (
          <div className="h-[18px]" />
        )}

        {footer && (
          <div className="sticky bottom-0 flex flex-wrap justify-end gap-2.5 border-t border-gray-100 bg-gray-50 px-[22px] py-3.5 max-md:flex-col-reverse max-md:[&>button]:w-full">
            {footer}
          </div>
        )}
      </div>
    </div>,
    document.body,
  )
}

export default Modal
