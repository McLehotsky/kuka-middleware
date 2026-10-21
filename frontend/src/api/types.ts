// DTOs of the middleware REST API (system-definition §8, "Mission DTOs (phase 6)").
// Enum values are exact and case-sensitive. Missing optional values are null.

export const MISSION_TYPES = ['RACK_MOVE', 'ROLLER_MOVE'] as const
export type MissionType = (typeof MISSION_TYPES)[number]

export const STEP_ACTIONS = ['Load', 'Unload', 'Wait'] as const
export type StepAction = (typeof STEP_ACTIONS)[number]

export const MISSION_STATUSES = [
  'Created',
  'Submitted',
  'InProgress',
  'Cancelling',
  'Completed',
  'Cancelled',
  'Failed',
] as const
export type MissionStatus = (typeof MISSION_STATUSES)[number]

export type StatusSource = 'Api' | 'Sync'

/** ISO-8601 timestamp string (timestamptz, UTC). */
export type IsoDateTime = string

export interface CreateMissionStep {
  nodeCode: string
  action: StepAction
}

export interface CreateMissionRequest {
  missionType: MissionType
  priority?: number | null
  robotId?: number | null
  externalRef?: string | null
  steps: CreateMissionStep[]
}

export interface CreateMissionResponse {
  id: string
  missionCode: string
}

export interface MissionSummary {
  id: string
  missionCode: string
  externalRef: string | null
  missionType: string
  priority: number
  status: MissionStatus
  robotName: string | null
  createdAt: IsoDateTime
  lastSyncedAt: IsoDateTime | null
}

export interface MissionStepDto {
  sequence: number
  nodeCode: string
  action: string
}

export interface MissionDetail extends MissionSummary {
  submittedAt: IsoDateTime | null
  finishedAt: IsoDateTime | null
  errorMessage: string | null
  createdBy: string
  steps: MissionStepDto[]
}

export interface MissionHistoryEntry {
  previousStatus: MissionStatus | null
  status: MissionStatus
  source: StatusSource
  message: string | null
  changedAt: IsoDateTime
}
