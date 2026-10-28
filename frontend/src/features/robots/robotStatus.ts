// Robot display rules: pill and battery colours from DESIGN.md (Colors → Status pill palette,
// Battery), stat-tile counts, search and status filter (Q-S, all on the client).
import { ROBOT_STATUSES, type RobotStatus, type RobotSummary } from '../../api/types'
import type { StatusTone } from '../missions/missionStatus'

const TONES: Record<RobotStatus, StatusTone> = {
  Available: 'completed',
  Busy: 'inprogress',
  Offline: 'created',
}

export function isRobotStatus(value: unknown): value is RobotStatus {
  return (ROBOT_STATUSES as readonly unknown[]).includes(value)
}

/** Robot states reuse the mission pill palette (DESIGN.md). */
export function robotStatusTone(status: RobotStatus): StatusTone {
  return TONES[status]
}

export type BatteryTone = 'high' | 'mid' | 'low' | 'offline'

/** ≥ 50 high, 25–49 mid, < 25 low; an offline robot is greyed out whatever its level. Null = unknown. */
export function batteryTone(level: number | null, status: RobotStatus): BatteryTone | null {
  if (level === null) return null
  if (status === 'Offline') return 'offline'
  if (level >= 50) return 'high'
  if (level >= 25) return 'mid'
  return 'low'
}

export type RobotCounts = Record<RobotStatus, number> & { total: number }

export function countRobots(robots: readonly RobotSummary[]): RobotCounts {
  const counts: RobotCounts = { total: robots.length, Available: 0, Busy: 0, Offline: 0 }
  for (const r of robots) counts[r.status]++
  return counts
}

export function robotDisplayName(robot: RobotSummary): string {
  return robot.name?.trim() || robot.kukaRobotId
}

/** Label in the New mission robot picker (Q-R). */
export function robotOptionLabel(robot: RobotSummary): string {
  return `${robotDisplayName(robot)} · ${robot.status}`
}

export type RobotStatusFilter = RobotStatus | 'all'

export interface RobotFilter {
  query: string
  status: RobotStatusFilter
}

/** Search by name or KUKA ID (case-insensitive) plus the status filter; keeps the API order. */
export function filterRobots(robots: readonly RobotSummary[], filter: RobotFilter): RobotSummary[] {
  const query = filter.query.trim().toLowerCase()
  return robots.filter(
    (r) =>
      (filter.status === 'all' || r.status === filter.status) &&
      (!query ||
        (r.name ?? '').toLowerCase().includes(query) ||
        r.kukaRobotId.toLowerCase().includes(query)),
  )
}
