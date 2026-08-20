import { useEffect, useRef, useState } from 'react'

type Msg = {
  from: 'user' | 'ava'
  text: string
  delay: number
  typing?: number
}

const SCRIPT: Msg[] = [
  { from: 'user', text: 'Oi! Vocês ainda têm vaga pro curso de inglês?', delay: 800 },
  { from: 'ava', text: 'Oi! 👋 Temos sim, turmas novas começam na próxima semana.', delay: 1400, typing: 1200 },
  { from: 'ava', text: 'Posso te passar os horários disponíveis. Qual seu nome?', delay: 900, typing: 900 },
  { from: 'user', text: 'Sou o Rodrigo', delay: 1200 },
  { from: 'ava', text: 'Prazer, Rodrigo! Temos turmas de manhã (8h), tarde (14h) e noite (19h30). Qual funciona melhor pra você?', delay: 1500, typing: 1500 },
  { from: 'user', text: 'À noite seria perfeito', delay: 1300 },
  { from: 'ava', text: 'Fechado! Me passa seu e-mail que já te envio a grade completa e o link pra matrícula. ✨', delay: 1200, typing: 1400 },
]

const formatTime = (offset: number) => {
  const d = new Date(Date.now() - offset)
  return d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

const TelegramDemo = () => {
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
    let timers: ReturnType<typeof setTimeout>[] = []

    const run = async (loop: number) => {
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

      // Pause before looping
      await new Promise<void>((res) => {
        const t = setTimeout(res, 4500)
        timers.push(t)
      })
      if (!cancelled) run(loop + 1)
    }

    run(0)
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
    <section ref={sectionRef} className="border-t border-gray-100 bg-gradient-to-b from-white to-gray-50/50">
      <div className="mx-auto max-w-6xl px-6 py-24 md:py-32">
        <div className="grid items-center gap-12 md:grid-cols-2 md:gap-16">
          {/* Copy */}
          <div>
            <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-[#2aabee]/30 bg-[#2aabee]/10 px-3 py-1 text-xs font-medium text-[#1b92d1]">
              <svg viewBox="0 0 24 24" fill="currentColor" className="h-3.5 w-3.5">
                <path d="M9.78 18.65l.28-4.23 7.68-6.92c.34-.31-.07-.46-.52-.19L7.74 13.3 3.64 12c-.88-.25-.89-.86.2-1.3l15.97-6.16c.73-.33 1.43.18 1.15 1.3l-2.72 12.81c-.19.91-.74 1.13-1.5.71L12.6 16.3l-1.99 1.93c-.23.23-.42.42-.83.42z" />
              </svg>
              Integração com Telegram
            </div>
            <h2 className="font-display text-3xl leading-tight tracking-tight text-gray-900 md:text-5xl md:leading-[1.1]">
              Sua Ava também
              <em className="not-italic text-ava-600"> atende no Telegram</em>
            </h2>
            <p className="mt-6 text-lg leading-relaxed text-gray-500">
              Conecte um bot do Telegram em segundos. A mesma base de conhecimento,
              a mesma personalidade — agora no app que seus clientes já usam todo dia.
            </p>

            <ul className="mt-8 space-y-4">
              {[
                'Configuração em 2 minutos, só cole o token do BotFather',
                'Histórico unificado com o chat do site',
                'Coleta leads direto na conversa, sem formulários',
              ].map((item) => (
                <li key={item} className="flex items-start gap-3">
                  <span className="mt-0.5 flex h-5 w-5 flex-shrink-0 items-center justify-center rounded-full bg-ava-100 text-ava-600">
                    <svg viewBox="0 0 20 20" fill="currentColor" className="h-3 w-3">
                      <path fillRule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clipRule="evenodd" />
                    </svg>
                  </span>
                  <span className="text-gray-600">{item}</span>
                </li>
              ))}
            </ul>
          </div>

          {/* Telegram mock */}
          <div className="relative mx-auto w-full max-w-sm">
            {/* Glow */}
            <div className="absolute -inset-8 -z-10 rounded-[3rem] bg-gradient-to-br from-[#2aabee]/20 via-ava-200/30 to-transparent blur-3xl" />

            <div className="overflow-hidden rounded-[2.5rem] border-[10px] border-gray-900 bg-gray-900 shadow-2xl shadow-gray-900/20">
              {/* Notch */}
              <div className="relative flex h-6 items-center justify-center bg-gray-900">
                <div className="h-1.5 w-20 rounded-full bg-gray-700" />
              </div>

              {/* Telegram header */}
              <div className="flex items-center gap-3 bg-gradient-to-b from-[#5aaee0] to-[#4d9dd1] px-4 py-3 text-white">
                <button className="rounded-full p-0.5 hover:bg-white/10" aria-label="voltar">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" className="h-5 w-5">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
                  </svg>
                </button>
                <div className="h-9 w-9 overflow-hidden rounded-full bg-white/20 ring-2 ring-white/30">
                  <img src={`${import.meta.env.BASE_URL}ava-foto.png`} alt="Ava" className="h-full w-full object-cover" />
                </div>
                <div className="flex-1 leading-tight">
                  <div className="text-sm font-semibold">Ava</div>
                  <div className="flex items-center gap-1 text-[11px] text-white/80">
                    <span className="h-1.5 w-1.5 rounded-full bg-green-400" />
                    online
                  </div>
                </div>
                <svg viewBox="0 0 24 24" fill="currentColor" className="h-5 w-5 opacity-90">
                  <path d="M12 6.75a5.25 5.25 0 00-5.25 5.25v4.5h10.5V12c0-2.9-2.35-5.25-5.25-5.25zM8.25 16.5V12a3.75 3.75 0 117.5 0v4.5" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </div>

              {/* Chat area with telegram pattern */}
              <div
                ref={scrollRef}
                className="relative h-[420px] overflow-y-auto scroll-smooth px-3 py-4"
                style={{
                  background:
                    'linear-gradient(135deg, #e5ecf3 0%, #d8e3ed 100%)',
                }}
              >
                {/* decorative pattern */}
                <div
                  className="pointer-events-none absolute inset-0 opacity-[0.08]"
                  style={{
                    backgroundImage:
                      "url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='40' height='40' viewBox='0 0 40 40'><circle cx='20' cy='20' r='1.2' fill='%23000'/></svg>\")",
                  }}
                />

                <div className="relative flex flex-col gap-1.5">
                  {/* date separator */}
                  <div className="mx-auto mb-2 rounded-full bg-black/20 px-3 py-0.5 text-[11px] font-medium text-white backdrop-blur-sm">
                    hoje
                  </div>

                  {visible.map((m, i) => (
                    <Bubble key={i} msg={m} />
                  ))}

                  {typing && <TypingBubble side={typing} />}
                </div>
              </div>

              {/* Input bar */}
              <div className="flex items-center gap-2 border-t border-gray-200 bg-white px-3 py-2.5">
                <svg viewBox="0 0 24 24" fill="none" stroke="#9ca3af" strokeWidth="1.8" className="h-5 w-5">
                  <circle cx="12" cy="12" r="10" />
                  <path strokeLinecap="round" d="M8 14s1.5 2 4 2 4-2 4-2M9 9h.01M15 9h.01" />
                </svg>
                <div className="flex-1 text-sm text-gray-400">Mensagem</div>
                <svg viewBox="0 0 24 24" fill="none" stroke="#9ca3af" strokeWidth="1.8" className="h-5 w-5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656L5.757 10.586a6 6 0 108.486 8.486L20.5 12.828" />
                </svg>
                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-[#2aabee] text-white">
                  <svg viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4">
                    <path d="M2 21l21-9L2 3v7l15 2-15 2v7z" />
                  </svg>
                </div>
              </div>
            </div>
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
        className={`relative max-w-[78%] rounded-2xl px-3 py-1.5 text-sm leading-snug shadow-sm ${
          isUser
            ? 'rounded-br-md bg-[#effdde] text-gray-900'
            : 'rounded-bl-md bg-white text-gray-900'
        }`}
      >
        <div className="whitespace-pre-wrap pr-12">{msg.text}</div>
        <div
          className={`absolute bottom-1 right-2 flex items-center gap-0.5 text-[10px] ${
            isUser ? 'text-[#4fae4e]' : 'text-gray-400'
          }`}
        >
          <span>{msg.time}</span>
          {isUser && (
            <svg viewBox="0 0 16 11" fill="currentColor" className="h-2.5 w-3">
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
        className={`rounded-2xl px-4 py-2.5 shadow-sm ${
          isUser ? 'rounded-br-md bg-[#effdde]' : 'rounded-bl-md bg-white'
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

export default TelegramDemo
