<script setup lang="ts">
import { computed, nextTick, ref, useTemplateRef } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { problemOf } from '../../api/http'
import { listMissions } from '../../api/missions'
import type { MissionSummary } from '../../api/types'
import AppIcon from '../../components/AppIcon.vue'
import ErrorPanel from '../../components/ErrorPanel.vue'
import StatusPill from '../../components/StatusPill.vue'
import { reportPollingError } from '../../composables/useLiveStatus'
import { usePolling } from '../../composables/usePolling'
import CancelMissionDialog from './CancelMissionDialog.vue'
import { EMPTY, formatRelative, formatShortDateTime, formatTime, orEmpty } from './format'
import {
  MISSION_TABS,
  countMissions,
  filterMissions,
  isCancellable,
  isMissionTab,
  type MissionTab,
} from './missionStatus'

const route = useRoute()
const router = useRouter()

const polling = usePolling((signal) => listMissions(signal))
reportPollingError(polling.error)

const missions = computed<MissionSummary[]>(() => polling.data.value ?? [])
const counts = computed(() => countMissions(missions.value))

const activeTab = computed<MissionTab>(() => (isMissionTab(route.query.tab) ? route.query.tab : 'all'))
const visible = computed(() => filterMissions(missions.value, activeTab.value))
const now = computed(() => polling.lastUpdated.value ?? new Date())
const problem = computed(() => (polling.error.value ? problemOf(polling.error.value) : null))

const tabRefs = useTemplateRef<HTMLButtonElement[]>('tabs')

function selectTab(tab: MissionTab, focus = false) {
  void router.replace({ query: { ...route.query, tab } })
  if (focus) {
    const index = MISSION_TABS.findIndex((t) => t.id === tab)
    tabRefs.value?.[index]?.focus()
  }
}

function onTabKeydown(event: KeyboardEvent, index: number) {
  const last = MISSION_TABS.length - 1
  const next =
    event.key === 'ArrowRight'
      ? index === last ? 0 : index + 1
      : event.key === 'ArrowLeft'
        ? index === 0 ? last : index - 1
        : event.key === 'Home'
          ? 0
          : event.key === 'End'
            ? last
            : null
  if (next === null) return
  event.preventDefault()
  selectTab(MISSION_TABS[next]!.id, true)
}

const EMPTY_TEXT: Record<MissionTab, string> = {
  active: 'No active missions right now.',
  finished: 'No finished missions yet.',
  all: 'No missions yet.',
}

// Cancel dialog
const cancelOpen = ref(false)
const cancelTarget = ref<MissionSummary | null>(null)

function askCancel(mission: MissionSummary) {
  cancelTarget.value = mission
  cancelOpen.value = true
}

async function onCancelClosed() {
  const target = cancelTarget.value
  await polling.refresh()
  await nextTick()
  // A cancelled row loses its Cancel button; move focus to that row's code link.
  if (target && (!document.activeElement || document.activeElement === document.body)) {
    document.getElementById(`code-${target.id}`)?.focus()
  }
}
</script>

