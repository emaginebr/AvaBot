interface ActionButtonsProps {
  onSelect: (optionText: string) => void
  color?: string
}

const ActionButtons = ({ onSelect, color = '#3668fc' }: ActionButtonsProps) => {
  return (
    <div className="flex flex-col gap-2 px-4 py-3">
      <button
        onClick={() => onSelect('Desejo retomar nossa última conversa')}
        className="w-full rounded-lg border-2 px-4 py-2.5 text-sm font-medium transition-colors hover:text-white"
        style={{
          borderColor: color,
          color: color,
        }}
        onMouseEnter={e => {
          e.currentTarget.style.backgroundColor = color
          e.currentTarget.style.color = '#fff'
        }}
        onMouseLeave={e => {
          e.currentTarget.style.backgroundColor = 'transparent'
          e.currentTarget.style.color = color
        }}
      >
        Desejo retomar nossa última conversa
      </button>
      <button
        onClick={() => onSelect('Iniciar uma nova conversa')}
        className="w-full rounded-lg border-2 px-4 py-2.5 text-sm font-medium transition-colors hover:text-white"
        style={{
          borderColor: color,
          color: color,
        }}
        onMouseEnter={e => {
          e.currentTarget.style.backgroundColor = color
          e.currentTarget.style.color = '#fff'
        }}
        onMouseLeave={e => {
          e.currentTarget.style.backgroundColor = 'transparent'
          e.currentTarget.style.color = color
        }}
      >
        Iniciar uma nova conversa
      </button>
    </div>
  )
}

export default ActionButtons
