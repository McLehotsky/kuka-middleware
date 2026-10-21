<script setup lang="ts">
import { nextTick, ref, useTemplateRef, watch } from 'vue'
import { problemOf } from '../../api/http'
import { cancelMission } from '../../api/missions'
import type { MissionSummary } from '../../api/types'
import AppIcon from '../../components/AppIcon.vue'

const open = defineModel<boolean>('open', { required: true })
const props = defineProps<{ mission: Pick<MissionSummary, 'id' | 'missionCode'> | null }>()
const emit = defineEmits<{
  /** Fired whenever the dialog closes; `cancelled` is the updated mission after a successful cancel. */
  closed: [cancelled: MissionSummary | null]
}>()

const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const keepButton = useTemplateRef<HTMLButtonElement>('keepButton')
const busy = ref(false)
const errorMessage = ref<string | null>(null)
let result: MissionSummary | null = null
let returnFocusTo: HTMLElement | null = null

watch(open, async (isOpen) => {
  const el = dialog.value
  if (!el) return
  if (isOpen && !el.open) {
    returnFocusTo = document.activeElement instanceof HTMLElement ? document.activeElement : null
    errorMessage.value = null
    result = null
    el.showModal()
    await nextTick()
    keepButton.value?.focus()
  } else if (!isOpen && el.open) {
    el.close()
  }
})

function onNativeClose() {
  open.value = false
  busy.value = false
  emit('closed', result)
  returnFocusTo?.focus()
  returnFocusTo = null
}

function onCancelEvent(event: Event) {
  // Escape while the request runs would hide its outcome; wait for it instead.
  if (busy.value) event.preventDefault()
}

async function confirm() {
  if (!props.mission || busy.value) return
  busy.value = true
  errorMessage.value = null
  try {
    result = await cancelMission(props.mission.id)
    open.value = false
  } catch (error) {
    const problem = problemOf(error)
    errorMessage.value =
      problem.status === 404 ? 'This mission no longer exists. It may have been removed.' : problem.message
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <dialog
    ref="dialog"
    class="dialog"
    aria-labelledby="cancel-dialog-title"
    aria-describedby="cancel-dialog-body"
    @close="onNativeClose"
    @cancel="onCancelEvent"
  >
    <div class="dialog__header">
      <span
        class="dialog__badge"
        aria-hidden="true"
      >
        <AppIcon
          name="alert"
          :size="16"
          :stroke-width="2.25"
        />
      </span>
      <h2
        id="cancel-dialog-title"
        class="dialog__title"
      >
        Cancel mission {{ mission?.missionCode }}?
      </h2>
    </div>
    <p
      id="cancel-dialog-body"
      class="dialog__body"
    >
      The mission moves to <strong>Cancelling</strong>. It becomes <strong>Cancelled</strong> only
      once the next synchronisation with KUKA FM confirms it.
    </p>
    <p
      v-if="errorMessage"
      class="dialog__error"
      role="alert"
    >
      <AppIcon
        name="alert"
        :size="14"
        :stroke-width="2"
      />
      {{ errorMessage }}
    </p>
    <div class="dialog__actions">
      <button
        ref="keepButton"
        type="button"
        class="btn"
        :disabled="busy"
        @click="open = false"
      >
        Keep mission
      </button>
      <button
        type="button"
        class="btn btn--danger-solid"
        :disabled="busy"
        :aria-busy="busy"
        @click="confirm"
      >
        {{ busy ? 'Cancelling…' : 'Yes, cancel mission' }}
      </button>
    </div>
  </dialog>
</template>

<style scoped>
.dialog {
  width: min(460px, calc(100vw - 2 * var(--space-lg)));
  max-width: none;
  padding: var(--space-xl);
  border: 1px solid transparent;
  border-radius: var(--radius-dialog);
  background: var(--color-surface);
  color: var(--color-text);
  box-shadow: var(--shadow-dialog);
  opacity: 0;
  scale: 0.98;
  transition-property: opacity, scale, overlay, display;
  transition-duration: var(--duration-exit);
  transition-timing-function: var(--ease-exit);
  transition-behavior: allow-discrete;
}

.dialog[open] {
  opacity: 1;
  scale: 1;
  transition-duration: var(--duration-slow);
  transition-timing-function: var(--ease-standard);
}

@starting-style {
  .dialog[open] {
    opacity: 0;
    scale: 0.98;
  }
}

.dialog::backdrop {
  background: var(--color-scrim);
  opacity: 0;
  transition-property: opacity, overlay, display;
  transition-duration: var(--duration-exit);
  transition-behavior: allow-discrete;
}

.dialog[open]::backdrop {
  opacity: 1;
  transition-duration: var(--duration-slow);
}

@starting-style {
  .dialog[open]::backdrop {
    opacity: 0;
  }
}

:root[data-theme='dark'] .dialog {
  border-color: var(--color-border);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .dialog {
    border-color: var(--color-border);
  }
}

@media (prefers-reduced-motion: reduce) {
  .dialog,
  .dialog[open] {
    scale: none;
  }
}

.dialog__header {
  display: flex;
  align-items: center;
  gap: var(--space-md);
}

.dialog__badge {
  display: grid;
  place-items: center;
  flex: none;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: var(--color-danger-surface);
  color: var(--color-danger);
}

.dialog__title {
  font: var(--text-dialog-title);
  overflow-wrap: anywhere;
}

.dialog__body {
  margin-top: var(--space-lg);
  text-wrap: pretty;
}

.dialog__error {
  display: flex;
  align-items: flex-start;
  gap: var(--space-sm);
  margin-top: var(--space-lg);
  padding: var(--space-md);
  border: 1px solid var(--color-danger-border);
  border-radius: var(--radius-card);
  background: var(--color-danger-surface);
  color: var(--color-danger-text);
  font: var(--text-body-small);
}

.dialog__error :deep(svg) {
  margin-top: 2px;
}

.dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-md);
  margin-top: var(--space-xl);
}

@media (max-width: 767px) {
  .dialog__actions {
    flex-direction: column-reverse;
  }

  .dialog__actions .btn {
    width: 100%;
  }
}
</style>
