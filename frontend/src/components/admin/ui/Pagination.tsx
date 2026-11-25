import type { ReactNode } from 'react'
import Button from './Button'

interface PaginationProps {
  /** Resumo à esquerda (ex.: "Mostrando 1 a 20 de 128 sessões"). */
  summary: ReactNode
  onPrevious: () => void
  onNext: () => void
  previousDisabled: boolean
  nextDisabled: boolean
  label?: string
}

/** Paginação Anterior/Próxima com resumo; os limites ficam com a página. */
const Pagination = ({ summary, onPrevious, onNext, previousDisabled, nextDisabled, label = 'Paginação' }: PaginationProps) => (
  <nav aria-label={label} className="mt-4 flex flex-wrap items-center justify-between gap-3 text-[12.5px] text-ink-3">
    <p className="m-0 tabular-nums">{summary}</p>
    <div className="flex items-center gap-2">
      <Button size="sm" onClick={onPrevious} disabled={previousDisabled}>
        Anterior
      </Button>
      <Button size="sm" onClick={onNext} disabled={nextDisabled}>
        Próxima
      </Button>
    </div>
  </nav>
)

export default Pagination
