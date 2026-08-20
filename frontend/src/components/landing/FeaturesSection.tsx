import type { ReactNode } from 'react'

const ChipRow = ({ items }: { items: { label: string; color: string }[] }) => (
  <div className="mt-5 flex flex-wrap items-center gap-1.5">
    {items.map((c) => (
      <span
        key={c.label}
        className="inline-flex shrink-0 items-center gap-1 whitespace-nowrap rounded-full border border-gray-200 bg-white px-2 py-0.5 text-[10px] font-medium text-gray-700 shadow-sm"
      >
        <span className={`h-1.5 w-1.5 flex-shrink-0 rounded-full ${c.color}`} />
        {c.label}
      </span>
    ))}
  </div>
)

type Feature = {
  title: string
  desc: string
  icon: ReactNode
  highlight?: boolean
  visual?: ReactNode
}

const FEATURES: Feature[] = [
  {
    title: 'Agentes inteligentes',
    desc: 'Prompts personalizados, personalidade própria e contexto do seu negócio em cada resposta.',
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="h-5 w-5">
        <path fillRule="evenodd" d="M9 4.5a.75.75 0 01.721.544l.813 2.846a3.75 3.75 0 002.576 2.576l2.846.813a.75.75 0 010 1.442l-2.846.813a3.75 3.75 0 00-2.576 2.576l-.813 2.846a.75.75 0 01-1.442 0l-.813-2.846a3.75 3.75 0 00-2.576-2.576l-2.846-.813a.75.75 0 010-1.442l2.846-.813A3.75 3.75 0 007.466 7.89l.813-2.846A.75.75 0 019 4.5zM18 1.5a.75.75 0 01.728.568l.258 1.036c.236.94.97 1.674 1.91 1.91l1.036.258a.75.75 0 010 1.456l-1.036.258c-.94.236-1.674.97-1.91 1.91l-.258 1.036a.75.75 0 01-1.456 0l-.258-1.036a2.625 2.625 0 00-1.91-1.91l-1.036-.258a.75.75 0 010-1.456l1.036-.258a2.625 2.625 0 001.91-1.91l.258-1.036A.75.75 0 0118 1.5z" clipRule="evenodd" />
      </svg>
    ),
  },
  {
    title: 'Base de conhecimento',
    desc: 'Faça upload de PDFs, manuais e FAQs. A IA responde com precisão usando seus próprios documentos.',
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="h-5 w-5">
        <path fillRule="evenodd" d="M5.625 1.5c-1.036 0-1.875.84-1.875 1.875v17.25c0 1.035.84 1.875 1.875 1.875h12.75c1.035 0 1.875-.84 1.875-1.875V12.75A3.75 3.75 0 0016.5 9h-1.875a1.875 1.875 0 01-1.875-1.875V5.25A3.75 3.75 0 009 1.5H5.625zM7.5 15a.75.75 0 01.75-.75h7.5a.75.75 0 010 1.5h-7.5A.75.75 0 017.5 15zm.75 2.25a.75.75 0 000 1.5H12a.75.75 0 000-1.5H8.25z" clipRule="evenodd" />
        <path d="M12.971 1.816A5.23 5.23 0 0114.25 5.25v1.875c0 .207.168.375.375.375H16.5a5.23 5.23 0 013.434 1.279 9.768 9.768 0 00-6.963-6.963z" />
      </svg>
    ),
  },
  {
    title: 'Integração com chatbot',
    desc: 'Widget pronto pra colar no seu site, WhatsApp, Telegram e onde mais seu cliente estiver.',
    highlight: true,
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="h-5 w-5">
        <path d="M4.913 2.658c2.075-.27 4.19-.408 6.337-.408 2.147 0 4.262.139 6.337.408 1.922.25 3.291 1.861 3.405 3.727a4.403 4.403 0 00-1.032-.211 50.89 50.89 0 00-8.42 0c-2.358.196-4.04 2.19-4.04 4.434v4.286a4.47 4.47 0 002.433 3.984L7.28 21.53A.75.75 0 016 21v-4.03a48.527 48.527 0 01-1.087-.128C2.905 16.58 1.5 14.833 1.5 12.862V6.638c0-1.97 1.405-3.718 3.413-3.979z" />
        <path d="M15.75 7.5c-1.376 0-2.739.057-4.086.169C10.124 7.797 9 9.103 9 10.609v4.285c0 1.507 1.128 2.814 2.67 2.94 1.243.102 2.5.157 3.768.165l2.782 2.781a.75.75 0 001.28-.53v-2.39l.33-.026c1.542-.125 2.67-1.433 2.67-2.94v-4.286c0-1.505-1.125-2.811-2.664-2.94A49.392 49.392 0 0015.75 7.5z" />
      </svg>
    ),
    visual: (
      <ChipRow
        items={[
          { label: 'Site', color: 'bg-ava-600' },
          { label: 'WhatsApp', color: 'bg-[#25d366]' },
          { label: 'Telegram', color: 'bg-[#2aabee]' },
          { label: 'Instagram', color: 'bg-gradient-to-br from-[#f58529] via-[#dd2a7b] to-[#8134af]' },
        ]}
      />
    ),
  },
  {
    title: 'Integração com sistemas da empresa',
    desc: 'Conecte CRM, ERP, planilhas e APIs internas. Sua Ava consulta pedidos, estoque e clientes em tempo real.',
    highlight: true,
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className="h-5 w-5">
        <rect x="3" y="3" width="7" height="7" rx="1.5" />
        <rect x="14" y="3" width="7" height="7" rx="1.5" />
        <rect x="3" y="14" width="7" height="7" rx="1.5" />
        <rect x="14" y="14" width="7" height="7" rx="1.5" />
      </svg>
    ),
    visual: (
      <ChipRow
        items={[
          { label: 'CRM', color: 'bg-ava-600' },
          { label: 'ERP', color: 'bg-emerald-500' },
          { label: 'API', color: 'bg-amber-500' },
          { label: 'Planilhas', color: 'bg-rose-500' },
        ]}
      />
    ),
  },
  {
    title: 'Chat em tempo real',
    desc: 'Streaming de respostas — o visitante vê a IA pensando, sem telas congeladas.',
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="h-5 w-5">
        <path d="M13 2L3 14h7l-1 8 10-12h-7l1-8z" />
      </svg>
    ),
  },
  {
    title: 'Coleta de leads',
    desc: 'Captura nome, e-mail e telefone de forma natural, dentro da própria conversa.',
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="h-5 w-5">
        <path d="M1.5 8.67v8.58a3 3 0 003 3h15a3 3 0 003-3V8.67l-8.928 5.493a3 3 0 01-3.144 0L1.5 8.67z" />
        <path d="M22.5 6.908V6.75a3 3 0 00-3-3h-15a3 3 0 00-3 3v.158l9.714 5.978a1.5 1.5 0 001.572 0L22.5 6.908z" />
      </svg>
    ),
  },
]

