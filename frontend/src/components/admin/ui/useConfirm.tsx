import { useCallback, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import ConfirmDialog from './ConfirmDialog'
import type { ConfirmOptions } from './ConfirmDialog'

/**
 * Substituto assíncrono de window.confirm: `if (!(await confirm({...}))) return`.
 * Devolve a função e o elemento do diálogo, que a página renderiza em qualquer lugar.
 */
export const useConfirm = (): [(options: ConfirmOptions) => Promise<boolean>, ReactNode] => {
  const [pending, setPending] = useState<ConfirmOptions | null>(null)
  const resolverRef = useRef<((value: boolean) => void) | null>(null)

  const confirm = useCallback((options: ConfirmOptions) => {
    // Um pedido novo cancela o anterior que ainda estivesse aberto.
    resolverRef.current?.(false)
    setPending(options)
    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve
    })
  }, [])

  const settle = useCallback((value: boolean) => {
    resolverRef.current?.(value)
    resolverRef.current = null
    setPending(null)
  }, [])

  const dialog = pending ? (
    <ConfirmDialog {...pending} onConfirm={() => settle(true)} onCancel={() => settle(false)} />
  ) : null

  return [confirm, dialog]
}
