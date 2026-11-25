import { useEffect, useRef, useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
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
import { MailIcon, UserIcon } from '../../components/auth/AuthIcons'

const MIN_PASSWORD = 8
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const validateName = (value: string): string | undefined =>
  value.trim().length < 2 ? 'Informe seu nome (ao menos 2 caracteres).' : undefined

const validateEmail = (value: string): string | undefined => {
  const v = value.trim()
  if (!v) return 'Informe seu e-mail.'
  if (!EMAIL_PATTERN.test(v)) return 'Informe um e-mail válido.'
  return undefined
}

const validatePassword = (value: string): string | undefined => {
  if (!value) return 'Informe uma senha.'
  if (value.length < MIN_PASSWORD) return `A senha deve ter ao menos ${MIN_PASSWORD} caracteres.`
  return undefined
}

interface FieldErrors {
  name?: string
  email?: string
  password?: string
}

const RegisterPage = () => {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [errors, setErrors] = useState<FieldErrors>({})
  const [apiError, setApiError] = useState<string | null>(null)
  // Muda a cada falha para remontar o alerta (repete o "tremor") e mover o foco até ele.
  const [apiErrorKey, setApiErrorKey] = useState(0)
  const nameRef = useRef<HTMLInputElement>(null)
  const emailRef = useRef<HTMLInputElement>(null)
  const passwordRef = useRef<HTMLInputElement>(null)
  const alertRef = useRef<HTMLDivElement>(null)
  const register = useAuthStore((state) => state.register)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const navigate = useNavigate()

  useEffect(() => {
    if (apiErrorKey > 0) alertRef.current?.focus()
  }, [apiErrorKey])

  // Edge case da spec: criar conta já autenticado leva direto ao painel.
  if (isAuthenticated) {
    return <Navigate to="/admin" replace />
  }

  const showApiError = (message: string) => {
    setApiError(message)
    setApiErrorKey((k) => k + 1)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (loading) return
    setApiError(null)

    const next: FieldErrors = {
      name: validateName(name),
      email: validateEmail(email),
      password: validatePassword(password),
    }
    setErrors(next)
    const firstInvalid = next.name ? nameRef : next.email ? emailRef : next.password ? passwordRef : null
    if (firstInvalid) {
      firstInvalid.current?.focus()
      return
    }

    setLoading(true)
    try {
      const result = await register({ name: name.trim(), email: email.trim(), password })
      if (result.sucesso) {
        toast.success('Conta criada com sucesso')
        navigate('/admin', { replace: true })
      } else {
        showApiError(result.mensagem || 'Não foi possível criar a conta.')
      }
    } catch {
      showApiError('Erro ao conectar com o servidor.')
    } finally {
      setLoading(false)
    }
  }

  const passwordTooShort = password.length > 0 && password.length < MIN_PASSWORD

  return (
    <AuthLayout>
      <AuthHeading
        badge="Nova conta"
        title="Crie sua"
        highlight="conta"
        lead="Comece a montar seus agentes de IA."
      />

      {apiError && (
        <FormAlert key={apiErrorKey} ref={alertRef} tone="error">
          {apiError}
        </FormAlert>
      )}

      <form onSubmit={handleSubmit} noValidate aria-label="Criar conta" className="flex flex-col gap-[18px]">
        <FormField id="name" label="Nome" error={errors.name}>
          <IconInput
            ref={nameRef}
            id="name"
            name="name"
            type="text"
            autoComplete="name"
            required
            autoFocus
            maxLength={260}
            placeholder="Como quer ser chamado"
            aria-describedby="name-error"
            invalid={!!errors.name}
            icon={<UserIcon className="h-full w-full" />}
            value={name}
            onChange={(e) => {
              setName(e.target.value)
              if (errors.name) setErrors((prev) => ({ ...prev, name: validateName(e.target.value) }))
              setApiError(null)
            }}
            onBlur={(e) => {
              if (e.target.value) setErrors((prev) => ({ ...prev, name: validateName(e.target.value) }))
            }}
          />
        </FormField>

        <FormField
          id="email"
          label="E-mail"
          error={errors.email}
          hint="Será o seu login. Não enviamos e-mail de confirmação."
        >
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
            maxLength={260}
            placeholder="voce@empresa.com"
            aria-describedby="email-error email-hint"
            invalid={!!errors.email}
            icon={<MailIcon className="h-full w-full" />}
            value={email}
            onChange={(e) => {
              setEmail(e.target.value)
              if (errors.email) setErrors((prev) => ({ ...prev, email: validateEmail(e.target.value) }))
              setApiError(null)
            }}
            onBlur={(e) => {
              if (e.target.value) setErrors((prev) => ({ ...prev, email: validateEmail(e.target.value) }))
            }}
          />
        </FormField>

        <FormField
          id="password"
          label="Senha"
          error={errors.password}
          // Com erro inline visível, a dica não repete a mesma mensagem em vermelho.
          hint={errors.password ? undefined : `A senha precisa ter ao menos ${MIN_PASSWORD} caracteres.`}
          hintClassName={passwordTooShort ? 'text-[#b42318]' : 'text-[#5d6683]'}
        >
          <PasswordInput
            ref={passwordRef}
            id="password"
            name="password"
            autoComplete="new-password"
            required
            minLength={MIN_PASSWORD}
            maxLength={128}
            placeholder={`Mínimo de ${MIN_PASSWORD} caracteres`}
            aria-describedby="password-error password-hint password-caps"
            invalid={!!errors.password}
            value={password}
            onChange={(e) => {
              setPassword(e.target.value)
              if (errors.password) setErrors((prev) => ({ ...prev, password: validatePassword(e.target.value) }))
              setApiError(null)
            }}
          />
        </FormField>

        <AuthSubmitButton loading={loading} loadingLabel="Criando conta...">
          Criar conta
        </AuthSubmitButton>
      </form>

      <AuthCardFooter>
        <p className="m-0">
          Já tem conta? <AuthTextLink to="/login">Entrar</AuthTextLink>
        </p>
      </AuthCardFooter>
    </AuthLayout>
  )
}

export default RegisterPage
