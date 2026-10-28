<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { problemOf } from '../../api/http'
import { listRobots } from '../../api/robots'
import { ROBOT_STATUSES, type RobotSummary } from '../../api/types'
import AppIcon from '../../components/AppIcon.vue'
import BatteryBar from '../../components/BatteryBar.vue'
import ErrorPanel from '../../components/ErrorPanel.vue'
import StatusPill from '../../components/StatusPill.vue'
import { reportPollingError } from '../../composables/useLiveStatus'
import { usePolling } from '../../composables/usePolling'
import { EMPTY, formatRelative, formatTime, orEmpty } from '../missions/format'
import {
  countRobots,
  filterRobots,
  isRobotStatus,
  robotDisplayName,
  type RobotCounts,
  type RobotStatusFilter,
} from './robotStatus'

const route = useRoute()
const router = useRouter()

// UC6: one GET /api/robots every 5 s; tiles, search and filter work on the client (Q-S).
const polling = usePolling((signal) => listRobots(signal))
reportPollingError(polling.error)

const robots = computed<RobotSummary[]>(() => polling.data.value ?? [])
const counts = computed(() => countRobots(robots.value))
const now = computed(() => polling.lastUpdated.value ?? new Date())
const problem = computed(() => (polling.error.value ? problemOf(polling.error.value) : null))

// View, search and status filter live in the query, so Back from a mission detail restores them.
type ViewMode = 'cards' | 'table'

function setQuery(key: string, value: string | undefined) {
  void router.replace({ query: { ...route.query, [key]: value } })
}

const view = computed<ViewMode>({
  get: () => (route.query.view === 'table' ? 'table' : 'cards'),
  set: (value) => setQuery('view', value === 'table' ? 'table' : undefined),
})

const search = computed<string>({
  get: () => (typeof route.query.q === 'string' ? route.query.q : ''),
  set: (value) => setQuery('q', value || undefined),
})

const statusFilter = computed<RobotStatusFilter>({
  get: () => (isRobotStatus(route.query.status) ? route.query.status : 'all'),
  set: (value) => setQuery('status', value === 'all' ? undefined : value),
})

const visible = computed(() =>
  filterRobots(robots.value, { query: search.value, status: statusFilter.value }),
)
const filtered = computed(() => search.value.trim() !== '' || statusFilter.value !== 'all')

function clearFilters() {
  void router.replace({ query: { ...route.query, q: undefined, status: undefined } })
  document.getElementById('robot-search')?.focus()
}

const TILES: readonly { key: keyof RobotCounts; label: string }[] = [
  { key: 'total', label: 'Total' },
  { key: 'Available', label: 'Available' },
  { key: 'Busy', label: 'Busy' },
  { key: 'Offline', label: 'Offline' },
]

function tileValue(key: keyof RobotCounts): string {
  if (polling.loading.value) return '…'
  return polling.data.value === null ? EMPTY : String(counts.value[key])
}

function updatedText(robot: RobotSummary): string {
  return `updated ${formatRelative(robot.updatedAt, now.value)}`
}

function lastSeenText(robot: RobotSummary): string {
  return robot.lastSeenAt ? `last seen ${formatRelative(robot.lastSeenAt, now.value)}` : `last seen ${EMPTY}`
}
</script>

