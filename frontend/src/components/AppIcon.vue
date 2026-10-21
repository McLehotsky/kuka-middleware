<script setup lang="ts">
// One outline icon set (24 × 24, currentColor, round caps), authored inline – no icon dependency.
export type IconName =
  | 'list'
  | 'plus'
  | 'robot'
  | 'menu'
  | 'refresh'
  | 'x'
  | 'arrow-up'
  | 'arrow-down'
  | 'trash'
  | 'alert'
  | 'check'
  | 'sun'
  | 'moon'
  | 'monitor'
  | 'chevron-right'
  | 'arrow-left'

withDefaults(defineProps<{ name: IconName; size?: number; strokeWidth?: number }>(), {
  size: 16,
  strokeWidth: 1.75,
})

const PATHS: Record<IconName, string[]> = {
  list: ['M8 6h12', 'M8 12h12', 'M8 18h12', 'M4 6h.01', 'M4 12h.01', 'M4 18h.01'],
  plus: ['M12 5v14', 'M5 12h14'],
  robot: [
    'M6 9a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2z',
    'M12 7V4',
    'M9.5 12h.01',
    'M14.5 12h.01',
    'M3 12v2',
    'M21 12v2',
  ],
  menu: ['M4 7h16', 'M4 12h16', 'M4 17h16'],
  refresh: ['M20 11a8 8 0 1 0-2.3 5.7', 'M20 5v6h-6'],
  x: ['M6 6l12 12', 'M18 6 6 18'],
  'arrow-up': ['M12 19V5', 'M6 11l6-6 6 6'],
  'arrow-down': ['M12 5v14', 'M18 13l-6 6-6-6'],
  trash: ['M4 7h16', 'M10 11v6', 'M14 11v6', 'M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12', 'M9 7V4h6v3'],
  alert: ['M12 4 2.5 20h19z', 'M12 10v4', 'M12 17h.01'],
  check: ['M5 12.5 10 17l9-10'],
  sun: [
    'M12 16a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
    'M12 2v2',
    'M12 20v2',
    'M4.9 4.9l1.4 1.4',
    'M17.7 17.7l1.4 1.4',
    'M2 12h2',
    'M20 12h2',
    'M4.9 19.1l1.4-1.4',
    'M17.7 6.3l1.4-1.4',
  ],
  moon: ['M20 14.5A8 8 0 0 1 9.5 4 8 8 0 1 0 20 14.5z'],
  monitor: ['M3 5h18v11H3z', 'M8 20h8', 'M12 16v4'],
  'chevron-right': ['M9 6l6 6-6 6'],
  'arrow-left': ['M19 12H5', 'M11 6l-6 6 6 6'],
}
</script>

<template>
  <svg
    class="icon"
    :width="size"
    :height="size"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    :stroke-width="strokeWidth"
    stroke-linecap="round"
    stroke-linejoin="round"
    aria-hidden="true"
    focusable="false"
  >
    <path
      v-for="d in PATHS[name]"
      :key="d"
      :d="d"
    />
  </svg>
</template>

<style scoped>
.icon {
  flex: none;
  display: block;
}
</style>
