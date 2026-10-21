<script setup lang="ts">
import { computed, nextTick, reactive, ref, useTemplateRef, watch } from 'vue'
import { useRouter } from 'vue-router'
import { problemOf } from '../../api/http'
import { createMission } from '../../api/missions'
import { MISSION_TYPES, STEP_ACTIONS } from '../../api/types'
import AppIcon from '../../components/AppIcon.vue'
import ErrorPanel from '../../components/ErrorPanel.vue'
import {
  ACTION_LABELS,
  PRIORITIES,
  fieldLabel,
  fromServerErrors,
  moveStep,
  newDraft,
  newStep,
  stepFieldKey,
  toCreateRequest,
  validateDraft,
  type FieldErrors,
} from './missionForm'

const router = useRouter()

const draft = reactive(newDraft())
const attempted = ref(false)
const sending = ref(false)
const serverErrors = ref<FieldErrors>({})
const requestError = ref<string | null>(null)
const summaryPanel = useTemplateRef<HTMLElement>('summaryPanel')

const clientErrors = computed(() => validateDraft(draft))
const errors = computed<FieldErrors>(() =>
  attempted.value ? { ...serverErrors.value, ...clientErrors.value } : serverErrors.value,
)
const errorList = computed(() =>
  Object.entries(errors.value).map(([key, message]) => ({ key, message, label: fieldLabel(key) })),
)

// Server errors refer to the draft as it was sent; any edit makes them stale.
watch(
  draft,
  () => {
    serverErrors.value = {}
    requestError.value = null
  },
  { deep: true },
)

function fieldId(key: string): string {
  const step = /^steps\[(\d+)\]\.(nodeCode|action)$/.exec(key)
  if (step) {
    const item = draft.steps[Number(step[1])]
    return item ? `step-${item.key}-${step[2]}` : 'steps-title'
  }
  return key === 'steps' ? 'steps-title' : `field-${key}`
}

function errorFor(key: string): string | undefined {
  return errors.value[key]
}

function describedBy(key: string, extra?: string): string | undefined {
  const ids = [errorFor(key) ? `${fieldId(key)}-error` : null, extra].filter(Boolean)
  return ids.length ? ids.join(' ') : undefined
}

function focusField(key: string) {
  document.getElementById(fieldId(key))?.focus()
}

// ---- Steps -------------------------------------------------------------
async function addStep() {
  const step = newStep()
  draft.steps.push(step)
  await nextTick()
  document.getElementById(`step-${step.key}-nodeCode`)?.focus()
}

async function removeStep(index: number) {
  if (draft.steps.length <= 1) return
  draft.steps.splice(index, 1)
  await nextTick()
  const next = draft.steps[Math.min(index, draft.steps.length - 1)]
  if (next) document.getElementById(`step-${next.key}-nodeCode`)?.focus()
}

async function move(index: number, delta: -1 | 1) {
  const key = draft.steps[index]?.key
  draft.steps = moveStep(draft.steps, index, delta)
  await nextTick()
  const newIndex = draft.steps.findIndex((s) => s.key === key)
  const atEdge = delta === -1 ? newIndex === 0 : newIndex === draft.steps.length - 1
  const direction = atEdge ? (delta === -1 ? 'down' : 'up') : delta === -1 ? 'up' : 'down'
  document.getElementById(`step-${key}-${direction}`)?.focus()
}

// ---- Summary -----------------------------------------------------------
const priorityLabel = computed(
  () => PRIORITIES.find((p) => p.value === draft.priority)?.label ?? String(draft.priority),
)
const route = computed(() => draft.steps.map((s) => s.nodeCode.trim() || '?').join(' → '))

// ---- Submit ------------------------------------------------------------
async function submit() {
  attempted.value = true
  requestError.value = null
  if (Object.keys(clientErrors.value).length > 0) {
    await nextTick()
    summaryPanel.value?.focus()
    return
  }

  sending.value = true
  try {
    const created = await createMission(toCreateRequest(draft))
    await router.push(`/missions/${created.id}`)
  } catch (error) {
    const problem = problemOf(error)
    const fieldErrors = fromServerErrors(problem.fieldErrors)
    serverErrors.value = fieldErrors
    requestError.value = Object.keys(fieldErrors).length > 0 ? null : problem.message
    await nextTick()
    summaryPanel.value?.focus()
  } finally {
    sending.value = false
  }
}

