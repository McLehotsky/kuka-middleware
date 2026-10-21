<script setup lang="ts">
import { computed, nextTick, ref, useTemplateRef, watch } from 'vue'
import { ApiError, problemOf } from '../../api/http'
import { getMission, getMissionHistory } from '../../api/missions'
import type { MissionDetail, MissionHistoryEntry } from '../../api/types'
import AppIcon from '../../components/AppIcon.vue'
import ErrorPanel from '../../components/ErrorPanel.vue'
import StatusPill from '../../components/StatusPill.vue'
import { reportPollingError } from '../../composables/useLiveStatus'
import { usePolling } from '../../composables/usePolling'
import CancelMissionDialog from './CancelMissionDialog.vue'
import { EMPTY, formatDateTime, formatHistoryTime, formatRelative, formatTime, orEmpty } from './format'
import { isCancellable, isFinal, statusTone } from './missionStatus'

const props = defineProps<{ missionId: string }>()

interface DetailData {
  mission: MissionDetail
  history: MissionHistoryEntry[]
}

// Polls only while the mission is not final (system-definition §5, §9).
const keepPolling = ref(true)
const polling = usePolling<DetailData>(
  async (signal) => {
    try {
      const [mission, history] = await Promise.all([
        getMission(props.missionId, signal),
        getMissionHistory(props.missionId, signal),
      ])
      keepPolling.value = !isFinal(mission.status)
      return { mission, history }
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) keepPolling.value = false
      throw error
    }
  },
  { enabled: keepPolling },
)

const mission = computed(() => polling.data.value?.mission ?? null)
const history = computed(() => polling.data.value?.history ?? [])
const problem = computed(() => (polling.error.value ? problemOf(polling.error.value) : null))
const notFound = computed(() => problem.value?.status === 404)
const now = computed(() => polling.lastUpdated.value ?? new Date())
// A missing mission is not a connection problem, so it does not pause the Live indicator.
reportPollingError(computed(() => (notFound.value ? null : polling.error.value)))

const info = computed(() => {
  const m = mission.value
  if (!m) return []
  return [
    { label: 'Robot', value: m.robotName ?? 'Automatic (KUKA FM decides)', accent: !!m.robotName },
    { label: 'Priority', value: String(m.priority) },
    { label: 'Created', value: formatDateTime(m.createdAt) },
    { label: 'Submitted', value: formatDateTime(m.submittedAt) },
    { label: 'Finished', value: formatDateTime(m.finishedAt) },
    { label: 'Last synchronisation', value: m.lastSyncedAt ? formatRelative(m.lastSyncedAt, now.value) : EMPTY },
    { label: 'Error', value: orEmpty(m.errorMessage), danger: !!m.errorMessage, wide: true },
  ]
})

const metaLine = computed(() => {
  const m = mission.value
  if (!m) return ''
  return [m.externalRef ? `External ref ${m.externalRef}` : null, m.missionType, `created by ${m.createdBy}`]
    .filter(Boolean)
    .join(' · ')
})

const cancelOpen = ref(false)

const titleRef = useTemplateRef<HTMLElement>('title')

async function onCancelClosed() {
  keepPolling.value = true
  await polling.refresh()
  await nextTick()
  // The Cancel button disappears once the mission is Cancelling; keep focus on the page.
  if (!document.activeElement || document.activeElement === document.body) titleRef.value?.focus()
}

watch(
  () => mission.value?.missionCode,
  (code) => {
    if (code) document.title = `${code} · ITS Middleware`
  },
)
</script>

