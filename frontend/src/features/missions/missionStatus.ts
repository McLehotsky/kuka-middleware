// Mission status rules from system-definition §5 (Mission state machine) and the pill palette
// from DESIGN.md (Colors → Status pill palette).
import type { MissionStatus, MissionSummary } from '../../api/types'

export type StatusTone =
  | 'created'
  | 'submitted'
  | 'inprogress'
  | 'cancelling'
  | 'completed'
  | 'cancelled'
  | 'failed'

const TONES: Record<MissionStatus, StatusTone> = {
  Created: 'created',
  Submitted: 'submitted',
  InProgress: 'inprogress',
  Cancelling: 'cancelling',
  Completed: 'completed',
  Cancelled: 'cancelled',
  Failed: 'failed',
}

const FINAL: ReadonlySet<MissionStatus> = new Set(['Completed', 'Cancelled', 'Failed'])
const CANCELLABLE: ReadonlySet<MissionStatus> = new Set(['Created', 'Submitted', 'InProgress'])

export function statusTone(status: MissionStatus): StatusTone {
  return TONES[status] ?? 'created'
}

/** Final statuses set FinishedAt; everything else is "not final" (the Active tab, Q-D). */
export function isFinal(status: MissionStatus): boolean {
  return FINAL.has(status)
}

/** UC2 / Q-C: Created, Submitted and InProgress may be cancelled; the rest answer 409. */
export function isCancellable(status: MissionStatus): boolean {
  return CANCELLABLE.has(status)
}

export type MissionTab = 'active' | 'finished' | 'all'

export const MISSION_TABS: readonly { id: MissionTab; label: string }[] = [
  { id: 'active', label: 'Active' },
  { id: 'finished', label: 'Finished' },
  { id: 'all', label: 'All' },
]

export function isMissionTab(value: unknown): value is MissionTab {
  return value === 'active' || value === 'finished' || value === 'all'
}

export function filterMissions(missions: readonly MissionSummary[], tab: MissionTab): MissionSummary[] {
  if (tab === 'all') return [...missions]
  const wantFinal = tab === 'finished'
  return missions.filter((m) => isFinal(m.status) === wantFinal)
}

/** Q-E: counts come from one unfiltered list call. */
export function countMissions(missions: readonly MissionSummary[]): Record<MissionTab, number> {
  let finished = 0
  for (const m of missions) if (isFinal(m.status)) finished++
  return { active: missions.length - finished, finished, all: missions.length }
}
