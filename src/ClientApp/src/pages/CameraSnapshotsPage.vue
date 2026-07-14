<script lang="ts" setup>
import type { CameraSnapshotResponse, CameraSnapshotTimelineItem } from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { Tooltip } from 'bootstrap';
import { computed, nextTick, onMounted, onUnmounted, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppDateTimePicker from '@/components/AppDateTimePicker.vue';
import AppPageHeading from '@/components/AppPageHeading.vue';
import ApiHelpers from '@/models/ApiHelpers';
import useMessageStore from '@/stores/messageStore';

const messageStore = useMessageStore();
const api = ApiHelpers.client;
const route = useRoute();
const router = useRouter();

// --- State ---

const data = reactive({
  cameras: [] as Array<CameraSnapshotResponse>,
  items: [] as Array<CameraSnapshotTimelineItem>,
  selectedCameraId: null as number | null,
  startDate: undefined as Date | undefined,
  endDate: undefined as Date | undefined,
  selectedItem: null as CameraSnapshotTimelineItem | null,
  isLoadingCameras: false,
  isLoadingTimeline: false,
});

// Zoom/pan state
const zoomLevel = ref(1);
const panX = ref(0);
const panY = ref(0);
const isPanning = ref(false);
const lastMousePos = ref({ x: 0, y: 0 });
const lastTouchDistance = ref<number | null>(null);
const previewContainer = ref<HTMLElement | null>(null);
const stripEl = ref<HTMLElement | null>(null);

// Info tooltip
const infoBtn = ref<HTMLElement | null>(null);
let bsTooltip: Tooltip | null = null;

function syncTooltip() {
  bsTooltip?.dispose();
  bsTooltip = null;
  if (infoBtn.value) {
    bsTooltip = new Tooltip(infoBtn.value, { html: true, placement: 'bottom' });
  }
}

watch(infoBtn, syncTooltip);
watch(() => data.selectedItem, syncTooltip);
onUnmounted(() => bsTooltip?.dispose());

const useLargeImage = computed(() => zoomLevel.value >= 2);

const previewSrc = computed(() => {
  if (!data.selectedItem) {
    return null;
  }
  return useLargeImage.value ? data.selectedItem.originalUrl : data.selectedItem.mediumUrl;
});

const previewStyle = computed(() => ({
  transform: `scale(${zoomLevel.value}) translate(${panX.value / zoomLevel.value}px, ${panY.value / zoomLevel.value}px)`,
  transformOrigin: 'center center',
  transition: isPanning.value ? 'none' : 'transform 0.1s ease',
  cursor: isPanning.value ? 'grabbing' : (zoomLevel.value > 1 ? 'grab' : 'default'),
  pointerEvents: 'none' as const,
}));

// --- API ---

async function getCameras() {
  data.isLoadingCameras = true;
  try {
    const response = await api().cameraSnapshotsGetAll();
    data.cameras = response.data.filter(c => !c.isHidden);
    if (data.cameras.length > 0 && data.selectedCameraId === null) {
      const firstCamera = data.cameras[0];
      data.selectedCameraId = firstCamera !== undefined && firstCamera.id !== undefined ? firstCamera.id : null;
    }
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  } finally {
    data.isLoadingCameras = false;
  }
}

async function loadTimeline() {
  if (!data.selectedCameraId) {
    return;
  }

  if (!data.startDate || !data.endDate) {
    messageStore.setErrorMessage('Both start and end dates are required.');
    return;
  }

  if (data.endDate <= data.startDate) {
    messageStore.setErrorMessage('End date must be after start date.');
    return;
  }

  const sixMonths = 182 * 24 * 60 * 60 * 1000;
  if (data.endDate.getTime() - data.startDate.getTime() > sixMonths) {
    messageStore.setErrorMessage('Date range cannot exceed 6 months.');
    return;
  }

  data.isLoadingTimeline = true;
  data.items = [];
  data.selectedItem = null;

  try {
    const response = await api().cameraSnapshotTimelineGetItems(
      {
        cameraId: data.selectedCameraId,
        start: data.startDate ? data.startDate.toISOString() : undefined,
        end: data.endDate ? data.endDate.toISOString() : undefined,
      },
      { signal: AbortSignal.timeout(30_000) },
    );
    data.items = response.data;
    if (data.items.length > 0) {
      const lastIndex = data.items.length - 1;
      data.selectedItem = data.items[lastIndex] ?? null;
      await nextTick();
      scrollStripToIndex(lastIndex);
    }
  } catch (error) {
    if (error instanceof DOMException && error.name === 'TimeoutError') {
      messageStore.setErrorMessage('Request timed out. Please try again.');
    } else {
      messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
    }
  } finally {
    data.isLoadingTimeline = false;
  }

  await syncQueryParams();
}

async function syncQueryParams() {
  await router.replace({
    name: 'cameraSnapshots',
    query: {
      cameraId: data.selectedCameraId ? String(data.selectedCameraId) : undefined,
      start: data.startDate ? data.startDate.toISOString() : undefined,
      end: data.endDate ? data.endDate.toISOString() : undefined,
    },
  });
}

// --- Date range helpers ---

function adjustDateRange(params: { days?: number; weeks?: number; months?: number }) {
  if (!data.startDate || !data.endDate) {
    return;
  }
  const totalDays = (params.days ?? 0) + (params.weeks ?? 0) * 7 + (params.months ?? 0) * 28;
  const spanMs = data.endDate.getTime() - data.startDate.getTime();
  const newStart = new Date(data.startDate.getTime() + totalDays * 24 * 60 * 60 * 1000);
  data.startDate = newStart;
  data.endDate = new Date(newStart.getTime() + spanMs);
}

function setAbsoluteRange(days: number) {
  data.endDate = defaultEndDate();
  const start = defaultEndDate();
  start.setDate(start.getDate() - days);
  data.startDate = start;
}

// --- Zoom / pan ---

function onWheel(event: WheelEvent) {
  if (!event.ctrlKey && !event.metaKey) {
    return;
  }
  event.preventDefault();
  const delta = event.deltaY > 0 ? -0.25 : 0.25;
  zoomLevel.value = Math.max(1, Math.min(8, zoomLevel.value + delta));

  if (zoomLevel.value === 1) {
    panX.value = 0;
    panY.value = 0;
  }
}

function onMouseDown(event: MouseEvent) {
  if (zoomLevel.value <= 1) {
    return;
  }
  isPanning.value = true;
  lastMousePos.value = { x: event.clientX, y: event.clientY };
}

function onMouseMove(event: MouseEvent) {
  if (!isPanning.value) {
    return;
  }
  panX.value += event.clientX - lastMousePos.value.x;
  panY.value += event.clientY - lastMousePos.value.y;
  lastMousePos.value = { x: event.clientX, y: event.clientY };
}

function onMouseUp() {
  isPanning.value = false;
}

function onMouseLeave() {
  isPanning.value = false;
}

function getTouchDistance(touches: TouchList): number {
  return Math.hypot(
    touches[1]!.clientX - touches[0]!.clientX,
    touches[1]!.clientY - touches[0]!.clientY,
  );
}

function onTouchStart(event: TouchEvent) {
  if (event.touches.length === 1) {
    if (zoomLevel.value <= 1) {
      return;
    }
    isPanning.value = true;
    lastMousePos.value = { x: event.touches[0]!.clientX, y: event.touches[0]!.clientY };
    lastTouchDistance.value = null;
  } else if (event.touches.length === 2) {
    isPanning.value = false;
    lastTouchDistance.value = getTouchDistance(event.touches);
  }
}

function onTouchMove(event: TouchEvent) {
  if (event.touches.length === 1 && isPanning.value) {
    const touch = event.touches[0]!;
    panX.value += touch.clientX - lastMousePos.value.x;
    panY.value += touch.clientY - lastMousePos.value.y;
    lastMousePos.value = { x: touch.clientX, y: touch.clientY };
  } else if (event.touches.length === 2 && lastTouchDistance.value !== null) {
    const dist = getTouchDistance(event.touches);
    const delta = (dist - lastTouchDistance.value) * 0.02;
    zoomLevel.value = Math.max(1, Math.min(8, zoomLevel.value + delta));
    lastTouchDistance.value = dist;
    if (zoomLevel.value === 1) {
      panX.value = 0;
      panY.value = 0;
    }
  }
}

function onTouchEnd() {
  isPanning.value = false;
  lastTouchDistance.value = null;
}

function resetZoom() {
  zoomLevel.value = 1;
  panX.value = 0;
  panY.value = 0;
}

// --- Strip navigation ---

function selectItem(item: CameraSnapshotTimelineItem) {
  data.selectedItem = item;
}

function navFirst() {
  const first = data.items[0];
  if (first) {
    selectItem(first);
    scrollStripToIndex(0);
  }
}

function navPrev() {
  if (!data.selectedItem) {
    return;
  }
  const idx = data.items.indexOf(data.selectedItem);
  if (idx > 0) {
    const item = data.items[idx - 1]!;
    selectItem(item);
    scrollStripToIndex(idx - 1);
  }
}

function navNext() {
  if (!data.selectedItem) {
    return;
  }
  const idx = data.items.indexOf(data.selectedItem);
  if (idx < data.items.length - 1) {
    const item = data.items[idx + 1]!;
    selectItem(item);
    scrollStripToIndex(idx + 1);
  }
}

function navLast() {
  const last = data.items[data.items.length - 1];
  if (last) {
    selectItem(last);
    scrollStripToIndex(data.items.length - 1);
  }
}

const selectedIndex = computed(() => data.selectedItem ? data.items.indexOf(data.selectedItem) : -1);

function onStripKeyDown(event: KeyboardEvent) {
  if (!data.selectedItem) {
    return;
  }
  const idx = data.items.indexOf(data.selectedItem);
  if (event.key === 'ArrowRight' && idx < data.items.length - 1) {
    event.preventDefault();
    const nextItem = data.items[idx + 1];
    if (nextItem) {
      selectItem(nextItem);
      scrollStripToIndex(idx + 1);
    }
  } else if (event.key === 'ArrowLeft' && idx > 0) {
    event.preventDefault();
    const prevItem = data.items[idx - 1];
    if (prevItem) {
      selectItem(prevItem);
      scrollStripToIndex(idx - 1);
    }
  }
}

function focusStrip() {
  stripEl.value?.focus();
}

function scrollStripToIndex(index: number) {
  const strip = stripEl.value;
  const item = strip?.children[index] as HTMLElement | undefined;
  item?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
}

function formatTimestamp(timestamp: string | undefined): string {
  if (!timestamp) {
    return '';
  }
  const d = new Date(timestamp);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

function defaultEndDate(): Date {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  d.setHours(0, 0, 0, 0);
  return d;
}

function defaultStartDate(): Date {
  const d = defaultEndDate();
  d.setDate(d.getDate() - 1);
  return d;
}

// --- Lifecycle ---

onMounted(async () => {
  // Restore from query params
  if (route.query.cameraId) {
    data.selectedCameraId = Number(route.query.cameraId);
  }
  if (route.query.start) {
    data.startDate = new Date(route.query.start as string);
  } else {
    data.startDate = defaultStartDate();
  }
  if (route.query.end) {
    data.endDate = new Date(route.query.end as string);
  } else {
    data.endDate = defaultEndDate();
  }

  await getCameras();
  await loadTimeline();

  // Reload whenever camera or date range changes
  watch(
    () => [data.selectedCameraId, data.startDate?.getTime(), data.endDate?.getTime()],
    () => loadTimeline(),
  );
});
</script>

<template>
  <div class="container-xxl">
    <AppPageHeading />

    <!-- Controls bar -->
    <div class="mt-3 d-flex flex-column align-items-center gap-2">
      <div class="d-flex flex-wrap justify-content-center gap-2 align-items-end">
        <div>
          <label for="camera-select" class="form-label mb-1">Camera</label>
          <select
            id="camera-select"
            v-model="data.selectedCameraId"
            class="form-select"
            style="min-width: 160px"
          >
            <option v-for="cam in data.cameras" :key="cam.id" :value="cam.id">
              {{ cam.name }}
            </option>
          </select>
        </div>

        <div class="d-flex gap-2 align-items-end">
          <div>
            <label for="start-date" class="form-label mb-1">From</label>
            <AppDateTimePicker id="start-date" v-model="data.startDate" />
          </div>

          <div>
            <label for="end-date" class="form-label mb-1">To</label>
            <AppDateTimePicker id="end-date" v-model="data.endDate" />
          </div>
        </div>
      </div>

      <!-- xs: each pair stacked -->
      <div class="d-flex d-sm-none flex-column align-items-center gap-2">
        <div class="btn-group btn-group-sm w-100">
          <button class="btn btn-outline-primary" title="Back 1 Month" @click="adjustDateRange({ months: -1 })">
            &laquo; Month
          </button>
          <button class="btn btn-outline-primary" title="Forward 1 Month" @click="adjustDateRange({ months: 1 })">
            Month &raquo;
          </button>
        </div>
        <div class="btn-group btn-group-sm w-100">
          <button class="btn btn-outline-primary" title="Back 1 Week" @click="adjustDateRange({ weeks: -1 })">
            &laquo; Week
          </button>
          <button class="btn btn-outline-primary" title="Forward 1 Week" @click="adjustDateRange({ weeks: 1 })">
            Week &raquo;
          </button>
        </div>
        <div class="btn-group btn-group-sm w-100">
          <button class="btn btn-outline-primary" title="Back 1 Day" @click="adjustDateRange({ days: -1 })">
            &laquo; Day
          </button>
          <button class="btn btn-outline-primary" title="Forward 1 Day" @click="adjustDateRange({ days: 1 })">
            Day &raquo;
          </button>
        </div>
        <div class="btn-group btn-group-sm w-100">
          <button class="btn btn-outline-primary" title="Last month" @click="setAbsoluteRange(30)">
            Last Month
          </button>
          <button class="btn btn-outline-primary" title="Last week" @click="setAbsoluteRange(7)">
            Last Week
          </button>
          <button class="btn btn-outline-primary" title="Last day" @click="setAbsoluteRange(1)">
            Last Day
          </button>
        </div>
      </div>

      <!-- sm+: single combined bar -->
      <div class="d-none d-sm-flex flex-column align-items-center gap-2">
        <div class="btn-group btn-group-sm">
          <button class="btn btn-outline-primary" title="Back 1 Month" @click="adjustDateRange({ months: -1 })">
            &laquo; Month
          </button>
          <button class="btn btn-outline-primary" title="Back 1 Week" @click="adjustDateRange({ weeks: -1 })">
            &laquo; Week
          </button>
          <button class="btn btn-outline-primary" title="Back 1 Day" @click="adjustDateRange({ days: -1 })">
            &laquo; Day
          </button>
          <button class="btn btn-outline-primary" title="Forward 1 Day" @click="adjustDateRange({ days: 1 })">
            Day &raquo;
          </button>
          <button class="btn btn-outline-primary" title="Forward 1 Week" @click="adjustDateRange({ weeks: 1 })">
            Week &raquo;
          </button>
          <button class="btn btn-outline-primary" title="Forward 1 Month" @click="adjustDateRange({ months: 1 })">
            Month &raquo;
          </button>
        </div>
        <div class="btn-group btn-group-sm">
          <button class="btn btn-outline-primary" title="Last month" @click="setAbsoluteRange(30)">
            Last Month
          </button>
          <button class="btn btn-outline-primary" title="Last week" @click="setAbsoluteRange(7)">
            Last Week
          </button>
          <button class="btn btn-outline-primary" title="Last day" @click="setAbsoluteRange(1)">
            Last Day
          </button>
        </div>
      </div>
    </div>

    <!-- Preview panel -->
    <div v-if="data.selectedItem" class="card mt-3 overflow-hidden">
      <div class="card-body d-flex flex-wrap align-items-center gap-2 py-2">
        <span class="text-body-primary small">{{ formatTimestamp(data.selectedItem.timestamp) }}</span>
        <button
          ref="infoBtn"
          type="button"
          class="btn btn-link btn-sm p-0 text-body-primary"
          :data-bs-title="`${data.selectedItem.fileName}<br><br>Ctrl+Scroll to zoom<br>Drag to pan when zoomed<br>Pinch to zoom on touch<br>← → to navigate`"
        >
          <FontAwesomeIcon icon="fa-circle-info" />
        </button>
        <span v-if="zoomLevel > 1" class="badge bg-secondary small">{{ Math.round(zoomLevel * 100) }}%</span>
        <button v-if="zoomLevel > 1" class="btn btn-outline-primary btn-sm py-0" @click="resetZoom()">
          Reset zoom
        </button>
        <a :href="data.selectedItem.originalUrl" target="_blank" rel="noopener noreferrer" class="btn btn-outline-primary btn-sm py-0 ms-auto">
          Open original
        </a>
      </div>

      <div
        ref="previewContainer"
        class="preview-container overflow-hidden d-flex align-items-center justify-content-center bg-black user-select-none"
        @click="focusStrip"
        @wheel="onWheel"
        @mousedown="onMouseDown"
        @mousemove="onMouseMove"
        @mouseup="onMouseUp"
        @mouseleave="onMouseLeave"
        @touchstart.prevent="onTouchStart"
        @touchmove.prevent="onTouchMove"
        @touchend="onTouchEnd"
      >
        <img
          :src="previewSrc ?? undefined"
          :alt="data.selectedItem.fileName"
          class="mw-100 mh-100 d-block"
          :style="previewStyle"
          draggable="false"
        >
      </div>
    </div>

    <div v-else-if="data.isLoadingTimeline" class="mt-4 text-center text-body-primary">
      <span class="spinner-border spinner-border-sm me-2" />
      Loading timeline…
    </div>

    <div v-else-if="data.cameras.length === 0 && !data.isLoadingCameras" class="mt-4 text-center text-body-primary">
      No cameras configured. <router-link :to="{ name: 'cameraSnapshotCamerasMain' }">
        Add a camera
      </router-link>.
    </div>

    <div v-else-if="data.items.length === 0 && !data.isLoadingTimeline && data.selectedCameraId" class="mt-4 text-center text-body-primary">
      No snapshots found for this date range.
    </div>

    <!-- Mobile snapshot navigation (xs only) -->
    <div v-if="data.items.length > 0 && data.selectedItem" class="d-flex d-sm-none justify-content-around gap-4 mt-3">
      <button class="btn btn-outline-primary btn-sm" :disabled="selectedIndex <= 0" title="First" @click="navFirst()">
        <FontAwesomeIcon icon="fa-angles-left" />
      </button>
      <button class="btn btn-outline-primary btn-sm" :disabled="selectedIndex <= 0" title="Previous" @click="navPrev()">
        <FontAwesomeIcon icon="fa-angle-left" />
      </button>
      <button class="btn btn-outline-primary btn-sm" :disabled="selectedIndex >= data.items.length - 1" title="Next" @click="navNext()">
        <FontAwesomeIcon icon="fa-angle-right" />
      </button>
      <button class="btn btn-outline-primary btn-sm" :disabled="selectedIndex >= data.items.length - 1" title="Last" @click="navLast()">
        <FontAwesomeIcon icon="fa-angles-right" />
      </button>
    </div>

    <!-- Scrub strip -->
    <div
      v-if="data.items.length > 0"
      ref="stripEl"
      class="snapshot-strip d-flex gap-1 overflow-x-auto mt-3"
      tabindex="0"
      role="listbox"
      aria-label="Snapshot timeline"
      @keydown="onStripKeyDown"
    >
      <button
        v-for="item in data.items"
        :key="item.fileName"
        class="strip-item p-0 bg-transparent flex-shrink-0"
        :class="{ 'strip-item--active': data.selectedItem?.fileName === item.fileName }"
        role="option"
        :aria-selected="data.selectedItem?.fileName === item.fileName"
        :title="formatTimestamp(item.timestamp)"
        @click="selectItem(item)"
      >
        <img
          :src="item.smallUrl"
          :alt="item.fileName"
          class="strip-thumb d-block rounded-1"
          loading="lazy"
          draggable="false"
        >
      </button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.preview-container {
  height: 65vh;
  max-height: 65vh;
  touch-action: none;
}

.snapshot-strip {
  scroll-snap-type: x proximity;
  padding: 6px 4px;

  &:focus {
    outline: 2px solid var(--bs-primary);
    outline-offset: 2px;
  }
}

.strip-item {
  border: 2px solid transparent;
  border-radius: var(--bs-border-radius-sm);
  cursor: pointer;
  scroll-snap-align: start;
  transition:
    border-color 0.1s,
    box-shadow 0.1s;

  &:focus-visible {
    outline: 2px solid var(--bs-primary);
    outline-offset: 2px;
  }

  &:hover {
    border-color: var(--bs-primary-border-subtle);
  }

  &--active {
    border-color: var(--bs-primary);
    box-shadow: 0 0 0 2px var(--bs-primary);
  }
}

.strip-thumb {
  height: 90px;
  width: 160px;
  object-fit: cover;
}
</style>
