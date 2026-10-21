import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, ref, type EffectScope } from 'vue'
import { usePolling, type PollingOptions } from './usePolling'

function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>((res) => {
    resolve = res
  })
  return { promise, resolve }
}

describe('usePolling', () => {
  let scope: EffectScope

  function start<T>(fetcher: (signal: AbortSignal) => Promise<T>, options: PollingOptions = {}) {
    return scope.run(() => usePolling(fetcher, { intervalMs: 1000, ...options }))!
  }

  beforeEach(() => {
    vi.useFakeTimers()
    scope = effectScope()
  })

  afterEach(() => {
    scope.stop()
    vi.useRealTimers()
  })

  it('fetches immediately on start', async () => {
    const fetcher = vi.fn().mockResolvedValue('data')
    start(fetcher)
    await vi.advanceTimersByTimeAsync(0)
    expect(fetcher).toHaveBeenCalledTimes(1)
  })

  it('exposes the data, clears loading and records lastUpdated after the first success', async () => {
    const polling = start(vi.fn().mockResolvedValue('data'))
    await vi.advanceTimersByTimeAsync(0)
    expect({
      data: polling.data.value,
      loading: polling.loading.value,
      updated: polling.lastUpdated.value instanceof Date,
    }).toEqual({ data: 'data', loading: false, updated: true })
  })

  it('waits the full interval before the next request', async () => {
    const fetcher = vi.fn().mockResolvedValue('data')
    start(fetcher, { intervalMs: 5000 })
    await vi.advanceTimersByTimeAsync(4999)
    expect(fetcher).toHaveBeenCalledTimes(1)
  })

  it('starts the next request once the interval has passed', async () => {
    const fetcher = vi.fn().mockResolvedValue('data')
    start(fetcher, { intervalMs: 5000 })
    await vi.advanceTimersByTimeAsync(5000)
    expect(fetcher).toHaveBeenCalledTimes(2)
  })

  it('never starts a timed request while one is still in flight', async () => {
    const fetcher = vi.fn().mockReturnValue(new Promise(() => {}))
    start(fetcher)
    await vi.advanceTimersByTimeAsync(10_000)
    expect(fetcher).toHaveBeenCalledTimes(1)
  })

  it('does not start a parallel request when refresh is called during one', async () => {
    const fetcher = vi.fn().mockReturnValue(new Promise(() => {}))
    const polling = start(fetcher)
    await vi.advanceTimersByTimeAsync(0)
    void polling.refresh()
    expect(fetcher).toHaveBeenCalledTimes(1)
  })

  it('runs one fresh request right after the in-flight one when refresh is called during it', async () => {
    const pending = deferred<string>()
    const fetcher = vi.fn().mockReturnValueOnce(pending.promise).mockResolvedValue('fresh')
    const polling = start(fetcher, { intervalMs: 5000 })
    await vi.advanceTimersByTimeAsync(0)
    const refreshed = polling.refresh()
    void polling.refresh() // a second call shares the queued request
    pending.resolve('stale')
    await refreshed
    expect({ calls: fetcher.mock.calls.length, data: polling.data.value }).toEqual({ calls: 2, data: 'fresh' })
  })

  it('schedules the next request only after a slow request settled', async () => {
    const pending = deferred<string>()
    const fetcher = vi.fn().mockReturnValueOnce(pending.promise).mockResolvedValue('next')
    start(fetcher)
    await vi.advanceTimersByTimeAsync(3000)
    pending.resolve('first')
    await vi.advanceTimersByTimeAsync(1000)
    expect(fetcher).toHaveBeenCalledTimes(2)
  })

  it('aborts the request in flight when its scope is disposed', async () => {
    const signals: AbortSignal[] = []
    start((signal) => {
      signals.push(signal)
      return new Promise<string>(() => {})
    })
    await vi.advanceTimersByTimeAsync(0)
    scope.stop()
    expect(signals[0]!.aborted).toBe(true)
  })

  it('makes no further requests after its scope is disposed', async () => {
    const fetcher = vi.fn().mockResolvedValue('data')
    start(fetcher)
    await vi.advanceTimersByTimeAsync(0)
    scope.stop()
    await vi.advanceTimersByTimeAsync(10_000)
    expect(fetcher).toHaveBeenCalledTimes(1)
  })

  it('keeps the last data and exposes the error when a request fails', async () => {
    const failure = new Error('down')
    const polling = start(vi.fn().mockResolvedValueOnce('good').mockRejectedValueOnce(failure))
    await vi.advanceTimersByTimeAsync(1000)
    expect({ data: polling.data.value, error: polling.error.value }).toEqual({ data: 'good', error: failure })
  })

  it('clears the error after the next success', async () => {
    const polling = start(
      vi.fn().mockResolvedValueOnce('good').mockRejectedValueOnce(new Error('down')).mockResolvedValue('better'),
    )
    await vi.advanceTimersByTimeAsync(2000)
    expect(polling.error.value).toBeNull()
  })

  it('does not schedule more requests while disabled', async () => {
    const fetcher = vi.fn().mockResolvedValue('x')
    start(fetcher, { enabled: ref(false) })
    await vi.advanceTimersByTimeAsync(5000)
    expect(fetcher).toHaveBeenCalledTimes(1) // only the first load
  })

  it('resumes polling when enabled again', async () => {
    const enabled = ref(false)
    const fetcher = vi.fn().mockResolvedValue('x')
    start(fetcher, { enabled })
    await vi.advanceTimersByTimeAsync(0)
    enabled.value = true
    await nextTick()
    await vi.advanceTimersByTimeAsync(1000)
    expect(fetcher).toHaveBeenCalledTimes(2)
  })
})