const FeaturesSection = () => {
  return (
    <section className="border-t border-gray-100 bg-gradient-to-b from-gray-50/70 to-white">
      <div className="mx-auto max-w-6xl px-6 py-24 md:py-32">
        <div className="mx-auto mb-14 max-w-2xl text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-ava-200 bg-ava-50 px-3 py-1 text-xs font-medium text-ava-700">
            Recursos
          </div>
          <h2 className="font-display text-3xl leading-tight tracking-tight text-gray-900 md:text-5xl">
            Tudo que você precisa para
            <em className="not-italic text-ava-600"> automatizar </em>
            seu atendimento
          </h2>
          <p className="mt-4 text-gray-500">
            Uma plataforma completa para criar, treinar, integrar e publicar agentes de IA
            que representam o seu negócio.
          </p>
        </div>

        <div className="grid auto-rows-fr gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {FEATURES.map((f) => (
            <FeatureCard key={f.title} feature={f} />
          ))}
        </div>
      </div>
    </section>
  )
}

const FeatureCard = ({ feature, className }: { feature: Feature; className?: string }) => {
  return (
    <div
      className={`group relative flex flex-col overflow-hidden rounded-2xl border border-gray-200 bg-white p-6 transition-all hover:-translate-y-0.5 hover:border-ava-200 hover:shadow-xl hover:shadow-ava-100/60 ${className ?? ''}`}
    >
      {/* Subtle decorative glow for highlight cards */}
      {feature.highlight && (
        <div className="pointer-events-none absolute -right-10 -top-10 h-36 w-36 rounded-full bg-ava-100/40 blur-3xl" />
      )}

      <div className="relative flex flex-1 flex-col">
        <div className="mb-4 flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-ava-50 to-ava-100 text-ava-600 ring-1 ring-ava-200/60 transition-all group-hover:from-ava-600 group-hover:to-ava-700 group-hover:text-white group-hover:ring-ava-600">
            {feature.icon}
          </div>
          {feature.highlight && (
            <span className="rounded-full border border-ava-200 bg-ava-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-ava-700">
              Destaque
            </span>
          )}
        </div>

        <h3 className="mb-2 text-base font-semibold text-gray-900">{feature.title}</h3>
        <p className="text-sm leading-relaxed text-gray-500">{feature.desc}</p>

        {feature.visual}
      </div>
    </div>
  )
}

export default FeaturesSection
