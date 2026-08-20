import { useEffect, useRef, useState } from 'react'

type Msg = {
  from: 'user' | 'ava'
  text: string
  delay: number
  typing?: number
}

const SCRIPT: Msg[] = [
  { from: 'user', text: 'Boa tarde! Queria saber o preço do plano premium', delay: 800 },
  { from: 'ava', text: 'Olá! Tudo bem? 😊 O plano Premium custa R$ 79/mês e inclui atendimento ilimitado.', delay: 1300, typing: 1400 },
  { from: 'ava', text: 'Quer que eu te mande um comparativo rápido com o plano Básico?', delay: 900, typing: 900 },
  { from: 'user', text: 'Sim, por favor!', delay: 1100 },
  { from: 'ava', text: 'Perfeito! 📋\n\n*Básico* — R$ 29/mês, até 500 conversas\n*Premium* — R$ 79/mês, ilimitado + relatórios', delay: 1200, typing: 1600 },
  { from: 'user', text: 'Fechei no Premium', delay: 1400 },
  { from: 'ava', text: 'Ótima escolha! ✅ Me passa seu e-mail que já envio o link de pagamento.', delay: 1000, typing: 1200 },
]

const formatTime = (offset: number) => {
  const d = new Date(Date.now() - offset)
  return d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

const WhatsappDemo = () => {
  const [visible, setVisible] = useState<Array<Msg & { time: string }>>([])
  const [typing, setTyping] = useState<'user' | 'ava' | null>(null)
  const [started, setStarted] = useState(false)
  const sectionRef = useRef<HTMLDivElement | null>(null)
  const scrollRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (!sectionRef.current) return
    const obs = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) {
          setStarted(true)
          obs.disconnect()
        }
      },
      { threshold: 0.3 }
    )
    obs.observe(sectionRef.current)
    return () => obs.disconnect()
  }, [])

  useEffect(() => {
    if (!started) return
    let cancelled = false
    const timers: ReturnType<typeof setTimeout>[] = []

    const run = async () => {
      if (cancelled) return
      setVisible([])
      setTyping(null)

      for (let i = 0; i < SCRIPT.length; i++) {
        const msg = SCRIPT[i]
        await new Promise<void>((res) => {
          const t = setTimeout(res, msg.delay)
          timers.push(t)
        })
        if (cancelled) return

        if (msg.typing) {
          setTyping(msg.from)
          await new Promise<void>((res) => {
            const t = setTimeout(res, msg.typing)
            timers.push(t)
          })
          if (cancelled) return
        }
        setTyping(null)
        setVisible((v) => [...v, { ...msg, time: formatTime((SCRIPT.length - i) * 1000) }])
      }

      await new Promise<void>((res) => {
        const t = setTimeout(res, 4500)
        timers.push(t)
      })
      if (!cancelled) run()
    }

    run()
    return () => {
      cancelled = true
      timers.forEach(clearTimeout)
    }
  }, [started])

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight
    }
  }, [visible, typing])

  return (
    <section ref={sectionRef} className="border-t border-gray-100 bg-gradient-to-b from-gray-50/50 to-white">
      <div className="mx-auto max-w-6xl px-6 py-24 md:py-32">
        <div className="grid items-center gap-12 md:grid-cols-2 md:gap-16">
          {/* WhatsApp mock (LEFT) */}
          <div className="relative order-2 mx-auto w-full max-w-sm md:order-1">
            {/* Glow */}
            <div className="absolute -inset-8 -z-10 rounded-[3rem] bg-gradient-to-br from-[#25d366]/25 via-[#128c7e]/20 to-transparent blur-3xl" />

            <div className="overflow-hidden rounded-[2.5rem] border-[10px] border-gray-900 bg-gray-900 shadow-2xl shadow-gray-900/20">
              {/* Notch */}
              <div className="relative flex h-6 items-center justify-center bg-gray-900">
                <div className="h-1.5 w-20 rounded-full bg-gray-700" />
              </div>

              {/* WhatsApp header */}
              <div className="flex items-center gap-3 bg-[#075e54] px-4 py-3 text-white">
                <button className="rounded-full p-0.5 hover:bg-white/10" aria-label="voltar">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" className="h-5 w-5">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
                  </svg>
                </button>
                <div className="h-9 w-9 overflow-hidden rounded-full bg-white/20 ring-2 ring-white/20">
                  <img src={`${import.meta.env.BASE_URL}ava-foto.png`} alt="Ava" className="h-full w-full object-cover" />
                </div>
                <div className="flex-1 leading-tight">
                  <div className="text-sm font-semibold">Ava</div>
                  <div className="text-[11px] text-white/80">online</div>
                </div>
                <svg viewBox="0 0 24 24" fill="currentColor" className="h-5 w-5 opacity-90">
                  <path d="M15.5 10.5a4.5 4.5 0 11-9 0 4.5 4.5 0 019 0zM19.5 7a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <svg viewBox="0 0 24 24" fill="currentColor" className="h-5 w-5 opacity-90">
                  <path d="M6 12a2 2 0 11-4 0 2 2 0 014 0zm8 0a2 2 0 11-4 0 2 2 0 014 0zm8 0a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>

              {/* Chat area with WhatsApp background */}
              <div
                ref={scrollRef}
                className="relative h-[420px] overflow-y-auto scroll-smooth px-3 py-4"
                style={{
                  background: '#ece5dd',
                }}
              >
                <div
                  className="pointer-events-none absolute inset-0 opacity-[0.12]"
                  style={{
                    backgroundImage:
                      "url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='60' height='60' viewBox='0 0 60 60'><path d='M30 10l4 8 9 1-6 6 1 9-8-4-8 4 1-9-6-6 9-1z' fill='%23000'/></svg>\")",
                  }}
                />

                <div className="relative flex flex-col gap-1.5">
                  <div className="mx-auto mb-2 rounded-md bg-[#e1f2fa] px-3 py-1 text-[11px] font-medium text-[#54656f] shadow-sm">
                    hoje
                  </div>

                  {visible.map((m, i) => (
                    <Bubble key={i} msg={m} />
                  ))}

                  {typing && <TypingBubble side={typing} />}
                </div>
              </div>

              {/* Input bar */}
              <div className="flex items-center gap-2 bg-[#f0f0f0] px-3 py-2.5">
                <svg viewBox="0 0 24 24" fill="none" stroke="#54656f" strokeWidth="1.8" className="h-5 w-5">
                  <circle cx="12" cy="12" r="10" />
                  <path strokeLinecap="round" d="M8 14s1.5 2 4 2 4-2 4-2M9 9h.01M15 9h.01" />
                </svg>
                <div className="flex flex-1 items-center gap-2 rounded-full bg-white px-3 py-1.5 text-sm text-gray-400">
                  Mensagem
                </div>
                <svg viewBox="0 0 24 24" fill="none" stroke="#54656f" strokeWidth="1.8" className="h-5 w-5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656L5.757 10.586a6 6 0 108.486 8.486L20.5 12.828" />
                </svg>
                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-[#128c7e] text-white">
                  <svg viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4">
                    <path d="M12 14a3 3 0 003-3V5a3 3 0 10-6 0v6a3 3 0 003 3zm5-3a5 5 0 01-10 0H5a7 7 0 006 6.92V21h2v-3.08A7 7 0 0019 11h-2z" />
                  </svg>
                </div>
              </div>
            </div>
          </div>

          {/* Copy (RIGHT) */}
          <div className="order-1 md:order-2">
            <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-[#25d366]/30 bg-[#25d366]/10 px-3 py-1 text-xs font-medium text-[#128c7e]">
              <svg viewBox="0 0 24 24" fill="currentColor" className="h-3.5 w-3.5">
                <path d="M17.5 14.4c-.3-.1-1.7-.8-2-.9-.3-.1-.5-.1-.7.2-.2.3-.8.9-.9 1.1-.2.2-.3.2-.6.1-1.8-.9-3-1.6-4.2-3.6-.3-.6.3-.5.9-1.7.1-.2 0-.4 0-.5 0-.2-.7-1.6-.9-2.2-.2-.5-.5-.5-.7-.5h-.6c-.2 0-.6.1-.9.4-.3.3-1.2 1.2-1.2 2.9 0 1.7 1.2 3.3 1.4 3.6.2.2 2.4 3.7 5.9 5.2 2.2.9 3 1 4.1.8.7-.1 1.7-.7 2-1.4.2-.6.2-1.2.2-1.4-.1-.1-.3-.2-.6-.3z" />
                <path fillRule="evenodd" d="M20.5 3.5A10 10 0 003.4 16L2 22l6.2-1.6a10 10 0 0012.3-17zM12 20.1a8 8 0 01-4.1-1.1l-.3-.2-3.7.9 1-3.6-.2-.3A8 8 0 1119.7 12a8 8 0 01-7.7 8.1z" clipRule="evenodd" />
              </svg>
              Integração com WhatsApp
            </div>
            <h2 className="font-display text-3xl leading-tight tracking-tight text-gray-900 md:text-5xl md:leading-[1.1]">
              A Ava atende
              <em className="not-italic text-[#128c7e]"> no WhatsApp </em>
              24/7
            </h2>
            <p className="mt-6 text-lg leading-relaxed text-gray-500">
              Conecte o WhatsApp Business e deixe a Ava responder seus clientes
              a qualquer hora. Sem fila, sem espera, com a mesma inteligência do chat do site.
            </p>

            <ul className="mt-8 space-y-4">
              {[
                'Integração via WhatsApp API',
                'Integração com grupos',
                'Respostas instantâneas com tom humano e natural',
                'Transfere pra um humano quando faz sentido',
              ].map((item) => (
                <li key={item} className="flex items-start gap-3">
                  <span className="mt-0.5 flex h-5 w-5 flex-shrink-0 items-center justify-center rounded-full bg-[#25d366]/15 text-[#128c7e]">
                    <svg viewBox="0 0 20 20" fill="currentColor" className="h-3 w-3">
                      <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
                    </svg>
                  </span>
                  <span className="text-gray-600">{item}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </section>
  )
}

const Bubble = ({ msg }: { msg: Msg & { time: string } }) => {
  const isUser = msg.from === 'user'
  return (
    <div
      className={`flex w-full ${isUser ? 'justify-end' : 'justify-start'}`}
      style={{ animation: 'tg-pop 260ms cubic-bezier(0.2, 0.9, 0.3, 1.2) both' }}
    >
      <div
        className={`relative max-w-[78%] rounded-lg px-2.5 py-1.5 text-sm leading-snug shadow-sm ${
          isUser
            ? 'rounded-tr-none bg-[#dcf8c6] text-gray-900'
            : 'rounded-tl-none bg-white text-gray-900'
        }`}
      >
        <div className="whitespace-pre-wrap pr-12">
          {msg.text.split(/(\*[^*]+\*)/g).map((part, i) =>
            part.startsWith('*') && part.endsWith('*') ? (
              <strong key={i}>{part.slice(1, -1)}</strong>
            ) : (
              <span key={i}>{part}</span>
            )
          )}
        </div>
        <div
          className={`absolute bottom-0.5 right-2 flex items-center gap-0.5 text-[10px] ${
            isUser ? 'text-gray-500' : 'text-gray-400'
          }`}
        >
          <span>{msg.time}</span>
          {isUser && (
            <svg viewBox="0 0 16 11" fill="#53bdeb" className="h-2.5 w-3">
              <path d="M11.071.653a.457.457 0 0 0-.304.135L6.121 5.8 3.808 3.393a.457.457 0 0 0-.652.643l2.643 2.75a.457.457 0 0 0 .652 0L11.375 1.43a.457.457 0 0 0-.304-.777z" />
              <path d="M15.071.653a.457.457 0 0 0-.304.135L9.121 6.8l-1.01-1.05a.457.457 0 0 0-.652.643l1.34 1.393a.457.457 0 0 0 .652 0L15.375 1.43a.457.457 0 0 0-.304-.777z" />
            </svg>
          )}
        </div>
      </div>
    </div>
  )
}

const TypingBubble = ({ side }: { side: 'user' | 'ava' }) => {
  const isUser = side === 'user'
  return (
    <div className={`flex w-full ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`rounded-lg px-4 py-2.5 shadow-sm ${
          isUser ? 'rounded-tr-none bg-[#dcf8c6]' : 'rounded-tl-none bg-white'
        }`}
      >
        <div className="flex items-center gap-1">
          <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-gray-400 [animation-delay:-0.3s]" />
          <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-gray-400 [animation-delay:-0.15s]" />
          <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-gray-400" />
        </div>
      </div>
    </div>
  )
}

export default WhatsappDemo
