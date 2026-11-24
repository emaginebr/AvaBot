import { useEffect, useRef } from 'react'
import type { ReactNode } from 'react'

interface WhatsappModalProps {
  title: string
  onClose: () => void
  children: ReactNode
  footer: ReactNode
}

const FOCUSABLE = 'input:not(:disabled), textarea:not(:disabled), button:not(:disabled)'

/** Modal simples das seções de controle de acesso do WhatsApp (mesmo padrão do McpKeyCreatedModal). */
const WhatsappModal = ({ title, onClose, children, footer }: WhatsappModalProps) => {
  const panelRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const opener = document.activeElement as HTMLElement | null
    panelRef.current?.querySelector<HTMLElement>(FOCUSABLE)?.focus()

    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('keydown', onKey)
      opener?.focus?.()
    }
  }, [onClose])

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="relative z-50 w-full max-w-md bg-white rounded-xl shadow-xl border border-gray-200"
      >
        <div className="px-6 pt-5 pb-3 border-b border-gray-100">
          <h3 className="text-lg font-semibold text-gray-900">{title}</h3>
        </div>
        <div className="px-6 py-4">{children}</div>
        <div className="px-6 pb-5 flex justify-end gap-2">{footer}</div>
      </div>
    </div>
  )
}

export default WhatsappModal
