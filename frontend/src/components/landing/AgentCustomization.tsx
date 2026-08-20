import { useState } from 'react'

type Agent = {
  id: string
  name: string
  role: string
  tone: string
  greeting: string
  color: string
  accent: string
  avatar: string | 'anon'
}

const AGENTS: Agent[] = [
  {
    id: 'ava',
    name: 'Ava',
    role: 'Atendimento & vendas',
    tone: 'Amigável, próxima, usa emojis',
    greeting: 'Oi! 👋 Sou a Ava. Como posso te ajudar hoje?',
    color: '#1f45f1',
    accent: 'from-ava-400 to-ava-700',
    avatar: 'ava-foto.png',
  },
  {
    id: 'biia',
    name: 'Biia',
    role: 'Suporte técnico',
    tone: 'Direta, objetiva, didática',
    greeting: 'Olá, eu sou a Biia. Em que posso ajudar?',
    color: '#a855f7',
    accent: 'from-fuchsia-400 to-purple-700',
    avatar: 'biia-foto.png',
  },
  {
    id: 'leo',
    name: 'Leo',
    role: 'Consultor financeiro',
    tone: 'Formal, claro, sem gírias',
    greeting: 'Bom dia. Sou o Leo, pronto para te atender.',
    color: '#0f766e',
    accent: 'from-teal-400 to-emerald-700',
    avatar: 'anon',
  },
]

const AnonAvatar = ({ color }: { color: string }) => (
  <svg viewBox="0 0 120 120" className="h-full w-full">
    <defs>
      <linearGradient id="anon-bg" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0%" stopColor={color} stopOpacity="0.15" />
        <stop offset="100%" stopColor={color} stopOpacity="0.4" />
      </linearGradient>
    </defs>
    <rect width="120" height="120" fill="url(#anon-bg)" />
    <circle cx="60" cy="48" r="20" fill={color} opacity="0.85" />
    <path
      d="M20 110c0-22 18-36 40-36s40 14 40 36"
      fill={color}
      opacity="0.85"
    />
  </svg>
)