<template>
  <div class="page">
    <nav
      class="breadcrumb"
      aria-label="Breadcrumb"
    >
      <RouterLink to="/missions">
        Missions
      </RouterLink>
      <span aria-hidden="true">/</span>
      <span aria-current="page">{{ mission?.missionCode ?? (notFound ? 'Not found' : '…') }}</span>
    </nav>

    <!-- 404 -->
    <div
      v-if="notFound"
      class="state"
    >
      <h1 class="page__title">
        Mission not found
      </h1>
      <p class="muted">
        This mission does not exist or has been removed.
      </p>
      <RouterLink
        to="/missions"
        class="btn"
      >
        <AppIcon name="arrow-left" />
        Back to missions
      </RouterLink>
    </div>

    <!-- First load failed for another reason -->
    <div
      v-else-if="problem && !mission && !polling.loading.value"
      class="state"
    >
      <h1 class="page__title">
        Mission
      </h1>
      <ErrorPanel title="The mission could not be loaded">
        <p>{{ problem.message }}</p>
        <p>
          <button
            type="button"
            class="btn btn--small"
            @click="polling.refresh()"
          >
            Try again
          </button>
        </p>
      </ErrorPanel>
    </div>

    <!-- Loading -->
    <div
      v-else-if="!mission"
      class="loading"
      aria-busy="true"
      aria-label="Loading mission"
    >
      <span
        class="skeleton"
        style="width: 180px; height: 24px"
      />
      <span
        class="skeleton"
        style="width: 320px"
      />
      <div class="layout">
        <div class="card skeleton-card">
          <span
            v-for="n in 4"
            :key="n"
            class="skeleton"
          />
        </div>
        <div class="card skeleton-card">
          <span
            v-for="n in 4"
            :key="n"
            class="skeleton"
          />
        </div>
      </div>
    </div>

    <template v-else>
      <div class="page__header">
        <div class="page__heading">
          <h1
            ref="title"
            class="page__title tabular"
            tabindex="-1"
          >
            {{ mission.missionCode }}
          </h1>
          <StatusPill :status="mission.status" />
        </div>
        <div class="page__actions">
          <button
            type="button"
            class="btn"
            :disabled="polling.refreshing.value"
            @click="polling.refresh()"
          >
            <AppIcon name="refresh" />
            Refresh
          </button>
          <button
            v-if="isCancellable(mission.status)"
            type="button"
            class="btn btn--danger"
            @click="cancelOpen = true"
          >
            Cancel mission
          </button>
        </div>
      </div>
      <p class="page__lead">
        {{ metaLine }}
      </p>

      <p
        v-if="problem"
        class="stale-note"
        role="status"
      >
        <AppIcon
          name="alert"
          :size="14"
          :stroke-width="2"
        />
        Showing data from {{ formatTime(polling.lastUpdated.value) }} – refresh failed. Retrying every 5 s.
      </p>
      <p
        v-else-if="mission.status === 'Cancelling'"
        class="pending-note"
        role="status"
      >
        Cancellation requested. The mission becomes Cancelled once KUKA FM confirms it.
      </p>

      <div class="layout">
        <div class="main">
          <section
            class="card"
            aria-labelledby="info-title"
          >
            <div class="card__header">
              <h2
                id="info-title"
                class="card__title"
              >
                Information
              </h2>
            </div>
            <dl class="card__body info">
              <div
                v-for="item in info"
                :key="item.label"
                :class="{ 'info--wide': item.wide }"
              >
                <dt>{{ item.label }}</dt>
                <dd
                  class="tabular"
                  :class="{ 'info__accent': item.accent, 'info__danger': item.danger, muted: item.value === EMPTY }"
                >
                  {{ item.value }}
                </dd>
              </div>
            </dl>
          </section>

          <section
            class="card"
            aria-labelledby="steps-title"
          >
            <div class="card__header">
              <h2
                id="steps-title"
                class="card__title"
              >
                Mission steps
              </h2>
              <span class="card__meta tabular">{{ mission.steps.length }} {{ mission.steps.length === 1 ? 'step' : 'steps' }}</span>
            </div>
            <ol class="card__body steps">
              <li
                v-for="step in mission.steps"
                :key="step.sequence"
                class="step"
              >
                <span
                  class="step__dot tabular"
                  aria-hidden="true"
                >{{ step.sequence }}</span>
                <span class="sr-only">Step {{ step.sequence }}:</span>
                <span class="step__node">{{ step.nodeCode }}</span>
                <span class="step__action">{{ step.action }}</span>
              </li>
            </ol>
          </section>
        </div>

        <section
          class="card"
          aria-labelledby="history-title"
        >
          <div class="card__header">
            <h2
              id="history-title"
              class="card__title"
            >
              Status history
            </h2>
            <span class="card__meta tabular">{{ history.length }} {{ history.length === 1 ? 'change' : 'changes' }}</span>
          </div>
          <ol class="card__body timeline">
            <li
              v-for="(entry, index) in history"
              :key="`${entry.changedAt}-${index}`"
              class="event"
            >
              <span
                class="event__dot"
                :class="`event__dot--${statusTone(entry.status)}`"
                aria-hidden="true"
              />
              <div class="event__line">
                <time
                  class="event__time tabular"
                  :datetime="entry.changedAt"
                  :title="formatDateTime(entry.changedAt)"
                >
                  {{ formatHistoryTime(entry.changedAt, mission.createdAt) }}
                </time>
                <span class="event__change">
                  <template v-if="entry.previousStatus">
                    <StatusPill
                      :status="entry.previousStatus"
                      size="sm"
                    />
                    <span
                      aria-hidden="true"
                      class="event__arrow"
                    >→</span>
                    <span class="sr-only">to</span>
                  </template>
                  <StatusPill
                    :status="entry.status"
                    size="sm"
                  />
                </span>
                <span
                  class="event__source"
                  :title="entry.source === 'Api' ? 'Client action' : 'Synchronisation'"
                >
                  {{ entry.source === 'Api' ? 'API' : 'Sync' }}
                </span>
              </div>
              <p
                v-if="entry.message"
                class="event__message"
              >
                {{ entry.message }}
              </p>
            </li>
            <li
              v-if="!isFinal(mission.status)"
              class="event event--waiting"
            >
              <span
                class="event__dot event__dot--waiting"
                aria-hidden="true"
              />
              <p class="event__waiting">
                Waiting for the next change (refresh every 5 s)…
              </p>
            </li>
          </ol>
        </section>
      </div>
    </template>

    <CancelMissionDialog
      v-model:open="cancelOpen"
      :mission="mission"
      @closed="onCancelClosed"
    />
  </div>
