// Display formatting for mission data. Missing values render as an em dash.
export const EMPTY = '—'

const dateTime = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
})

const shortDateTime = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
})

const timeOnly = new Intl.DateTimeFormat('en-GB', {
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
})

function parse(value: string | null | undefined): Date | null {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

export function formatDateTime(value: string | null | undefined): string {
  const d = parse(value)
  return d ? dateTime.format(d) : EMPTY
}

export function formatShortDateTime(value: string | null | undefined): string {
  const d = parse(value)
  return d ? shortDateTime.format(d) : EMPTY
}

export function formatTime(value: string | Date | null | undefined): string {
  const d = value instanceof Date ? value : parse(value)
  return d ? timeOnly.format(d) : EMPTY
}

/** "2 s ago", "4 min ago", "3 h ago"; falls back to the date after a day. */
export function formatRelative(value: string | null | undefined, now: Date): string {
  const d = parse(value)
  if (!d) return EMPTY
  const seconds = Math.max(0, Math.round((now.getTime() - d.getTime()) / 1000))
  if (seconds < 60) return `${seconds} s ago`
  const minutes = Math.round(seconds / 60)
  if (minutes < 60) return `${minutes} min ago`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `${hours} h ago`
  return formatShortDateTime(value)
}

export function orEmpty(value: string | number | null | undefined): string {
  return value === null || value === undefined || value === '' ? EMPTY : String(value)
}
