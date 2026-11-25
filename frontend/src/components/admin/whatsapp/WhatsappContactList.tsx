import { useCallback, useState } from 'react'
import { toast } from 'sonner'
import { useWhatsappAccessStore } from '../../../stores/useWhatsappAccessStore'
import { WHATSAPP_CONTACT_SOURCE } from '../../../types/whatsappAccess'
import type { WhatsappContactInfo } from '../../../types/whatsappAccess'
import Alert from '../ui/Alert'
import Button from '../ui/Button'
import { Chip } from '../ui/Chip'
import DataTable from '../ui/DataTable'
import type { DataColumn } from '../ui/DataTable'
import Field from '../ui/Field'
import { Input } from '../ui/Input'
import Modal from '../ui/Modal'
import { EmptyState, LoadingState } from '../ui/States'
import { useConfirm } from '../ui/useConfirm'

interface WhatsappContactListProps {
  slug: string
}

interface ContactFormProps {
  slug: string
  contact: WhatsappContactInfo | null
  onClose: () => void
}

interface ContactErrors {
  name?: string
  phone?: string
  /** Erros devolvidos pela API (lista). */
  server: string[]
}

const NO_ERRORS: ContactErrors = { server: [] }

const ContactForm = ({ slug, contact, onClose }: ContactFormProps) => {
  const { addContact, updateContact } = useWhatsappAccessStore()
  const [name, setName] = useState(contact?.name ?? '')
  const [phone, setPhone] = useState(contact?.phone ?? contact?.phoneNormalized ?? '')
  const [saving, setSaving] = useState(false)
  const [errors, setErrors] = useState<ContactErrors>(NO_ERRORS)

  const handleSave = async () => {
    // Mesmas validações e mensagens de antes; agora cada uma aparece junto ao seu campo.
    const local: ContactErrors = { server: [] }
    if (!name.trim()) local.name = 'Informe o nome.'
    if (!phone.trim()) local.phone = 'Informe o telefone.'
    if (local.name || local.phone) {
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
      setErrors({ server: result.erros?.length ? result.erros : [result.mensagem || 'Não foi possível salvar o contato'] })
    }
  }

  return (
    <Modal
      title={contact ? 'Editar contato' : 'Adicionar contato'}
      description="Contatos autorizados conversam com o agente quando o acesso é restrito."
      icon="user"
      onClose={onClose}
      footer={
        <>
          <Button onClick={onClose}>Cancelar</Button>
          <Button variant="primary" onClick={handleSave} loading={saving}>
            {saving ? 'Salvando...' : 'Salvar'}
          </Button>
        </>
      }
    >
      <Field id="whatsapp-contato-nome" label="Nome" required error={errors.name}>
        <Input
          data-autofocus
          value={name}
          onChange={(e) => setName(e.target.value)}
          maxLength={120}
        />
      </Field>
      <Field
        id="whatsapp-contato-telefone"
        label="Telefone"
        required
        hint="Com DDD. Sem código do país, assumimos Brasil (+55)."
        error={errors.phone}
      >
        <Input
          value={phone}
          onChange={(e) => setPhone(e.target.value)}
          placeholder="(11) 98765-4321"
          inputMode="tel"
          maxLength={30}
          className="font-ui-mono tabular-nums"
        />
      </Field>
      {errors.server.length > 0 && (
        <Alert tone="err" live>
          <ul className="m-0 list-disc pl-4">
            {errors.server.map((e) => (
              <li key={e}>{e}</li>
            ))}
          </ul>
        </Alert>
      )}
    </Modal>
  )
}

const WhatsappContactList = ({ slug }: WhatsappContactListProps) => {
  const { contacts, loadingContacts, removeContact } = useWhatsappAccessStore()
  // undefined = fechado; null = novo contato; objeto = edição
  const [editing, setEditing] = useState<WhatsappContactInfo | null | undefined>(undefined)
  const [confirm, confirmDialog] = useConfirm()

  const closeForm = useCallback(() => setEditing(undefined), [])

  const handleRemove = async (contact: WhatsappContactInfo) => {
    const ok = await confirm({
      title: 'Remover contato?',
      message: `Remover ${contact.name} da lista de contatos autorizados?`,
      confirmLabel: 'Remover',
    })
    if (!ok) return
    const result = await removeContact(slug, contact.whatsappContactId)
    if (result.sucesso) toast.success('Contato removido')
    else toast.error(result.mensagem || 'Não foi possível remover o contato')
  }

  const columns: DataColumn<WhatsappContactInfo>[] = [
    {
      key: 'name',
      header: 'Nome',
      kind: 'main',
      cell: (c) => <span className="block font-medium [overflow-wrap:anywhere] text-ink">{c.name}</span>,
    },
    {
      key: 'phone',
      header: 'Telefone',
      nowrap: true,
      cell: (c) =>
        (c.phone ?? c.phoneNormalized) ? (
          <span className="font-ui-mono tabular-nums">{c.phone ?? c.phoneNormalized}</span>
        ) : (
          <span className="text-ink-3">Não identificado</span>
        ),
    },
    {
      key: 'source',
      header: 'Origem',
      cell: (c) => <Chip>{c.source === WHATSAPP_CONTACT_SOURCE.APPROVAL ? 'Aprovação' : 'Manual'}</Chip>,
    },
    {
      key: 'actions',
      header: 'Ações',
      hideHeader: true,
      kind: 'actions',
      cell: (c) => (
        <>
          <Button size="sm" icon="edit" onClick={() => setEditing(c)} aria-label={`Editar ${c.name}`}>
            Editar
          </Button>
          <Button size="sm" variant="dangerGhost" onClick={() => handleRemove(c)} aria-label={`Remover ${c.name}`}>
            Remover
          </Button>
        </>
      ),
    },
  ]

  return (
    <div>
      <div className="mb-3.5 flex flex-wrap items-center justify-between gap-2.5">
        <p className="m-0 text-[12.5px] text-ink-3">Quem pode usar o agente quando o acesso está restrito.</p>
        <Button variant="primary" icon="plus" onClick={() => setEditing(null)}>
          Adicionar contato
        </Button>
      </div>

      {loadingContacts && contacts.length === 0 ? (
        <LoadingState label="Carregando contatos…" rows={3} />
      ) : contacts.length === 0 ? (
        <EmptyState
          icon="user"
          title="Nenhum contato autorizado"
          description="Adicione contatos manualmente ou aprove os pedidos de acesso."
        />
      ) : (
        <DataTable columns={columns} rows={contacts} rowKey={(c) => c.whatsappContactId} caption="Contatos autorizados" />
      )}

      {editing !== undefined && (
        <ContactForm key={editing?.whatsappContactId ?? 'new'} slug={slug} contact={editing} onClose={closeForm} />
      )}
      {confirmDialog}
    </div>
  )
}

export default WhatsappContactList
