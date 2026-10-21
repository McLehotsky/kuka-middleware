import { describe, expect, it } from 'vitest'
import { NETWORK_ERROR_MESSAGE, toProblemInfo } from './problem'

describe('toProblemInfo', () => {
  it('maps a ValidationProblem to field errors keyed like the API', () => {
    const info = toProblemInfo(400, {
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: { 'steps[0].nodeCode': ['Must be 1 to 64 characters and not blank.'], priority: 'Must be between 1 and 3.' },
    })
    expect(info.status).toBe(400)
    expect(info.fieldErrors).toEqual({
      'steps[0].nodeCode': ['Must be 1 to 64 characters and not blank.'],
      priority: ['Must be between 1 and 3.'],
    })
    expect(info.message).toMatch(/not valid/)
  })

  it('uses the server detail for 409 and plain words for 401, 404 and 502', () => {
    expect(toProblemInfo(409, { detail: 'A mission in status Cancelling cannot be cancelled.' }).message).toBe(
      'A mission in status Cancelling cannot be cancelled.',
    )
    expect(toProblemInfo(401, null).message).toMatch(/API key/)
    expect(toProblemInfo(404, { title: 'Mission not found' }).message).toMatch(/Not found/)
    expect(toProblemInfo(502, {}).message).toMatch(/KUKA FM/)
  })

  it('handles network failures, server errors and odd bodies', () => {
    expect(toProblemInfo(null, null)).toEqual({ status: null, message: NETWORK_ERROR_MESSAGE, fieldErrors: {} })
    expect(toProblemInfo(500, 'boom').message).toMatch(/ran into an error/)
    expect(toProblemInfo(418, { detail: 'teapot' }).message).toBe('teapot')
    expect(toProblemInfo(400, { errors: ['not', 'a', 'map'] }).fieldErrors).toEqual({})
  })
})
