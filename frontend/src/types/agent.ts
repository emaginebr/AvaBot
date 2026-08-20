export interface AgentInfo {
  agentId: number;
  name: string;
  slug: string;
  description: string | null;
  systemPrompt: string;
  status: number;
  collectName: boolean;
  collectEmail: boolean;
  collectPhone: boolean;
  chatModel: string;
  telegramBotName: string | null;
  telegramBotToken: string | null;
  telegramWebhookSecret: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface AgentInsertInfo {
  name: string;
  description: string | null;
  systemPrompt: string;
  collectName: boolean;
  collectEmail: boolean;
  collectPhone: boolean;
  chatModel: string;
  telegramBotName: string | null;
  telegramBotToken: string | null;
}

export interface TelegramWebhookInfo {
  agentId: number;
  agentSlug: string;
  webhookUrl: string | null;
  isConfigured: boolean;
}

export interface WhatsappQrCodeInfo {
  agentSlug: string;
  qrCode: string;
}

export interface WhatsappStatusInfo {
  agentSlug: string;
  status: string;
  isConnected: boolean;
}

export interface AgentChatConfigInfo {
  name: string;
  description: string | null;
  collectName: boolean;
  collectEmail: boolean;
  collectPhone: boolean;
}

export interface AgentTestMessage {
  role: 'assistant' | 'user';
  content: string;
}

export interface AgentTestResult {
  searchQuery: string;
  searchResults: string[];
  systemPrompt: string;
  messages: AgentTestMessage[];
  assistantResponse: string;
}
