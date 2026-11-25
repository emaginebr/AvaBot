/** Formatação pt-BR usada pelos blocos do dashboard. */

const DAY_MS = 86_400_000

/** Datas da API sem fuso são UTC (mesma regra de WhatsappAccessRequestList). */
export const parseApiDate = (value: string): Date =>
  new Date(/[zZ]$|[+-]\d{2}:?\d{2}$/.test(value) ? value : `${value}Z`)

export const fmtInt = (n: number): string => n.toLocaleString('pt-BR')

export const fmtDecimal = (n: number, digits = 1): string =>
  n.toLocaleString('pt-BR', { minimumFractionDigits: digits, maximumFractionDigits: digits })

export const fmtPercent = (fraction: number, digits = 1): string => `${fmtDecimal(fraction * 100, digits)}%`

export const fmtBytes = (bytes: number): string => {
  if (bytes < 1024) return `${fmtInt(bytes)} B`
  const units = ['KB', 'MB', 'GB', 'TB']
  let value = bytes / 1024
  let unit = 0
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024
    unit++
  }
  return `${fmtDecimal(value, value < 10 ? 1 : 0)} ${units[unit]}`
}

export const fmtDuration = (ms: number): string =>
  ms < 1000 ? `${fmtInt(Math.round(ms))} ms` : `${fmtDecimal(ms / 1000, 1)} s`

/** "agora", "há 8 min", "há 2 h", "ontem", "há 3 dias" ou a data. */
export const fmtRelative = (date: Date, now: number): string => {
  const diff = now - date.getTime()
  const min = Math.round(diff / 60_000)
  if (min < 1) return 'agora'
  if (min < 60) return `há ${min} min`
  const hours = Math.round(min / 60)
  if (hours < 24) return `há ${hours} h`
  const days = Math.floor(diff / DAY_MS)
  if (days <= 1) return 'ontem'
  if (days < 7) return `há ${days} dias`
  return date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: '2-digit' })
}

/** Início do dia local. */
export const startOfDay = (time: number): number => {
  const d = new Date(time)
  d.setHours(0, 0, 0, 0)
  return d.getTime()
}

/** Início do dia `offset` dias antes de `time` (offset 0 = hoje). */
export const dayStart = (time: number, offset: number): number => {
  const d = new Date(startOfDay(time))
  d.setDate(d.getDate() - offset)
  return d.getTime()
}

export const fmtDayShort = (date: Date): string =>
  date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })

export const fmtDayLong = (date: Date): string =>
  date.toLocaleDateString('pt-BR', { weekday: 'short', day: 'numeric', month: 'short' })

export const plural = (n: number, singular: string, pluralForm: string): string =>
  `${fmtInt(n)} ${n === 1 ? singular : pluralForm}`
