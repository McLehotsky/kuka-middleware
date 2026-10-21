import { describe, expect, it } from 'vitest'
import { MISSION_STATUSES, type MissionStatus, type MissionSummary } from '../../api/types'
import { countMissions, filterMissions, isCancellable, isFinal, statusTone } from './missionStatus'

function mission(status: MissionStatus, n = 1): MissionSummary {
  return {
    id: `${status}-${n}`,
    missionCode: `M-2026-${String(n).padStart(4, '0')}`,
    externalRef: null,
    missionType: 'RACK_MOVE',
    priority: 2,
    status,
    robotName: null,
    createdAt: '2026-10-21T10:00:00Z',
    lastSyncedAt: null,
  }
}

describe('mission status rules', () => {
  it.each([
    ['Created', false],
    ['Submitted', false],
    ['InProgress', false],
    ['Cancelling', false],
    ['Completed', true],
    ['Cancelled', true],
    ['Failed', true],
  ] as const)('isFinal(%s) is %s', (status, final) => {
    expect(isFinal(status)).toBe(final)
  })

  it.each([
    ['Created', true],
    ['Submitted', true],
    ['InProgress', true],
    ['Cancelling', false],
    ['Completed', false],
    ['Cancelled', false],
    ['Failed', false],
  ] as const)('isCancellable(%s) is %s', (status, cancellable) => {
    expect(isCancellable(status)).toBe(cancellable)
  })

  it('gives every status its own tone, so Created and Cancelled look different', () => {
    expect(new Set(MISSION_STATUSES.map(statusTone)).size).toBe(MISSION_STATUSES.length)
  })

  it('maps a status to its lower-case tone', () => {
    expect(statusTone('InProgress')).toBe('inprogress')
  })
})

describe('mission tabs', () => {
  const list = MISSION_STATUSES.map((s, i) => mission(s, i))

  it('Active holds every non-final status, including Created', () => {
    expect(filterMissions(list, 'active').map((m) => m.status)).toEqual([
      'Created',
      'Submitted',
      'InProgress',
      'Cancelling',
    ])
  })

  it('Finished holds the final statuses', () => {
    expect(filterMissions(list, 'finished').map((m) => m.status)).toEqual([
      'Completed',
      'Cancelled',
      'Failed',
    ])
  })

  it('All keeps every mission in the API order', () => {
    expect(filterMissions(list, 'all')).toEqual(list)
  })

  it('counts all tabs from one unfiltered list', () => {
    expect(countMissions(list)).toEqual({ active: 4, finished: 3, all: 7 })
  })

  it('counts zero for an empty list', () => {
    expect(countMissions([])).toEqual({ active: 0, finished: 0, all: 0 })
  })
})
