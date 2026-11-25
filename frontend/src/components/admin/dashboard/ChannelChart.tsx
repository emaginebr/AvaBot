import { useEffect, useId, useRef, useState } from 'react'
import { fmtDayLong, fmtDayShort, fmtInt } from './format'

export interface ChartSeries {
  key: string
  label: string
  /** Cor CSS (ex.: var(--color-ava-700)). */
  color: string
}

export interface ChartDay {
  date: Date
  values: Record<string, number>
}

interface ChannelChartProps {
  days: ChartDay[]
  /** Séries empilhadas de baixo para cima (canais, quando a API informar). */
  series: ChartSeries[]
  /** Descrição para leitores de tela. */
  description: string
  unitLabel?: string
}

const HEIGHT = 260
const PAD = { left: 34, right: 6, top: 10, bottom: 26 }
const TIP_WIDTH = 170

/** Escala com 4 divisões inteiras. */
const niceMax = (value: number) => {
  if (value <= 4) return 4
  const rough = value / 4
  const magnitude = 10 ** Math.floor(Math.log10(rough))
  const step = [1, 2, 2.5, 5, 10].map((m) => m * magnitude).find((s) => s >= rough && Number.isInteger(s)) ?? Math.ceil(rough)
  return step * 4
}

/** Segmentos empilhados de um dia (de baixo para cima). */
const stackDay = (values: Record<string, number>, series: ChartSeries[], max: number, innerH: number) => {
  const segments: { key: string; color: string; y: number; h: number; top: boolean }[] = []
  let y = PAD.top + innerH
  for (let i = 0; i < series.length; i++) {
    const h = ((values[series[i].key] ?? 0) / max) * innerH
    y -= h
    if (h > 0) segments.push({ key: series[i].key, color: series[i].color, y, h, top: i === series.length - 1 })
  }
  return segments
}

/**
 * Barras empilhadas por dia em SVG próprio, com legenda, eixo, rótulos e dica por foco/hover.
 * A largura acompanha o contêiner via ResizeObserver.
 */
const ChannelChart = ({ days, series, description, unitLabel = 'sessões' }: ChannelChartProps) => {
  const wrapRef = useRef<HTMLDivElement>(null)
  const [width, setWidth] = useState(600)
  const [hover, setHover] = useState<number | null>(null)
  const descId = useId()

  useEffect(() => {
    const el = wrapRef.current
    if (!el || typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver(([entry]) => {
      const w = Math.round(entry.contentRect.width)
      if (w > 0) setWidth(w)
    })
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  const totals = days.map((d) => series.reduce((sum, s) => sum + (d.values[s.key] ?? 0), 0))
  const grandTotal = totals.reduce((a, b) => a + b, 0)
  const seriesTotals = series.map((s) => days.reduce((sum, d) => sum + (d.values[s.key] ?? 0), 0))

  const max = niceMax(Math.max(0, ...totals))
  const innerW = Math.max(1, width - PAD.left - PAD.right)
  const innerH = HEIGHT - PAD.top - PAD.bottom
  const step = innerW / Math.max(1, days.length)
  const barW = Math.max(6, Math.min(26, step * 0.58))
  const labelEvery = days.length > 14 ? 5 : days.length > 7 ? 2 : 1

  const hovered = hover !== null ? days[hover] : null
  const tipLeft =
    hover !== null ? Math.max(0, Math.min(PAD.left + step * hover + step / 2 - TIP_WIDTH / 2, width - TIP_WIDTH)) : 0

  return (
    <div>
      <ul className="m-0 flex list-none flex-wrap gap-x-[18px] gap-y-1.5 p-0">
        {series.map((s, i) => (
          <li key={s.key} className="flex items-center gap-2 text-[13px] text-ink-2">
            <span aria-hidden="true" className="h-2.5 w-2.5 rounded-[3px]" style={{ background: s.color }} />
            {s.label}
            <strong className="font-ui-mono text-[12.5px] font-medium text-ink">{fmtInt(seriesTotals[i])}</strong>
            {series.length > 1 && grandTotal > 0 && (
              <span className="text-xs text-ink-3">({Math.round((seriesTotals[i] / grandTotal) * 100)}%)</span>
            )}
          </li>
        ))}
      </ul>

      <div ref={wrapRef} className="relative mt-3.5">
        <svg
          role="img"
          aria-describedby={descId}
          viewBox={`0 0 ${width} ${HEIGHT}`}
          className="block h-[260px] w-full"
          onMouseLeave={() => setHover(null)}
        >
          {Array.from({ length: 5 }, (_, g) => {
            const v = (max / 4) * g
            const y = PAD.top + innerH - (v / max) * innerH
            return (
              <g key={g}>
                <line x1={PAD.left} x2={width - PAD.right} y1={y} y2={y} stroke="#eff1f7" />
                <text x={PAD.left - 8} y={y + 4} textAnchor="end" fontSize={11} fill="#5d6683" className="font-ui-mono">
                  {fmtInt(v)}
                </text>
              </g>
            )
          })}

          {days.map((d, i) => {
            const x = PAD.left + step * i + (step - barW) / 2
            const showLabel = i % labelEvery === (days.length - 1) % labelEvery
            return (
              <g
                key={d.date.getTime()}
                tabIndex={0}
                aria-label={`${fmtDayLong(d.date)}: ${fmtInt(totals[i])} ${unitLabel}`}
                className="cursor-pointer outline-none [&:focus-visible>rect:first-child]:stroke-ava-600 [&:focus-visible>rect:first-child]:stroke-2"
                onMouseEnter={() => setHover(i)}
                onFocus={() => setHover(i)}
                onBlur={() => setHover(null)}
              >
                <rect
                  x={PAD.left + step * i + 1}
                  y={PAD.top}
                  width={Math.max(0, step - 2)}
                  height={innerH}
                  rx={6}
                  fill={hover === i ? 'var(--color-ava-50)' : 'transparent'}
                />
                {stackDay(d.values, series, max, innerH).map((seg) => (
                  <rect
                    key={seg.key}
                    x={x}
                    y={seg.y}
                    width={barW}
                    height={Math.max(seg.h - 1.5, 1)}
                    rx={seg.top ? 4 : 0}
                    fill={seg.color}
                  />
                ))}
                {showLabel && (
                  <text x={x + barW / 2} y={HEIGHT - 8} textAnchor="middle" fontSize={11} fill="#5d6683">
                    {fmtDayShort(d.date)}
                  </text>
                )}
              </g>
            )
          })}
        </svg>
        <p id={descId} className="sr-only">
          {description}
        </p>

        {hovered && hover !== null && (
          <div
            role="presentation"
            className="pointer-events-none absolute top-0 z-[5] rounded-[10px] bg-ava-950 px-3 py-2.5 text-xs text-white shadow-[0_10px_30px_-10px_rgb(20_27_86/0.5)]"
            style={{ left: tipLeft, width: TIP_WIDTH }}
          >
            <b className="mb-1.5 block font-medium text-[#c9d3ff]">{fmtDayLong(hovered.date)}</b>
            {series.map((s) => (
              <div key={s.key} className="flex justify-between gap-3.5">
                <span>{s.label}</span>
                <span className="font-ui-mono">{fmtInt(hovered.values[s.key] ?? 0)}</span>
              </div>
            ))}
            {series.length > 1 && (
              <div className="mt-1 flex justify-between gap-3.5 border-t border-white/15 pt-1">
                <span>Total</span>
                <span className="font-ui-mono">{fmtInt(totals[hover])}</span>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

export default ChannelChart
