import {
  getCurrentScope,
  onScopeDispose,
  ref,
  shallowRef,
  toValue,
  watch,
  type MaybeRefOrGetter,
  type Ref,
  type ShallowRef,
} from 'vue'

export interface PollingOptions {
  /** Pause between the end of one request and the start of the next (default 5 s, Q2). */
  intervalMs?: number
  /** While false, no further requests are scheduled (e.g. a mission reached a final state). */
  enabled?: MaybeRefOrGetter<boolean>
  /** Start right away (default true). */
  immediate?: boolean
}

export interface Polling<T> {
  data: ShallowRef<T | null>
  /** Error of the last request; cleared by the next success. Data from before stays. */
  error: ShallowRef<unknown>
  /** True until the first request settles. */
  loading: Ref<boolean>
  /** True while any request is in flight. */
  refreshing: Ref<boolean>
  lastUpdated: Ref<Date | null>
  /** Runs a request now (or right after the one in flight) and restarts the interval. */
  refresh: () => Promise<void>
  start: () => void
  stop: () => void
}

/**
 * Polls `fetcher` every `intervalMs`. The next request is scheduled only after the previous one
 * settled, so requests never overlap. Stops (and aborts the request in flight) when the calling
 * component or effect scope is disposed.
 */
export function usePolling<T>(
  fetcher: (signal: AbortSignal) => Promise<T>,
  options: PollingOptions = {},
): Polling<T> {
  const intervalMs = options.intervalMs ?? 5000
  const enabled = options.enabled ?? true

  const data = shallowRef<T | null>(null)
  const error = shallowRef<unknown>(null)
  const loading = ref(true)
  const refreshing = ref(false)
  const lastUpdated = ref<Date | null>(null)

  let running = false
  let timer: ReturnType<typeof setTimeout> | undefined
  let inFlight: Promise<void> | null = null
  let controller: AbortController | null = null

  function clearTimer() {
    if (timer !== undefined) {
      clearTimeout(timer)
      timer = undefined
    }
  }

  function schedule() {
    clearTimer()
    if (running && toValue(enabled)) {
      timer = setTimeout(() => void tick(), intervalMs)
    }
  }

  function tick(): Promise<void> {
    if (inFlight) return inFlight
    clearTimer()
    const current = new AbortController()
    controller = current
    refreshing.value = true

    const request = (async () => {
      try {
        const result = await fetcher(current.signal)
        if (current.signal.aborted) return
        data.value = result
        error.value = null
        lastUpdated.value = new Date()
      } catch (cause) {
        if (current.signal.aborted) return
        error.value = cause
      } finally {
        // Only the request that is still current may reset shared state (stop() may have replaced it).
        if (controller === current) {
          controller = null
          inFlight = null
          refreshing.value = false
          loading.value = false
          schedule()
        }
      }
    })()
    if (controller === current) inFlight = request
    return request
  }

  function start() {
    running = true
    if (!inFlight) void tick()
  }

  function stop() {
    running = false
    clearTimer()
    controller?.abort()
    controller = null
    inFlight = null
    refreshing.value = false
  }

  let queued: Promise<void> | null = null

  /**
   * Fresh data now. If a request is in flight it may have started before the caller changed
   * something (e.g. a cancel), so one new request is queued right after it – never in parallel.
   */
  function refresh(): Promise<void> {
    if (!running) running = true
    if (!inFlight) return tick()
    if (!queued) {
      queued = inFlight.then(() => {
        queued = null
        return running ? tick() : undefined
      })
    }
    return queued
  }

  watch(
    () => toValue(enabled),
    (isEnabled) => {
      if (!isEnabled) clearTimer()
      else if (running && !inFlight) schedule()
    },
  )

  if (getCurrentScope()) onScopeDispose(stop)
  if (options.immediate ?? true) start()

  return { data, error, loading, refreshing, lastUpdated, refresh, start, stop }
}
