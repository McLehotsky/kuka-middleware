// The only place in the app that calls fetch. Relative /api URLs (proxied to the API by nginx).
// The API key header is added in phase 8 (system-definition Q8).
import { toProblemInfo, type ProblemInfo } from './problem'

export class ApiError extends Error {
  readonly problem: ProblemInfo

  constructor(problem: ProblemInfo) {
    super(problem.message)
    this.name = 'ApiError'
    this.problem = problem
  }

  get status(): number | null {
    return this.problem.status
  }
}

async function readBody(response: Response): Promise<unknown> {
  const text = await response.text()
  if (!text) return null
  try {
    return JSON.parse(text) as unknown
  } catch {
    return text
  }
}

export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...(init.body ? { 'Content-Type': 'application/json' } : {}),
        ...init.headers,
      },
    })
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') throw cause
    throw new ApiError(toProblemInfo(null, null))
  }

  const body = await readBody(response)
  if (!response.ok) throw new ApiError(toProblemInfo(response.status, body))
  return body as T
}

/** Normalises anything thrown by the client into a ProblemInfo for display. */
export function problemOf(error: unknown): ProblemInfo {
  if (error instanceof ApiError) return error.problem
  return toProblemInfo(null, null)
}