function discard() {
  void router.push('/missions')
}
</script>

<template>
  <div class="page">
    <nav
      class="breadcrumb"
      aria-label="Breadcrumb"
    >
      <RouterLink to="/missions">
        Missions
      </RouterLink>
      <span aria-hidden="true">/</span>
      <span aria-current="page">New mission</span>
    </nav>
    <h1 class="page__title">
      New mission
    </h1>
    <p class="page__lead">
      The mission is saved. Dispatch to KUKA FM is not connected yet.
    </p>

    <form
      class="layout"
      novalidate
      @submit.prevent="submit"
    >
      <div class="main">
        <section
          class="card"
          aria-labelledby="params-title"
        >
          <div class="card__header">
            <h2
              id="params-title"
              class="card__title"
            >
              Mission parameters
            </h2>
          </div>
          <div class="card__body grid-2">
            <div class="field">
              <label
                class="field__label"
                for="field-missionType"
              >Mission type *</label>
              <select
                id="field-missionType"
                v-model="draft.missionType"
                class="control"
                :aria-invalid="!!errorFor('missionType')"
                :aria-describedby="describedBy('missionType')"
              >
                <option
                  v-for="type in MISSION_TYPES"
                  :key="type"
                  :value="type"
                >
                  {{ type }}
                </option>
              </select>
              <p
                v-if="errorFor('missionType')"
                id="field-missionType-error"
                class="field__error"
              >
                {{ errorFor('missionType') }}
              </p>
            </div>

            <div class="field">
              <label
                class="field__label"
                for="field-priority"
              >Priority *</label>
              <select
                id="field-priority"
                v-model.number="draft.priority"
                class="control"
                :aria-invalid="!!errorFor('priority')"
                :aria-describedby="describedBy('priority')"
              >
                <option
                  v-for="p in PRIORITIES"
                  :key="p.value"
                  :value="p.value"
                >
                  {{ p.label }}
                </option>
              </select>
              <p
                v-if="errorFor('priority')"
                id="field-priority-error"
                class="field__error"
              >
                {{ errorFor('priority') }}
              </p>
            </div>

            <div class="field">
              <label
                class="field__label"
                for="field-robotId"
              >Robot</label>
              <select
                id="field-robotId"
                class="control"
                aria-describedby="field-robotId-hint"
                :aria-invalid="!!errorFor('robotId')"
              >
                <option value="">
                  Automatic (KUKA FM decides)
                </option>
              </select>
              <p
                id="field-robotId-hint"
                class="field__hint"
              >
                KUKA FM assigns a free robot.
              </p>
              <p
                v-if="errorFor('robotId')"
                id="field-robotId-error"
                class="field__error"
              >
                {{ errorFor('robotId') }}
              </p>
            </div>

            <div class="field">
              <label
                class="field__label"
                for="field-externalRef"
              >External reference</label>
              <input
                id="field-externalRef"
                v-model="draft.externalRef"
                class="control"
                type="text"
                autocomplete="off"
                placeholder="e.g. ERP order number"
                :aria-invalid="!!errorFor('externalRef')"
                :aria-describedby="describedBy('externalRef')"
              >
              <p
                v-if="errorFor('externalRef')"
                id="field-externalRef-error"
                class="field__error"
              >
                {{ errorFor('externalRef') }}
              </p>
            </div>
          </div>
        </section>

        <section
          class="card"
          aria-labelledby="steps-title"
        >
          <div class="card__header">
            <h2
              id="steps-title"
              class="card__title"
              tabindex="-1"
            >
              Target points (steps)
            </h2>
            <span class="card__meta">at least 1 step</span>
          </div>
          <div class="card__body">
            <p
              v-if="errorFor('steps')"
              id="steps-error"
              class="field__error steps-error"
            >
              {{ errorFor('steps') }}
            </p>
            <div
              class="steps-head"
              aria-hidden="true"
            >
              <span>#</span>
              <span>Node</span>
              <span>Action</span>
            </div>
            <ol class="steps">
              <li
                v-for="(step, index) in draft.steps"
                :key="step.key"
                class="step"
              >
                <span
                  class="step__number tabular"
                  aria-hidden="true"
                >{{ index + 1 }}</span>
                <div class="field step__node">
                  <label
                    class="sr-only"
                    :for="`step-${step.key}-nodeCode`"
                  >Step {{ index + 1 }} node</label>
                  <input
                    :id="`step-${step.key}-nodeCode`"
                    v-model="step.nodeCode"
                    class="control"
                    type="text"
                    autocomplete="off"
                    spellcheck="false"
                    placeholder="Enter node…"
                    :aria-invalid="!!errorFor(stepFieldKey(index, 'nodeCode'))"
                    :aria-describedby="describedBy(stepFieldKey(index, 'nodeCode'))"
                  >
                  <p
                    v-if="errorFor(stepFieldKey(index, 'nodeCode'))"
                    :id="`step-${step.key}-nodeCode-error`"
                    class="field__error"
                  >
                    {{ errorFor(stepFieldKey(index, 'nodeCode')) }}
                  </p>
                </div>
                <div class="field step__action">
                  <label
                    class="sr-only"
                    :for="`step-${step.key}-action`"
                  >Step {{ index + 1 }} action</label>
                  <select
                    :id="`step-${step.key}-action`"
                    v-model="step.action"
                    class="control"
                    :class="{ 'control--placeholder': step.action === '' }"
                    :aria-invalid="!!errorFor(stepFieldKey(index, 'action'))"
                    :aria-describedby="describedBy(stepFieldKey(index, 'action'))"
                  >
                    <option
                      value=""
                      disabled
                    >
                      Choose action
                    </option>
                    <option
                      v-for="action in STEP_ACTIONS"
                      :key="action"
                      :value="action"
                    >
                      {{ ACTION_LABELS[action] }}
                    </option>
                  </select>
                  <p
                    v-if="errorFor(stepFieldKey(index, 'action'))"
                    :id="`step-${step.key}-action-error`"
                    class="field__error"
                  >
                    {{ errorFor(stepFieldKey(index, 'action')) }}
                  </p>
                </div>
                <div class="step__tools">
                  <button
                    :id="`step-${step.key}-up`"
                    type="button"
                    class="btn btn--icon"
                    :disabled="index === 0"
                    :aria-label="`Move step ${index + 1} up`"
                    @click="move(index, -1)"
                  >
                    <AppIcon name="arrow-up" />
                  </button>
                  <button
                    :id="`step-${step.key}-down`"
                    type="button"
                    class="btn btn--icon"
                    :disabled="index === draft.steps.length - 1"
                    :aria-label="`Move step ${index + 1} down`"
                    @click="move(index, 1)"
                  >
                    <AppIcon name="arrow-down" />
                  </button>
                  <button
                    type="button"
                    class="btn btn--icon"
                    :disabled="draft.steps.length <= 1"
                    :aria-label="`Remove step ${index + 1}`"
                    @click="removeStep(index)"
                  >
                    <AppIcon name="x" />
                  </button>
                </div>
              </li>
            </ol>
            <button
              type="button"
              class="add-step"
              :aria-describedby="errorFor('steps') ? 'steps-error' : undefined"
              @click="addStep"
            >
              <AppIcon
                name="plus"
                :stroke-width="2.25"
              />
              Add step
            </button>
          </div>
        </section>
      </div>

      <aside
        class="side"
        aria-label="Summary and actions"
      >
        <section
          class="card"
          aria-labelledby="summary-title"
        >
          <div class="card__header">
            <h2
              id="summary-title"
              class="card__title"
            >
              Summary
            </h2>
          </div>
          <dl class="card__body summary">
            <div><dt>Type</dt><dd>{{ draft.missionType || '—' }}</dd></div>
            <div><dt>Priority</dt><dd>{{ priorityLabel }}</dd></div>
            <div><dt>Robot</dt><dd>Automatic</dd></div>
            <div>
              <dt>Steps</dt><dd class="tabular">
                {{ draft.steps.length }}
              </dd>
            </div>
            <div>
              <dt>Route</dt><dd class="summary__route">
                {{ route }}
              </dd>
            </div>
          </dl>
        </section>

        <div
          v-if="errorList.length > 0 || requestError"
          ref="summaryPanel"
          class="error-summary"
          tabindex="-1"
        >
          <ErrorPanel :title="requestError ? 'The mission was not sent' : 'The form contains errors'">
            <p v-if="requestError">
              {{ requestError }}
            </p>
            <ul v-if="errorList.length > 0">
              <li
                v-for="item in errorList"
                :key="item.key"
              >
                <a
                  :href="`#${fieldId(item.key)}`"
                  @click.prevent="focusField(item.key)"
                >
                  {{ item.label }}: {{ item.message }}
                </a>
              </li>
            </ul>
          </ErrorPanel>
        </div>

        <div class="form-actions">
          <button
            type="button"
            class="btn btn--large"
            @click="discard"
          >
            Discard
          </button>
          <button
            type="submit"
            class="btn btn--primary btn--large"
            :disabled="sending"
            :aria-busy="sending"
          >
            {{ sending ? 'Sending…' : 'Send mission' }}
          </button>
        </div>
        <p class="form-note">
          After sending you go straight to the mission detail.
        </p>
      </aside>
    </form>
  </div>