<template>
  <div class="page">
    <p class="breadcrumb">
      Robots
    </p>
    <h1 class="page__title">
      Robots
    </h1>

    <ul
      class="tiles"
      aria-label="Robot states"
    >
      <li
        v-for="tile in TILES"
        :key="tile.key"
        class="tile"
        :class="`tile--${tile.key.toLowerCase()}`"
      >
        <span class="tile__label">{{ tile.label }}</span>
        <span class="tile__value tabular">{{ tileValue(tile.key) }}</span>
      </li>
    </ul>

    <!-- First load failed: nothing to show yet. -->
    <ErrorPanel
      v-if="problem && polling.data.value === null && !polling.loading.value"
      class="block"
      title="Robots could not be loaded"
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
        class="stale-note block"
        role="status"
      >
        <AppIcon
          name="alert"
          :size="14"
          :stroke-width="2"
        />
        Showing data from {{ formatTime(polling.lastUpdated.value) }} – refresh failed. Retrying every 5 s.
      </p>

      <div class="toolbar block">
        <div class="search">
          <AppIcon
            name="search"
            class="search__icon"
            :size="15"
          />
          <input
            id="robot-search"
            v-model="search"
            type="search"
            class="control search__input"
            placeholder="Search robot…"
            aria-label="Search robots by name or KUKA ID"
            autocomplete="off"
            spellcheck="false"
          >
        </div>
        <select
          v-model="statusFilter"
          class="control status-filter"
          aria-label="Filter by status"
        >
          <option value="all">
            Status: all
          </option>
          <option
            v-for="status in ROBOT_STATUSES"
            :key="status"
            :value="status"
          >
            {{ status }}
          </option>
        </select>
        <div
          class="view-toggle"
          role="group"
          aria-label="View"
        >
          <button
            type="button"
            class="view-toggle__btn"
            :aria-pressed="view === 'cards'"
            @click="view = 'cards'"
          >
            <AppIcon name="grid" />
            Cards
          </button>
          <button
            type="button"
            class="view-toggle__btn"
            :aria-pressed="view === 'table'"
            @click="view = 'table'"
          >
            <AppIcon name="list" />
            Table
          </button>
        </div>
      </div>

      <section
        class="results block"
        aria-label="Robots"
        :aria-busy="polling.loading.value"
      >
        <!-- Loading: skeletons at real size (first load only). -->
        <div
          v-if="polling.loading.value"
          class="grid"
          aria-hidden="true"
        >
          <div
            v-for="n in 6"
            :key="n"
            class="robot robot--skeleton"
          >
            <div class="robot__head">
              <span
                class="skeleton"
                style="width: 40%; height: 16px"
              />
            </div>
            <div class="robot__facts">
              <span
                v-for="line in 3"
                :key="line"
                class="skeleton"
                :style="{ width: `${50 + ((n + line) % 4) * 12}%` }"
              />
            </div>
          </div>
        </div>

        <div
          v-else-if="robots.length === 0"
          class="empty"
        >
          <AppIcon
            name="robot"
            :size="24"
          />
          <p class="empty__title">
            No robots yet
          </p>
          <p>Robot states appear here once the sync with KUKA FM is connected.</p>
        </div>

        <div
          v-else-if="visible.length === 0"
          class="empty"
        >
          <p class="empty__title">
            No robots match the filter
          </p>
          <p>Try another name or KUKA ID, or show every status.</p>
          <button
            type="button"
            class="btn"
            @click="clearFilters"
          >
            Clear filters
          </button>
        </div>

        <!-- Cards -->
        <ul
          v-else-if="view === 'cards'"
          class="grid"
        >
          <li
            v-for="robot in visible"
            :key="robot.id"
          >
            <article
              class="robot"
              :class="{ 'robot--offline': robot.status === 'Offline' }"
              :aria-labelledby="`robot-${robot.id}`"
            >
              <header class="robot__head">
                <div class="robot__ident">
                  <h2
                    :id="`robot-${robot.id}`"
                    class="robot__name"
                  >
                    {{ robotDisplayName(robot) }}
                  </h2>
                  <p
                    v-if="robot.name?.trim()"
                    class="robot__id"
                  >
                    {{ robot.kukaRobotId }}
                  </p>
                </div>
                <StatusPill :status="robot.status" />
              </header>
              <dl class="robot__facts">
                <div class="fact">
                  <dt>Battery</dt>
                  <dd>
                    <BatteryBar
                      :level="robot.batteryLevel"
                      :status="robot.status"
                    />
                  </dd>
                </div>
                <div class="fact">
                  <dt>Node</dt>
                  <dd :class="{ muted: !robot.currentNode }">
                    {{ orEmpty(robot.currentNode) }}
                  </dd>
                </div>
                <div class="fact">
                  <dt>Mission</dt>
                  <dd>
                    <RouterLink
                      v-if="robot.currentMission"
                      :to="`/missions/${robot.currentMission.id}`"
                      class="mission-link tabular"
                    >
                      {{ robot.currentMission.missionCode }}
                      <AppIcon
                        name="chevron-right"
                        :size="14"
                        :stroke-width="2"
                      />
                    </RouterLink>
                    <span
                      v-else
                      class="muted"
                    >{{ EMPTY }}</span>
                  </dd>
                </div>
              </dl>
              <footer class="robot__foot tabular">
                <span
                  v-if="robot.status === 'Offline'"
                  class="last-seen"
                >
                  <AppIcon
                    name="alert"
                    :size="13"
                    :stroke-width="2"
                  />
                  {{ lastSeenText(robot) }}
                </span>
                <template v-else>
                  {{ updatedText(robot) }}
                </template>
              </footer>
            </article>
          </li>
        </ul>

        <!-- Table -->
        <div
          v-else
          class="table-wrap"
        >
          <table class="table">
            <thead>
              <tr>
                <th scope="col">
                  Robot
                </th>
                <th scope="col">
                  KUKA ID
                </th>
                <th scope="col">
                  Status
                </th>
                <th scope="col">
                  Battery
                </th>
                <th scope="col">
                  Node
                </th>
                <th scope="col">
                  Mission
                </th>
                <th scope="col">
                  Updated
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="robot in visible"
                :key="robot.id"
                :class="{ 'row--offline': robot.status === 'Offline' }"
              >
                <th
                  scope="row"
                  class="cell-name"
                >
                  {{ robotDisplayName(robot) }}
                </th>
                <td
                  data-label="KUKA ID"
                  class="muted"
                >
                  {{ robot.kukaRobotId }}
                </td>
                <td
                  data-label="Status"
                  class="cell-status"
                >
                  <StatusPill :status="robot.status" />
                </td>
                <td
                  data-label="Battery"
                  class="cell-battery"
                >
                  <BatteryBar
                    :level="robot.batteryLevel"
                    :status="robot.status"
                  />
                </td>
                <td
                  data-label="Node"
                  :class="{ muted: !robot.currentNode }"
                >
                  {{ orEmpty(robot.currentNode) }}
                </td>
                <td data-label="Mission">
                  <RouterLink
                    v-if="robot.currentMission"
                    :to="`/missions/${robot.currentMission.id}`"
                    class="mission-link tabular"
                  >
                    {{ robot.currentMission.missionCode }}
                  </RouterLink>
                  <span
                    v-else
                    class="muted"
                  >{{ EMPTY }}</span>
                </td>
                <td
                  data-label="Updated"
                  class="tabular"
                >
                  <span
                    v-if="robot.status === 'Offline'"
                    class="last-seen"
                  >
                    <AppIcon
                      name="alert"
                      :size="13"
                      :stroke-width="2"
                    />
                    {{ lastSeenText(robot) }}
                  </span>
                  <span
                    v-else
                    class="muted"
                  >{{ updatedText(robot) }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <p
          v-if="!polling.loading.value && robots.length > 0 && visible.length > 0"
          class="foot tabular"
          role="status"
        >
          <template v-if="filtered">
            Showing {{ visible.length }} of {{ robots.length }} robots
          </template>
          <template v-else>
            {{ robots.length }} {{ robots.length === 1 ? 'robot' : 'robots' }}
          </template>
        </p>
      </section>
    </template>
  </div>
</template>

<style scoped>
.breadcrumb {
  padding-top: 18px;
  font: var(--text-small);
  color: var(--color-text-muted);
}

.page__title {
  margin-top: 6px;
  font: var(--text-page-title);
}

.block {
  margin-top: var(--space-lg);
}

.muted {
  color: var(--color-text-muted);
}

/* ---- Stat tiles ------------------------------------------------------- */
.tiles {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: var(--space-lg);
  margin-top: var(--space-lg);
}

.tile {
  position: relative;
  display: grid;
  align-content: center;
  gap: 2px;
  height: 72px;
  padding: 0 18px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-card);
  background: var(--color-surface);
  overflow: hidden;
}

