import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { toast } from 'sonner'
import ReactMarkdown from 'react-markdown'
import { ChatHistoryService } from '../../Services/ChatHistoryService'
import { type ChatMessageInfo, SenderType } from '../../types/chatMessage'
import AdminIcon from '../../components/admin/AdminIcon'
import { Card } from '../../components/admin/ui/Card'
import PageHeader from '../../components/admin/ui/PageHeader'
import Pagination from '../../components/admin/ui/Pagination'
import { EmptyState, LoadingState } from '../../components/admin/ui/States'
import { cx } from '../../components/admin/ui/cx'

const pageSize = 50

const SessionDetailPage = () => {
  const { sessionId } = useParams<{ sessionId: string }>()
  const [messages, setMessages] = useState<ChatMessageInfo[]>([])
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  // Chave da última consulta concluída: "carregando" é derivado dela, sem setState síncrono no effect.
  const [loadedKey, setLoadedKey] = useState<string | null>(null)
  const id = Number(sessionId)
  const loading = `${id}:${page}` !== loadedKey

  useEffect(() => {
    if (!id) return
    const key = `${id}:${page}`
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
      .finally(() => setLoadedKey(key))
  }, [id, page])

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        backTo={{ to: '/admin/sessions', label: 'Voltar para sessões' }}
        title="Histórico da conversa"
        description={
          <>
            Sessão <span className="font-ui-mono tabular-nums">#{sessionId}</span> —{' '}
            <span className="font-ui-mono tabular-nums">{total}</span> mensagens
          </>
        }
      />

      <Card aria-label="Mensagens da sessão">
        {loading ? (
          <LoadingState label="Carregando mensagens..." rows={5} rowClassName="h-14" />
        ) : messages.length === 0 ? (
          <EmptyState
            icon="chat"
            title="Nenhuma mensagem nesta sessão"
            description="A sessão foi aberta, mas nenhuma mensagem foi trocada."
          />
        ) : (
          <>
            <ol className="m-0 flex max-w-[860px] list-none flex-col gap-3.5 p-0">
              {messages.map((msg) => {
                const fromUser = msg.senderType === SenderType.User
                return (
                  <li
                    key={msg.chatMessageId}
                    className={cx('flex max-w-[min(640px,92%)] items-end gap-2.5', fromUser && 'ml-auto flex-row-reverse')}
                  >
                    <span
                      aria-hidden="true"
                      className={cx(
                        'grid h-7 w-7 flex-none place-items-center rounded-full',
                        fromUser ? 'bg-ava-100 text-ava-800' : 'bg-ava-600 text-white',
                      )}
                    >
                      <AdminIcon name={fromUser ? 'user' : 'bot'} className="h-3.5 w-3.5" />
                    </span>
                    <div
                      className={cx(
                        'min-w-0 rounded-2xl px-3.5 py-2.5 text-sm [overflow-wrap:anywhere]',
                        fromUser ? 'rounded-br-md bg-ava-600 text-white' : 'rounded-bl-md bg-gray-100 text-ink',
                      )}
                    >
                      <span className="sr-only">{fromUser ? 'Visitante:' : 'Agente:'}</span>
                      {msg.senderType === SenderType.Assistant ? (
                        <div className="prose prose-sm max-w-none prose-p:my-1 prose-ul:my-1 prose-ol:my-1">
                          <ReactMarkdown>{msg.content}</ReactMarkdown>
                        </div>
                      ) : (
                        <p className="m-0 whitespace-pre-wrap">{msg.content}</p>
                      )}
                      <time
                        dateTime={msg.createdAt}
                        className={cx('mt-1.5 block font-ui-mono text-[11px] tabular-nums', fromUser ? 'text-[#dbe4ff]' : 'text-ink-3')}
                      >
                        {new Date(msg.createdAt).toLocaleString('pt-BR')}
                      </time>
                    </div>
                  </li>
                )
              })}
            </ol>

            {totalPages > 1 && (
              <Pagination
                summary={`Página ${page} de ${totalPages} (${total} mensagens)`}
                onPrevious={() => setPage((p) => Math.max(1, p - 1))}
                onNext={() => setPage((p) => Math.min(totalPages, p + 1))}
                previousDisabled={page === 1}
                nextDisabled={page === totalPages}
              />
            )}
          </>
        )}
      </Card>
    </div>
  )
}

export default SessionDetailPage
