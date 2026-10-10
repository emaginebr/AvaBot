import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useAuthStore } from '../../stores/useAuthStore'

const MIN_PASSWORD = 8
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const RegisterPage = () => {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const register = useAuthStore((state) => state.register)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const navigate = useNavigate()

  // Edge case da spec: criar conta ja autenticado leva direto ao painel.
  if (isAuthenticated) {
    return <Navigate to="/admin" replace />
  }

  const validate = (): string | null => {
    if (name.trim().length < 2) return 'Informe seu nome (ao menos 2 caracteres)'
    if (!EMAIL_PATTERN.test(email.trim())) return 'Informe um e-mail válido'
    if (password.length < MIN_PASSWORD) return `A senha deve ter ao menos ${MIN_PASSWORD} caracteres`
    return null
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    const error = validate()
    if (error) {
      toast.error(error)
      return
    }

    setLoading(true)
    try {
      const result = await register({ name: name.trim(), email: email.trim(), password })
      if (result.sucesso) {
        toast.success('Conta criada com sucesso')
        navigate('/admin', { replace: true })
      } else {
        toast.error(result.mensagem || 'Não foi possível criar a conta')
      }
    } catch {
      toast.error('Erro ao conectar com o servidor')
    } finally {
      setLoading(false)
    }
  }

  const inputClass =
    'w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow'

  return (
    <div className="min-h-screen bg-gradient-to-br from-ava-50 to-ava-100 flex items-center justify-center px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="font-display text-4xl text-ava-900 mb-2">AvaBot</h1>
          <p className="text-gray-500">Crie sua conta e comece a montar seus agentes</p>
        </div>

        <div className="bg-white rounded-2xl shadow-xl p-8">
          <form onSubmit={handleSubmit} className="space-y-5" noValidate>
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
                autoFocus
                autoComplete="name"
                maxLength={260}
                className={inputClass}
                placeholder="Como quer ser chamado"
              />
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1.5">
                E-mail
              </label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
                maxLength={260}
                className={inputClass}
                placeholder="voce@empresa.com"
              />
              <p className="mt-1 text-xs text-gray-400">Será o seu login. Não enviamos e-mail de confirmação.</p>
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1.5">
                Senha
              </label>
              <input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="new-password"
                minLength={MIN_PASSWORD}
                maxLength={128}
                className={inputClass}
                placeholder={`Mínimo de ${MIN_PASSWORD} caracteres`}
              />
              <p className={`mt-1 text-xs ${password.length > 0 && password.length < MIN_PASSWORD ? 'text-red-500' : 'text-gray-400'}`}>
                A senha precisa ter ao menos {MIN_PASSWORD} caracteres.
              </p>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-2.5 bg-ava-600 text-white rounded-lg font-medium hover:bg-ava-700 focus:outline-none focus:ring-2 focus:ring-ava-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {loading ? 'Criando conta...' : 'Criar conta'}
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-gray-500">
            Já tem conta?{' '}
            <Link to="/login" className="font-medium text-ava-600 hover:text-ava-700">
              Entrar
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}

export default RegisterPage
