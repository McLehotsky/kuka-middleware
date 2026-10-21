import { describe, expect, it } from 'vitest'
import { NETWORK_ERROR_MESSAGE, toProblemInfo } from './problem'

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
    expect(toProblemInfo(null, null)).toEqual({ status: null, message: NETWORK_ERROR_MESSAGE, fieldErrors: {} })
  })

  it('falls back to the server detail for other statuses', () => {
    expect(toProblemInfo(418, { detail: 'teapot' }).message).toBe('teapot')
  })

  it('ignores an errors value that is not a map', () => {
    expect(toProblemInfo(400, { errors: ['not', 'a', 'map'] }).fieldErrors).toEqual({})
  })
})
