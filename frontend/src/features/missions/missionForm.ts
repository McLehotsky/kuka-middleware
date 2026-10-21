// New mission form model and client validation mirroring system-definition §8 (CreateMissionRequest).
// Error keys match the API's ValidationProblem keys, steps indexed from 0 (`steps[0].nodeCode`).
import {
  MISSION_TYPES,
  STEP_ACTIONS,
  type CreateMissionRequest,
  type MissionType,
  type StepAction,
} from '../../api/types'

export const EXTERNAL_REF_MAX = 100
export const NODE_CODE_MAX = 64

export const PRIORITIES: readonly { value: number; label: string }[] = [
  { value: 1, label: '1 – high' },
  { value: 2, label: '2 – normal' },
  { value: 3, label: '3 – low' },
]

export const ACTION_LABELS: Record<StepAction, string> = {
  Load: 'Load',
  Unload: 'Unload',
  Wait: 'Wait',
}

export interface StepDraft {
  /** Stable client-side key for v-for and focus; never sent. */
  key: number
  nodeCode: string
  action: StepAction | ''
}

export interface MissionDraft {
  missionType: MissionType | ''
  priority: number
  externalRef: string
  steps: StepDraft[]
}

export type FieldErrors = Record<string, string>

let nextKey = 1
export function newStep(): StepDraft {
  return { key: nextKey++, nodeCode: '', action: '' }
}

export function newDraft(): MissionDraft {
  return { missionType: 'RACK_MOVE', priority: 2, externalRef: '', steps: [newStep()] }
}

export function stepFieldKey(index: number, field: 'nodeCode' | 'action'): string {
  return `steps[${index}].${field}`
}

export function validateDraft(draft: MissionDraft): FieldErrors {
  const errors: FieldErrors = {}

  if (!(MISSION_TYPES as readonly string[]).includes(draft.missionType)) {
    errors.missionType = 'Choose a mission type.'
  }
  if (!Number.isInteger(draft.priority) || draft.priority < 1 || draft.priority > 3) {
    errors.priority = 'Priority must be 1, 2 or 3.'
  }
  if (draft.externalRef.trim().length > EXTERNAL_REF_MAX) {
    errors.externalRef = `External reference can be at most ${EXTERNAL_REF_MAX} characters.`
  }
  if (draft.steps.length === 0) {
    errors.steps = 'Add at least one step.'
  }
  draft.steps.forEach((step, i) => {
    const node = step.nodeCode.trim()
    if (!node) errors[stepFieldKey(i, 'nodeCode')] = 'Node is required.'
    else if (node.length > NODE_CODE_MAX)
      errors[stepFieldKey(i, 'nodeCode')] = `Node can be at most ${NODE_CODE_MAX} characters.`
    if (!(STEP_ACTIONS as readonly string[]).includes(step.action)) {
      errors[stepFieldKey(i, 'action')] = 'Choose an action.'
    }
  })
  return errors
}

/** Call only with a draft that passed validateDraft. */
export function toCreateRequest(draft: MissionDraft): CreateMissionRequest {
  const externalRef = draft.externalRef.trim()
  return {
    missionType: draft.missionType as MissionType,
    priority: draft.priority,
    robotId: null, // Q-I: KUKA FM decides until phase 7
    externalRef: externalRef || null,
    steps: draft.steps.map((s) => ({ nodeCode: s.nodeCode.trim(), action: s.action as StepAction })),
  }
}

/** Takes the first message per key from a server ValidationProblem. */
export function fromServerErrors(errors: Record<string, string[]>): FieldErrors {
  const result: FieldErrors = {}
  for (const [key, messages] of Object.entries(errors)) {
    if (messages[0]) result[key] = messages[0]
  }
  return result
}

const FIELD_LABELS: Record<string, string> = {
  missionType: 'Mission type',
  priority: 'Priority',
  robotId: 'Robot',
  externalRef: 'External reference',
  steps: 'Steps',
}

/** Human label for an error key, e.g. `steps[2].nodeCode` → "Step 3 – node". */
export function fieldLabel(key: string): string {
  const step = /^steps\[(\d+)\]\.(nodeCode|action)$/.exec(key)
  if (step) return `Step ${Number(step[1]) + 1} – ${step[2] === 'nodeCode' ? 'node' : 'action'}`
  return FIELD_LABELS[key] ?? key
}

export function moveStep(steps: StepDraft[], index: number, delta: -1 | 1): StepDraft[] {
  const target = index + delta
  if (target < 0 || target >= steps.length) return steps
  const copy = [...steps]
  const [item] = copy.splice(index, 1)
  copy.splice(target, 0, item!)
  return copy
}
