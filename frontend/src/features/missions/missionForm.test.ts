import { describe, expect, it } from 'vitest'
import {
  fieldLabel,
  fromServerErrors,
  moveStep,
  newDraft,
  toCreateRequest,
  validateDraft,
  type MissionDraft,
} from './missionForm'

function validDraft(): MissionDraft {
  const draft = newDraft()
  draft.steps[0]!.nodeCode = 'A-01-PICK'
  draft.steps[0]!.action = 'Load'
  return draft
}

describe('validateDraft', () => {
  it('accepts a minimal valid mission with the default priority 2', () => {
    const draft = validDraft()
    expect(draft.priority).toBe(2)
    expect(validateDraft(draft)).toEqual({})
  })

  it('uses the API keys, with steps indexed from 0', () => {
    const draft = validDraft()
    draft.steps.push({ key: 99, nodeCode: '   ', action: '' })
    expect(validateDraft(draft)).toEqual({
      'steps[1].nodeCode': 'Node is required.',
      'steps[1].action': 'Choose an action.',
    })
  })

  it('checks the length boundaries: node 64, external ref 100', () => {
    const draft = validDraft()
    draft.steps[0]!.nodeCode = 'N'.repeat(64)
    draft.externalRef = 'E'.repeat(100)
    expect(validateDraft(draft)).toEqual({})

    draft.steps[0]!.nodeCode = 'N'.repeat(65)
    draft.externalRef = 'E'.repeat(101)
    expect(Object.keys(validateDraft(draft)).sort()).toEqual(['externalRef', 'steps[0].nodeCode'])
  })

  it('rejects a priority outside 1–3, a missing type and an empty step list', () => {
    const draft = validDraft()
    draft.priority = 4
    draft.missionType = ''
    draft.steps = []
    expect(Object.keys(validateDraft(draft)).sort()).toEqual(['missionType', 'priority', 'steps'])
  })

  it('treats mission type and action as exact, case-sensitive values', () => {
    const draft = validDraft()
    // @ts-expect-error – simulates a wrong-case value reaching the form
    draft.steps[0]!.action = 'load'
    expect(validateDraft(draft)).toHaveProperty(['steps[0].action'])
  })
})

describe('toCreateRequest', () => {
  it('trims values, sends no robot and null for an empty external ref', () => {
    const draft = validDraft()
    draft.steps[0]!.nodeCode = '  A-01-PICK '
    draft.externalRef = '   '
    expect(toCreateRequest(draft)).toEqual({
      missionType: 'RACK_MOVE',
      priority: 2,
      robotId: null,
      externalRef: null,
      steps: [{ nodeCode: 'A-01-PICK', action: 'Load' }],
    })
  })
})

describe('server errors and labels', () => {
  it('keeps the first message per key', () => {
    expect(fromServerErrors({ 'steps[2].nodeCode': ['first', 'second'], robotId: [] })).toEqual({
      'steps[2].nodeCode': 'first',
    })
  })

  it('names fields in plain words with 1-based step numbers', () => {
    expect(fieldLabel('steps[2].nodeCode')).toBe('Step 3 – node')
    expect(fieldLabel('steps[0].action')).toBe('Step 1 – action')
    expect(fieldLabel('robotId')).toBe('Robot')
  })
})

describe('moveStep', () => {
  const steps = [1, 2, 3].map((key) => ({ key, nodeCode: `N${key}`, action: 'Wait' as const }))

  it('moves a step up or down and ignores moves past the ends', () => {
    expect(moveStep(steps, 1, -1).map((s) => s.key)).toEqual([2, 1, 3])
    expect(moveStep(steps, 1, 1).map((s) => s.key)).toEqual([1, 3, 2])
    expect(moveStep(steps, 0, -1)).toBe(steps)
    expect(moveStep(steps, 2, 1)).toBe(steps)
  })
})