<template>
  <div class="page">
    <p class="breadcrumb">
      Missions
    </p>
    <div class="page__header">
      <h1 class="page__title">
        Missions
      </h1>
      <RouterLink
        to="/missions/new"
        class="btn btn--primary"
      >
        <AppIcon
          name="plus"
          :stroke-width="2.25"
        />
        New mission
      </RouterLink>
    </div>

    <div
      class="tabs"
      role="tablist"
      aria-label="Filter missions"
    >
      <button
        v-for="(tab, index) in MISSION_TABS"
        :id="`tab-${tab.id}`"
        ref="tabs"
        :key="tab.id"
        type="button"
        role="tab"
        class="tab"
        :aria-selected="activeTab === tab.id"
        :tabindex="activeTab === tab.id ? 0 : -1"
        aria-controls="missions-panel"
        @click="selectTab(tab.id)"
        @keydown="onTabKeydown($event, index)"
      >
        {{ tab.label }}
        <span class="tab__count tabular">
          <template v-if="polling.loading.value">…</template>
          <template v-else>{{ counts[tab.id] }}</template>
        </span>
      </button>
    </div>

    <section
      id="missions-panel"
      role="tabpanel"
      :aria-labelledby="`tab-${activeTab}`"
      :aria-busy="polling.loading.value"
      class="panel"
    >
      <!-- First load failed: nothing to show yet. -->
      <ErrorPanel
        v-if="problem && polling.data.value === null && !polling.loading.value"
        title="Missions could not be loaded"
      >
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

      <template v-else>
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

        <div class="table-wrap">
          <table
            class="table"
            role="table"
          >
            <thead role="rowgroup">
              <tr role="row">
                <th
                  role="columnheader"
                  scope="col"
                >
                  Mission code
                </th>
                <th
                  role="columnheader"
                  scope="col"
                >
                  External ref
                </th>
                <th
                  role="columnheader"
                  scope="col"
                >
                  Type
                </th>
                <th
                  role="columnheader"
                  scope="col"
                >
                  Robot
                </th>
                <th
                  role="columnheader"
                  scope="col"
                  class="num"
                >
                  Priority
                </th>
                <th
                  role="columnheader"
                  scope="col"
                >
                  Status
                </th>
                <th
                  role="columnheader"
                  scope="col"
                  aria-sort="descending"
                >
                  Created <span aria-hidden="true">↓</span>
                </th>
                <th
                  role="columnheader"
                  scope="col"
                >
                  Last sync
                </th>
                <th
                  role="columnheader"
                  scope="col"
                >
                  Actions
                </th>
              </tr>
            </thead>

            <tbody
              v-if="polling.loading.value"
              role="rowgroup"
            >
              <tr
                v-for="n in 5"
                :key="n"
                role="row"
                class="row--skeleton"
                aria-hidden="true"
              >
                <td
                  v-for="c in 9"
                  :key="c"
                  role="cell"
                >
                  <span
                    class="skeleton"
                    :style="{ width: `${40 + ((n * c) % 5) * 10}%` }"
                  />
                </td>
              </tr>
            </tbody>

            <tbody
              v-else-if="visible.length > 0"
              role="rowgroup"
            >
              <tr
                v-for="mission in visible"
                :key="mission.id"
                role="row"
              >
                <td
                  role="cell"
                  data-label="Mission code"
                  class="cell-code"
                >
                  <RouterLink
                    :id="`code-${mission.id}`"
                    :to="`/missions/${mission.id}`"
                    class="code-link tabular"
                  >
                    {{ mission.missionCode }}
                  </RouterLink>
                </td>
                <td
                  role="cell"
                  data-label="External ref"
                  :class="{ muted: !mission.externalRef }"
                >
                  {{ orEmpty(mission.externalRef) }}
                </td>
                <td
                  role="cell"
                  data-label="Type"
                  class="cell-type"
                >
                  {{ mission.missionType }}
                </td>
                <td
                  role="cell"
                  data-label="Robot"
                  :class="{ muted: !mission.robotName }"
                >
                  {{ mission.robotName ?? 'Automatic' }}
                </td>
                <td
                  role="cell"
                  data-label="Priority"
                  class="num tabular"
                >
                  {{ mission.priority }}
                </td>
                <td
                  role="cell"
                  data-label="Status"
                  class="cell-status"
                >
                  <StatusPill :status="mission.status" />
                </td>
                <td
                  role="cell"
                  data-label="Created"
                  class="tabular"
                >
                  {{ formatShortDateTime(mission.createdAt) }}
                </td>
                <td
                  role="cell"
                  data-label="Last sync"
                  class="muted tabular"
                >
                  {{ mission.lastSyncedAt ? formatRelative(mission.lastSyncedAt, now) : EMPTY }}
                </td>
                <td
                  role="cell"
                  class="cell-actions"
                >
                  <div class="actions">
                    <RouterLink
                      :to="`/missions/${mission.id}`"
                      class="btn btn--small"
                      :aria-label="`Detail of ${mission.missionCode}`"
                    >
                      Detail
                    </RouterLink>
                    <button
                      v-if="isCancellable(mission.status)"
                      type="button"
                      class="btn btn--small btn--danger"
                      :aria-label="`Cancel ${mission.missionCode}`"
                      @click="askCancel(mission)"
                    >
                      Cancel
                    </button>
                    <span
                      v-else-if="mission.status === 'Cancelling'"
                      class="pending"
                    >cancelling…</span>
                  </div>
                </td>
              </tr>
            </tbody>

            <tbody
              v-else
              role="rowgroup"
            >
              <tr
                role="row"
                class="row--empty"
              >
                <td
                  role="cell"
                  colspan="9"
                >
                  <div class="empty">
                    <p>{{ EMPTY_TEXT[activeTab] }}</p>
                    <RouterLink
                      v-if="activeTab !== 'finished'"
                      to="/missions/new"
                      class="btn btn--primary"
                    >
                      <AppIcon
                        name="plus"
                        :stroke-width="2.25"
                      />
                      Dispatch a mission
                    </RouterLink>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p
          v-if="!polling.loading.value && visible.length > 0"
          class="table-foot tabular"
        >
          Showing {{ visible.length }} of {{ counts.all }} missions
        </p>
      </template>
    </section>

    <CancelMissionDialog
      v-model:open="cancelOpen"
      :mission="cancelTarget"
      @closed="onCancelClosed"
    />
  </div>
</template>

<style scoped>
.breadcrumb {
  padding-top: 18px;
  font: var(--text-small);
  color: var(--color-text-muted);
}

.page__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-lg);
  margin-top: 6px;
}

.page__title {
  font: var(--text-page-title);
}

/* ---- Tabs ------------------------------------------------------------- */
.tabs {
  display: flex;
  gap: 6px;
  margin-top: var(--space-lg);
  border-bottom: 1px solid var(--color-border);
}

.tab {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: var(--space-sm);
  height: 40px;
  padding: 0 var(--space-md);
  border: 0;
  background: transparent;
  color: var(--color-text-muted);
  font: var(--text-nav);
  cursor: pointer;
  transition: color var(--duration-fast) var(--ease-standard);
}

