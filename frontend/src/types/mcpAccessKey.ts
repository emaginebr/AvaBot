export interface McpAccessKeyInfo {
  mcpAccessKeyId: number;
  name: string;
  keyPrefix: string;
  /** 1 = ativa, 0 = revogada */
  status: number;
  createdAt: string;
  lastUsedAt: string | null;
  revokedAt: string | null;
}

export interface McpAccessKeyCreatedInfo {
  /** Valor em claro: exibido uma única vez, nunca guardado no estado nem no console. */
  key: string;
  accessKey: McpAccessKeyInfo;
}