</template>

<style scoped>
.breadcrumb {
  display: flex;
  gap: var(--space-sm);
  padding-top: 18px;
  font: var(--text-small);
  color: var(--color-text-muted);
}

.breadcrumb a {
  color: inherit;
}

.page__header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-md) var(--space-lg);
  margin-top: 6px;
}

.page__heading {
  display: flex;
  align-items: center;
  gap: var(--space-md);
  min-width: 0;
}

.page__title {
  font: var(--text-page-title);
}

.page__actions {
  display: flex;
  gap: var(--space-md);
}

.page__lead {
  margin-top: var(--space-xs);
  color: var(--color-text-muted);
}

.muted {
  color: var(--color-text-muted);
}

.stale-note,
.pending-note {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  margin-top: var(--space-md);
  color: var(--color-warning-text);
  font: var(--text-body-small);
}

.pending-note {
  font-style: italic;
}

.state {
  display: grid;
  justify-items: start;
  gap: var(--space-md);
  margin-top: 6px;
  max-width: 560px;
}

.loading {
  display: grid;
  gap: var(--space-md);
  margin-top: var(--space-md);
}

.skeleton-card {
  display: grid;
  gap: var(--space-lg);
  padding: var(--space-lg);
  min-height: 200px;
  align-content: start;
}

.layout {
  display: grid;
  grid-template-columns: minmax(0, 560px) minmax(300px, 1fr);
  gap: var(--space-xl);
  align-items: start;
  margin-top: var(--space-lg);
}

.main {
  display: grid;
  gap: 18px;
}

/* ---- Info grid -------------------------------------------------------- */
.info {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-lg);
}

.info dt {
  color: var(--color-text-muted);
  font: var(--text-meta);
}

.info dd {
  margin-top: 2px;
  font: var(--text-value);
  overflow-wrap: anywhere;
}

.info--wide {
  grid-column: 1 / -1;
}

.info__accent {
  color: var(--color-primary-text);
}

.info__danger {
  color: var(--color-danger-text);
}

/* ---- Steps ------------------------------------------------------------ */
.steps {
  display: grid;
  gap: var(--space-md);
}

.step {
  display: grid;
  grid-template-columns: 20px minmax(0, 140px) 1fr;
  align-items: center;
  gap: var(--space-md);
}

.step__dot {
  display: grid;
  place-items: center;
  width: 20px;
  height: 20px;
  border: 1.5px solid var(--color-primary-text);
  border-radius: 50%;
  color: var(--color-primary-text);
  font: var(--text-count);
}

.step__node {
  font: var(--text-value);
  overflow-wrap: anywhere;
}

.step__action {
  color: var(--color-text-muted);
}

/* ---- Timeline --------------------------------------------------------- */
.timeline {
  display: grid;
}

.event {
  position: relative;
  padding: 0 0 22px 28px;
}

.event:not(:last-child)::before {
  content: '';
  position: absolute;
  top: 18px;
  bottom: 4px;
  left: 6px;
  width: 2px;
  border-radius: 1px;
  background: var(--color-border);
}

.event:last-child {
  padding-bottom: 0;
}

.event__dot {
  position: absolute;
  top: 3px;
  left: 0;
  width: 14px;
  height: 14px;
  border-radius: 50%;
}

.event__dot--created { background: var(--status-created-fg); }
.event__dot--submitted { background: var(--status-submitted-fg); }
.event__dot--inprogress { background: var(--status-inprogress-fg); }
.event__dot--cancelling { background: var(--status-cancelling-fg); }
.event__dot--completed { background: var(--status-completed-fg); }
.event__dot--cancelled { background: var(--status-cancelled-fg); }
.event__dot--failed { background: var(--status-failed-fg); }

.event__dot--waiting {
  border: 2px dashed var(--color-border);
  background: var(--color-surface);
}

.event__line {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-sm) var(--space-md);
}

.event__time {
  color: var(--color-text-muted);
  font: var(--text-body-small-strong);
}

.event__change {
  display: inline-flex;
  align-items: center;
  gap: var(--space-xs);
}

.event__arrow {
  color: var(--color-text-muted);
}

.event__source {
  margin-left: auto;
  padding: 2px var(--space-sm);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-tag);
  background: var(--color-panel);
  color: var(--color-text-muted);
  font: var(--text-tag);
}

.event__message {
  margin-top: var(--space-xs);
  font: var(--text-body-small);
  overflow-wrap: anywhere;
}

.event__waiting {
  color: var(--color-text-muted);
  font: var(--text-body-small);
  font-style: italic;
}

@media (max-width: 1099px) {
  .layout {
    grid-template-columns: minmax(0, 1fr);
    max-width: 680px;
  }
}

@media (max-width: 767px) {
  .page__actions {
    width: 100%;
  }

  .page__actions .btn {
    flex: 1;
  }

  .info {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
