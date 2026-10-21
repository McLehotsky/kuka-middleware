import { readonly, ref } from 'vue'

export type ThemeChoice = 'light' | 'dark' | 'system'

const STORAGE_KEY = 'its-middleware.theme'
const choice = ref<ThemeChoice>('system')

function readStored(): ThemeChoice {
  try {
    const value = window.localStorage.getItem(STORAGE_KEY)
    return value === 'light' || value === 'dark' ? value : 'system'
  } catch {
    return 'system'
  }
}

function apply(value: ThemeChoice) {
  const root = document.documentElement
  if (value === 'system') delete root.dataset.theme
  else root.dataset.theme = value
}

/** Call once before mounting so the stored theme applies without a flash. */
export function initTheme() {
  choice.value = readStored()
  apply(choice.value)
}

export function useTheme() {
  function setTheme(value: ThemeChoice) {
    choice.value = value
    apply(value)
    try {
      if (value === 'system') window.localStorage.removeItem(STORAGE_KEY)
      else window.localStorage.setItem(STORAGE_KEY, value)
    } catch {
      // Storage blocked (private mode, policy): the choice still applies for this session.
    }
  }

  return { theme: readonly(choice), setTheme }
}