.tab::after {
  content: '';
  position: absolute;
  inset: auto 0 -1px;
  height: 3px;
  border-radius: 1px;
  background: var(--color-primary-text);
  scale: 0 1;
  transition: scale var(--duration-base) var(--ease-standard);
}

.tab:hover {
  color: var(--color-text);
}

.tab[aria-selected='true'] {
  color: var(--color-primary-text);
  font-weight: 700;
}

.tab[aria-selected='true']::after {
  scale: 1 1;
}

.tab__count {
  display: inline-grid;
  place-items: center;
  min-width: 24px;
  height: 18px;
  padding: 0 6px;
  border-radius: var(--radius-pill);
  background: var(--color-skeleton);
  color: var(--color-text-muted);
  font: var(--text-count);
}

.tab[aria-selected='true'] .tab__count {
  background: var(--color-nav-active);
  color: var(--color-primary-text);
}

/* ---- Table ------------------------------------------------------------ */
.panel {
  margin-top: var(--space-lg);
}

.stale-note {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  margin-bottom: var(--space-md);
  color: var(--color-warning-text);
  font: var(--text-body-small);
}

.table-wrap {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-card);
  background: var(--color-surface);
  overflow: hidden;
}

.table {
  width: 100%;
  border-collapse: collapse;
}

.table th {
  height: var(--table-header-height);
  padding: 0 var(--space-lg);
  background: var(--color-panel);
  border-bottom: 1px solid var(--color-border);
  color: var(--color-text-muted);
  font: var(--text-table-header);
  letter-spacing: var(--tracking-table-header);
  text-align: left;
  text-transform: uppercase;
  white-space: nowrap;
}

.table td {
  height: var(--table-row-height);
  padding: 0 var(--space-lg);
  border-top: 1px solid var(--color-divider);
  white-space: nowrap;
}

.table tbody tr:first-child td {
  border-top: 0;
}

.table tbody tr:not(.row--skeleton, .row--empty):hover td {
  background: var(--color-row-highlight);
}

.table .num {
  text-align: center;
}

.cell-type {
  font: var(--text-body-small);
}

.muted {
  color: var(--color-text-muted);
}

.code-link {
  color: var(--color-primary-text);
  font-weight: 600;
  text-decoration: none;
}

.code-link:hover {
  text-decoration: underline;
}

.actions {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
}

.pending {
  padding-left: var(--space-xxs);
  color: var(--color-warning-text);
  font: var(--text-small);
  font-style: italic;
}

.empty {
  display: grid;
  justify-items: center;
  gap: var(--space-md);
  padding: var(--space-xxl) var(--space-lg);
  color: var(--color-text-muted);
  white-space: normal;
}

.row--empty td {
  height: auto;
}

.table-foot {
  margin-top: var(--space-md);
  font: var(--text-small);
  color: var(--color-text-muted);
}

/* Narrow desktop: let the table scroll inside its frame rather than the page. */
@media (min-width: 768px) and (max-width: 1180px) {
  .table-wrap {
    overflow-x: auto;
  }
}

/* ---- < 768 px: every row becomes a stacked card ------------------------ */
@media (max-width: 767px) {
  .page__header .btn {
    padding: 0 var(--space-md);
  }

  .tabs {
    gap: 0;
  }

  .tab {
    flex: 1;
    justify-content: center;
    padding: 0 var(--space-xs);
    height: 44px;
  }

  .table-wrap {
    border: 0;
    background: transparent;
    overflow: visible;
  }

  .table thead {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip: rect(0 0 0 0);
  }

  .table,
  .table tbody,
  .table tr,
  .table td {
    display: block;
  }

  .table tbody {
    display: grid;
    gap: var(--space-md);
  }

  .table tr {
    display: grid;
    grid-template-columns: 1fr auto;
    gap: var(--space-sm) var(--space-md);
    padding: var(--space-lg);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-card);
    background: var(--color-surface);
  }

  .table td {
    display: grid;
    grid-template-columns: 96px 1fr;
    grid-column: 1 / -1;
    align-items: center;
    height: auto;
    padding: 0;
    border: 0;
    white-space: normal;
  }

  .table td::before {
    content: attr(data-label);
    color: var(--color-text-muted);
    font: var(--text-meta);
  }

  .table .num {
    text-align: left;
  }

  .table td.cell-code,
  .table td.cell-status {
    display: block;
    grid-column: auto;
  }

  .table td.cell-code::before,
  .table td.cell-status::before,
  .table td.cell-actions::before {
    content: none;
  }

  .table td.cell-code {
    order: -2;
  }

  .table td.cell-status {
    order: -1;
    justify-self: end;
  }

  .table tbody tr td.cell-actions {
    display: block;
    margin-top: var(--space-xs);
    padding-top: var(--space-md);
    border-top: 1px solid var(--color-divider);
  }

  .actions .btn {
    flex: 1;
  }

  .table tbody tr:not(.row--skeleton, .row--empty):hover td {
    background: transparent;
  }

  .row--skeleton td::before,
  .row--empty td::before {
    content: none;
  }

  .row--skeleton td {
    display: block;
  }

  .table tr.row--empty {
    display: block;
  }

  .row--empty td {
    display: block;
  }
}
</style>
