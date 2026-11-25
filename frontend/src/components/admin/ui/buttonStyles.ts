import { cx } from './cx'

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'dangerGhost'
export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg'

const BASE =
  'inline-flex items-center justify-center gap-[7px] whitespace-nowrap border font-medium cursor-pointer ' +
  'transition-[background-color,border-color,color,box-shadow] duration-150 motion-reduce:transition-none ' +
  'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ava-600 ' +
  'disabled:cursor-not-allowed disabled:opacity-50 disabled:shadow-none aria-disabled:cursor-not-allowed aria-disabled:opacity-50'

// "not-disabled:hover" vale para <button> e para <a> (links com cara de botão).
const VARIANTS: Record<ButtonVariant, string> = {
  primary:
    'border-ava-600 bg-ava-600 text-white shadow-ava not-disabled:hover:border-ava-700 not-disabled:hover:bg-ava-700 not-disabled:hover:shadow-ava-lg',
  secondary: 'border-gray-200 bg-white text-gray-700 not-disabled:hover:border-gray-300 not-disabled:hover:bg-gray-50',
  ghost: 'border-transparent bg-transparent text-ava-700 not-disabled:hover:bg-ava-50',
  danger: 'border-err bg-err text-white not-disabled:hover:border-[#912018] not-disabled:hover:bg-[#912018]',
  dangerGhost: 'border-err-line bg-white text-err not-disabled:hover:border-[#ef9f98] not-disabled:hover:bg-err-bg',
}

// Alvos de 44 px em telas de toque (pointer: coarse), como na proposta.
const SIZES: Record<ButtonSize, string> = {
  // Ação textual discreta junto a rótulos (ex.: "Restaurar padrão").
  xs: 'min-h-7 rounded-lg px-2 text-xs pointer-coarse:min-h-11 [&_svg]:h-3.5 [&_svg]:w-3.5',
  sm: 'min-h-[34px] rounded-[10px] px-3 text-[13px] pointer-coarse:min-h-11 [&_svg]:h-3.5 [&_svg]:w-3.5',
  md: 'min-h-10 rounded-xl px-4 text-[13.5px] pointer-coarse:min-h-11 [&_svg]:h-4 [&_svg]:w-4',
  lg: 'min-h-11 rounded-xl px-5 text-[13.5px] font-semibold [&_svg]:h-4 [&_svg]:w-4',
}

/** Classes do botão do kit; também servem para links com cara de botão. */
export const buttonClasses = (variant: ButtonVariant = 'secondary', size: ButtonSize = 'md', className?: string) =>
  cx(BASE, VARIANTS[variant], SIZES[size], className)
