export interface KnowledgeFileInfo {
  knowledgeFileId: number;
  agentId: number;
  fileName: string;
  fileSize: number;
  processingStatus: ProcessingStatus;
  errorMessage: string | null;
  createdAt: string;
  updatedAt: string;
}

export const ProcessingStatus = {
  Processing: 0,
  Ready: 1,
  Error: 2,
} as const

export type ProcessingStatus = (typeof ProcessingStatus)[keyof typeof ProcessingStatus]
