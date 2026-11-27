import { describe, expect, it } from 'vitest'
import { API_UNAVAILABLE_MESSAGE, NETWORK_ERROR_MESSAGE, toProblemInfo } from './problem'

describe('toProblemInfo', () => {
  it('maps ValidationProblem errors to field errors keyed like the API', () => {
    const info = toProblemInfo(400, {
      title: 'One or more validation errors occurred.',
      errors: { 'steps[0].nodeCode': ['Must be 1 to 64 characters and not blank.'], priority: 'Must be between 1 and 3.' },
    })
    expect(info.fieldErrors).toEqual({
      'steps[0].nodeCode': ['Must be 1 to 64 characters and not blank.'],
      priority: ['Must be between 1 and 3.'],
    })
  })

  it('gives a ValidationProblem a plain summary message', () => {
    expect(toProblemInfo(400, { errors: {} }).message).toMatch(/not valid/)
  })

  it('uses the server detail for a 409', () => {
    expect(toProblemInfo(409, { detail: 'A mission in status Cancelling cannot be cancelled.' }).message).toBe(
      'A mission in status Cancelling cannot be cancelled.',
    )
  })

  it.each([
    [401, /API key/],
    [404, /Not found/],
    [502, /KUKA FM/],
    [500, /ran into an error/],
  ])('uses plain words for HTTP %i', (status, pattern) => {
    expect(toProblemInfo(status, { title: 'Some title' }).message).toMatch(pattern)
  })

  it('reports a network failure with status null', () => {
    expect(toProblemInfo(null, null)).toEqual({
      status: null,
      message: NETWORK_ERROR_MESSAGE,
      fieldErrors: {},
      missionId: null,
    })
  })

  it('falls back to the server detail for other statuses', () => {
    expect(toProblemInfo(418, { detail: 'teapot' }).message).toBe('teapot')
  })

  it('ignores an errors value that is not a map', () => {
    expect(toProblemInfo(400, { errors: ['not', 'a', 'map'] }).fieldErrors).toEqual({})
  })

  it('uses the server detail for a 502 (Q20)', () => {
    expect(toProblemInfo(502, { title: 'Bad Gateway', detail: 'KUKA FM is unreachable.' }).message).toBe(
      'KUKA FM is unreachable.',
    )
  })

  it('falls back to the KUKA FM message for a 502 ProblemDetails without detail', () => {
    expect(toProblemInfo(502, { title: 'Bad Gateway' }).message).toBe(
      'KUKA FM did not respond. Try again in a moment.',
    )
  })

  it('reads the failed mission id from a 502 extension (Q20)', () => {
    const id = '0b7c3f2e-6a1d-4c55-9f00-1f2e3d4c5b6a'
    expect(toProblemInfo(502, { detail: 'KUKA FM is unreachable.', missionId: id }).missionId).toBe(id)
  })

  it.each([[42], [null], [{ id: 'x' }]])('ignores a missionId that is not a string (%j)', (missionId) => {
    expect(toProblemInfo(502, { detail: 'x', missionId }).missionId).toBeNull()
  })

  it('has no mission id when the body has none', () => {
    expect(toProblemInfo(409, { detail: 'x' }).missionId).toBeNull()
  })

  it.each([
    [502, '<html><body>502 Bad Gateway</body></html>'],
    [503, '<html><body>503 Service Unavailable</body></html>'],
    [504, null],
    [502, null],
    [503, ''],
  ])('reports the API as unavailable for HTTP %i without ProblemDetails (Q16)', (status, body) => {
    expect(toProblemInfo(status, body)).toEqual({
      status,
      message: API_UNAVAILABLE_MESSAGE,
      fieldErrors: {},
      missionId: null,
    })
  })
})
