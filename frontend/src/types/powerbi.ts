export const PowerBISchemaStatus = {
  NotGenerated: 0,
  Generated: 1,
  Partial: 2,
  Error: 3,
} as const

export type PowerBISchemaStatus = (typeof PowerBISchemaStatus)[keyof typeof PowerBISchemaStatus]

export const PowerBIQueryStatus = {
  Success: 1,
  Error: 2,
  Timeout: 3,
} as const

export type PowerBIQueryStatus = (typeof PowerBIQueryStatus)[keyof typeof PowerBIQueryStatus]

export type PowerBISchemaDescriptionKind = 'table' | 'column' | 'measure'

export interface PowerBIConfigInfo {
  agentId: number;
  enabled: boolean;
  isConfigured: boolean;
  tenantId: string | null;
  clientId: string | null;
  hasClientSecret: boolean;
  clientSecretMasked: string | null;
  lastTestAt: string | null;
  lastTestSuccess: boolean | null;
  lastTestMessage: string | null;
}

export interface PowerBIConfigUpdateInfo {
  tenantId: string;
  clientId: string;
  clientSecret: string | null;
}

export interface PowerBIEnabledUpdateInfo {
  enabled: boolean;
}

export interface PowerBIConnectionTestStepInfo {
  step: string;
  success: boolean;
  message: string | null;
}

export interface PowerBIConnectionTestInfo {
  success: boolean;
  steps: PowerBIConnectionTestStepInfo[];
}

export interface PowerBIWorkspaceDatasetInfo {
  datasetId: string;
  name: string;
}

export interface PowerBIWorkspaceInfo {
  workspaceId: string;
  name: string;
  datasets: PowerBIWorkspaceDatasetInfo[];
}

export interface PowerBIDatasetInfo {
  powerBIDatasetId: number;
  workspaceId: string;
  datasetId: string;
  name: string;
  description: string | null;
  toolKey: string;
  schemaStatus: PowerBISchemaStatus;
  schemaGeneratedAt: string | null;
  schemaError: string | null;
  tableCount: number;
  columnCount: number;
  measureCount: number;
}

export interface PowerBIDatasetInsertInfo {
  workspaceId: string;
  datasetId: string;
  name: string;
  description: string | null;
}

export interface PowerBISchemaColumnInfo {
  name: string;
  dataType: string | null;
  description: string | null;
  userDescription: string | null;
}

export interface PowerBISchemaMeasureInfo {
  name: string;
  description: string | null;
  userDescription: string | null;
}

export interface PowerBISchemaTableInfo {
  name: string;
  description: string | null;
  userDescription: string | null;
  columns: PowerBISchemaColumnInfo[];
  measures: PowerBISchemaMeasureInfo[];
}

export interface PowerBIDatasetSchemaInfo {
  powerBIDatasetId: number;
  schemaStatus: PowerBISchemaStatus;
  schemaGeneratedAt: string | null;
  schemaError: string | null;
  tables: PowerBISchemaTableInfo[];
}

export interface PowerBISchemaDescriptionItemInfo {
  kind: PowerBISchemaDescriptionKind;
  table: string;
  name: string | null;
  userDescription: string | null;
}

export interface PowerBISchemaDescriptionUpdateInfo {
  items: PowerBISchemaDescriptionItemInfo[];
}

export interface PowerBIQueryLogInfo {
  powerBIQueryLogId: number;
  createdAt: string;
  chatSessionId: number | null;
  datasetName: string | null;
  toolName: string;
  userQuestion: string | null;
  query: string | null;
  durationMs: number;
  rowCount: number | null;
  truncated: boolean;
  status: PowerBIQueryStatus;
  errorMessage: string | null;
}

export interface PowerBIQueryLogPageInfo {
  page: number;
  pageSize: number;
  total: number;
  items: PowerBIQueryLogInfo[];
}

export interface AgentTestPowerBIQueryInfo {
  toolName: string;
  datasetName: string | null;
  query: string | null;
  durationMs: number;
  rowCount: number | null;
  truncated: boolean;
  success: boolean;
  error: string | null;
  resultPreview: string | null;
}
