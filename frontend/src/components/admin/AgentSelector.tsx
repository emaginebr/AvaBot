import { useState, useRef, useEffect } from 'react'
import { useAgentStore } from '../../stores/useAgentStore'

const AgentSelector = () => {
  const { agents, selectedAgent, selectAgent, loadAgents } = useAgentStore()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    loadAgents()
  }, [loadAgents])

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-2 px-3 py-1.5 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors min-w-[180px]"
      >
        <span className="w-2 h-2 rounded-full shrink-0" style={{
          backgroundColor: selectedAgent ? (selectedAgent.status === 1 ? '#16a34a' : '#9ca3af') : '#d1d5db'
        }} />
        <span className="truncate text-gray-700">
          {selectedAgent ? selectedAgent.name : 'Selecionar agente'}
        </span>
        <svg className="w-4 h-4 text-gray-400 ml-auto shrink-0" viewBox="0 0 20 20" fill="currentColor">
          <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clipRule="evenodd" />
        </svg>
      </button>

      {open && (
        <div className="absolute top-full left-0 mt-1 w-64 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50">
          {selectedAgent && (
            <button
              onClick={() => { selectAgent(null); setOpen(false) }}
              className="w-full text-left px-4 py-2 text-sm text-gray-400 hover:bg-gray-50"
            >
              Limpar seleção
            </button>
          )}
          {agents.length === 0 ? (
            <p className="px-4 py-2 text-sm text-gray-400">Nenhum agente cadastrado</p>
          ) : (
            agents.map((agent) => (
              <button
                key={agent.agentId}
                onClick={() => { selectAgent(agent); setOpen(false) }}
                className={`w-full text-left px-4 py-2 text-sm flex items-center gap-2 transition-colors ${
                  selectedAgent?.agentId === agent.agentId
                    ? 'bg-ava-50 text-ava-700 font-medium'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                <span className={`w-2 h-2 rounded-full shrink-0 ${
                  agent.status === 1 ? 'bg-green-500' : 'bg-gray-300'
                }`} />
                <span className="truncate">{agent.name}</span>
                {agent.status === 0 && (
                  <span className="text-xs text-gray-400 ml-auto">Inativo</span>
                )}
              </button>
            ))
          )}
        </div>
      )}
    </div>
  )
}

export default AgentSelector
