const YEAR = new Date().getFullYear()

/** Rodapé do painel admin, no mesmo tom do rodapé de avabot.net. */
const AdminFooter = () => (
  <footer className="border-t border-gray-100">
    <div className="flex w-full max-w-[1440px] flex-col gap-1 px-4 py-5 text-xs text-ink-3 sm:flex-row sm:items-center sm:justify-between sm:px-8">
      <p>&copy; {YEAR} AvaBot. Todos os direitos reservados.</p>
      <p>
        Desenvolvido por{' '}
        <a
          href="https://emagine.com.br"
          target="_blank"
          rel="noopener noreferrer"
          className="rounded font-semibold text-ava-600 transition-colors hover:text-ava-700"
        >
          Emagine
        </a>
      </p>
    </div>
  </footer>
)

export default AdminFooter
