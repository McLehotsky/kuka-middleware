<script setup lang="ts">
import { computed } from 'vue'
import type { MissionStatus, RobotStatus } from '../api/types'
import { statusTone } from '../features/missions/missionStatus'
import { isRobotStatus, robotStatusTone } from '../features/robots/robotStatus'
import AppIcon from './AppIcon.vue'

const props = withDefaults(defineProps<{ status: MissionStatus | RobotStatus; size?: 'md' | 'sm' }>(), {
  size: 'md',
})

// Robot states reuse the mission palette (DESIGN.md → Status pill palette).
const tone = computed(() =>
  isRobotStatus(props.status) ? robotStatusTone(props.status) : statusTone(props.status),
)
</script>

<template>
  <span
    class="pill"
    :class="[`pill--${tone}`, `pill--${size}`]"
  >
    <AppIcon
      v-if="tone === 'cancelled'"
      name="x"
      :size="size === 'sm' ? 10 : 11"
      :stroke-width="2.25"
    />
    {{ status }}
  </span>
</template>

<style scoped>
.pill {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  height: 20px;
  padding: 0 9px;
  border: 1px solid transparent;
  border-radius: var(--radius-pill);
  font: var(--text-pill);
  white-space: nowrap;
  transition-property: background-color, color, border-color;
  transition-duration: var(--duration-base);
  transition-timing-function: var(--ease-standard);
}

.pill--sm {
  height: 18px;
  padding: 0 7px;
  font: var(--text-pill-sm);
}

.pill--created {
  background: var(--status-created-bg);
  color: var(--status-created-fg);
}
.pill--submitted {
  background: var(--status-submitted-bg);
  color: var(--status-submitted-fg);
}
.pill--inprogress {
  background: var(--status-inprogress-bg);
  color: var(--status-inprogress-fg);
}
.pill--cancelling {
  background: var(--status-cancelling-bg);
  color: var(--status-cancelling-fg);
}
.pill--completed {
  background: var(--status-completed-bg);
  color: var(--status-completed-fg);
}
.pill--cancelled {
  padding-left: 7px;
  border-color: var(--status-cancelled-border);
  background: var(--status-cancelled-bg);
  color: var(--status-cancelled-fg);
}
.pill--failed {
  background: var(--status-failed-bg);
  color: var(--status-failed-fg);
}
</style>
