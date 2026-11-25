import { useState, useRef, useEffect, useId } from 'react'
import { useAgentStore } from '../../stores/useAgentStore'
import AdminIcon from './AdminIcon'
import { initialsOf } from './layout/initials'

interface AgentSelectorProps {
  /** `sidebar`: botão escuro do topo da sidebar do painel; `default`: botão claro. */
  variant?: 'default' | 'sidebar'
}

const AgentSelector = ({ variant = 'default' }: AgentSelectorProps) => {
  const { agents, selectedAgent, selectAgent, loadAgents } = useAgentStore()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)
  const listId = useId()

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

  // Esc fecha só a lista (sem fechar o drawer da sidebar junto) e devolve o foco ao botão.
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape' && open) {
      e.preventDefault()
      e.stopPropagation()
      setOpen(false)
      buttonRef.current?.focus()
    }
  }

  const isSidebar = variant === 'sidebar'

  return (
    <div className="relative" ref={ref} onKeyDown={handleKeyDown}>
      {isSidebar ? (
        <button
          ref={buttonRef}
          type="button"
          onClick={() => setOpen(!open)}
          aria-haspopup="listbox"
          aria-expanded={open}
          aria-controls={open ? listId : undefined}
          aria-label={`Trocar agente ativo: ${selectedAgent ? selectedAgent.name : 'nenhum selecionado'}`}
          className="flex w-full items-center gap-2.5 rounded-[10px] border border-white/12 bg-white/5 px-2 py-[7px] text-left text-white/75 transition-colors hover:border-white/20 hover:bg-white/10"
        >
          <span
            aria-hidden="true"
            className={`grid h-[26px] w-[26px] flex-none place-items-center rounded-full text-[10.5px] font-semibold ${
              selectedAgent ? 'bg-ava-100 text-ava-800' : 'bg-white/10 text-ava-200'
            }`}
          >
            {selectedAgent ? initialsOf(selectedAgent.name, 'AG') : '?'}
          </span>
          <span className="min-w-0 flex-1">
            <small className="flex items-center gap-1.5 text-[10.5px] leading-tight text-ava-200">
              Agente ativo
              {selectedAgent && (
                <span
                  aria-hidden="true"
                  className={`h-1.5 w-1.5 rounded-full ${selectedAgent.status === 1 ? 'bg-emerald-400' : 'bg-white/30'}`}
                />
              )}
            </small>
            <strong className="block truncate text-[13px] leading-snug font-medium text-white">
              {selectedAgent ? selectedAgent.name : 'Selecionar agente'}
            </strong>
          </span>
          <AdminIcon name="chevron" className={`h-4 w-4 transition-transform motion-reduce:transition-none ${open ? 'rotate-180' : ''}`} />
        </button>
      ) : (
        <button
          ref={buttonRef}
          type="button"
          onClick={() => setOpen(!open)}
          aria-haspopup="listbox"
          aria-expanded={open}
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
      )}

      {open && (
        <div
          id={listId}
          role="listbox"
          aria-label="Agentes"
          className={`absolute top-full left-0 z-50 mt-1 max-h-[min(18rem,60dvh)] overflow-y-auto rounded-xl border border-gray-200 bg-white py-1 shadow-lg ${
            isSidebar ? 'w-full text-ink' : 'w-64'
          }`}
        >
          {selectedAgent && (
            <button
              type="button"
              onClick={() => { selectAgent(null); setOpen(false) }}
              className="w-full text-left px-4 py-2 text-sm text-gray-500 hover:bg-gray-50"
            >
              Limpar seleção
            </button>
          )}
          {agents.length === 0 ? (
            <p className="px-4 py-2 text-sm text-gray-500">Nenhum agente cadastrado</p>
          ) : (
            agents.map((agent) => {
              const selected = selectedAgent?.agentId === agent.agentId
              return (
                <button
                  type="button"
                  role="option"
                  aria-selected={selected}
                  key={agent.agentId}
                  onClick={() => { selectAgent(agent); setOpen(false) }}
                  className={`w-full text-left px-4 py-2 text-sm flex items-center gap-2 transition-colors ${
                    selected ? 'bg-ava-50 text-ava-700 font-medium' : 'text-gray-700 hover:bg-gray-50'
                  }`}
                >
                  <span className={`w-2 h-2 rounded-full shrink-0 ${agent.status === 1 ? 'bg-green-500' : 'bg-gray-300'}`} />
                  <span className="truncate">{agent.name}</span>
                  {agent.status === 0 && <span className="text-xs text-gray-500 ml-auto">Inativo</span>}
                </button>
              )
            })
          )}
        </div>
      )}
    </div>
  )
}

export default AgentSelector
