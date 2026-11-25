import type { ReactNode } from 'react'
import type { AdminIconName } from '../AdminIcon'
import Button from './Button'
import Modal from './Modal'
import type { ModalIconTone } from './Modal'

export interface ConfirmOptions {
  title: ReactNode
  message: ReactNode
  confirmLabel?: string
  cancelLabel?: string
  /** danger (padrão) usa botão vermelho; primary para confirmações não destrutivas. */
  tone?: 'danger' | 'primary'
  icon?: AdminIconName
  iconTone?: ModalIconTone
}

interface ConfirmDialogProps extends ConfirmOptions {
  onConfirm: () => void
  onCancel: () => void
}

/** Confirmação (role="alertdialog"); o foco inicial fica em Cancelar, a opção segura. */
const ConfirmDialog = ({
  title,
  message,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  tone = 'danger',
  icon = 'alert',
  iconTone,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) => (
  <Modal
    role="alertdialog"
    title={title}
    description={message}
    icon={icon}
    iconTone={iconTone ?? (tone === 'danger' ? 'err' : 'warn')}
    showClose={false}
    onClose={onCancel}
    footer={
      <>
        <Button data-autofocus onClick={onCancel}>
          {cancelLabel}
        </Button>
        <Button variant={tone === 'danger' ? 'danger' : 'primary'} onClick={onConfirm}>
          {confirmLabel}
        </Button>
      </>
    }
  />
)

export default ConfirmDialog
