import { useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'
import AdminIcon from '../AdminIcon'
import Button from '../ui/Button'
import { Card } from '../ui/Card'
import Modal from '../ui/Modal'
import Switch from '../ui/Switch'

const EXPOSURE_WARNING = 'Os dados dos datasets vinculados ficarão acessíveis a qualquer usuário que converse com este agente, em todos os canais (web, Telegram, WhatsApp)'

interface PowerBIEnableToggleProps {
  slug: string
  enabled: boolean
  onChanged: (enabled: boolean) => void
}

const PowerBIEnableToggle = ({ slug, enabled, onChanged }: PowerBIEnableToggleProps) => {
  const [showWarning, setShowWarning] = useState(false)
  const [saving, setSaving] = useState(false)

  const applyEnabled = async (next: boolean) => {
    setSaving(true)
    try {
      const result = await PowerBIService.setEnabled(slug, next)
      if (result.sucesso && result.dados) {
        onChanged(result.dados.enabled)
        toast.success(next ? 'Power BI ativado para este agente' : 'Power BI desativado para este agente')
      } else {
        toast.error(result.mensagem || 'Erro ao alterar o status do Power BI')
        console.error('[PowerBIEnableToggle] applyEnabled — erro:', result.mensagem, result.erros)
      }
    } catch (err) {
      console.error('[PowerBIEnableToggle] applyEnabled — exceção:', err)
      toast.error('Erro de rede ao alterar o status do Power BI')
    } finally {
      setSaving(false)
      setShowWarning(false)
    }
  }

  const handleToggle = () => {
    if (saving) return

    if (enabled) {
      applyEnabled(false)
      return
    }

    setShowWarning(true)
  }

  return (
    <Card>
      <div className="flex items-center justify-between gap-4">
        <div className="flex min-w-0 items-center gap-3">
          <span aria-hidden="true" className="grid h-[34px] w-[34px] flex-none place-items-center rounded-[10px] bg-warn-bg text-warn">
            <AdminIcon name="chart" className="h-[18px] w-[18px]" />
          </span>
          <div className="min-w-0">
            <h2 id="powerbi-ativo-rotulo" className="font-brand text-[17px] leading-[1.3] font-semibold tracking-[-0.01em] text-ink">
              Power BI ativo
            </h2>
            <p id="powerbi-ativo-descricao" className="m-0 mt-[3px] text-[13px] text-ink-2">
              {enabled
                ? 'O agente pode consultar os datasets vinculados em todos os canais.'
                : 'Com a flag desligada, o agente responde apenas com a base de conhecimento.'}
            </p>
          </div>
        </div>

        <Switch
          checked={enabled}
          onChange={handleToggle}
          disabled={saving}
          labelledBy="powerbi-ativo-rotulo"
          describedBy="powerbi-ativo-descricao"
        />
      </div>

      {showWarning && (
        <Modal
          role="alertdialog"
          title="Ativar o Power BI"
          description={`${EXPOSURE_WARNING}.`}
          icon="alert"
          iconTone="warn"
          showClose={false}
          onClose={() => setShowWarning(false)}
          footer={
            <>
              <Button data-autofocus onClick={() => setShowWarning(false)}>
                Cancelar
              </Button>
              <Button variant="primary" onClick={() => applyEnabled(true)} loading={saving}>
                {saving ? 'Ativando...' : 'Ativar'}
              </Button>
            </>
          }
        />
      )}
    </Card>
  )
}

export default PowerBIEnableToggle
