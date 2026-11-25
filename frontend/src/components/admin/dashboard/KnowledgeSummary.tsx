import { ProcessingStatus } from '../../../types/knowledgeFile'
import type { KnowledgeFileInfo } from '../../../types/knowledgeFile'
import StatusPill from '../ui/StatusBadge'
import { fmtInt } from './format'

interface KnowledgeSummaryProps {
  files: KnowledgeFileInfo[]
  /** Quantos arquivos recentes listar abaixo do gráfico. */
  recent?: number
}

const SEGMENTS = [
  { status: ProcessingStatus.Ready, label: 'Prontos', color: 'var(--color-ava-600)' },
  { status: ProcessingStatus.Processing, label: 'Processando', color: '#e0a100' },
  { status: ProcessingStatus.Error, label: 'Com erro', color: 'var(--color-err)' },
] as const

const extensionBadge = (fileName: string) => {
  const ext = fileName.includes('.') ? fileName.split('.').pop()?.toLowerCase() ?? '' : ''
  if (ext === 'pdf') return { label: 'PDF', className: 'bg-err-bg text-err' }
  if (['doc', 'docx', 'odt', 'rtf'].includes(ext)) return { label: 'DOC', className: 'bg-ava-50 text-ava-700' }
  if (['xls', 'xlsx', 'csv', 'ods'].includes(ext)) return { label: 'XLS', className: 'bg-ok-bg text-ok' }
  if (['md', 'txt'].includes(ext)) return { label: ext.toUpperCase(), className: 'bg-gray-100 text-ink-2' }
  return { label: (ext || 'ARQ').slice(0, 4).toUpperCase(), className: 'bg-gray-100 text-ink-2' }
}

const STATUS_PILL = {
  [ProcessingStatus.Ready]: { tone: 'ok', label: 'Pronto', icon: 'check' },
  [ProcessingStatus.Processing]: { tone: 'warn', label: 'Processando', icon: 'clock' },
  [ProcessingStatus.Error]: { tone: 'err', label: 'Erro', icon: 'x' },
} as const

/** Arcos da rosca (circunferência 100, início às 12 h). */
const donutArcs = (counts: number[], total: number) => {
  const arcs: { key: number; color: string; pct: number; offset: number }[] = []
  let offset = 25
  for (let i = 0; i < SEGMENTS.length; i++) {
    const pct = total ? (counts[i] / total) * 100 : 0
    arcs.push({ key: SEGMENTS[i].status, color: SEGMENTS[i].color, pct, offset })
    offset -= pct
  }
  return arcs
}

/** Rosca de status (prontos/processando/erro) e os arquivos alterados mais recentemente. */
const KnowledgeSummary = ({ files, recent = 3 }: KnowledgeSummaryProps) => {
  const counts = SEGMENTS.map((s) => files.filter((f) => f.processingStatus === s.status).length)
  const total = files.length
  const readyPct = total ? Math.round((counts[0] / total) * 100) : 0
  const latest = [...files].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)).slice(0, recent)

  const arcs = donutArcs(counts, total)

  return (
    <>
      <div className="mb-4 flex items-center gap-[18px]">
        <svg
          viewBox="0 0 42 42"
          role="img"
          aria-label={SEGMENTS.map((s, i) => `${counts[i]} ${s.label.toLowerCase()}`).join(', ')}
          className="h-[104px] w-[104px] flex-none"
        >
          <circle cx="21" cy="21" r="15.9155" fill="none" stroke="#f3f4f6" strokeWidth="5" />
          {arcs.map((a) =>
            a.pct > 0 ? (
              <circle
                key={a.key}
                cx="21"
                cy="21"
                r="15.9155"
                fill="none"
                stroke={a.color}
                strokeWidth="5"
                strokeDasharray={`${a.pct} ${100 - a.pct}`}
                strokeDashoffset={a.offset}
              />
            ) : null,
          )}
          <text x="21" y="21" textAnchor="middle" fontSize="8" fontWeight="600" fill="#0f1430" className="font-brand">
            {readyPct}%
          </text>
          <text x="21" y="27" textAnchor="middle" fontSize="3.6" fill="#5d6683">
            prontos
          </text>
        </svg>
        <ul className="m-0 flex min-w-0 flex-1 list-none flex-col gap-[7px] p-0 text-[13px]">
          {SEGMENTS.map((s, i) => (
            <li key={s.status} className="flex items-center gap-2 text-ink-2">
              <span aria-hidden="true" className="h-2.5 w-2.5 rounded-[3px]" style={{ background: s.color }} />
              {s.label}
              <span className="ml-auto pl-3 font-ui-mono text-ink">{fmtInt(counts[i])}</span>
            </li>
          ))}
        </ul>
      </div>

      <ul className="m-0 list-none p-0">
        {latest.map((file) => {
          const badge = extensionBadge(file.fileName)
          const pill = STATUS_PILL[file.processingStatus] ?? STATUS_PILL[ProcessingStatus.Processing]
          return (
            <li key={file.knowledgeFileId} className="flex items-center gap-2.5 border-t border-gray-100 py-[9px] text-[13px]">
              <span
                aria-hidden="true"
                className={`grid h-[30px] w-[30px] flex-none place-items-center rounded-lg font-ui-mono text-[9.5px] font-medium ${badge.className}`}
              >
                {badge.label}
              </span>
              <span className="min-w-0 flex-1 truncate" title={file.errorMessage ?? file.fileName}>
                {file.fileName}
              </span>
              <StatusPill tone={pill.tone} label={pill.label} icon={pill.icon} />
            </li>
          )
        })}
      </ul>
    </>
  )
}

export default KnowledgeSummary
