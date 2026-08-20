import { useState } from 'react'

interface UserDataFormProps {
  fields: string[]
  onSubmit: (data: { name?: string; email?: string; phone?: string }) => void
}

const UserDataForm = ({ fields, onSubmit }: UserDataFormProps) => {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    onSubmit({
      ...(fields.includes('name') ? { name } : {}),
      ...(fields.includes('email') ? { email } : {}),
      ...(fields.includes('phone') ? { phone } : {}),
    })
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 max-w-sm mx-auto">
      <p className="text-center text-gray-600 mb-4">Por favor, preencha seus dados para iniciar:</p>

      {fields.includes('name') && (
        <input type="text" placeholder="Seu nome" value={name} onChange={e => setName(e.target.value)} required
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500" />
      )}
      {fields.includes('email') && (
        <input type="email" placeholder="Seu e-mail" value={email} onChange={e => setEmail(e.target.value)} required
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500" />
      )}
      {fields.includes('phone') && (
        <input type="tel" placeholder="Seu telefone" value={phone} onChange={e => setPhone(e.target.value)} required
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500" />
      )}

      <button type="submit" className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
        Iniciar conversa
      </button>
    </form>
  )
}

export default UserDataForm
