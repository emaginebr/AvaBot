import { Link } from 'react-router-dom'
import ChatWidget from '../components/chat/ChatWidget'
import AvatarBubble from '../components/chat/AvatarBubble'
import TelegramDemo from '../components/landing/TelegramDemo'
import WhatsappDemo from '../components/landing/WhatsappDemo'
import AgentCustomization from '../components/landing/AgentCustomization'
import FeaturesSection from '../components/landing/FeaturesSection'

const LandingPage = () => {
  return (
    <div className="min-h-screen bg-white text-gray-900">
      {/* ─── Navbar ─── */}
      <nav className="sticky top-0 z-40 border-b border-gray-100 bg-white/80 backdrop-blur-lg">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <a href="/" className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-ava-600 text-white shadow-md shadow-ava-600/25">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-5 w-5">
                <path d="M4.913 2.658c2.075-.27 4.19-.408 6.337-.408 2.147 0 4.262.139 6.337.408 1.922.25 3.291 1.861 3.405 3.727a4.403 4.403 0 00-1.032-.211 50.89 50.89 0 00-8.42 0c-2.358.196-4.04 2.19-4.04 4.434v4.286a4.47 4.47 0 002.433 3.984L7.28 21.53A.75.75 0 016 21v-4.03a48.527 48.527 0 01-1.087-.128C2.905 16.58 1.5 14.833 1.5 12.862V6.638c0-1.97 1.405-3.718 3.413-3.979z" />
                <path d="M15.75 7.5c-1.376 0-2.739.057-4.086.169C10.124 7.797 9 9.103 9 10.609v4.285c0 1.507 1.128 2.814 2.67 2.94 1.243.102 2.5.157 3.768.165l2.782 2.781a.75.75 0 001.28-.53v-2.39l.33-.026c1.542-.125 2.67-1.433 2.67-2.94v-4.286c0-1.505-1.125-2.811-2.664-2.94A49.392 49.392 0 0015.75 7.5z" />
              </svg>
            </div>
            <span className="text-xl font-bold tracking-tight text-gray-900">AvaBot</span>
          </a>
          <Link
            to="/admin/agents"
            className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 transition-all hover:border-ava-200 hover:bg-ava-50 hover:text-ava-700"
          >
            Admin
          </Link>
        </div>
      </nav>

      {/* ─── Hero ─── */}
      <section className="relative overflow-hidden bg-white">
        {/* Background decoration */}
        <div className="pointer-events-none absolute inset-0 overflow-hidden">
          <div className="absolute -right-40 -top-40 h-[600px] w-[600px] rounded-full bg-ava-100/70 blur-3xl" />
          <div className="absolute -left-32 top-60 h-[400px] w-[400px] rounded-full bg-ava-200/50 blur-3xl" />
          <div
            className="absolute inset-0 opacity-[0.035]"
            style={{
              backgroundImage:
                "radial-gradient(circle at 1px 1px, #1f45f1 1px, transparent 0)",
              backgroundSize: '32px 32px',
            }}
          />
          <div className="absolute bottom-0 left-1/2 h-px w-[80%] -translate-x-1/2 bg-gradient-to-r from-transparent via-gray-200 to-transparent" />
        </div>

        <div className="relative mx-auto max-w-6xl px-6 pb-20 pt-16 md:pb-28 md:pt-24">
          <div className="grid items-center gap-10 md:grid-cols-[1.1fr_1fr] md:gap-8">
            {/* Copy */}
            <div className="text-center md:text-left">
              <div className="mb-6 inline-flex items-center gap-2 rounded-full border border-ava-200 bg-ava-50 px-4 py-1.5 text-sm font-medium text-ava-700">
                <span className="relative flex h-2 w-2">
                  <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-ava-400 opacity-75" />
                  <span className="relative inline-flex h-2 w-2 rounded-full bg-ava-500" />
                </span>
                Plataforma de agentes inteligentes
              </div>

              <h1 className="font-display text-5xl leading-tight tracking-tight text-gray-900 md:text-6xl md:leading-[1.05] lg:text-7xl">
                Converse com seus
                <span className="relative mx-2 inline-block text-ava-600">
                  clientes
                  <svg className="absolute -bottom-1 left-0 w-full" viewBox="0 0 200 12" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M2 8.5C50 2.5 150 2.5 198 8.5" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
                  </svg>
                </span>
                usando IA
              </h1>

              <p className="mx-auto mt-6 max-w-xl text-lg leading-relaxed text-gray-500 md:mx-0 md:text-xl">
                Crie agentes de IA personalizados que entendem seu negocio,
                respondem em tempo real e coletam dados dos visitantes de
                forma natural e conversacional.
              </p>

              <div className="mt-10 flex flex-col items-center gap-4 sm:flex-row md:justify-start sm:justify-center">
                <a
                  href="#como-funciona"
                  className="inline-flex items-center gap-2 rounded-xl bg-ava-600 px-7 py-3.5 text-sm font-semibold text-white shadow-lg shadow-ava-600/25 transition-all hover:bg-ava-700 hover:shadow-xl hover:shadow-ava-600/30"
                >
                  Comece agora
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="h-4 w-4">
                    <path fillRule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638L10.23 5.29a.75.75 0 111.04-1.08l5.5 5.25a.75.75 0 010 1.08l-5.5 5.25a.75.75 0 11-1.04-1.08l4.158-3.96H3.75A.75.75 0 013 10z" clipRule="evenodd" />
                  </svg>
                </a>
                <Link
                  to="/admin/agents"
                  className="inline-flex items-center gap-2 rounded-xl border border-gray-200 px-7 py-3.5 text-sm font-semibold text-gray-700 transition-all hover:border-gray-300 hover:bg-gray-50"
                >
                  Acessar painel
                </Link>
              </div>
            </div>

            {/* Ava portrait */}
            <div className="relative mx-auto w-full max-w-md md:max-w-none">
              {/* Background blob */}
              <div className="absolute inset-0 -z-10 flex items-center justify-center">
                <div className="h-[420px] w-[420px] rounded-full bg-gradient-to-br from-ava-300/50 via-ava-500/30 to-ava-700/20 blur-3xl md:h-[520px] md:w-[520px]" />
              </div>

              <div className="relative mx-auto w-full max-w-[460px]">
                <img
                  src={`${import.meta.env.BASE_URL}ava-inicial.png`}
                  alt="Ava — atendente de IA"
                  className="relative z-0 w-full"
                  style={{
                    WebkitMaskImage:
                      'linear-gradient(to top, transparent 0%, rgba(0,0,0,0.2) 12%, #000 40%)',
                    maskImage:
                      'linear-gradient(to top, transparent 0%, rgba(0,0,0,0.2) 12%, #000 40%)',
                  }}
                />

                {/* Floating status badge */}
                <div className="absolute left-2 top-8 z-10 flex items-center gap-2 rounded-full border border-gray-100 bg-white px-3 py-2 shadow-lg">
                  <span className="relative flex h-2.5 w-2.5">
                    <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-green-400 opacity-75" />
                    <span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-green-500" />
                  </span>
                  <span className="text-xs font-medium text-gray-700">Online agora</span>
                </div>

                {/* Floating message card */}
                <div
                  className="absolute -left-2 top-1/2 z-10 max-w-[220px] -translate-y-1/2 rounded-2xl rounded-bl-md border border-gray-100 bg-white p-3 shadow-xl md:-left-6"
                  style={{ animation: 'tg-pop 500ms ease-out 400ms both' }}
                >
                  <div className="mb-1 flex items-center gap-1.5 text-[11px] font-medium text-ava-600">
                    <svg viewBox="0 0 20 20" fill="currentColor" className="h-3 w-3">
                      <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                    </svg>
                    Ava respondeu
                  </div>
                  <p className="text-xs leading-snug text-gray-600">
                    Oi! Posso te ajudar a escolher o plano ideal ✨
                  </p>
                </div>

                {/* Floating stat card */}
                <div className="absolute -right-2 bottom-16 z-10 rounded-2xl border border-gray-100 bg-white px-3.5 py-2.5 shadow-xl md:-right-6">
                  <div className="text-[10px] font-medium uppercase tracking-wide text-gray-400">
                    Respostas/dia
                  </div>
                  <div className="font-display text-xl text-ava-700">+2.4k</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <WhatsappDemo />

      <TelegramDemo />

      {/* ─── Features ─── */}
      <FeaturesSection />

      <AgentCustomization />

      {/* ─── Footer ─── */}
      <footer className="border-t border-gray-100">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-8">
          <div className="flex items-center gap-2">
            <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-ava-600 text-white">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-3.5 w-3.5">
                <path d="M4.913 2.658c2.075-.27 4.19-.408 6.337-.408 2.147 0 4.262.139 6.337.408 1.922.25 3.291 1.861 3.405 3.727a4.403 4.403 0 00-1.032-.211 50.89 50.89 0 00-8.42 0c-2.358.196-4.04 2.19-4.04 4.434v4.286a4.47 4.47 0 002.433 3.984L7.28 21.53A.75.75 0 016 21v-4.03a48.527 48.527 0 01-1.087-.128C2.905 16.58 1.5 14.833 1.5 12.862V6.638c0-1.97 1.405-3.718 3.413-3.979z" />
                <path d="M15.75 7.5c-1.376 0-2.739.057-4.086.169C10.124 7.797 9 9.103 9 10.609v4.285c0 1.507 1.128 2.814 2.67 2.94 1.243.102 2.5.157 3.768.165l2.782 2.781a.75.75 0 001.28-.53v-2.39l.33-.026c1.542-.125 2.67-1.433 2.67-2.94v-4.286c0-1.505-1.125-2.811-2.664-2.94A49.392 49.392 0 0015.75 7.5z" />
              </svg>
            </div>
            <span className="text-sm font-medium text-gray-900">AvaBot</span>
          </div>
          <div className="flex flex-col items-end gap-1 text-sm text-gray-400">
            <p>&copy; {new Date().getFullYear()} AvaBot. Todos os direitos reservados.</p>
            <p>
              Desenvolvido por{' '}
              <a
                href="https://emagine.com.br"
                target="_blank"
                rel="noopener noreferrer"
                className="font-semibold text-ava-600 transition-colors hover:text-ava-700"
              >
                Emagine
              </a>
            </p>
          </div>
        </div>
      </footer>

      <ChatWidget
        slug="ava"
        greeting="Oi, eu sou a Ava. Em que posso ajudar?"
        agentAvatar={`${import.meta.env.BASE_URL}ava-foto.png`}
        renderBubble={(props) => (
          <AvatarBubble
            message={props.message}
            onClick={props.onClick}
            isOpen={props.isOpen}
            color={props.color}
            avatarSrc={`${import.meta.env.BASE_URL}ava-image.png`}
          />
        )}
      />
    </div>
  )
}

export default LandingPage
