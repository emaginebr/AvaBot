import type { ReactNode } from 'react'

/** Estado vazio pontilhado dos cartões do dashboard. */
const EmptyState = ({ children }: { children: ReactNode }) => (
  <div className="rounded-xl border border-dashed border-gray-200 px-2 py-6 text-center text-[13px] text-ink-3">
    {children}
  </div>
)

export default EmptyState
