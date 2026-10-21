import { getCurrentScope, onScopeDispose, readonly, ref, watch, type Ref } from 'vue'

// App-wide "Live · refresh 5 s" state shown in the top bar. Screens that poll report their last
// request error here; the top bar switches to "Paused · retrying" while one is failing.
const failing = ref(false)

export function useLiveStatus() {
  return { failing: readonly(failing) }
}

/** Mirrors a polling error ref into the top-bar Live indicator; resets when the screen unmounts. */
export function reportPollingError(error: Readonly<Ref<unknown>>) {
  watch(error, (value) => (failing.value = value !== null && value !== undefined), {
    immediate: true,
  })
  if (getCurrentScope()) onScopeDispose(() => (failing.value = false))
}
