import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { useAgentStore } from '../../stores/useAgentStore'
import { useKnowledgeFileStore } from '../../stores/useKnowledgeFileStore'
import { KnowledgeFileService } from '../../Services/KnowledgeFileService'
import FileUpload from '../../components/admin/FileUpload'
import FileStatusBadge from '../../components/admin/FileStatusBadge'
import { ProcessingStatus } from '../../types/knowledgeFile'

const KnowledgeFilesPage = () => {
  const selectedAgent = useAgentStore((state) => state.selectedAgent)
  const { files, loading, error, loadFiles } = useKnowledgeFileStore()
  const [uploading, setUploading] = useState(false)
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)

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
      setDeleteConfirm(null)
      await loadFiles(agentId)
    } catch (err) {
      console.error('[KnowledgeFilesPage] delete — exceção:', err)
      toast.error('Erro de rede ao remover arquivo')
      setDeleteConfirm(null)
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
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-400 mb-2">Nenhum agente selecionado</p>
          <p className="text-sm text-gray-300">Selecione um agente na navbar para gerenciar a base de conhecimento.</p>
        </div>
      </div>
    )
  }

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Arquivos da Base de Conhecimento</h1>
        <p className="text-sm text-gray-500 mt-1">Agente: {selectedAgent.name}</p>
      </div>

      <div className="mb-6">
        <FileUpload onUpload={handleUpload} loading={uploading} />
      </div>

      {loading && files.length === 0 ? (
        <div className="flex items-center gap-2 text-gray-500">
          <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
          Carregando...
        </div>
      ) : files.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-xl border border-gray-200">
          <p className="text-gray-400">Nenhum arquivo enviado para este agente.</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Arquivo</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Tamanho</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                  <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Enviado em</th>
                  <th className="text-right px-5 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {files.map((file) => (
                  <tr key={file.knowledgeFileId} className="hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-3">
                      <code className="text-sm text-gray-700 bg-gray-50 px-2 py-0.5 rounded">{file.fileName}</code>
                    </td>
                    <td className="px-5 py-3 text-sm text-gray-500">{formatFileSize(file.fileSize)}</td>
                    <td className="px-5 py-3">
                      <FileStatusBadge status={file.processingStatus} />
                      {file.errorMessage && (
                        <p className="text-xs text-red-500 mt-1">{file.errorMessage}</p>
                      )}
                    </td>
                    <td className="px-5 py-3 text-sm text-gray-500">
                      {new Date(file.createdAt).toLocaleString('pt-BR')}
                    </td>
                    <td className="px-5 py-3 text-right">
                      <div className="flex items-center justify-end gap-2">
                        {file.processingStatus === ProcessingStatus.Error && (
                          <button
                            onClick={() => handleReprocess(file.knowledgeFileId)}
                            className="text-sm text-ava-600 hover:text-ava-700 font-medium"
                          >
                            Reprocessar
                          </button>
                        )}
                        {deleteConfirm === file.knowledgeFileId ? (
                          <div className="flex items-center gap-1">
                            <button
                              onClick={() => handleDelete(file.knowledgeFileId)}
                              className="text-sm text-white bg-red-600 hover:bg-red-700 px-2 py-0.5 rounded font-medium"
                            >
                              Confirmar
                            </button>
                            <button
                              onClick={() => setDeleteConfirm(null)}
                              className="text-sm text-gray-500 hover:text-gray-700 font-medium"
                            >
                              Cancelar
                            </button>
                          </div>
                        ) : (
                          <button
                            onClick={() => setDeleteConfirm(file.knowledgeFileId)}
                            className="text-sm text-red-600 hover:text-red-700 font-medium"
                          >
                            Remover
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

export default KnowledgeFilesPage
