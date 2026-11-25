export const DASHBOARD_PERIODS = [7, 14, 30] as const
export type DashboardPeriod = (typeof DASHBOARD_PERIODS)[number]
