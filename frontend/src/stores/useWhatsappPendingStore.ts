import { create } from 'zustand'
import { WhatsappAccessService } from '../Services/WhatsappAccessService'

/** Intervalo mínimo entre consultas automáticas (a cada navegação) do contador. */
const MIN_INTERVAL_MS = 15_000

interface WhatsappPendingStoreState {
  /** Slug do agente ao qual o contador se refere. */
  slug: string | null
  /** Pedidos de acesso do WhatsApp aguardando aprovação (feature 018). */
  count: number
  fetchedAt: number
  /** Atualiza o contador; `force` ignora o intervalo mínimo (ex.: após aprovar/recusar). */
  refresh: (slug: string | null, force?: boolean) => Promise<void>
}

/**
 * Contador de pedidos pendentes do badge da sidebar e do dashboard.
 * Usa `pendingRequestCount` de GET /whatsapp/{slug}/access: uma chamada leve por agente.
 */
export const useWhatsappPendingStore = create<WhatsappPendingStoreState>((set, get) => ({
  slug: null,
  count: 0,
  fetchedAt: 0,

  refresh: async (slug, force = false) => {
    if (!slug) {
      set({ slug: null, count: 0, fetchedAt: 0 })
      return
    }
    const state = get()
    if (!force && state.slug === slug && Date.now() - state.fetchedAt < MIN_INTERVAL_MS) return

    // Troca de agente: zera o contador antigo enquanto a nova consulta não volta.
    if (state.slug !== slug) set({ slug, count: 0, fetchedAt: 0 })

    const result = await WhatsappAccessService.getConfig(slug)
    if (get().slug !== slug) return
    set({ count: result.sucesso ? result.dados?.pendingRequestCount ?? 0 : 0, fetchedAt: Date.now() })
  },
}))
