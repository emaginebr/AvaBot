import { useCallback, useState } from 'react'
import { toast } from 'sonner'
import { useWhatsappAccessStore } from '../../../stores/useWhatsappAccessStore'
import { WHATSAPP_CONTACT_SOURCE } from '../../../types/whatsappAccess'
import type { WhatsappContactInfo } from '../../../types/whatsappAccess'
import WhatsappModal from './WhatsappModal'

interface WhatsappContactListProps {
  slug: string
}

interface ContactFormProps {
  slug: string
  contact: WhatsappContactInfo | null
  onClose: () => void
}

const ContactForm = ({ slug, contact, onClose }: ContactFormProps) => {
  const { addContact, updateContact } = useWhatsappAccessStore()
  const [name, setName] = useState(contact?.name ?? '')
  const [phone, setPhone] = useState(contact?.phone ?? contact?.phoneNormalized ?? '')
  const [saving, setSaving] = useState(false)
  const [errors, setErrors] = useState<string[]>([])

  const handleSave = async () => {
    const local: string[] = []
    if (!name.trim()) local.push('Informe o nome.')
    if (!phone.trim()) local.push('Informe o telefone.')
    if (local.length) {
      setErrors(local)
      return
    }

    setSaving(true)
    const info = { name: name.trim(), phone: phone.trim() }
    const result = contact
      ? await updateContact(slug, contact.whatsappContactId, info)
      : await addContact(slug, info)
    setSaving(false)

    if (result.sucesso) {
      toast.success(contact ? 'Contato atualizado' : 'Contato autorizado')
      onClose()
    } else {
      setErrors(result.erros?.length ? result.erros : [result.mensagem || 'Não foi possível salvar o contato'])
    }
  }

  return (
    <WhatsappModal
      title={contact ? 'Editar contato' : 'Adicionar contato'}
      onClose={onClose}
      footer={
        <>
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 rounded-lg hover:bg-gray-100">
            Cancelar
          </button>
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-4 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700 disabled:opacity-50"
          >
            {saving ? 'Salvando...' : 'Salvar'}
          </button>
        </>
      }
    >
      <div className="space-y-3">
        <label className="block">
          <span className="text-sm font-medium text-gray-700">Nome</span>
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={120}
            className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ava-500"
          />
        </label>
        <label className="block">
          <span className="text-sm font-medium text-gray-700">Telefone</span>
          <input
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder="(11) 98765-4321"
            maxLength={30}
            className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ava-500"
          />
          <span className="text-xs text-gray-400">Com DDD. Sem código do país, assumimos Brasil (+55).</span>
        </label>
        {errors.length > 0 && (
          <ul className="text-sm text-red-600 list-disc pl-5">
            {errors.map((e) => <li key={e}>{e}</li>)}
          </ul>
        )}
      </div>
    </WhatsappModal>
  )
}

const WhatsappContactList = ({ slug }: WhatsappContactListProps) => {
  const { contacts, loadingContacts, removeContact } = useWhatsappAccessStore()
  // undefined = fechado; null = novo contato; objeto = edição
  const [editing, setEditing] = useState<WhatsappContactInfo | null | undefined>(undefined)

  const closeForm = useCallback(() => setEditing(undefined), [])

  const handleRemove = async (contact: WhatsappContactInfo) => {
    if (!confirm(`Remover ${contact.name} da lista de contatos autorizados?`)) return
    const result = await removeContact(slug, contact.whatsappContactId)
    if (result.sucesso) toast.success('Contato removido')
    else toast.error(result.mensagem || 'Não foi possível remover o contato')
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">Contatos autorizados</h2>
          <p className="text-sm text-gray-500 mt-0.5">Quem pode usar o agente quando o acesso está restrito.</p>
        </div>
        <button
          onClick={() => setEditing(null)}
          className="px-3 py-2 bg-ava-600 text-white text-sm font-medium rounded-lg hover:bg-ava-700"
        >
          Adicionar contato
        </button>
      </div>

      {loadingContacts && contacts.length === 0 ? (
        <p className="text-sm text-gray-400">Carregando...</p>
      ) : contacts.length === 0 ? (
        <p className="text-sm text-gray-400">Nenhum contato autorizado.</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-gray-500 border-b border-gray-100">
              <th className="py-2 font-medium">Nome</th>
              <th className="py-2 font-medium">Telefone</th>
              <th className="py-2 font-medium">Origem</th>
              <th className="py-2" />
            </tr>
          </thead>
          <tbody>
            {contacts.map((c) => (
              <tr key={c.whatsappContactId} className="border-b border-gray-50 last:border-0">
                <td className="py-2 text-gray-900">{c.name}</td>
                <td className="py-2 text-gray-600">{c.phone ?? c.phoneNormalized ?? <span className="text-gray-400">Não identificado</span>}</td>
                <td className="py-2 text-gray-500">{c.source === WHATSAPP_CONTACT_SOURCE.APPROVAL ? 'Aprovação' : 'Manual'}</td>
                <td className="py-2 text-right whitespace-nowrap">
                  <button onClick={() => setEditing(c)} className="text-ava-600 hover:underline mr-3">Editar</button>
                  <button onClick={() => handleRemove(c)} className="text-red-600 hover:underline">Remover</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {editing !== undefined && (
        <ContactForm key={editing?.whatsappContactId ?? 'new'} slug={slug} contact={editing} onClose={closeForm} />
      )}
    </div>
  )
}

export default WhatsappContactList