</template>

<style scoped>
.breadcrumb {
  display: flex;
  gap: var(--space-sm);
  padding-top: 18px;
  font: var(--text-small);
  color: var(--color-text-muted);
}

.breadcrumb a {
  color: inherit;
}

.page__title {
  margin-top: 6px;
  font: var(--text-page-title);
}

.page__lead {
  margin-top: var(--space-xs);
  color: var(--color-text-muted);
}

.layout {
  display: grid;
  grid-template-columns: minmax(0, 600px) minmax(280px, 1fr);
  gap: var(--space-xl);
  align-items: start;
  margin-top: var(--space-lg);
}

.main,
.side {
  display: grid;
  gap: 18px;
}

.grid-2 {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 20px var(--space-lg);
}

.control--placeholder {
  color: var(--color-text-muted);
  font-style: italic;
}

/* ---- Steps ------------------------------------------------------------ */
.steps-error {
  margin-bottom: var(--space-md);
}

.steps-head,
.step {
  display: grid;
  grid-template-columns: 24px minmax(0, 1.3fr) minmax(0, 1fr) auto;
  gap: var(--space-md);
}

.steps-head {
  padding-bottom: var(--space-sm);
  color: var(--color-text-muted);
  font: var(--text-table-header);
  letter-spacing: var(--tracking-table-header);
  text-transform: uppercase;
}

