<script setup lang="ts">
import AppIcon from './AppIcon.vue'

defineProps<{ title: string }>()
</script>

<template>
  <div
    class="error-panel"
    role="alert"
  >
    <p class="error-panel__title">
      <AppIcon
        name="alert"
        :size="16"
        :stroke-width="2"
      />
      {{ title }}
    </p>
    <div class="error-panel__body">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.error-panel {
  padding: var(--space-lg);
  border: 1px solid var(--color-danger-border);
  border-radius: var(--radius-card);
  background: var(--color-danger-surface);
  color: var(--color-danger-text);
}

.error-panel__title {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  font: 700 13px/1.4 var(--font-sans);
}

.error-panel__body {
  display: grid;
  gap: var(--space-sm);
  margin-top: var(--space-sm);
  font: 400 12.5px/1.5 var(--font-sans);
  color: var(--color-danger-text-soft);
}

.error-panel__body :deep(ul) {
  display: grid;
  gap: var(--space-xxs);
  padding-left: var(--space-lg);
  list-style: disc;
  color: var(--color-danger-text);
}

.error-panel__body :deep(a) {
  color: inherit;
}
</style>
