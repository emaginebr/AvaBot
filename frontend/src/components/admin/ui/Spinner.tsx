import { cx } from './cx'

interface SpinnerProps {
  className?: string
}

/** Círculo girando na cor do texto; decorativo (o texto ao lado diz o que está acontecendo). */
const Spinner = ({ className = 'h-4 w-4' }: SpinnerProps) => (
  <span
    aria-hidden="true"
    className={cx(
      'inline-block flex-none animate-spin rounded-full border-2 border-current border-r-transparent motion-reduce:animate-none',
      className,
    )}
  />
)

export default Spinner
