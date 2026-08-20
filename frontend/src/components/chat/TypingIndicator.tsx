interface TypingIndicatorProps {
  name?: string
}

const TypingIndicator = ({ name }: TypingIndicatorProps) => (
  <div className="flex justify-start mb-3">
    <div className="bg-gray-100 px-3 py-2 rounded-lg rounded-bl-none flex items-center gap-2">
      <span className="text-xs text-gray-500 italic">{name ?? 'Assistente'} esta digitando</span>
      <div className="flex space-x-0.5">
        <div className="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
        <div className="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
        <div className="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
      </div>
    </div>
  </div>
)

export default TypingIndicator
