import { cx } from './cx'

/** Aparência comum de Input, Textarea e Select (borda gray-300, foco com anel ava-100, erro em vermelho). */
export const controlClasses = (options: { mono?: boolean; className?: string } = {}) =>
  cx(
    'w-full min-w-0 min-h-[42px] rounded-xl border border-gray-300 bg-white px-3 py-[9px] text-sm text-ink',
    'placeholder:text-[#6b7389] transition-[border-color,box-shadow] duration-150 motion-reduce:transition-none',
    'hover:border-[#b9bfcc] focus:border-ava-500 focus:shadow-[0_0_0_3px_var(--color-ava-100)] focus:outline-none',
    'read-only:bg-gray-50 read-only:text-ink-2',
    'disabled:cursor-not-allowed disabled:bg-gray-100 disabled:text-ink-3',
    'aria-invalid:border-err aria-invalid:bg-[#fffafa] aria-invalid:focus:shadow-[0_0_0_3px_var(--color-err-bg)]',
    options.mono && 'font-ui-mono text-[13px]',
    options.className,
  )
