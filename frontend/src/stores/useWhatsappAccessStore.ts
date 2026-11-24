import { create } from 'zustand'
import { WhatsappAccessService } from '../Services/WhatsappAccessService'
import { WHATSAPP_REQUEST_STATUS } from '../types/whatsappAccess'
import type {
  WhatsappAccessConfigInfo,
  WhatsappAccessConfigUpdateInfo,
  WhatsappAccessRequestInfo,
  WhatsappContactInfo,
  WhatsappContactInsertInfo,
} from '../types/whatsappAccess'
import type { Result } from '../types/result'

interface WhatsappAccessStoreState {
  config: WhatsappAccessConfigInfo | null
  contacts: WhatsappContactInfo[]
  requests: WhatsappAccessRequestInfo[]
  requestFilter: number
  loadingConfig: boolean
  loadingContacts: boolean
  loadingRequests: boolean
  error: string | null

  reset: () => void
  loadAll: (slug: string) => Promise<void>
  loadConfig: (slug: string) => Promise<void>
  saveConfig: (slug: string, info: WhatsappAccessConfigUpdateInfo) => Promise<Result<WhatsappAccessConfigInfo>>

  loadContacts: (slug: string) => Promise<void>
  addContact: (slug: string, info: WhatsappContactInsertInfo) => Promise<Result<WhatsappContactInfo>>
  updateContact: (slug: string, id: number, info: WhatsappContactInsertInfo) => Promise<Result<WhatsappContactInfo>>
  removeContact: (slug: string, id: number) => Promise<Result<boolean>>

  setRequestFilter: (slug: string, status: number) => Promise<void>
  loadRequests: (slug: string) => Promise<void>
  approveRequest: (slug: string, id: number, name?: string) => Promise<Result<WhatsappContactInfo>>
  rejectRequest: (slug: string, id: number) => Promise<Result<WhatsappAccessRequestInfo>>
}

const initialState = {
  config: null,
  contacts: [],
  requests: [],
  requestFilter: WHATSAPP_REQUEST_STATUS.PENDING as number,
  loadingConfig: false,
  loadingContacts: false,
  loadingRequests: false,
  error: null,
}

export const useWhatsappAccessStore = create<WhatsappAccessStoreState>((set, get) => ({
  ...initialState,

  reset: () => set({ ...initialState }),

  loadAll: async (slug) => {
    await Promise.all([get().loadConfig(slug), get().loadContacts(slug), get().loadRequests(slug)])
  },

  loadConfig: async (slug) => {
    set({ loadingConfig: true })
    const result = await WhatsappAccessService.getConfig(slug)
    if (result.sucesso) set({ config: result.dados, loadingConfig: false })
    else set({ error: result.mensagem || 'Não foi possível carregar a configuração', loadingConfig: false })
  },

  saveConfig: async (slug, info) => {
    const result = await WhatsappAccessService.updateConfig(slug, info)
    if (result.sucesso) set({ config: result.dados })
    return result
  },

  loadContacts: async (slug) => {
    set({ loadingContacts: true })
    const result = await WhatsappAccessService.listContacts(slug)
    if (result.sucesso) set({ contacts: result.dados ?? [], loadingContacts: false })
    else set({ error: result.mensagem || 'Não foi possível carregar os contatos', loadingContacts: false })
  },

  addContact: async (slug, info) => {
    const result = await WhatsappAccessService.addContact(slug, info)
    if (result.sucesso) await Promise.all([get().loadContacts(slug), get().loadConfig(slug), get().loadRequests(slug)])
    return result
  },

  updateContact: async (slug, id, info) => {
    const result = await WhatsappAccessService.updateContact(slug, id, info)
    if (result.sucesso) await Promise.all([get().loadContacts(slug), get().loadRequests(slug), get().loadConfig(slug)])
    return result
  },

  removeContact: async (slug, id) => {
    const result = await WhatsappAccessService.removeContact(slug, id)
    if (result.sucesso) await Promise.all([get().loadContacts(slug), get().loadConfig(slug)])
    return result
  },

  setRequestFilter: async (slug, status) => {
    set({ requestFilter: status })
    await get().loadRequests(slug)
  },

  loadRequests: async (slug) => {
    set({ loadingRequests: true })
    const result = await WhatsappAccessService.listRequests(slug, get().requestFilter)
    if (result.sucesso) set({ requests: result.dados ?? [], loadingRequests: false })
    else set({ error: result.mensagem || 'Não foi possível carregar as solicitações', loadingRequests: false })
  },

  approveRequest: async (slug, id, name) => {
    const result = await WhatsappAccessService.approveRequest(slug, id, name)
    if (result.sucesso) await Promise.all([get().loadRequests(slug), get().loadContacts(slug), get().loadConfig(slug)])
    return result
  },

  rejectRequest: async (slug, id) => {
    const result = await WhatsappAccessService.rejectRequest(slug, id)
    if (result.sucesso) await Promise.all([get().loadRequests(slug), get().loadConfig(slug)])
    return result
  },
}))
