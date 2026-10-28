<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, useTemplateRef, watch } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from './AppIcon.vue'
import LiveIndicator from './LiveIndicator.vue'
import ThemeToggle from './ThemeToggle.vue'

const route = useRoute()

// Below 768 px the sidebar becomes an off-canvas drawer (DESIGN.md → Layout → Responsive).
const mobileQuery = typeof window !== 'undefined' ? window.matchMedia('(max-width: 767px)') : null
const isMobile = ref(mobileQuery?.matches ?? false)
const drawerOpen = ref(false)
const menuButton = useTemplateRef<HTMLButtonElement>('menuButton')
const nav = useTemplateRef<HTMLElement>('nav')

function onMediaChange(event: MediaQueryListEvent) {
  isMobile.value = event.matches
  if (!event.matches) drawerOpen.value = false
}

async function openDrawer() {
  drawerOpen.value = true
  await nextTick()
  nav.value?.querySelector<HTMLElement>('a[href]')?.focus()
}

async function closeDrawer(returnFocus = true) {
  if (!drawerOpen.value) return
  drawerOpen.value = false
  await nextTick() // main is no longer inert after the re-render
  if (returnFocus) menuButton.value?.focus()
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape' && drawerOpen.value) {
    event.preventDefault()
    void closeDrawer()
  }
}

watch(
  () => route.fullPath,
  () => void closeDrawer(false),
)

onMounted(() => {
  mobileQuery?.addEventListener('change', onMediaChange)
  document.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => {
  mobileQuery?.removeEventListener('change', onMediaChange)
  document.removeEventListener('keydown', onKeydown)
})

function isMissionsSection() {
  return route.name === 'missions' || route.name === 'mission-detail'
}
</script>

<template>
  <a
    class="skip-link"
    href="#main"
  >Skip to content</a>
  <header class="topbar">
    <button
      ref="menuButton"
      type="button"
      class="topbar__menu"
      aria-controls="app-nav"
      :aria-expanded="drawerOpen"
      :aria-label="drawerOpen ? 'Close navigation' : 'Open navigation'"
      @click="drawerOpen ? closeDrawer() : openDrawer()"
    >
      <AppIcon
        name="menu"
        :size="20"
      />
    </button>
    <RouterLink
      to="/missions"
      class="topbar__brand"
    >
      <span
        class="topbar__mark"
        aria-hidden="true"
      >ITS</span>
      <span class="topbar__name">ITS Middleware</span>
    </RouterLink>
    <span class="topbar__subtitle">KUKA AMR Fleet Manager</span>
    <div class="topbar__end">
      <LiveIndicator />
      <ThemeToggle />
    </div>
  </header>

  <div class="layout">
    <nav
      id="app-nav"
      ref="nav"
      class="sidebar"
      :class="{ 'sidebar--open': drawerOpen }"
      aria-label="Main"
    >
      <p
        class="sidebar__section"
        aria-hidden="true"
      >
        Navigation
      </p>
      <ul class="sidebar__list">
        <li>
          <RouterLink
            to="/missions"
            class="nav-item"
            :class="{ 'nav-item--active': isMissionsSection() }"
            :aria-current="isMissionsSection() ? 'page' : undefined"
          >
            <AppIcon name="list" />
            Missions
          </RouterLink>
        </li>
        <li>
          <RouterLink
            to="/missions/new"
            class="nav-item"
            active-class=""
            exact-active-class="nav-item--active"
          >
            <AppIcon name="plus" />
            New mission
          </RouterLink>
        </li>
        <li>
          <RouterLink
            to="/robots"
            class="nav-item"
            active-class="nav-item--active"
          >
            <AppIcon name="robot" />
            Robots
          </RouterLink>
        </li>
      </ul>
      <p class="sidebar__version">
        v0.1
      </p>
    </nav>
    <button
      v-if="drawerOpen"
      type="button"
      class="scrim"
      tabindex="-1"
      aria-label="Close navigation"
      @click="closeDrawer()"
    />

    <main
      id="main"
      class="content"
      tabindex="-1"
      :inert="isMobile && drawerOpen"
    >
      <RouterView :key="route.path" />
    </main>
  </div>
</template>

<style scoped>
.skip-link {
  position: absolute;
  top: -100px;
  left: var(--space-sm);
  z-index: 100;
  padding: var(--space-sm) var(--space-md);
  border-radius: var(--radius-button);
  background: var(--color-surface);
  color: var(--color-primary-text);
  font: var(--text-button);
}

.skip-link:focus {
  top: var(--space-sm);
}

/* ---- Top bar ---------------------------------------------------------- */
.topbar {
  position: sticky;
  top: 0;
  z-index: 20;
  display: flex;
  align-items: center;
  gap: var(--space-md);
  height: var(--topbar-height);
  padding: 0 20px;
  background: var(--color-topbar);
  color: var(--color-on-primary);
}

.topbar :focus-visible {
  outline-color: var(--color-focus-ring-on-dark);
}

.topbar__menu {
  display: none;
  place-items: center;
  width: var(--touch-target);
  height: var(--touch-target);
  margin-left: -12px;
  padding: 0;
  border: 0;
  border-radius: var(--radius-button);
  background: transparent;
  color: inherit;
  cursor: pointer;
}

.topbar__brand {
  display: inline-flex;
  align-items: center;
  gap: var(--space-md);
  color: inherit;
  text-decoration: none;
  border-radius: var(--radius-button);
}

.topbar__mark {
  display: grid;
  place-items: center;
  width: 24px;
  height: 24px;
  border-radius: var(--radius-button);
  background: var(--color-on-primary);
  color: var(--color-topbar);
  font: var(--text-mark);
}

.topbar__name {
  font: var(--text-brand);
}

.topbar__subtitle {
  font: var(--text-body);
  color: var(--color-topbar-subtitle);
}

.topbar__end {
  display: flex;
  align-items: center;
  gap: var(--space-md);
  margin-left: auto;
}

/* ---- Layout ----------------------------------------------------------- */
.layout {
  display: grid;
  grid-template-columns: var(--sidebar-width) minmax(0, 1fr);
  min-height: calc(100dvh - var(--topbar-height));
}

.sidebar {
  position: sticky;
  top: var(--topbar-height);
  display: flex;
  flex-direction: column;
  height: calc(100dvh - var(--topbar-height));
  padding: var(--space-xl) 10px 20px;
  border-right: 1px solid var(--color-border);
  background: var(--color-panel);
}

.sidebar__section {
  padding: 0 10px 14px;
  font: var(--text-nav-section);
  letter-spacing: var(--tracking-nav-section);
  text-transform: uppercase;
  color: var(--color-text-muted);
}

.sidebar__list {
  display: grid;
  gap: 6px;
}

.nav-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 36px;
  padding: 0 12px 0 20px;
  border-radius: var(--radius-button);
  color: var(--color-text);
  font: var(--text-nav);
  text-decoration: none;
  transition: background-color var(--duration-fast) var(--ease-standard);
}