/* 4 px state bar (DESIGN.md → Stat tile); Total has none and sits on the panel tone. */
.tile::before {
  content: '';
  position: absolute;
  inset: 0 auto 0 0;
  width: 4px;
  background: var(--tile-accent, transparent);
}

.tile--total {
  background: var(--color-panel);
}
.tile--available {
  --tile-accent: var(--status-completed-fg);
}
.tile--busy {
  --tile-accent: var(--status-inprogress-fg);
}
.tile--offline {
  --tile-accent: var(--status-created-fg);
}

.tile__label {
  font: var(--text-body-small-strong);
  color: var(--color-text-muted);
}

.tile__value {
  font: var(--text-stat-value);
  color: var(--tile-accent, var(--color-text));
}

/* ---- Toolbar ---------------------------------------------------------- */
.stale-note {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  color: var(--color-warning-text);
  font: var(--text-body-small);
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-md);
}

.search {
  position: relative;
  width: 300px;
}

.search__icon {
  position: absolute;
  top: 50%;
  left: 10px;
  translate: 0 -50%;
  color: var(--color-text-muted);
  pointer-events: none;
}

.search__input {
  padding-left: 32px;
}

.status-filter {
  width: 200px;
}

.view-toggle {
  display: inline-flex;
  margin-left: auto;
  padding: 2px;
  border: 1px solid var(--color-control-border);
  border-radius: var(--radius-button);
  background: var(--color-surface);
}

