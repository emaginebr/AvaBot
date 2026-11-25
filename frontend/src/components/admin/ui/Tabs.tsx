import { useRef } from 'react'
import type { KeyboardEvent, ReactNode } from 'react'
import { cx } from './cx'

export interface TabItem<T extends string> {
  id: T
  label: ReactNode
  /** Contador âmbar ao lado do rótulo (ex.: pedidos pendentes). */
  count?: number
  /** Texto para leitores de tela junto ao contador. */
  countLabel?: string
}

interface TabsProps<T extends string> {
  /** Prefixo dos ids de abas e painéis (único na página). */
  idPrefix: string
  label: string
  items: TabItem<T>[]
  value: T
  onChange: (id: T) => void
  className?: string
}

const tabId = (prefix: string, id: string) => `${prefix}-tab-${id}`
const panelId = (prefix: string, id: string) => `${prefix}-panel-${id}`

/** Abas com role="tablist": setas, Home e End movem e ativam a aba (foco itinerante). */
const Tabs = <T extends string>({ idPrefix, label, items, value, onChange, className }: TabsProps<T>) => {
  const listRef = useRef<HTMLDivElement>(null)

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const index = items.findIndex((item) => item.id === value)
    let next = index
    if (event.key === 'ArrowRight') next = (index + 1) % items.length
    else if (event.key === 'ArrowLeft') next = (index - 1 + items.length) % items.length
    else if (event.key === 'Home') next = 0
    else if (event.key === 'End') next = items.length - 1
    else return

    event.preventDefault()
    const target = items[next]
    onChange(target.id)
    listRef.current?.querySelector<HTMLButtonElement>(`#${CSS.escape(tabId(idPrefix, target.id))}`)?.focus()
  }

  return (
    <div
      ref={listRef}
      role="tablist"
      aria-label={label}
      onKeyDown={handleKeyDown}
      className={cx('scrollbar-none mb-[18px] flex gap-1 overflow-x-auto border-b border-gray-200', className)}
    >
      {items.map((item) => {
        const selected = item.id === value
        return (
          <button
            key={item.id}
            type="button"
            role="tab"
            id={tabId(idPrefix, item.id)}
            aria-selected={selected}
            aria-controls={panelId(idPrefix, item.id)}
            tabIndex={selected ? 0 : -1}
            onClick={() => onChange(item.id)}
            className={cx(
              'relative inline-flex min-h-[42px] flex-none cursor-pointer items-center rounded-t-lg px-3.5 text-[13.5px] font-medium whitespace-nowrap pointer-coarse:min-h-11',
              'focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ava-600',
              selected
                ? 'text-ava-700 after:absolute after:right-2.5 after:-bottom-px after:left-2.5 after:h-0.5 after:rounded-sm after:bg-ava-600 after:content-[""]'
                : 'text-ink-3 hover:bg-gray-50 hover:text-ink',
            )}
          >
            {item.label}
            {item.count !== undefined && item.count > 0 && (
              <span className="ml-1.5 rounded-full bg-[#ffd27a] px-1.5 font-ui-mono text-[11px] font-semibold text-[#3a2600]">
                {item.count}
                {item.countLabel && <span className="sr-only"> {item.countLabel}</span>}
              </span>
            )}
          </button>
        )
      })}
    </div>
  )
}

interface TabPanelProps {
  idPrefix: string
  tabId: string
  /** Mantém o painel montado mas oculto (preserva rascunhos ao trocar de aba). */
  hidden?: boolean
  className?: string
  children: ReactNode
}

/** Painel ligado à aba de mesmo id; renderize só o ativo ou use `hidden` nos inativos. */
export const TabPanel = ({ idPrefix, tabId: id, hidden, className, children }: TabPanelProps) => (
  <div role="tabpanel" id={panelId(idPrefix, id)} aria-labelledby={tabId(idPrefix, id)} hidden={hidden} className={className}>
    {children}
  </div>
)

export default Tabs
