import { Fragment } from 'react'
import type { Key, KeyboardEvent, ReactNode } from 'react'
import { cx } from './cx'

export interface DataColumn<T> {
  key: string
  header: ReactNode
  /** Cabeçalho só para leitores de tela (ex.: coluna de ações). */
  hideHeader?: boolean
  cell: (row: T) => ReactNode
  /**
   * main: vira o título do cartão no celular (sem rótulo);
   * actions: vira o rodapé do cartão no celular.
   */
  kind?: 'main' | 'actions'
  /** Rótulo da célula no modo cartão; padrão: o cabeçalho, se for texto. */
  label?: string
  align?: 'left' | 'right'
  nowrap?: boolean
  className?: string
}

interface ExpandableConfig<T> {
  isExpanded: (row: T) => boolean
  onToggle: (row: T) => void
  render: (row: T) => ReactNode
}

interface DataTableProps<T> {
  columns: DataColumn<T>[]
  rows: T[]
  rowKey: (row: T) => Key
  /** Legenda da tabela para leitores de tela. */
  caption?: string
  /** Linhas expansíveis com clique, Enter ou Espaço. */
  expandable?: ExpandableConfig<T>
  /** Encosta a tabela nas bordas do cartão (padrão). */
  flush?: boolean
  rowClassName?: (row: T) => string | undefined
}

const cellBase =
  'border-b border-gray-100 px-4 py-3 align-middle first:pl-5 last:pr-5 ' +
  'max-md:flex max-md:items-center max-md:justify-between max-md:gap-3 max-md:px-0 max-md:py-[9px] max-md:text-right max-md:first:pl-0 max-md:last:pr-0 max-md:last:border-b-0'

/**
 * Tabela do kit. Abaixo de md cada linha vira um cartão: a coluna "main" é o título,
 * as demais mostram o rótulo à esquerda e a coluna "actions" vira o rodapé do cartão.
 */
const DataTable = <T,>({ columns, rows, rowKey, caption, expandable, flush = true, rowClassName }: DataTableProps<T>) => {
  const handleRowKey = (event: KeyboardEvent<HTMLTableRowElement>, row: T) => {
    if (!expandable || event.target !== event.currentTarget) return
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault()
      expandable.onToggle(row)
    }
  }

  return (
    <div className={cx('overflow-x-auto max-md:overflow-visible', flush && '-mx-5 max-md:mx-0')}>
      <table className="w-full border-collapse text-[13.5px] max-md:block">
        {caption && <caption className="sr-only">{caption}</caption>}
        <thead className="max-md:sr-only">
          <tr>
            {columns.map((col) => (
              <th
                key={col.key}
                scope="col"
                className={cx(
                  'border-b border-gray-200 bg-gray-50 px-4 py-2.5 text-xs font-medium whitespace-nowrap text-ink-3 first:pl-5 last:pr-5',
                  col.align === 'right' ? 'text-right' : 'text-left',
                )}
              >
                {col.hideHeader ? <span className="sr-only">{col.header}</span> : col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="max-md:block">
          {rows.map((row) => {
            const expanded = expandable?.isExpanded(row) ?? false
            return (
              <Fragment key={rowKey(row)}>
                <tr
                  onClick={expandable ? () => expandable.onToggle(row) : undefined}
                  onKeyDown={expandable ? (event) => handleRowKey(event, row) : undefined}
                  tabIndex={expandable ? 0 : undefined}
                  aria-expanded={expandable ? expanded : undefined}
                  className={cx(
                    'transition-colors duration-100 hover:bg-[#f7f9ff] motion-reduce:transition-none [&:last-child>td]:border-b-0',
                    'max-md:mb-2.5 max-md:block max-md:rounded-xl max-md:border max-md:border-gray-200 max-md:bg-white max-md:px-3.5 max-md:py-1 max-md:hover:bg-white',
                    expandable &&
                      'cursor-pointer focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ava-600',
                    expanded && 'max-md:mb-0 max-md:rounded-b-none [&>td]:border-b-0',
                    rowClassName?.(row),
                  )}
                >
                  {columns.map((col) => {
                    const label = col.label ?? (typeof col.header === 'string' ? col.header : undefined)
                    if (col.kind === 'main') {
                      return (
                        <td key={col.key} className={cx(cellBase, 'max-md:block max-md:text-left', col.className)}>
                          {col.cell(row)}
                        </td>
                      )
                    }
                    if (col.kind === 'actions') {
                      return (
                        <td key={col.key} className={cx(cellBase, 'max-md:block max-md:border-b-0 max-md:pt-3', col.className)}>
                          <div className="flex flex-wrap justify-end gap-1.5 max-md:justify-start">{col.cell(row)}</div>
                        </td>
                      )
                    }
                    return (
                      <td
                        key={col.key}
                        className={cx(
                          cellBase,
                          col.align === 'right' && 'text-right',
                          col.nowrap && 'whitespace-nowrap',
                          col.className,
                        )}
                      >
                        {label && (
                          <span className="hidden flex-none text-left text-xs font-medium whitespace-nowrap text-ink-3 max-md:inline">
                            {label}
                          </span>
                        )}
                        <span className="min-w-0 max-md:[overflow-wrap:anywhere]">{col.cell(row)}</span>
                      </td>
                    )
                  })}
                </tr>
                {expandable && expanded && (
                  <tr className="max-md:mb-2.5 max-md:block max-md:rounded-b-xl max-md:border max-md:border-t-0 max-md:border-gray-200 max-md:bg-gray-50">
                    <td
                      colSpan={columns.length}
                      className="border-b border-gray-100 bg-gray-50 px-5 pt-1 pb-4 max-md:block max-md:border-b-0 max-md:bg-transparent max-md:px-3.5"
                    >
                      {expandable.render(row)}
                    </td>
                  </tr>
                )}
              </Fragment>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

export default DataTable