.view-toggle__btn {
  display: inline-flex;
  align-items: center;
  gap: var(--space-xs);
  height: 28px;
  padding: 0 var(--space-md);
  border: 0;
  border-radius: var(--radius-control);
  background: transparent;
  color: var(--color-text-muted);
  font: var(--text-body);
  cursor: pointer;
  transition-property: background-color, color;
  transition-duration: var(--duration-fast);
  transition-timing-function: var(--ease-standard);
}

.view-toggle__btn:hover {
  color: var(--color-text);
}

.view-toggle__btn[aria-pressed='true'] {
  background: var(--color-nav-active);
  color: var(--color-primary-text);
  font-weight: 700;
}

/* ---- Card grid -------------------------------------------------------- */
.grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--space-lg);
}

.robot {
  display: flex;
  flex-direction: column;
  height: 100%;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-card);
  background: var(--color-surface);
  transition: background-color var(--duration-base) var(--ease-standard);
}

.robot__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-md);
  padding: var(--space-lg) var(--space-lg) 0;
}

.robot__ident {
  min-width: 0;
}

.robot__name {
  font: var(--text-brand);
  overflow-wrap: anywhere;
}

.robot__id {
  margin-top: 2px;
  font: var(--text-small);
  color: var(--color-text-muted);
  overflow-wrap: anywhere;
}

.robot__facts {
  display: grid;
  gap: var(--space-md);
  padding: var(--space-lg);
}

.fact {
  display: grid;
  grid-template-columns: 72px minmax(0, 1fr);
  align-items: center;
  min-height: 20px;
}

.fact dt {
  font: var(--text-meta);
  color: var(--color-text-muted);
}

.fact dd {
  min-width: 0;
  font: var(--text-value);
  overflow-wrap: anywhere;
}

.mission-link {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  color: var(--color-primary-text);
  font-weight: 600;
  text-decoration: none;
  border-radius: var(--radius-control);
}

