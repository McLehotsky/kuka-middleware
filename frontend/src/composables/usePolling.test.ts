import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, ref } from 'vue'
import { usePolling } from './usePolling'

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

describe('usePolling', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('fetches immediately and then every interval after the previous request settled', async () => {
    const fetcher = vi.fn().mockResolvedValue('data')
    const scope = effectScope()
    const polling = scope.run(() => usePolling(fetcher, { intervalMs: 5000 }))!

    await vi.advanceTimersByTimeAsync(0)
    expect(fetcher).toHaveBeenCalledTimes(1)
    expect(polling.data.value).toBe('data')
    expect(polling.loading.value).toBe(false)
    expect(polling.lastUpdated.value).toBeInstanceOf(Date)

    await vi.advanceTimersByTimeAsync(4999)
    expect(fetcher).toHaveBeenCalledTimes(1)
    await vi.advanceTimersByTimeAsync(1)
    expect(fetcher).toHaveBeenCalledTimes(2)

    scope.stop()
  })

  it('never starts a request while one is still in flight', async () => {
    const pending = deferred<string>()
    const fetcher = vi.fn().mockReturnValueOnce(pending.promise).mockResolvedValue('next')
    const scope = effectScope()
    const polling = scope.run(() => usePolling(fetcher, { intervalMs: 1000 }))!

    await vi.advanceTimersByTimeAsync(10_000)
    expect(fetcher).toHaveBeenCalledTimes(1)

    void polling.refresh() // joins the request in flight
    expect(fetcher).toHaveBeenCalledTimes(1)

    pending.resolve('first')
    await vi.advanceTimersByTimeAsync(0)
    expect(polling.data.value).toBe('first')
    await vi.advanceTimersByTimeAsync(1000)
    expect(fetcher).toHaveBeenCalledTimes(2)

    scope.stop()
  })

  it('stops polling and aborts the request in flight when its scope is disposed', async () => {
    const signals: AbortSignal[] = []
    const fetcher = vi.fn((signal: AbortSignal) => {
      signals.push(signal)
      return new Promise<string>(() => {})
    })
    const scope = effectScope()
    scope.run(() => usePolling(fetcher, { intervalMs: 1000 }))

    await vi.advanceTimersByTimeAsync(0)
    scope.stop()
    expect(signals[0]!.aborted).toBe(true)

    await vi.advanceTimersByTimeAsync(10_000)
    expect(fetcher).toHaveBeenCalledTimes(1)
  })

  it('keeps the last data on error, exposes the error and clears it after the next success', async () => {
    const failure = new Error('down')
    const fetcher = vi
      .fn()
      .mockResolvedValueOnce('good')
      .mockRejectedValueOnce(failure)
      .mockResolvedValueOnce('better')
    const scope = effectScope()
    const polling = scope.run(() => usePolling(fetcher, { intervalMs: 1000 }))!

    await vi.advanceTimersByTimeAsync(0)
    await vi.advanceTimersByTimeAsync(1000)
    expect(polling.error.value).toBe(failure)
    expect(polling.data.value).toBe('good')

    await vi.advanceTimersByTimeAsync(1000)
    expect(polling.error.value).toBeNull()
    expect(polling.data.value).toBe('better')

    scope.stop()
  })

  it('pauses while disabled and resumes when enabled again', async () => {
    const enabled = ref(false)
    const fetcher = vi.fn().mockResolvedValue('x')
    const scope = effectScope()
    scope.run(() => usePolling(fetcher, { intervalMs: 1000, enabled }))

    await vi.advanceTimersByTimeAsync(5000)
    expect(fetcher).toHaveBeenCalledTimes(1) // the first load always runs

    enabled.value = true
    await nextTick()
    await vi.advanceTimersByTimeAsync(1000)
    expect(fetcher).toHaveBeenCalledTimes(2)

    scope.stop()
  })
})
