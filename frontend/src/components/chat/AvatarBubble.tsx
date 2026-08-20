import { useEffect, useState } from 'react'

interface AvatarBubbleProps {
  message: string
  onClick: () => void
  isOpen: boolean
  avatarSrc: string
  color?: string
}

const AvatarBubble = ({ message, onClick, isOpen, avatarSrc, color = '#3668fc' }: AvatarBubbleProps) => {
  const [showAvatar, setShowAvatar] = useState(false)
  const [showBubble, setShowBubble] = useState(false)
  const [leaving, setLeaving] = useState(false)
  const [hidden, setHidden] = useState(false)

  // Entrance animation
  useEffect(() => {
    if (isOpen) return
    const t1 = setTimeout(() => setShowAvatar(true), 300)
    const t2 = setTimeout(() => setShowBubble(true), 1100)
    return () => { clearTimeout(t1); clearTimeout(t2) }
  }, [])

  // Exit animation when isOpen becomes true
  useEffect(() => {
    if (!isOpen) {
      // Re-entering: reset hidden
      if (hidden) {
        setHidden(false)
        setLeaving(false)
        setShowAvatar(false)
        setShowBubble(false)
        // Re-trigger entrance
        const t1 = setTimeout(() => setShowAvatar(true), 300)
        const t2 = setTimeout(() => setShowBubble(true), 1100)
        return () => { clearTimeout(t1); clearTimeout(t2) }
      }
      return
    }

    // Start leaving: first fade bubble, then slide avatar down
    setShowBubble(false)
    setLeaving(true)
    const t = setTimeout(() => {
      setShowAvatar(false)
      const t2 = setTimeout(() => setHidden(true), 700)
      return () => clearTimeout(t2)
    }, 300)
    return () => clearTimeout(t)
  }, [isOpen])

  if (hidden) return null

  return (
    <div className="fixed bottom-0 right-6 z-50 pointer-events-none">
      <button
        onClick={onClick}
        className="group flex flex-col items-end pointer-events-auto"
        aria-label="Abrir chat"
        disabled={leaving}
      >
        {/* Balao de fala */}
        <div
          className="relative mb-2 max-w-56 rounded-2xl px-4 py-3 text-sm font-medium text-white shadow-lg"
          style={{
            backgroundColor: color,
            opacity: showBubble ? 1 : 0,
            transform: showBubble ? 'translateY(0) scale(1)' : 'translateY(10px) scale(0.9)',
            transition: 'opacity 400ms ease, transform 400ms ease',
          }}
        >
          {message}
          <div
            className="absolute -bottom-2 right-8 h-0 w-0 border-l-[8px] border-r-[8px] border-t-[8px] border-l-transparent border-r-transparent"
            style={{ borderTopColor: color }}
          />
        </div>
        {/* Avatar */}
        <div
          style={{
            height: 220,
            opacity: showAvatar ? 1 : 0,
            transform: showAvatar ? 'translateY(0)' : 'translateY(80px)',
            transition: leaving
              ? 'opacity 600ms ease, transform 600ms ease'
              : 'opacity 700ms ease-out, transform 700ms ease-out',
            filter: 'drop-shadow(0 8px 16px rgba(0,0,0,0.3))',
          }}
        >
          <img
            src={avatarSrc}
            alt="Assistente"
            className="h-full w-auto object-contain transition-transform duration-300 group-hover:scale-105"
          />
        </div>
      </button>
    </div>
  )
}

export default AvatarBubble
