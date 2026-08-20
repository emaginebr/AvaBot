export interface ChatSessionStartInfo {
  userName?: string;
  userEmail?: string;
  userPhone?: string;
}

export interface ChatSessionInfo {
  chatSessionId: number;
  agentId: number;
  userName: string | null;
  userEmail: string | null;
  userPhone: string | null;
  resumeToken?: string;
  startedAt: string;
  endedAt: string | null;
  messageCount?: number;
}

export interface ChatSessionResumeInfo {
  chatSessionId: number;
  agentId: number | null;
  userName: string | null;
  userEmail: string | null;
  userPhone: string | null;
  resumeToken: string;
  startedAt: string;
  endedAt: string | null;
  messageCount: number;
  messages: ChatResumeMessageInfo[];
}

export interface ChatResumeMessageInfo {
  chatMessageId: number;
  chatSessionId: number;
  senderType: number;
  content: string;
  createdAt: string;
}
