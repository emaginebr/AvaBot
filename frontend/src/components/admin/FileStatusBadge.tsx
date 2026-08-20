import { ProcessingStatus } from '../../types/knowledgeFile'

interface FileStatusBadgeProps {
  status: ProcessingStatus
}

const FileStatusBadge = ({ status }: FileStatusBadgeProps) => {
  const config = {
    [ProcessingStatus.Processing]: { label: 'Processando', className: 'bg-yellow-100 text-yellow-700' },
    [ProcessingStatus.Ready]: { label: 'Pronto', className: 'bg-green-100 text-green-700' },
    [ProcessingStatus.Error]: { label: 'Erro', className: 'bg-red-100 text-red-700' },
  }

  const { label, className } = config[status] ?? config[ProcessingStatus.Processing]

  return (
    <span className={`px-2 py-1 rounded-full text-xs font-medium ${className}`}>
      {label}
    </span>
  )
}

export default FileStatusBadge
