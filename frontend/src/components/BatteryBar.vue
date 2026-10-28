<script setup lang="ts">
import { computed } from 'vue'
import type { RobotStatus } from '../api/types'
import { EMPTY } from '../features/missions/format'
import { batteryTone } from '../features/robots/robotStatus'

// DESIGN.md → Battery bar: coloured by level, greyed out when offline, always with the percentage.
const props = defineProps<{ level: number | null; status: RobotStatus }>()

const tone = computed(() => batteryTone(props.level, props.status))
const percent = computed(() =>
  props.level === null ? null : Math.min(100, Math.max(0, Math.round(props.level))),
)
</script>

<template>
  <span class="battery">
    <span
      v-if="percent !== null"
      class="battery__track"
      role="meter"
      aria-label="Battery"
      aria-valuemin="0"
      aria-valuemax="100"
      :aria-valuenow="percent"
      :aria-valuetext="`${percent} %${status === 'Offline' ? ', robot offline' : ''}`"
    >
      <span
        class="battery__fill"
        :class="`battery__fill--${tone}`"
        :style="{ width: `${percent}%` }"
      />
    </span>
    <span
      class="battery__value tabular"
      :class="{ 'battery__value--empty': percent === null }"
      :aria-hidden="percent !== null"
    >
      <template v-if="percent !== null">{{ percent }} %</template>
      <template v-else>
        <span aria-hidden="true">{{ EMPTY }}</span>
        <span class="sr-only">Battery level unknown</span>
      </template>
    </span>
  </span>
</template>

<style scoped>
.battery {
  display: flex;
  align-items: center;
  gap: var(--space-md);
  min-width: 0;
}

.battery__track {
  flex: 1;
  height: 12px;
  border-radius: var(--radius-card);
  background: var(--color-battery-track);
  overflow: hidden;
}

.battery__fill {
  display: block;
  height: 100%;
  border-radius: var(--radius-card);
  transition: background-color var(--duration-base) var(--ease-standard);
}

.battery__fill--high {
  background: var(--color-battery-high);
}
.battery__fill--mid {
  background: var(--color-battery-mid);
}
.battery__fill--low {
  background: var(--color-battery-low);
}
.battery__fill--offline {
  background: var(--color-battery-offline);
}

.battery__value {
  flex: none;
  min-width: 4ch;
  font: var(--text-strong);
  text-align: right;
}

.battery__value--empty {
  flex: 1;
  color: var(--color-text-muted);
  font-weight: 400;
  text-align: left;
}
</style>
