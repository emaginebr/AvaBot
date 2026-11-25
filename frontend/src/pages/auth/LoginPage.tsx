import { useRef, useState } from 'react'
import { Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { toast } from 'sonner'
import { useAuthStore } from '../../stores/useAuthStore'
import AuthLayout from '../../components/auth/AuthLayout'
import AuthHeading from '../../components/auth/AuthHeading'
import FormAlert from '../../components/auth/FormAlert'
import FormField from '../../components/auth/FormField'
import IconInput from '../../components/auth/IconInput'
import PasswordInput from '../../components/auth/PasswordInput'
import AuthSubmitButton from '../../components/auth/AuthSubmitButton'
import { AuthCardFooter, AuthTextLink } from '../../components/auth/AuthCardFooter'
import { MailIcon } from '../../components/auth/AuthIcons'

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const validateEmail = (value: string): string | undefined => {
  const v = value.trim()
  if (!v) return 'Informe seu e-mail.'
  if (!EMAIL_PATTERN.test(v)) return 'Informe um e-mail válido.'
  return undefined
}

const validatePassword = (value: string): string | undefined => (value ? undefined : 'Informe sua senha.')

interface FieldErrors {
  email?: string
  password?: string
}

const LoginPage = () => {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [errors, setErrors] = useState<FieldErrors>({})
  const [apiError, setApiError] = useState<string | null>(null)
  // Muda a cada falha para remontar o alerta e repetir o "tremor".
  const [apiErrorKey, setApiErrorKey] = useState(0)
  const emailRef = useRef<HTMLInputElement>(null)
  const passwordRef = useRef<HTMLInputElement>(null)
  const login = useAuthStore((state) => state.login)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const expired = searchParams.get('expired') === '1'

  if (isAuthenticated) {
    return <Navigate to="/admin" replace />
  }

  const showApiError = (message: string) => {
    setApiError(message)
    setApiErrorKey((k) => k + 1)
    // A API não diz qual campo errou (FR-006): só limpa a senha e devolve o foco a ela.
    setPassword('')
    passwordRef.current?.focus()
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (loading) return
    setApiError(null)

    const next: FieldErrors = { email: validateEmail(email), password: validatePassword(password) }
    setErrors(next)
    if (next.email || next.password) {
      const firstInvalid = next.email ? emailRef : passwordRef
      firstInvalid.current?.focus()
      return
    }

    setLoading(true)
    try {
      const result = await login({ email: email.trim(), password })
      if (result.sucesso) {
        toast.success('Login realizado com sucesso')
        navigate('/admin', { replace: true })
      } else {
        // A API nunca diz se foi o e-mail ou a senha (FR-006); o aviso de bloqueio vem na mesma mensagem.
        showApiError(result.mensagem || 'Credenciais inválidas.')
      }
    } catch {
      showApiError('Erro ao conectar com o servidor.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <AuthLayout>
      <AuthHeading title="Bem-vindo de" highlight="volta" lead="Acesse o painel administrativo." />

      {expired && <FormAlert tone="warning">Sessão expirada, entre novamente.</FormAlert>}

      {apiError && (
        <FormAlert key={apiErrorKey} tone="error">
          {apiError}
        </FormAlert>
      )}

      <form onSubmit={handleSubmit} noValidate aria-label="Entrar no painel" className="flex flex-col gap-[18px]">
        <FormField id="email" label="E-mail" error={errors.email}>
          <IconInput
            ref={emailRef}
            id="email"
            name="email"
            type="email"
            inputMode="email"
            autoComplete="email"
            autoCapitalize="off"
            spellCheck={false}
            required
            autoFocus
            placeholder="Digite seu e-mail"
            aria-describedby="email-error"
            invalid={!!errors.email}
            icon={<MailIcon className="h-full w-full" />}
            value={email}
            onChange={(e) => {
              setEmail(e.target.value)
              if (errors.email) setErrors((prev) => ({ ...prev, email: validateEmail(e.target.value) }))
              setApiError(null)
            }}
            onBlur={(e) => {
              // Valida ao sair do campo só se algo foi digitado.
              if (e.target.value) setErrors((prev) => ({ ...prev, email: validateEmail(e.target.value) }))
            }}
          />
        </FormField>

        <FormField id="password" label="Senha" error={errors.password}>
          <PasswordInput
            ref={passwordRef}
            id="password"
            name="password"
            autoComplete="current-password"
            required
            placeholder="Digite sua senha"
            aria-describedby="password-error password-caps"
            invalid={!!errors.password}
            value={password}
            onChange={(e) => {
              setPassword(e.target.value)
              if (errors.password) setErrors((prev) => ({ ...prev, password: validatePassword(e.target.value) }))
              setApiError(null)
            }}
          />
        </FormField>

        <AuthSubmitButton loading={loading} loadingLabel="Entrando...">
          Entrar
        </AuthSubmitButton>
      </form>

      <AuthCardFooter>
        <p className="m-0">
          Não tem conta? <AuthTextLink to="/register">Criar conta</AuthTextLink>
        </p>
        {/* FR-017: sem recuperação automática nesta versão */}
        <p className="m-0 text-[13px] text-[#5d6683]">Esqueceu a senha? Fale com o suporte.</p>
      </AuthCardFooter>
    </AuthLayout>
  )
}

export default LoginPage
