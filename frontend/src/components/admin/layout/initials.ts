/** Iniciais (até 2 letras) para avatares do painel. */
export const initialsOf = (name: string | null | undefined, fallback = '?'): string => {
  const letters = (name ?? '')
    .split(/[\s—–-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
  return letters || fallback
}
