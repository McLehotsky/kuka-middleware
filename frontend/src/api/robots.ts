import { request } from './http'
import type { RobotSummary } from './types'

/** Ordered by name, falling back to kukaRobotId (Q-O). */
export function listRobots(signal?: AbortSignal): Promise<RobotSummary[]> {
  return request<RobotSummary[]>('/robots', { signal })
}
