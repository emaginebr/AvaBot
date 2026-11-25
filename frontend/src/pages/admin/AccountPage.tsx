import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { useAuthStore } from '../../stores/useAuthStore'
import McpAccessKeysPanel from '../../components/admin/McpAccessKeysPanel'
import Button from '../../components/admin/ui/Button'
import { Card, CardFooter, CardHeader, CardSection } from '../../components/admin/ui/Card'
import Field from '../../components/admin/ui/Field'
import { Input } from '../../components/admin/ui/Input'
import KeyValueList from '../../components/admin/ui/KeyValueList'
import PageHeader from '../../components/admin/ui/PageHeader'
import PasswordInput from '../../components/admin/ui/PasswordInput'

const MIN_PASSWORD = 8

type PasswordErrors = Partial<Record<'current' | 'new' | 'confirm', string>>

const AccountPage = () => {
  const user = useAuthStore((state) => state.user)
  const refreshUser = useAuthStore((state) => state.refreshUser)
  const updateName = useAuthStore((state) => state.updateName)
  const changePassword = useAuthStore((state) => state.changePassword)

  const [name, setName] = useState(user?.name ?? '')
  const [nameError, setNameError] = useState<string | null>(null)
  const [savingName, setSavingName] = useState(false)

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordErrors, setPasswordErrors] = useState<PasswordErrors>({})
  const [savingPassword, setSavingPassword] = useState(false)

  // Sincroniza nome/e-mail com a API ao abrir a página (o storage pode estar defasado).
  useEffect(() => {
    refreshUser()
  }, [refreshUser])

  const handleSaveName = async (e: React.FormEvent) => {
    e.preventDefault()
    if (name.trim().length < 2) {
      setNameError('O nome precisa ter ao menos 2 caracteres.')
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
    // Mesmas regras e mensagens de antes; o erro agora aparece também junto ao campo.
    if (!currentPassword) {
      setPasswordErrors({ current: 'Informe a senha atual.' })
      toast.error('Informe a senha atual')
      return
    }
    if (newPassword.length < MIN_PASSWORD) {
      setPasswordErrors({ new: `A nova senha deve ter ao menos ${MIN_PASSWORD} caracteres.` })
      toast.error(`A nova senha deve ter ao menos ${MIN_PASSWORD} caracteres`)
      return
    }
    if (newPassword !== confirmPassword) {
      setPasswordErrors({ confirm: 'A confirmação não confere com a nova senha.' })
      toast.error('A confirmação não confere com a nova senha')
      return
    }
    setPasswordErrors({})

    setSavingPassword(true)
    try {
      // Senhas vão só no corpo da requisição; nada delas no console.
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

  const clearPasswordError = (key: keyof PasswordErrors) =>
    setPasswordErrors((prev) => (prev[key] ? { ...prev, [key]: undefined } : prev))

  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        title="Minha conta"
        description="Seus dados de acesso, sua senha e as chaves que conectam assistentes de IA à sua conta."
      />

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
        <Card aria-labelledby="conta-dados-titulo">
          <CardHeader titleId="conta-dados-titulo" icon="user" title="Dados da conta" subtitle="Informações do seu usuário." />
          <KeyValueList
            items={[
              { label: 'E-mail', value: user?.email ?? '—' },
              {
                label: 'Conta criada em',
                value: user?.createdAt ? new Date(user.createdAt).toLocaleDateString('pt-BR') : '—',
                numeric: Boolean(user?.createdAt),
              },
            ]}
          />
          <CardSection>
            <form onSubmit={handleSaveName} className="flex flex-col gap-4">
              <Field id="name" label="Nome" error={nameError}>
                <Input
                  type="text"
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value)
                    setNameError(null)
                  }}
                  required
                  maxLength={260}
                  autoComplete="name"
                />
              </Field>
              <CardFooter note="O nome aparece no menu e no cabeçalho.">
                <Button
                  type="submit"
                  variant="primary"
                  loading={savingName}
                  disabled={name.trim() === (user?.name ?? '')}
                >
                  {savingName ? 'Salvando...' : 'Salvar nome'}
                </Button>
              </CardFooter>
            </form>
          </CardSection>
        </Card>

        <Card as="form" onSubmit={handleChangePassword} aria-labelledby="conta-senha-titulo">
          <CardHeader
            titleId="conta-senha-titulo"
            icon="lock"
            title="Alterar senha"
            subtitle="Use uma senha que você não usa em outros serviços."
          />
          <div className="flex flex-col gap-4">
            <Field id="currentPassword" label="Senha atual" required error={passwordErrors.current}>
              <PasswordInput
                value={currentPassword}
                onChange={(e) => {
                  setCurrentPassword(e.target.value)
                  clearPasswordError('current')
                }}
                required
                autoComplete="current-password"
              />
            </Field>
            <Field
              id="newPassword"
              label="Nova senha"
              required
              hint={`Ao menos ${MIN_PASSWORD} caracteres.`}
              error={passwordErrors.new}
            >
              <PasswordInput
                value={newPassword}
                onChange={(e) => {
                  setNewPassword(e.target.value)
                  clearPasswordError('new')
                }}
                required
                autoComplete="new-password"
                minLength={MIN_PASSWORD}
                maxLength={128}
              />
            </Field>
            <Field id="confirmPassword" label="Confirmar nova senha" required error={passwordErrors.confirm}>
              <Input
                type="password"
                value={confirmPassword}
                onChange={(e) => {
                  setConfirmPassword(e.target.value)
                  clearPasswordError('confirm')
                }}
                required
                autoComplete="new-password"
              />
            </Field>
          </div>
          <CardFooter>
            <Button type="submit" variant="primary" loading={savingPassword}>
              {savingPassword ? 'Alterando...' : 'Alterar senha'}
            </Button>
          </CardFooter>
        </Card>

        {/* Âncora do item "Chaves MCP" da sidebar; scroll-mt compensa o cabeçalho fixo. */}
        <Card as="section" id="chaves-mcp" aria-labelledby="conta-mcp-titulo" className="scroll-mt-24 lg:col-span-2">
          <CardHeader
            titleId="conta-mcp-titulo"
            icon="key"
            title="Assistentes de IA (MCP)"
            subtitle="Conecte o Claude, o Cursor, o VS Code ou outro assistente compatível com MCP à sua conta para criar, configurar e inspecionar os seus agentes por conversa."
          />
          <McpAccessKeysPanel />
        </Card>
      </div>
    </div>
  )
}

export default AccountPage
