<script setup lang="ts">
import { computed } from 'vue'
import { useTheme, type ThemeChoice } from '../composables/useTheme'
import AppIcon, { type IconName } from './AppIcon.vue'

const { theme, setTheme } = useTheme()

const OPTIONS: readonly { value: ThemeChoice; label: string; icon: IconName }[] = [
  { value: 'light', label: 'Light theme', icon: 'sun' },
  { value: 'dark', label: 'Dark theme', icon: 'moon' },
  { value: 'system', label: 'System theme', icon: 'monitor' },
]

// Below 768 px the three options collapse into one 44 px button that cycles through them.
const currentIndex = computed(() => Math.max(0, OPTIONS.findIndex((o) => o.value === theme.value)))
const current = computed(() => OPTIONS[currentIndex.value]!)
const next = computed(() => OPTIONS[(currentIndex.value + 1) % OPTIONS.length]!)
</script>

<template>
  <div
    class="theme-toggle"
    role="group"
    aria-label="Colour theme"
  >
    <button
      v-for="option in OPTIONS"
      :key="option.value"
      type="button"
      class="theme-toggle__option"
      :aria-pressed="theme === option.value"
      :aria-label="option.label"
      :title="option.label"
      @click="setTheme(option.value)"
    >
      <AppIcon
        :name="option.icon"
        :size="15"
      />
    </button>
  </div>
  <button
    type="button"
    class="theme-cycle"
    :aria-label="`${current.label}. Switch to ${next.label.toLowerCase()}`"
    :title="current.label"
    @click="setTheme(next.value)"
  >
    <AppIcon
      :name="current.icon"
      :size="18"
    />
  </button>
</template>

<style scoped>
.theme-toggle {
  display: inline-flex;
  gap: 2px;
  padding: 2px;
  border-radius: var(--radius-pill);
  background: var(--color-topbar-chip);
}

.theme-toggle__option {
  position: relative;
  display: grid;
  place-items: center;
  width: 26px;
  height: 22px;
  padding: 0;
  border: 0;
  border-radius: var(--radius-pill);
  background: transparent;
  color: var(--color-topbar-subtitle);
  cursor: pointer;
  transition-property: background-color, color;
  transition-duration: var(--duration-fast);
  transition-timing-function: var(--ease-standard);
}

/* Desktop: extends the hit area to 40 px tall without changing the look. */
.theme-toggle__option::after {
  content: '';
  position: absolute;
  inset: -9px 0;
}

.theme-toggle__option:hover {
  color: var(--color-on-primary);
}

.theme-toggle__option[aria-pressed='true'] {
  background: var(--color-on-primary);
  color: var(--color-topbar);
}

.theme-toggle__option:focus-visible,
.theme-cycle:focus-visible {
  outline-color: var(--color-focus-ring-on-dark);
}

.theme-cycle {
  display: none;
  place-items: center;
  width: var(--touch-target);
  height: var(--touch-target);
  margin-right: -10px;
  padding: 0;
  border: 0;
  border-radius: var(--radius-pill);
  background: transparent;
  color: var(--color-on-primary);
  cursor: pointer;
}

@media (max-width: 767px) {
  .theme-toggle {
    display: none;
  }

  .theme-cycle {
    display: grid;
  }
}
</style>
