import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { useAuthStore } from '../../stores/useAuthStore'

const MIN_PASSWORD = 8

const inputClass =
  'w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow'

const buttonClass =
  'px-5 py-2.5 bg-ava-600 text-white rounded-lg font-medium hover:bg-ava-700 focus:outline-none focus:ring-2 focus:ring-ava-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors'

const AccountPage = () => {
  const user = useAuthStore((state) => state.user)
  const refreshUser = useAuthStore((state) => state.refreshUser)
  const updateName = useAuthStore((state) => state.updateName)
  const changePassword = useAuthStore((state) => state.changePassword)

  const [name, setName] = useState(user?.name ?? '')
  const [savingName, setSavingName] = useState(false)

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [savingPassword, setSavingPassword] = useState(false)

  // Sincroniza nome/e-mail com a API ao abrir a pagina (o storage pode estar defasado).
  useEffect(() => {
    refreshUser()
  }, [refreshUser])

  const handleSaveName = async (e: React.FormEvent) => {
    e.preventDefault()
    if (name.trim().length < 2) {
      toast.error('O nome precisa ter ao menos 2 caracteres')
      return
    }

    setSavingName(true)
    try {
      const result = await updateName(name.trim())
      if (result.sucesso) toast.success('Nome atualizado')
      else toast.error(result.mensagem || 'Não foi possível atualizar o nome')
    } catch {
      toast.error('Erro ao conectar com o servidor')
    } finally {
      setSavingName(false)
    }
  }

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!currentPassword) {
      toast.error('Informe a senha atual')
      return
    }
    if (newPassword.length < MIN_PASSWORD) {
      toast.error(`A nova senha deve ter ao menos ${MIN_PASSWORD} caracteres`)
      return
    }
    if (newPassword !== confirmPassword) {
      toast.error('A confirmação não confere com a nova senha')
      return
    }

    setSavingPassword(true)
    try {
      // Senhas vao so no corpo da requisicao; nada delas no console.
      const result = await changePassword({ currentPassword, newPassword })
      if (result.sucesso) {
        toast.success('Senha alterada com sucesso')
        setCurrentPassword('')
        setNewPassword('')
        setConfirmPassword('')
      } else {
        toast.error(result.mensagem || 'Não foi possível alterar a senha')
      }
    } catch {
      toast.error('Erro ao conectar com o servidor')
    } finally {
      setSavingPassword(false)
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Minha conta</h1>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 max-w-5xl">
        <div className="bg-white rounded-xl border border-gray-200 p-6 space-y-6">
          <div>
            <h2 className="font-semibold text-gray-900 mb-3">Dados da conta</h2>
            <dl className="space-y-2 text-sm">
              <div className="flex justify-between gap-4">
                <dt className="text-gray-500">E-mail</dt>
                <dd className="font-medium text-gray-900 truncate">{user?.email ?? '—'}</dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-gray-500">Conta criada em</dt>
                <dd className="font-medium text-gray-900">
                  {user?.createdAt ? new Date(user.createdAt).toLocaleDateString('pt-BR') : '—'}
                </dd>
              </div>
            </dl>
          </div>

          <form onSubmit={handleSaveName} className="space-y-4 border-t border-gray-100 pt-5">
            <h2 className="font-semibold text-gray-900">Alterar nome</h2>
            <div>
              <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1.5">
                Nome
              </label>
              <input
                id="name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                maxLength={260}
                className={inputClass}
              />
            </div>
            <button type="submit" disabled={savingName || name.trim() === (user?.name ?? '')} className={buttonClass}>
              {savingName ? 'Salvando...' : 'Salvar nome'}
            </button>
          </form>
        </div>

        <form onSubmit={handleChangePassword} className="bg-white rounded-xl border border-gray-200 p-6 space-y-4">
          <h2 className="font-semibold text-gray-900">Alterar senha</h2>

          <div>
            <label htmlFor="currentPassword" className="block text-sm font-medium text-gray-700 mb-1.5">
              Senha atual
            </label>
            <input
              id="currentPassword"
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
              autoComplete="current-password"
              className={inputClass}
            />
          </div>

          <div>
            <label htmlFor="newPassword" className="block text-sm font-medium text-gray-700 mb-1.5">
              Nova senha
            </label>
            <input
              id="newPassword"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              autoComplete="new-password"
              minLength={MIN_PASSWORD}
              maxLength={128}
              className={inputClass}
            />
            <p className="mt-1 text-xs text-gray-400">Ao menos {MIN_PASSWORD} caracteres.</p>
          </div>

          <div>
            <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1.5">
              Confirmar nova senha
            </label>
            <input
              id="confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
              autoComplete="new-password"
              className={inputClass}
            />
          </div>

          <button type="submit" disabled={savingPassword} className={buttonClass}>
            {savingPassword ? 'Alterando...' : 'Alterar senha'}
          </button>
        </form>
      </div>
    </div>
  )
}

export default AccountPage
