/** Trechos numerados da base de conhecimento (busca na base e teste do agente). */
const ChunkList = ({ chunks }: { chunks: string[] }) => (
  <ol className="m-0 flex list-none flex-col gap-2.5 p-0">
    {chunks.map((chunk, index) => (
      <li key={index} className="flex min-w-0 gap-3 rounded-xl border border-gray-200 p-3.5 text-[13.5px] text-ink-2">
        <span
          aria-hidden="true"
          className="grid h-[22px] min-w-[30px] flex-none place-items-center rounded-md bg-ava-50 font-ui-mono text-xs font-medium text-ava-700"
        >
          #{index + 1}
        </span>
        <p className="m-0 min-w-0 leading-relaxed whitespace-pre-wrap [overflow-wrap:anywhere]">
          <span className="sr-only">Trecho {index + 1}: </span>
          {chunk}
        </p>
      </li>
    ))}
  </ol>
)

export default ChunkList
