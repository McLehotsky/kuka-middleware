// Turns an RFC 7807 ProblemDetails / ValidationProblem response into plain words for the operator.

export interface ProblemInfo {
  /** HTTP status, or null when the request never got a response (network error). */
  status: number | null
  /** One sentence the UI can show as is. */
  message: string
  /** Field errors from a ValidationProblem, keyed like the API (`steps[0].nodeCode`). */
  fieldErrors: Record<string, string[]>
}

interface ProblemBody {
  title?: unknown
  detail?: unknown
  errors?: unknown
}

const DEFAULT_MESSAGES: Record<number, string> = {
  400: 'Some values are not valid. Check the highlighted fields.',
  401: 'The request was not authorised. The API key is missing or invalid.',
  404: 'Not found. It may have been removed.',
  409: 'The action is not possible in the current state.',
  502: 'KUKA FM did not respond. Try again in a moment.',
}

export const NETWORK_ERROR_MESSAGE =
  'Cannot reach the middleware. Check the connection and try again.'

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function readFieldErrors(errors: unknown): Record<string, string[]> {
  if (!isRecord(errors)) return {}
  const result: Record<string, string[]> = {}
  for (const [key, value] of Object.entries(errors)) {
    const messages = Array.isArray(value)
      ? value.filter((m): m is string => typeof m === 'string')
      : typeof value === 'string'
        ? [value]
        : []
    if (messages.length > 0) result[key] = messages
  }
  return result
}

/** Builds the operator-facing problem for a failed response (status null = network failure). */
export function toProblemInfo(status: number | null, body: unknown): ProblemInfo {
  if (status === null) {
    return { status: null, message: NETWORK_ERROR_MESSAGE, fieldErrors: {} }
  }

  const problem: ProblemBody = isRecord(body) ? body : {}
  const fieldErrors = readFieldErrors(problem.errors)
  const detail = typeof problem.detail === 'string' && problem.detail.trim() ? problem.detail : null

  let message: string
  if (status === 409 && detail) {
    message = detail
  } else if (DEFAULT_MESSAGES[status]) {
    message = DEFAULT_MESSAGES[status]
  } else if (status >= 500) {
    message = 'The middleware ran into an error. Try again in a moment.'
  } else {
    message = detail ?? `The request failed (HTTP ${status}).`
  }

  return { status, message, fieldErrors }
}
