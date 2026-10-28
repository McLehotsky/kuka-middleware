import { describe, expect, it } from 'vitest'
import type { RobotStatus, RobotSummary } from '../../api/types'
import {
  batteryTone,
  countRobots,
  filterRobots,
  robotDisplayName,
  robotOptionLabel,
  robotStatusTone,
} from './robotStatus'

function robot(overrides: Partial<RobotSummary> = {}): RobotSummary {
  return {
    id: 1,
    kukaRobotId: 'AMR-1001',
    name: 'Robot A',
    status: 'Available',
    batteryLevel: 80,
    currentNode: 'N-01',
    currentMission: null,
    lastSeenAt: '2026-10-28T10:00:00Z',
    updatedAt: '2026-10-28T10:00:00Z',
    ...overrides,
  }
}

describe('robotStatusTone', () => {
  it.each([
    ['Available', 'completed'],
    ['Busy', 'inprogress'],
    ['Offline', 'created'],
  ] as const)('%s reuses the %s pill colours', (status, tone) => {
    expect(robotStatusTone(status)).toBe(tone)
  })
})

describe('batteryTone', () => {
  it.each([
    [100, 'high'],
    [50, 'high'],
    [49, 'mid'],
    [25, 'mid'],
    [24, 'low'],
    [0, 'low'],
  ] as const)('%i %% is %s', (level, tone) => {
    expect(batteryTone(level, 'Available')).toBe(tone)
  })

  it('is offline for an offline robot regardless of the level', () => {
    expect(batteryTone(90, 'Offline')).toBe('offline')
    expect(batteryTone(10, 'Offline')).toBe('offline')
  })

  it('has no tone when the level is unknown', () => {
    expect(batteryTone(null, 'Available')).toBeNull()
    expect(batteryTone(null, 'Offline')).toBeNull()
  })
})

describe('countRobots', () => {
  it('counts the total and each state from one list', () => {
    const statuses: RobotStatus[] = ['Available', 'Busy', 'Available', 'Offline', 'Available']
    const list = statuses.map((status, i) => robot({ id: i, status }))
    expect(countRobots(list)).toEqual({ total: 5, Available: 3, Busy: 1, Offline: 1 })
  })

  it('counts zero for an empty list', () => {
    expect(countRobots([])).toEqual({ total: 0, Available: 0, Busy: 0, Offline: 0 })
  })
})

describe('robotDisplayName', () => {
  it('uses the name', () => {
    expect(robotDisplayName(robot({ name: 'KMP-01' }))).toBe('KMP-01')
  })

  it.each([null, '', '   '])('falls back to the KUKA ID when the name is %j', (name) => {
    expect(robotDisplayName(robot({ name, kukaRobotId: 'AMR-7' }))).toBe('AMR-7')
  })
})

describe('robotOptionLabel', () => {
  it('shows the name and the state', () => {
    expect(robotOptionLabel(robot({ name: 'KMP-02', status: 'Busy' }))).toBe('KMP-02 · Busy')
  })
})

describe('filterRobots', () => {
  const list = [
    robot({ id: 1, name: 'Alpha', kukaRobotId: 'AMR-1001', status: 'Available' }),
    robot({ id: 2, name: 'Bravo', kukaRobotId: 'AMR-1002', status: 'Busy' }),
    robot({ id: 3, name: null, kukaRobotId: 'AMR-2003', status: 'Offline' }),
  ]
  const ids = (robots: RobotSummary[]) => robots.map((r) => r.id)

  it('keeps every robot in the API order without a query or status', () => {
    expect(filterRobots(list, { query: '', status: 'all' })).toEqual(list)
  })

  it('matches the name case-insensitively', () => {
    expect(ids(filterRobots(list, { query: 'bRAV', status: 'all' }))).toEqual([2])
  })

  it('matches the KUKA ID case-insensitively', () => {
    expect(ids(filterRobots(list, { query: 'amr-2', status: 'all' }))).toEqual([3])
  })

  it('ignores surrounding whitespace in the query', () => {
    expect(ids(filterRobots(list, { query: '  alpha ', status: 'all' }))).toEqual([1])
  })

  it('filters by status', () => {
    expect(ids(filterRobots(list, { query: '', status: 'Busy' }))).toEqual([2])
  })

  it('combines query and status', () => {
    expect(ids(filterRobots(list, { query: 'amr-100', status: 'Available' }))).toEqual([1])
    expect(ids(filterRobots(list, { query: 'amr-2', status: 'Available' }))).toEqual([])
  })
})
