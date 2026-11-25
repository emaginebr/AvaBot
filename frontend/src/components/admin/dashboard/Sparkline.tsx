interface SparklineProps {
  values: number[]
  color?: string
}

/** Minigráfico de linha 96×32 com área suave (decorativo; o número ao lado é a informação). */
const Sparkline = ({ values, color = 'var(--color-ava-600)' }: SparklineProps) => {
  if (values.length < 2) return null
  const max = Math.max(...values)
  const min = Math.min(...values)
  const range = max - min || 1
  const points = values.map((v, i) => [
    (i / (values.length - 1)) * 94 + 1,
    max === min ? 24 : 30 - ((v - min) / range) * 26,
  ])
  const line = points.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join(' ')

  return (
    <svg viewBox="0 0 96 32" aria-hidden="true" className="h-8 w-24 flex-none">
      <path d={`${line} L95 32 L1 32Z`} fill={color} opacity={0.08} />
      <path d={line} fill="none" stroke={color} strokeWidth={1.75} strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

export default Sparkline
