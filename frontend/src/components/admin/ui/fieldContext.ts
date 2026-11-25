import { createContext, useContext } from 'react'
import type { AriaAttributes } from 'react'

export interface FieldControlContext {
  id: string
  describedBy?: string
  invalid: boolean
  required: boolean
}

/** Liga o controle (Input, Textarea, Select, PasswordInput) ao rótulo, à ajuda e ao erro do Field. */
export const FieldContext = createContext<FieldControlContext | null>(null)

interface ControlA11yProps {
  id?: string
  'aria-describedby'?: string
  'aria-invalid'?: AriaAttributes['aria-invalid']
}

/** Props de acessibilidade do controle dentro de um Field; valores explícitos têm prioridade. */
export const useFieldControl = (props: ControlA11yProps): ControlA11yProps => {
  const field = useContext(FieldContext)
  return {
    id: props.id ?? field?.id,
    'aria-describedby': props['aria-describedby'] ?? field?.describedBy,
    'aria-invalid': props['aria-invalid'] ?? (field?.invalid ? true : undefined),
  }
}