const AgentCustomization = () => {
  const [selected, setSelected] = useState<Agent>(AGENTS[0])

  return (
    <section className="border-t border-gray-100 bg-gradient-to-b from-white via-gray-50/60 to-white">
      <div className="mx-auto max-w-6xl px-6 py-24 md:py-32">
        <div className="mx-auto mb-14 max-w-2xl text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-ava-200 bg-ava-50 px-3 py-1 text-xs font-medium text-ava-700">
            <svg viewBox="0 0 24 24" fill="currentColor" className="h-3.5 w-3.5">
              <path d="M12 2l2.4 5.4L20 9l-4 4 1 6-5-3-5 3 1-6-4-4 5.6-1.6z" />
            </svg>
            Personalize seu agente
          </div>
          <h2 className="font-display text-3xl leading-tight tracking-tight text-gray-900 md:text-5xl">
            Cada negócio tem uma
            <em className="not-italic text-ava-600"> voz </em>
            — o seu agente também
          </h2>
          <p className="mt-5 text-lg text-gray-500">
            Escolha um avatar, defina o tom da conversa e a personalidade.
            Seu agente vai soar exatamente como a sua marca.
          </p>
        </div>

        <div className="grid items-start gap-8 md:grid-cols-[1fr_1.1fr] md:gap-12">
          {/* Agent cards */}
          <div className="flex flex-col gap-4">
            {AGENTS.map((agent) => {
              const active = selected.id === agent.id
              return (
                <button
                  key={agent.id}
                  type="button"
                  onClick={() => setSelected(agent)}
                  className={`group relative flex items-center gap-4 rounded-2xl border bg-white p-4 text-left transition-all ${
                    active
                      ? 'border-transparent shadow-xl'
                      : 'border-gray-200 hover:border-gray-300 hover:shadow-md'
                  }`}
                  style={
                    active
                      ? {
                          boxShadow: `0 10px 40px -10px ${agent.color}40`,
                          outline: `2px solid ${agent.color}`,
                          outlineOffset: '-2px',
                        }
                      : undefined
                  }
                >
                  <div
                    className={`h-16 w-16 flex-shrink-0 overflow-hidden rounded-2xl bg-gradient-to-br ${agent.accent} ring-2 ring-white`}
                  >
                    {agent.avatar === 'anon' ? (
                      <AnonAvatar color={agent.color} />
                    ) : (
                      <img
                        src={`${import.meta.env.BASE_URL}${agent.avatar}`}
                        alt={agent.name}
                        className="h-full w-full object-cover"
                      />
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-display text-xl text-gray-900">{agent.name}</span>
                      {active && (
                        <span
                          className="rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-white"
                          style={{ backgroundColor: agent.color }}
                        >
                          ativo
                        </span>
                      )}
                    </div>
                    <div className="text-sm text-gray-500">{agent.role}</div>
                    <div className="mt-1 text-xs text-gray-400">{agent.tone}</div>
                  </div>
                  <svg
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    className={`h-5 w-5 flex-shrink-0 transition-all ${
                      active ? 'text-gray-900' : 'text-gray-300 group-hover:text-gray-500'
                    }`}
                  >
                    <path
                      fillRule="evenodd"
                      d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
                      clipRule="evenodd"
                    />
                  </svg>
                </button>
              )
            })}
          </div>

          {/* Preview card */}
          <div className="relative">
            <div
              className="absolute -inset-6 -z-10 rounded-3xl blur-3xl"
              style={{
                background: `radial-gradient(circle at 50% 50%, ${selected.color}30, transparent 70%)`,
              }}
            />

            <div className="overflow-hidden rounded-3xl border border-gray-200 bg-white shadow-2xl shadow-gray-900/10">
              {/* Header */}
              <div
                className="relative flex items-center gap-3 px-6 py-5 text-white"
                style={{
                  background: `linear-gradient(135deg, ${selected.color} 0%, ${selected.color}cc 100%)`,
                }}
              >
                <div className="h-12 w-12 overflow-hidden rounded-full ring-2 ring-white/40">
                  {selected.avatar === 'anon' ? (
                    <AnonAvatar color="#ffffff" />
                  ) : (
                    <img
                      src={`${import.meta.env.BASE_URL}${selected.avatar}`}
                      alt={selected.name}
                      className="h-full w-full object-cover"
                    />
                  )}
                </div>
                <div className="flex-1">
                  <div className="font-display text-xl">{selected.name}</div>
                  <div className="flex items-center gap-1.5 text-xs text-white/85">
                    <span className="h-1.5 w-1.5 rounded-full bg-green-300" />
                    {selected.role}
                  </div>
                </div>
                <button
                  type="button"
                  className="rounded-lg bg-white/15 px-3 py-1.5 text-xs font-medium backdrop-blur transition hover:bg-white/25"
                >
                  Editar
                </button>
              </div>

              {/* Settings grid */}
              <div className="grid gap-4 p-6 sm:grid-cols-2">
                <Field label="Nome">
                  <span className="font-medium text-gray-900">{selected.name}</span>
                </Field>
                <Field label="Função">
                  <span className="text-gray-700">{selected.role}</span>
                </Field>
                <Field label="Tom de voz" full>
                  <span className="text-gray-700">{selected.tone}</span>
                </Field>
                <Field label="Cor da marca" full>
                  <div className="flex items-center gap-2">
                    <span
                      className="h-5 w-5 rounded-full border border-gray-200"
                      style={{ backgroundColor: selected.color }}
                    />
                    <code className="font-mono text-sm text-gray-700">{selected.color}</code>
                  </div>
                </Field>
              </div>

              {/* Preview bubble */}
              <div className="border-t border-gray-100 bg-gray-50/70 px-6 py-5">
                <div className="mb-2 text-[11px] font-medium uppercase tracking-wide text-gray-400">
                  Pré-visualização da primeira mensagem
                </div>
                <div className="flex items-start gap-3">
                  <div className="h-8 w-8 flex-shrink-0 overflow-hidden rounded-full">
                    {selected.avatar === 'anon' ? (
                      <AnonAvatar color={selected.color} />
                    ) : (
                      <img
                        src={`${import.meta.env.BASE_URL}${selected.avatar}`}
                        alt=""
                        className="h-full w-full object-cover"
                      />
                    )}
                  </div>
                  <div
                    key={selected.id}
                    className="max-w-[85%] rounded-2xl rounded-tl-md px-4 py-2.5 text-sm text-white shadow-sm"
                    style={{
                      backgroundColor: selected.color,
                      animation: 'tg-pop 320ms cubic-bezier(0.2,0.9,0.3,1.2) both',
                    }}
                  >
                    {selected.greeting}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

const Field = ({ label, full, children }: { label: string; full?: boolean; children: React.ReactNode }) => (
  <div className={full ? 'sm:col-span-2' : ''}>
    <div className="mb-1 text-[11px] font-medium uppercase tracking-wide text-gray-400">{label}</div>
    <div className="rounded-xl border border-gray-200 bg-gray-50/50 px-3 py-2 text-sm">
      {children}
    </div>
  </div>
)

export default AgentCustomization
