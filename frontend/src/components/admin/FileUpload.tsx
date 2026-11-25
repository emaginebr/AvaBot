import { useCallback, useImperativeHandle } from 'react'
import type { Ref } from 'react'
import { useDropzone } from 'react-dropzone'
import { toast } from 'sonner'
import AdminIcon from './AdminIcon'
import Spinner from './ui/Spinner'
import { cx } from './ui/cx'

/** Permite que a página abra o seletor de arquivos (botão "Enviar arquivo" do cabeçalho). */
export interface FileUploadHandle {
  open: () => void
}

interface FileUploadProps {
  onUpload: (file: File) => Promise<void>
  loading: boolean
  ref?: Ref<FileUploadHandle>
}

const FileUpload = ({ onUpload, loading, ref }: FileUploadProps) => {
  const onDrop = useCallback(async (acceptedFiles: File[], rejectedFiles: unknown[]) => {
    if (Array.isArray(rejectedFiles) && rejectedFiles.length > 0) {
      toast.error('Arquivo rejeitado. Verifique se é um arquivo .md com no máximo 10MB.')
      return
    }
    for (const file of acceptedFiles) {
      if (file.size > 10 * 1024 * 1024) {
        toast.error(`O arquivo "${file.name}" excede o limite de 10MB (${(file.size / (1024 * 1024)).toFixed(1)}MB).`)
        continue
      }
      await onUpload(file)
    }
  }, [onUpload])

  const { getRootProps, getInputProps, isDragActive, open } = useDropzone({
    onDrop,
    accept: { 'text/markdown': ['.md'] },
    maxSize: 10 * 1024 * 1024,
    disabled: loading,
  })

  useImperativeHandle(ref, () => ({ open }), [open])

  return (
    <div
      {...getRootProps({
        'aria-label': 'Enviar arquivos .md para a base de conhecimento',
        'aria-describedby': 'kb-dropzone-dica',
        'aria-disabled': loading || undefined,
      })}
      className={cx(
        'flex cursor-pointer flex-col items-center gap-1.5 rounded-2xl border-2 border-dashed px-4 py-[30px] text-center transition-colors motion-reduce:transition-none',
        'bg-[linear-gradient(180deg,var(--color-ava-50),#fff)] hover:border-ava-500',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600',
        isDragActive ? 'border-ava-500 bg-ava-50' : 'border-ava-200',
        loading && 'cursor-progress opacity-60',
      )}
    >
      <input {...getInputProps()} />
      <span
        aria-hidden="true"
        className="mb-1.5 grid h-[46px] w-[46px] place-items-center rounded-[14px] bg-white text-ava-600 shadow-ava"
      >
        {loading ? <Spinner className="h-5 w-5" /> : <AdminIcon name="upload" className="h-[22px] w-[22px]" />}
      </span>
      <b className="font-semibold text-ink">
        {isDragActive
          ? 'Solte o arquivo aqui...'
          : loading
            ? 'Enviando arquivo...'
            : 'Arraste arquivos .md aqui ou clique para selecionar'}
      </b>
      <p id="kb-dropzone-dica" className="m-0 text-[12.5px] text-ink-3">
        Somente Markdown (.md) · máximo de 10 MB por arquivo
      </p>
    </div>
  )
}

export default FileUpload