.mission-link:hover {
  text-decoration: underline;
}

.robot__foot {
  margin-top: auto;
  padding: 10px var(--space-lg);
  border-top: 1px solid var(--color-divider);
  font: var(--text-hint);
  color: var(--color-text-muted);
}

.last-seen {
  display: inline-flex;
  align-items: center;
  gap: var(--space-xxs);
  color: var(--color-warning-text);
  font: var(--text-hint);
  font-style: italic;
}

.robot--offline {
  background: var(--color-offline-surface);
}

.robot--offline .robot__name,
.robot--offline .fact dd {
  color: var(--color-text-muted);
}

.robot--skeleton .robot__facts {
  gap: var(--space-lg);
}

/* ---- Table ------------------------------------------------------------ */
.table-wrap {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-card);
  background: var(--color-surface);
  overflow-x: auto;
}

.table {
  width: 100%;
  border-collapse: collapse;
}

.table thead th {
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

.table tbody th,
.table td {
  height: var(--table-row-height);
  padding: 0 var(--space-lg);
  border-top: 1px solid var(--color-divider);
  text-align: left;
  white-space: nowrap;
}

.table tbody tr:first-child > * {
  border-top: 0;
}

.table tbody tr:hover > * {
  background: var(--color-row-highlight);
}

.cell-name {
  font: var(--text-strong);
}

.cell-battery {
  width: 200px;
  min-width: 160px;
}

.row--offline > * {
  background: var(--color-offline-surface);
}

.row--offline .cell-name {
  color: var(--color-text-muted);
}

/* ---- Empty / foot ----------------------------------------------------- */
.empty {
  display: grid;
  justify-items: center;
  gap: var(--space-sm);
  padding: var(--space-xxl) var(--space-lg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-card);
  background: var(--color-surface);
  color: var(--color-text-muted);
  text-align: center;
}

.empty__title {
  color: var(--color-text);
  font: var(--text-strong);
}

.empty .btn {
  margin-top: var(--space-xs);
}

.foot {
  margin-top: var(--space-md);
  font: var(--text-small);
  color: var(--color-text-muted);
}

/* ---- 768–1099 px: 2 × 2 tiles, 2-column grid -------------------------- */
@media (max-width: 1099px) {
  .tiles {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

/* ---- < 768 px: one column, stacked toolbar, table rows as cards ------- */
@media (max-width: 767px) {
  .tiles {
    gap: var(--space-md);
  }

  .grid {
    grid-template-columns: minmax(0, 1fr);
    gap: var(--space-md);
  }

  .search {
    width: 100%;
  }

  .status-filter {
    flex: 1;
    width: auto;
  }

  .view-toggle__btn {
    height: var(--touch-target);
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
  .table tbody {
    display: block;
  }

  .table tbody {
    display: grid;
    gap: var(--space-md);
  }

  .table tr {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: var(--space-sm) var(--space-md);
    padding: var(--space-lg);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-card);
    background: var(--color-surface);
  }

  .table tr.row--offline {
    background: var(--color-offline-surface);
  }

  .table tbody th,
  .table td {
    display: grid;
    grid-template-columns: 72px minmax(0, 1fr);
    grid-column: 1 / -1;
    align-items: center;
    height: auto;
    padding: 0;
    border: 0;
    background: transparent;
    white-space: normal;
  }

  .table tbody tr:hover > * {
    background: transparent;
  }

  .table td::before {
    content: attr(data-label);
    color: var(--color-text-muted);
    font: var(--text-meta);
  }

  .table tbody th.cell-name,
  .table td.cell-status {
    display: block;
    grid-column: auto;
  }

  .table td.cell-status::before {
    content: none;
  }

  .table td.cell-status {
    order: -1;
    justify-self: end;
  }

  .table tbody th.cell-name {
    order: -2;
  }

  .cell-battery {
    width: auto;
    min-width: 0;
  }
}
</style>
