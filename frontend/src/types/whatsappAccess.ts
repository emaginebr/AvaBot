// Controle de acesso do WhatsApp (feature 018).

export const WHATSAPP_ACCESS_MODE = {
  ALL: 1,
  RESTRICTED: 2,
} as const

export const WHATSAPP_REQUEST_STATUS = {
  PENDING: 1,
  APPROVED: 2,
  REJECTED: 3,
} as const

export const WHATSAPP_CONTACT_SOURCE = {
  MANUAL: 1,
  APPROVAL: 2,
} as const

export const WHATSAPP_MESSAGE_MAX_LENGTH = 500

export interface WhatsappAccessMessageInfo {
  /** Texto efetivo: o personalizado ou, se não houver, o padrão. */
  text: string;
  isDefault: boolean;
  defaultText: string;
}

export interface WhatsappAccessConfigInfo {
  /** 1 = Todos, 2 = Somente contatos autorizados */
  mode: number;
  offerMessage: WhatsappAccessMessageInfo;
  confirmationMessage: WhatsappAccessMessageInfo;
  waitingMessage: WhatsappAccessMessageInfo;
  declinedMessage: WhatsappAccessMessageInfo;
  contactCount: number;
  pendingRequestCount: number;
}

export interface WhatsappAccessConfigUpdateInfo {
  mode: number;
  /** null = restaurar o texto padrão */
  offerMessage: string | null;
  confirmationMessage: string | null;
  waitingMessage: string | null;
  declinedMessage: string | null;
}

export interface WhatsappContactInfo {
  whatsappContactId: number;
  name: string;
  phone: string | null;
  phoneNormalized: string | null;
  whatsappId: string | null;
  /** 1 = manual, 2 = aprovação */
  source: number;
  createdAt: string;
  updatedAt: string;
}

export interface WhatsappContactInsertInfo {
  name: string;
  phone: string;
}

export interface WhatsappAccessRequestInfo {
  whatsappAccessRequestId: number;
  whatsappId: string;
  phone: string | null;
  profileName: string | null;
  /** 1 = pendente, 2 = aprovada, 3 = rejeitada */
  status: number;
  requestedAt: string;
  decidedAt: string | null;
  whatsappContactId: number | null;
}
