interface NavBadgeProps {
  count: number
  /** Texto lido por leitores de tela no lugar do número. */
  label: string
}

/** Contador âmbar da sidebar (pedidos pendentes). */
const NavBadge = ({ count, label }: NavBadgeProps) => (
  <>
    <span
      aria-hidden="true"
      className="ml-auto rounded-full bg-[#ffd27a] px-1.5 font-ui-mono text-[10.5px] leading-4 font-semibold tracking-normal text-[#3a2600] normal-case"
    >
      {count > 99 ? '99+' : count}
    </span>
    <span className="sr-only">, {label}</span>
  </>
)

export default NavBadge
