import { describe, expect, it } from 'vitest'
import { formatHistoryTime } from './format'

// Local times without a zone, so the test does not depend on the machine's time zone.
const created = '2026-10-21T08:00:00'

describe('formatHistoryTime', () => {
  it('shows only the time on the day the mission was created', () => {
    expect(formatHistoryTime('2026-10-21T13:40:12', created)).toBe('13:40:12')
  })

  it('adds the date when the entry is on a later day', () => {
    expect(formatHistoryTime('2026-10-22T09:05:00', created)).toMatch(/^22 Oct.*09:05:00$/)
  })

  it('shows a dash for an invalid timestamp', () => {
    expect(formatHistoryTime('not a date', created)).toBe('—')
  })
})