a.nav-item:hover {
  background: var(--color-divider);
}

.nav-item--active,
a.nav-item--active:hover {
  background: var(--color-nav-active);
  color: var(--color-primary-text);
  font-weight: 700;
}

.nav-item--active::before {
  content: '';
  position: absolute;
  inset: 0 auto 0 0;
  width: 4px;
  border-radius: var(--radius-hairline);
  background: var(--color-primary-text);
}

.sidebar__version {
  margin-top: auto;
  padding: 0 10px;
  font: var(--text-caption);
  color: var(--color-text-muted);
}

.content {
  min-width: 0;
  padding: 0 var(--content-padding) 40px;
}

.content:focus {
  outline: none;
}

.scrim {
  display: none;
}

/* ---- Mobile: drawer --------------------------------------------------- */
@media (max-width: 767px) {
  .topbar {
    padding: 0 var(--space-lg);
    gap: var(--space-sm);
  }

  .topbar__menu {
    display: grid;
  }

  .topbar__subtitle {
    display: none;
  }

  .topbar__name {
    font: var(--text-brand-compact);
  }

  .topbar__end {
    gap: var(--space-sm);
  }

  .layout {
    display: block;
  }

  .sidebar {
    position: fixed;
    top: var(--topbar-height);
    left: 0;
    z-index: 30;
    width: min(280px, 85vw);
    height: calc(100dvh - var(--topbar-height));
    visibility: hidden;
    translate: -100% 0;
    transition:
      translate var(--duration-exit) var(--ease-exit),
      visibility 0s linear var(--duration-exit);
  }

  .sidebar--open {
    visibility: visible;
    translate: 0 0;
    transition:
      translate var(--duration-slow) var(--ease-standard),
      visibility 0s;
  }

  .nav-item {
    min-height: 44px;
  }

  .scrim {
    position: fixed;
    inset: var(--topbar-height) 0 0 0;
    z-index: 25;
    display: block;
    border: 0;
    background: var(--color-scrim);
  }

  .content {
    padding: 0 var(--space-lg) var(--space-xxl);
  }
}

@media (max-width: 420px) {
  .topbar__mark {
    display: none;
  }
}
</style>
