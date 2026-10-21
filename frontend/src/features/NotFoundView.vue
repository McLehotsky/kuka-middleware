<script setup lang="ts">
import AppIcon from '../components/AppIcon.vue'
</script>

<template>
  <div class="not-found">
    <h1 class="title">
      Page not found
    </h1>
    <p class="text">
      There is no page at this address.
    </p>
    <RouterLink
      to="/missions"
      class="btn"
    >
      <AppIcon name="arrow-left" />
      Back to missions
    </RouterLink>
  </div>
</template>

<style scoped>
.not-found {
  display: grid;
  justify-items: start;
  gap: var(--space-md);
  padding-top: 46px;
}

.title {
  font: var(--text-page-title);
}

.text {
  color: var(--color-text-muted);
}
</style>
