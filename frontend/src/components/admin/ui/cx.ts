/** Junta classes ignorando valores falsos (evita template strings longas nos componentes do kit). */
export const cx = (...parts: Array<string | false | null | undefined>): string => parts.filter(Boolean).join(' ')
