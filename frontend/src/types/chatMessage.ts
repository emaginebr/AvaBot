export interface ChatMessageInfo {
  chatMessageId: number;
  chatSessionId: number;
  senderType: SenderType;
  content: string;
  createdAt: string;
}

export const SenderType = {
  User: 0,
  Assistant: 1,
} as const

export type SenderType = (typeof SenderType)[keyof typeof SenderType]
