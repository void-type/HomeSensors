<script lang="ts" setup>
import type { CameraSnapshotResponse, CameraSnapshotTimelineItem } from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import { computed, onMounted, reactive, ref } from 'vue';
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
const previewContainer = ref<HTMLElement | null>(null);

const useLargeImage = computed(() => zoomLevel.value >= 2);

const previewSrc = computed(() => {
  if (!data.selectedItem) {
    return null;
  }
  return useLargeImage.value ? data.selectedItem.largeUrl : data.selectedItem.mediumUrl;
});

const previewStyle = computed(() => ({
  transform: `scale(${zoomLevel.value}) translate(${panX.value / zoomLevel.value}px, ${panY.value / zoomLevel.value}px)`,
  transformOrigin: 'center center',
  transition: isPanning.value ? 'none' : 'transform 0.1s ease',
  cursor: isPanning.value ? 'grabbing' : (zoomLevel.value > 1 ? 'grab' : 'zoom-in'),
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

  data.isLoadingTimeline = true;
  data.items = [];
  data.selectedItem = null;

  try {
    const response = await api().cameraSnapshotTimelineGetItems({
      cameraId: data.selectedCameraId,
      start: data.startDate ? data.startDate.toISOString() : undefined,
      end: data.endDate ? data.endDate.toISOString() : undefined,
    });
    data.items = response.data;
    if (data.items.length > 0) {
      data.selectedItem = data.items[0] ?? null;
    }
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
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

// --- Zoom / pan ---

function onWheel(event: WheelEvent) {
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

function resetZoom() {
  zoomLevel.value = 1;
  panX.value = 0;
  panY.value = 0;
}

function onPreviewClick() {
  if (data.selectedItem && zoomLevel.value === 1) {
    window.open(data.selectedItem.originalUrl, '_blank');
  }
}

// --- Strip navigation ---

function selectItem(item: CameraSnapshotTimelineItem) {
  data.selectedItem = item;
  resetZoom();
}

function onStripKeyDown(event: KeyboardEvent) {
  if (!data.selectedItem) {
    return;
  }
  const idx = data.items.indexOf(data.selectedItem);
  if (event.key === 'ArrowRight' && idx < data.items.length - 1) {
    const nextItem = data.items[idx + 1];
    if (nextItem) {
      selectItem(nextItem);
      scrollStripToIndex(idx + 1);
    }
  } else if (event.key === 'ArrowLeft' && idx > 0) {
    const prevItem = data.items[idx - 1];
    if (prevItem) {
      selectItem(prevItem);
      scrollStripToIndex(idx - 1);
    }
  }
}

function scrollStripToIndex(index: number) {
  const strip = document.getElementById('snapshot-strip');
  const item = strip?.children[index] as HTMLElement | undefined;
  item?.scrollIntoView({ inline: 'nearest', behavior: 'smooth' });
}

function formatTimestamp(timestamp: string | undefined): string {
  if (!timestamp) {
    return '';
  }
  return new Date(timestamp).toLocaleString();
}

// --- Lifecycle ---

onMounted(async () => {
  // Restore from query params
  const hasQueryParams = !!(route.query.cameraId || route.query.start || route.query.end);

  if (route.query.cameraId) {
    data.selectedCameraId = Number(route.query.cameraId);
  }
  if (route.query.start) {
    data.startDate = new Date(route.query.start as string);
  }
  if (route.query.end) {
    data.endDate = new Date(route.query.end as string);
  }

  await getCameras();

  // Only auto-load when restoring from URL state; otherwise wait for user to click Load
  if (hasQueryParams) {
    await loadTimeline();
  }
});
</script>

<template>
  <div class="container-xxl">
    <AppPageHeading />

    <!-- Controls bar -->
    <div class="mt-3 d-flex flex-wrap gap-2 align-items-end">
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

      <div>
        <label for="start-date" class="form-label mb-1">From</label>
        <AppDateTimePicker id="start-date" v-model="data.startDate" />
      </div>

      <div>
        <label for="end-date" class="form-label mb-1">To</label>
        <AppDateTimePicker id="end-date" v-model="data.endDate" />
      </div>

      <button class="btn btn-primary" :disabled="data.isLoadingTimeline" @click="loadTimeline()">
        <span v-if="data.isLoadingTimeline" class="spinner-border spinner-border-sm me-1" />
        Load
      </button>

      <button
        v-if="data.startDate || data.endDate"
        class="btn btn-outline-secondary"
        @click="data.startDate = undefined; data.endDate = undefined; loadTimeline()"
      >
        Clear dates
      </button>
    </div>

    <!-- Preview panel -->
    <div v-if="data.selectedItem" class="preview-panel mt-3">
      <div class="preview-meta mb-1 d-flex align-items-center gap-3">
        <span class="text-body-secondary small">{{ formatTimestamp(data.selectedItem.timestamp) }}</span>
        <span class="text-body-secondary small">{{ data.selectedItem.fileName }}</span>
        <span v-if="zoomLevel > 1" class="badge bg-secondary small">{{ Math.round(zoomLevel * 100) }}%</span>
        <button v-if="zoomLevel > 1" class="btn btn-outline-secondary btn-sm py-0" @click="resetZoom()">
          Reset zoom
        </button>
        <a :href="data.selectedItem.originalUrl" target="_blank" rel="noopener noreferrer" class="btn btn-outline-secondary btn-sm py-0 ms-auto">
          Open original
        </a>
      </div>

      <div
        ref="previewContainer"
        class="preview-container"
        @wheel.prevent="onWheel"
        @mousedown="onMouseDown"
        @mousemove="onMouseMove"
        @mouseup="onMouseUp"
        @mouseleave="onMouseLeave"
        @click="onPreviewClick"
      >
        <img
          :src="previewSrc ?? undefined"
          :alt="data.selectedItem.fileName"
          class="preview-image"
          :style="previewStyle"
          draggable="false"
        >
      </div>
      <p class="text-body-secondary small mt-1">
        Scroll to zoom · Drag to pan when zoomed · Click at 1× to open original
      </p>
    </div>

    <div v-else-if="data.isLoadingTimeline" class="mt-4 text-center text-body-secondary">
      <span class="spinner-border spinner-border-sm me-2" />
      Loading timeline…
    </div>

    <div v-else-if="data.cameras.length === 0 && !data.isLoadingCameras" class="mt-4 text-center text-body-secondary">
      No cameras configured. <router-link :to="{ name: 'cameraSnapshotCamerasMain' }">
        Add a camera
      </router-link>.
    </div>

    <div v-else-if="data.items.length === 0 && !data.isLoadingTimeline && data.selectedCameraId" class="mt-4 text-center text-body-secondary">
      No snapshots found for this date range.
    </div>

    <!-- Scrub strip -->
    <div
      v-if="data.items.length > 0"
      id="snapshot-strip"
      class="snapshot-strip mt-2"
      tabindex="0"
      role="listbox"
      aria-label="Snapshot timeline"
      @keydown="onStripKeyDown"
    >
      <button
        v-for="item in data.items"
        :key="item.fileName"
        class="strip-item"
        :class="{ 'strip-item--active': data.selectedItem?.fileName === item.fileName }"
        role="option"
        :aria-selected="data.selectedItem?.fileName === item.fileName"
        :title="formatTimestamp(item.timestamp)"
        @click="selectItem(item)"
      >
        <img
          :src="item.smallUrl"
          :alt="item.fileName"
          class="strip-thumb"
          loading="lazy"
          draggable="false"
        >
      </button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.preview-panel {
  background: var(--bs-secondary-bg);
  border-radius: 0.5rem;
  padding: 0.75rem;
}

.preview-container {
  overflow: hidden;
  max-height: 75vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #000;
  border-radius: 0.375rem;
  user-select: none;
}

.preview-image {
  max-width: 100%;
  height: auto;
  display: block;
  pointer-events: none;
}

.snapshot-strip {
  display: flex;
  gap: 4px;
  overflow-x: auto;
  padding: 6px 0;
  scroll-snap-type: x proximity;

  &:focus {
    outline: 2px solid var(--bs-primary);
    outline-offset: 2px;
  }
}

.strip-item {
  flex: 0 0 auto;
  padding: 0;
  border: 2px solid transparent;
  border-radius: 4px;
  background: none;
  cursor: pointer;
  scroll-snap-align: start;
  transition: border-color 0.1s;

  &:hover {
    border-color: var(--bs-primary-border-subtle);
  }

  &--active {
    border-color: var(--bs-primary);
  }
}

.strip-thumb {
  height: 90px;
  width: auto;
  display: block;
  border-radius: 2px;
}
</style>
