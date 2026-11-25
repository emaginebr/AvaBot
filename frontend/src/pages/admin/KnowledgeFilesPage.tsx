import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { useKnowledgeFileStore } from '../../stores/useKnowledgeFileStore'
import { KnowledgeFileService } from '../../Services/KnowledgeFileService'
import FileUpload from '../../components/admin/FileUpload'
import type { FileUploadHandle } from '../../components/admin/FileUpload'
import { ProcessingStatus } from '../../types/knowledgeFile'
import type { KnowledgeFileInfo } from '../../types/knowledgeFile'
import Button from '../../components/admin/ui/Button'
import { Card, CardHeader } from '../../components/admin/ui/Card'
import DataTable from '../../components/admin/ui/DataTable'
import type { DataColumn } from '../../components/admin/ui/DataTable'
import PageHeader from '../../components/admin/ui/PageHeader'
import { EmptyState, LoadingState, NoAgentState } from '../../components/admin/ui/States'
import StatusBadge from '../../components/admin/ui/StatusBadge'
import { useConfirm } from '../../components/admin/ui/useConfirm'
import { cx } from '../../components/admin/ui/cx'

const PAGE_TITLE = 'Arquivos da base de conhecimento'
const PAGE_DESCRIPTION = 'Envie documentos em Markdown; o agente usa o conteúdo deles para responder.'

