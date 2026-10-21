import { request } from './http'
import type {
  CreateMissionRequest,
  CreateMissionResponse,
  MissionDetail,
  MissionHistoryEntry,
  MissionSummary,
} from './types'

export function listMissions(signal?: AbortSignal): Promise<MissionSummary[]> {
  return request<MissionSummary[]>('/missions', { signal })
}

export function getMission(missionId: string, signal?: AbortSignal): Promise<MissionDetail> {
  return request<MissionDetail>(`/missions/${encodeURIComponent(missionId)}`, { signal })
}

export function getMissionHistory(
  missionId: string,
  signal?: AbortSignal,
): Promise<MissionHistoryEntry[]> {
  return request<MissionHistoryEntry[]>(`/missions/${encodeURIComponent(missionId)}/history`, {
    signal,
  })
}

export function createMission(body: CreateMissionRequest): Promise<CreateMissionResponse> {
  return request<CreateMissionResponse>('/missions', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export function cancelMission(missionId: string): Promise<MissionSummary> {
  return request<MissionSummary>(`/missions/${encodeURIComponent(missionId)}/cancel`, {
    method: 'POST',
  })
}
