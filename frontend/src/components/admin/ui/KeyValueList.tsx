import { Fragment } from 'react'
import type { ReactNode } from 'react'
import { cx } from './cx'

export interface KeyValueItem {
  label: ReactNode
  value: ReactNode
  /** Valor em fonte mono tabular (datas, números). */
  numeric?: boolean
}

/** Lista de pares rótulo/valor (dl) em duas colunas; o valor quebra em qualquer ponto. */
const KeyValueList = ({ items, className }: { items: KeyValueItem[]; className?: string }) => (
  <dl className={cx('m-0 grid grid-cols-[minmax(110px,auto)_minmax(0,1fr)] gap-x-4 gap-y-2.5 text-[13.5px]', className)}>
    {items.map((item, i) => (
      <Fragment key={i}>
        <dt className="text-ink-3">{item.label}</dt>
        <dd className={cx('m-0 font-medium [overflow-wrap:anywhere]', item.numeric && 'font-ui-mono tabular-nums')}>
          {item.value}
        </dd>
      </Fragment>
    ))}
  </dl>
)

export default KeyValueList