const formatFileSize = (bytes: number) => {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

const FileStatus = ({ status }: { status: ProcessingStatus }) => {
  if (status === ProcessingStatus.Ready) return <StatusBadge tone="ok" label="Pronto" />
  if (status === ProcessingStatus.Error) return <StatusBadge tone="err" label="Erro" />
  return <StatusBadge tone="warn" pulse label="Processando" />
}

const KnowledgeFilesPage = () => {
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const { files, loading, error, loadFiles } = useKnowledgeFileStore()
  const [uploading, setUploading] = useState(false)
  const [deletingId, setDeletingId] = useState<number | null>(null)
  const uploadRef = useRef<FileUploadHandle>(null)
  const [confirm, confirmDialog] = useConfirm()

  const agentId = selectedAgent?.agentId

  useEffect(() => {
    if (agentId) loadFiles(agentId)
  }, [agentId, loadFiles])

  useEffect(() => {
    if (error) toast.error(error)
  }, [error])

  // Poll for processing status
  useEffect(() => {
    if (!agentId) return
    const hasProcessing = files.some((f) => f.processingStatus === ProcessingStatus.Processing)
    if (!hasProcessing) return

    const interval = setInterval(() => loadFiles(agentId), 3000)
    return () => clearInterval(interval)
  }, [files, agentId, loadFiles])

  const handleUpload = async (file: File) => {
    if (!agentId) return
    setUploading(true)
    try {
      const result = await KnowledgeFileService.upload(agentId, file)
      if (!result.sucesso) {
        toast.error(result.mensagem || 'Erro ao enviar arquivo')
        console.error('[KnowledgeFilesPage] upload — erro:', result.mensagem)
      } else {
        toast.success(`Arquivo "${file.name}" enviado com sucesso`)
      }
      await loadFiles(agentId)
    } catch (err) {
      console.error('[KnowledgeFilesPage] upload — exceção:', err)
      toast.error('Erro de rede ao enviar arquivo')
    } finally {
      setUploading(false)
    }
  }

  const handleDelete = async (fileId: number) => {
    if (!agentId) return
    try {
      const result = await KnowledgeFileService.delete(agentId, fileId)
      if (!result.sucesso) {
        toast.error(result.mensagem || 'Erro ao remover arquivo')
        console.error('[KnowledgeFilesPage] delete — erro:', result.mensagem)
      } else {
        toast.success('Arquivo removido com sucesso')
      }
      await loadFiles(agentId)
    } catch (err) {
      console.error('[KnowledgeFilesPage] delete — exceção:', err)
      toast.error('Erro de rede ao remover arquivo')
    }
  }

  const askDelete = async (file: KnowledgeFileInfo) => {
    const ok = await confirm({
      title: 'Remover arquivo?',
      message: (
        <>
          O arquivo <strong className="[overflow-wrap:anywhere]">{file.fileName}</strong> será removido da base de conhecimento
          do agente.
        </>
      ),
      confirmLabel: 'Remover',
    })
    if (!ok) return
    setDeletingId(file.knowledgeFileId)
    try {
      await handleDelete(file.knowledgeFileId)
    } finally {
      setDeletingId(null)
    }
  }

  const handleReprocess = async (fileId: number) => {
    if (!agentId) return
    try {
      const result = await KnowledgeFileService.reprocess(agentId, fileId)
      if (!result.sucesso) {
        toast.error(result.mensagem || 'Erro ao reprocessar arquivo')
        console.error('[KnowledgeFilesPage] reprocess — erro:', result.mensagem)
      } else {
        toast.info('Reprocessamento iniciado')
      }
      await loadFiles(agentId)
    } catch (err) {
      console.error('[KnowledgeFilesPage] reprocess — exceção:', err)
      toast.error('Erro de rede ao reprocessar arquivo')
    }
  }

  if (!selectedAgent) {
    return (
      <div className="flex flex-col gap-5">
        <PageHeader title={PAGE_TITLE} description={PAGE_DESCRIPTION} />
        <NoAgentState purpose="gerenciar a base de conhecimento" />
      </div>
    )
  }

  const openPicker = () => uploadRef.current?.open()

  const columns: DataColumn<KnowledgeFileInfo>[] = [
    {
      key: 'file',
      header: 'Arquivo',
      kind: 'main',
      cell: (file) => {
        const failed = file.processingStatus === ProcessingStatus.Error
        return (
          <span className="flex min-w-0 items-center gap-2.5">
            <span
              aria-hidden="true"
              className={cx(
                'grid h-8 w-8 flex-none place-items-center rounded-lg font-ui-mono text-[9.5px] font-medium',
                failed ? 'bg-err-bg text-err' : 'bg-ava-50 text-ava-700',
              )}
            >
              MD
            </span>
            <span className="min-w-0">
              <code className="block font-ui-mono text-[13px] font-medium [overflow-wrap:anywhere] text-ink">{file.fileName}</code>
              {file.errorMessage && (
                <span className="mt-px block max-w-[420px] text-[12.5px] [overflow-wrap:anywhere] text-err max-md:max-w-none">
                  {file.errorMessage}
                </span>
              )}
            </span>
          </span>
        )
      },
    },
    {
      key: 'size',
      header: 'Tamanho',
      align: 'right',
      nowrap: true,
      cell: (file) => <span className="font-ui-mono tabular-nums">{formatFileSize(file.fileSize)}</span>,
    },
    { key: 'status', header: 'Situação', cell: (file) => <FileStatus status={file.processingStatus} /> },
    {
      key: 'sent',
      header: 'Enviado em',
      nowrap: true,
      cell: (file) => (
        <span className="font-ui-mono text-[12.5px] tabular-nums text-ink-2">{new Date(file.createdAt).toLocaleString('pt-BR')}</span>
      ),
    },
    {
      key: 'actions',
      header: 'Ações',
      hideHeader: true,
      kind: 'actions',
      cell: (file) => (
        <>
          {file.processingStatus === ProcessingStatus.Error && (
            <Button size="sm" icon="refresh" onClick={() => handleReprocess(file.knowledgeFileId)}>
              Reprocessar
            </Button>
          )}
          <Button
            size="sm"
            variant="dangerGhost"
            icon="trash"
            onClick={() => askDelete(file)}
            loading={deletingId === file.knowledgeFileId}
            aria-label={`Remover ${file.fileName}`}
          >
            Remover
          </Button>
        </>
      ),
    },
  ]

  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        agentName={selectedAgent.name}
        title={PAGE_TITLE}
        description={PAGE_DESCRIPTION}
        actions={
          <Button variant="primary" size="lg" icon="upload" onClick={openPicker} disabled={uploading}>
            Enviar arquivo
          </Button>
        }
      />

      <FileUpload ref={uploadRef} onUpload={handleUpload} loading={uploading} />

      <Card aria-labelledby="arquivos-lista-titulo">
        <CardHeader
          titleId="arquivos-lista-titulo"
          title="Arquivos enviados"
          subtitle="A lista se atualiza sozinha enquanto houver arquivos em processamento."
        />
        {loading && files.length === 0 ? (
          <LoadingState label="Carregando arquivos…" rows={3} />
        ) : files.length === 0 ? (
          <EmptyState
            icon="file"
            title="Nenhum arquivo enviado para este agente"
            description="Envie o primeiro arquivo .md para montar a base de conhecimento."
            action={
              <Button variant="primary" icon="upload" onClick={openPicker} disabled={uploading}>
                Enviar arquivo
              </Button>
            }
          />
        ) : (
          <DataTable columns={columns} rows={files} rowKey={(file) => file.knowledgeFileId} caption="Arquivos da base de conhecimento" />
        )}
      </Card>
      {confirmDialog}
    </div>
  )
}

export default KnowledgeFilesPage
