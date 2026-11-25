import { cx } from './cx'

interface SkeletonProps {
  className?: string
}

/** Bloco cinza com brilho para carregamento (estático com movimento reduzido). */
const Skeleton = ({ className = 'h-4 w-full' }: SkeletonProps) => (
  <span
    aria-hidden="true"
    className={cx(
      'block animate-shimmer rounded-md bg-[linear-gradient(90deg,#f3f4f6_0%,#e9ebf0_50%,#f3f4f6_100%)] bg-[length:200%_100%] motion-reduce:animate-none',
      className,
    )}
  />
)

/** Várias linhas de esqueleto, com texto acessível de carregamento. */
export const SkeletonRows = ({
  rows = 3,
  className = 'h-10',
  label = 'Carregando…',
}: {
  rows?: number
  className?: string
  label?: string
}) => (
  <div role="status" className="flex flex-col gap-2.5">
    <span className="sr-only">{label}</span>
    {Array.from({ length: rows }, (_, i) => (
      <Skeleton key={i} className={cx('w-full rounded-[10px]', className)} />
    ))}
  </div>
)

export default Skeleton
