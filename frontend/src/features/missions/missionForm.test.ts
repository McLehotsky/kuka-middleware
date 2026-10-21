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

describe('newDraft', () => {
  it('defaults the priority to 2 (normal)', () => {
    expect(newDraft().priority).toBe(2)
  })
})

describe('validateDraft', () => {
  it('accepts a minimal valid mission', () => {
    expect(validateDraft(validDraft())).toEqual({})
  })

  it('keys step errors like the API, indexed from 0', () => {
    const draft = validDraft()
    draft.steps.push({ key: 99, nodeCode: '   ', action: '' })
    expect(validateDraft(draft)).toEqual({
      'steps[1].nodeCode': 'Node is required.',
      'steps[1].action': 'Choose an action.',
    })
  })

  it('accepts a node code of exactly 64 characters', () => {
    const draft = validDraft()
    draft.steps[0]!.nodeCode = 'N'.repeat(64)
    expect(validateDraft(draft)).toEqual({})
  })

  it('rejects a node code of 65 characters', () => {
    const draft = validDraft()
    draft.steps[0]!.nodeCode = 'N'.repeat(65)
    expect(Object.keys(validateDraft(draft))).toEqual(['steps[0].nodeCode'])
  })

  it('accepts an external reference of exactly 100 characters', () => {
    const draft = validDraft()
    draft.externalRef = 'E'.repeat(100)
    expect(validateDraft(draft)).toEqual({})
  })

  it('rejects an external reference of 101 characters', () => {
    const draft = validDraft()
    draft.externalRef = 'E'.repeat(101)
    expect(Object.keys(validateDraft(draft))).toEqual(['externalRef'])
  })

  it.each([0, 4])('rejects priority %i', (priority) => {
    const draft = validDraft()
    draft.priority = priority
    expect(Object.keys(validateDraft(draft))).toEqual(['priority'])
  })

  it('rejects a missing mission type', () => {
    const draft = validDraft()
    draft.missionType = ''
    expect(Object.keys(validateDraft(draft))).toEqual(['missionType'])
  })

  it('rejects an empty step list', () => {
    const draft = validDraft()
    draft.steps = []
    expect(Object.keys(validateDraft(draft))).toEqual(['steps'])
  })

  it('treats the action as an exact, case-sensitive value', () => {
    const draft = validDraft()
    // @ts-expect-error – simulates a wrong-case value reaching the form
    draft.steps[0]!.action = 'load'
    expect(Object.keys(validateDraft(draft))).toEqual(['steps[0].action'])
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

describe('fromServerErrors', () => {
  it('keeps the first message per key and drops empty lists', () => {
    expect(fromServerErrors({ 'steps[2].nodeCode': ['first', 'second'], robotId: [] })).toEqual({
      'steps[2].nodeCode': 'first',
    })
  })
})

describe('fieldLabel', () => {
  it.each([
    ['steps[2].nodeCode', 'Step 3 – node'],
    ['steps[0].action', 'Step 1 – action'],
    ['robotId', 'Robot'],
  ])('names %s as "%s"', (key, label) => {
    expect(fieldLabel(key)).toBe(label)
  })
})

describe('moveStep', () => {
  const steps = [1, 2, 3].map((key) => ({ key, nodeCode: `N${key}`, action: 'Wait' as const }))

  it('moves a step up', () => {
    expect(moveStep(steps, 1, -1).map((s) => s.key)).toEqual([2, 1, 3])
  })

  it('moves a step down', () => {
    expect(moveStep(steps, 1, 1).map((s) => s.key)).toEqual([1, 3, 2])
  })

  it.each([
    [0, -1],
    [2, 1],
  ] as const)('leaves the list unchanged when step %i moves past the end (%i)', (index, delta) => {
    expect(moveStep(steps, index, delta)).toBe(steps)
  })
})
