import { useState } from 'react'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { toast } from 'sonner'
import { useAuthStore } from '../../stores/useAuthStore'

const LoginPage = () => {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const login = useAuthStore((state) => state.login)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const expired = searchParams.get('expired') === '1'

  if (isAuthenticated) {
    return <Navigate to="/admin" replace />
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)

    try {
      const result = await login({ email, password })
      if (result.sucesso) {
        toast.success('Login realizado com sucesso')
        navigate('/admin', { replace: true })
      } else {
        // A API nunca diz se foi o e-mail ou a senha (FR-006); o aviso de bloqueio vem na mesma mensagem.
        toast.error(result.mensagem || 'Credenciais inválidas')
      }
    } catch {
      toast.error('Erro ao conectar com o servidor')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-ava-50 to-ava-100 flex items-center justify-center px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="font-display text-4xl text-ava-900 mb-2">AvaBot</h1>
          <p className="text-gray-500">Acesse o painel administrativo</p>
        </div>

        <div className="bg-white rounded-2xl shadow-xl p-8">
          {expired && (
            <div className="mb-5 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
              Sessão expirada, entre novamente.
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">
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
                autoFocus
                autoComplete="email"
                className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow"
                placeholder="Digite seu e-mail"
              />
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
                autoComplete="current-password"
                className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-ava-500 focus:border-transparent transition-shadow"
                placeholder="Digite sua senha"
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-2.5 bg-ava-600 text-white rounded-lg font-medium hover:bg-ava-700 focus:outline-none focus:ring-2 focus:ring-ava-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {loading ? 'Entrando...' : 'Entrar'}
            </button>
          </form>

          <div className="mt-6 space-y-2 text-center text-sm">
            <p className="text-gray-500">
              Não tem conta?{' '}
              <Link to="/register" className="font-medium text-ava-600 hover:text-ava-700">
                Criar conta
              </Link>
            </p>
            {/* FR-017: sem recuperacao automatica nesta versao */}
            <p className="text-gray-400">Esqueceu a senha? Fale com o suporte.</p>
          </div>
        </div>
      </div>
    </div>
  )
}

export default LoginPage
