import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import ReactMarkdown from 'react-markdown'
import { ChatHistoryService } from '../../Services/ChatHistoryService'
import { type ChatMessageInfo, SenderType } from '../../types/chatMessage'

const SessionDetailPage = () => {
  const { sessionId } = useParams<{ sessionId: string }>()
  const navigate = useNavigate()
  const [messages, setMessages] = useState<ChatMessageInfo[]>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const pageSize = 50
  const id = Number(sessionId)

  useEffect(() => {
    if (!id) return
    setLoading(true)
    ChatHistoryService.getMessages(id, page, pageSize)
      .then((result) => {
        if (result.sucesso) {
          setMessages(result.dados.items)
          setTotal(result.dados.total)
        } else {
          toast.error(result.mensagem || 'Erro ao carregar mensagens')
          console.error('[SessionDetailPage] getMessages — erro:', result.mensagem)
        }
      })
      .catch((err) => {
        console.error('[SessionDetailPage] getMessages — exceção:', err)
        toast.error('Erro de rede ao carregar mensagens')
      })
      .finally(() => setLoading(false))
  }, [id, page])

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div className="max-w-4xl">
      <div className="flex items-center gap-4 mb-6">
        <button
          onClick={() => navigate('/admin/sessions')}
          className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 transition-colors"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M9.707 16.707a1 1 0 01-1.414 0l-6-6a1 1 0 010-1.414l6-6a1 1 0 011.414 1.414L5.414 9H17a1 1 0 110 2H5.414l4.293 4.293a1 1 0 010 1.414z" clipRule="evenodd" />
          </svg>
          Voltar para sessões
        </button>
      </div>

      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Histórico da Conversa</h1>
        <p className="text-sm text-gray-500 mt-1">Sessão #{sessionId} — {total} mensagens</p>
      </div>

      {loading ? (
        <div className="flex items-center gap-2 text-gray-500">
          <div className="w-4 h-4 border-2 border-ava-600 border-t-transparent rounded-full animate-spin" />
          Carregando mensagens...
        </div>
      ) : messages.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border border-gray-200">
          <p className="text-gray-400">Nenhuma mensagem nesta sessão.</p>
        </div>
      ) : (
        <>
          <div className="bg-white rounded-xl border border-gray-200 p-6 space-y-4">
            {messages.map((msg) => (
              <div
                key={msg.chatMessageId}
                className={`flex ${msg.senderType === SenderType.User ? 'justify-end' : 'justify-start'}`}
              >
                <div
                  className={`max-w-[75%] px-4 py-3 rounded-2xl ${
                    msg.senderType === SenderType.User
                      ? 'bg-ava-600 text-white rounded-br-md'
                      : 'bg-gray-100 text-gray-800 rounded-bl-md'
                  }`}
                >
                  {msg.senderType === SenderType.Assistant ? (
                    <div className="prose prose-sm max-w-none prose-p:my-1 prose-ul:my-1 prose-ol:my-1">
                      <ReactMarkdown>{msg.content}</ReactMarkdown>
                    </div>
                  ) : (
                    <p className="text-sm whitespace-pre-wrap">{msg.content}</p>
                  )}
                  <p className={`text-[11px] mt-1.5 ${
                    msg.senderType === SenderType.User ? 'text-white/60' : 'text-gray-400'
                  }`}>
                    {new Date(msg.createdAt).toLocaleString('pt-BR')}
                  </p>
                </div>
              </div>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <p className="text-sm text-gray-500">
                Página {page} de {totalPages} ({total} mensagens)
              </p>
              <div className="flex gap-2">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  Anterior
                </button>
                <button
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                  className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  Próxima
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default SessionDetailPage