.steps {
  display: grid;
  gap: var(--space-md);
}

.step__number {
  height: var(--control-height);
  display: grid;
  place-items: center;
  font: var(--text-strong);
}

.step__node,
.step__action {
  gap: var(--space-xxs);
}

.step__tools {
  display: flex;
  gap: var(--space-xxs);
  height: var(--control-height);
  align-items: center;
}

.add-step {
  display: inline-flex;
  align-items: center;
  gap: var(--space-xs);
  margin-top: var(--space-lg);
  padding: var(--space-xs) var(--space-xxs);
  border: 0;
  border-radius: var(--radius-control);
  background: transparent;
  color: var(--color-primary-text);
  font: var(--text-strong);
  cursor: pointer;
}

.add-step:hover {
  text-decoration: underline;
  text-underline-offset: 3px;
}

/* ---- Summary ---------------------------------------------------------- */
.summary {
  display: grid;
  gap: var(--space-md);
}

.summary div {
  display: grid;
  grid-template-columns: 110px minmax(0, 1fr);
  gap: var(--space-md);
}

.summary dt {
  color: var(--color-text-muted);
}

.summary dd {
  font-weight: 600;
  overflow-wrap: anywhere;
}

.error-summary:focus {
  outline: none;
}

.error-summary:focus-visible {
  outline: 2px solid var(--color-focus-ring);
  outline-offset: 2px;
  border-radius: var(--radius-card);
}

.form-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-md);
}

.form-note {
  text-align: center;
  font: var(--text-small);
  color: var(--color-text-muted);
}

@media (max-width: 1099px) {
  .layout {
    grid-template-columns: minmax(0, 1fr);
    max-width: 680px;
  }
}

@media (max-width: 767px) {
  .grid-2 {
    grid-template-columns: minmax(0, 1fr);
  }

  .steps-head {
    display: none;
  }

  .step {
    grid-template-columns: 24px minmax(0, 1fr);
    padding-bottom: var(--space-md);
    border-bottom: 1px solid var(--color-divider);
  }

  .step__action,
  .step__tools {
    grid-column: 2;
  }

  .step__number {
    height: var(--touch-target);
  }

  .step__tools {
    height: auto;
    justify-content: flex-end;
  }

  .add-step {
    min-height: 44px;
  }
}
</style>
