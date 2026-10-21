<script setup lang="ts">
import { useLiveStatus } from '../composables/useLiveStatus'

const { failing } = useLiveStatus()
</script>

<template>
  <span
    class="live"
    :class="{ 'live--paused': failing }"
    role="status"
  >
    <span
      class="live__dot"
      aria-hidden="true"
    />
    <span v-if="failing">Paused · retrying</span>
    <span v-else>Live<span class="live__interval"> · refresh 5 s</span></span>
  </span>
</template>

<style scoped>
.live {
  display: inline-flex;
  align-items: center;
  gap: var(--space-sm);
  height: 26px;
  padding: 0 14px 0 10px;
  border: 1px solid var(--color-live-border);
  border-radius: var(--radius-pill);
  background: var(--color-live-surface);
  color: var(--color-success-text);
  font: var(--text-small);
  white-space: nowrap;
  transition-property: background-color, color, border-color;
  transition-duration: var(--duration-base);
  transition-timing-function: var(--ease-standard);
}

.live__dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--color-success);
}

.live--paused {
  border-color: var(--status-cancelling-fg);
  background: var(--status-cancelling-bg);
  color: var(--status-cancelling-fg);
}

.live--paused .live__dot {
  background: var(--status-cancelling-fg);
}

@media (max-width: 767px) {
  .live__interval {
    display: none;
  }
}
</style>
