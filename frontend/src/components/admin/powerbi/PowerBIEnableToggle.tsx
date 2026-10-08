import { useState } from 'react'
import { toast } from 'sonner'
import { PowerBIService } from '../../../Services/PowerBIService'

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
    <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6 flex items-center justify-between gap-4">
      <div>
        <h2 className="text-lg font-semibold text-gray-900">Power BI ativo</h2>
        <p className="text-sm text-gray-500 mt-0.5">
          {enabled
            ? 'O agente pode consultar os datasets vinculados em todos os canais.'
            : 'Com a flag desligada, o agente responde apenas com a base de conhecimento.'}
        </p>
      </div>

      <button
        type="button"
        role="switch"
        aria-checked={enabled}
        onClick={handleToggle}
        disabled={saving}
        className={`relative inline-flex h-6 w-11 shrink-0 rounded-full transition-colors disabled:opacity-50 disabled:cursor-not-allowed ${
          enabled ? 'bg-ava-600' : 'bg-gray-300'
        }`}
      >
        <span
          className={`inline-block h-5 w-5 transform rounded-full bg-white shadow transition-transform ${
            enabled ? 'translate-x-5' : 'translate-x-0.5'
          }`}
        />
      </button>

      {showWarning && (
        <div className="fixed inset-0 z-40 flex items-center justify-center p-4">
          <div
            className="absolute inset-0 bg-black/30"
            onClick={() => setShowWarning(false)}
          />
          <div className="relative bg-white rounded-xl border border-gray-200 shadow-lg max-w-md w-full p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Ativar o Power BI</h3>
            <p className="text-sm text-gray-600">{EXPOSURE_WARNING}.</p>
            <div className="flex gap-2 justify-end mt-6">
              <button
                onClick={() => setShowWarning(false)}
                className="px-4 py-2 bg-white text-gray-700 text-sm rounded-lg border border-gray-300 hover:bg-gray-50 font-medium"
              >
                Cancelar
              </button>
              <button
                onClick={() => applyEnabled(true)}
                disabled={saving}
                className="px-4 py-2 bg-ava-600 text-white text-sm rounded-lg hover:bg-ava-700 disabled:opacity-50 font-medium"
              >
                {saving ? 'Ativando...' : 'Ativar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default PowerBIEnableToggle
