<script lang="ts" setup>
import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router';
import type { CameraSnapshotResponse, IItemSetOfIFailure } from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import type { ModalParameters } from '@/models/ModalParameters';
import { Collapse } from 'bootstrap';
import { nextTick, onBeforeUnmount, onMounted, reactive } from 'vue';
import { onBeforeRouteLeave, onBeforeRouteUpdate } from 'vue-router';
import AppPageHeading from '@/components/AppPageHeading.vue';
import ApiHelper from '@/models/ApiHelper';
import useAppStore from '@/stores/appStore';
import useMessageStore from '@/stores/messageStore';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelper.client;

const data = reactive({
  cameras: [] as Array<CameraSnapshotResponse>,
  errors: [] as Array<string>,
  originalCameras: new Map<number, string>(),
  hasDirtyCameras: false,
});

function trackOriginalState(camera: CameraSnapshotResponse) {
  if (camera.id !== undefined) {
    data.originalCameras.set(
      camera.id,
      JSON.stringify({
        name: camera.name,
        snapshotsPath: camera.snapshotsPath,
        isHidden: camera.isHidden,
      }),
    );
  }
}

function isCameraDirty(camera: CameraSnapshotResponse): boolean {
  if (camera.id === 0) {
    return true;
  }
  if (camera.id === undefined) {
    return false;
  }

  const original = data.originalCameras.get(camera.id);
  if (!original) {
    return false;
  }

  return original !== JSON.stringify({
    name: camera.name,
    snapshotsPath: camera.snapshotsPath,
    isHidden: camera.isHidden,
  });
}

function updateDirtyState() {
  data.hasDirtyCameras = data.cameras.some(c => isCameraDirty(c));
}

function handleBeforeUnload(event: BeforeUnloadEvent) {
  if (data.hasDirtyCameras) {
    event.preventDefault();
    event.returnValue = '';
    return '';
  }
  return null;
}

function onCameraInput() {
  updateDirtyState();
}

async function getCameras() {
  try {
    const response = await api().cameraSnapshotsGetAll();
    data.cameras = response.data;
    data.cameras.forEach(c => trackOriginalState(c));
    updateDirtyState();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function newCamera() {
  if (data.cameras.findIndex(c => (c.id || 0) < 1) > -1) {
    return;
  }

  data.cameras.unshift({
    id: 0,
    name: '',
    snapshotsPath: '',
    isHidden: false,
  });
  updateDirtyState();

  await nextTick();
  const collapseEl = document.getElementById('collapse-0');
  if (collapseEl) {
    new Collapse(collapseEl, { toggle: false }).show();
  }
}

async function reallyDeleteCamera(camera: CameraSnapshotResponse) {
  if (camera.id === null || typeof camera.id === 'undefined') {
    return;
  }

  try {
    const response = await api().cameraSnapshotsDelete({ id: camera.id });
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }
    await getCameras();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function deleteCamera(camera: CameraSnapshotResponse) {
  const parameters: ModalParameters = {
    title: 'Delete camera',
    description: 'Do you really want to delete this camera? This will not delete any snapshots or cached thumbnails.',
    okAction: () => reallyDeleteCamera(camera),
  };
  appStore.showModal(parameters);
}

async function saveCamera(camera: CameraSnapshotResponse): Promise<boolean> {
  data.errors = [];

  const request = {
    id: camera.id,
    name: camera.name,
    snapshotsPath: camera.snapshotsPath || '',
    isHidden: camera.isHidden,
  };

  try {
    const response = await api().cameraSnapshotsSave(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    const isNewCamera = camera.id === 0;
    if (isNewCamera) {
      const newItem = data.cameras.find(c => c.id === 0);
      if (newItem) {
        newItem.id = response.data.id;
        trackOriginalState(newItem);
      }
    } else {
      const existingIndex = data.cameras.findIndex(c => c.id === camera.id);
      if (existingIndex >= 0) {
        data.cameras[existingIndex] = { ...data.cameras[existingIndex], ...request };
        trackOriginalState(data.cameras[existingIndex]);
      }
    }

    updateDirtyState();
    return true;
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);
    const failures = (response.error as IItemSetOfIFailure).items || [];
    failures.forEach(x => data.errors.push(`${x.uiHandle}-${camera.id}`));
    return false;
  }
}

async function saveAllDirty() {
  const dirtyItems = data.cameras.filter(c => isCameraDirty(c));
  for (const item of dirtyItems) {
    if (!await saveCamera(item)) {
      break;
    }
  }
}

function beforeRouteChange(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) {
  if (data.hasDirtyCameras) {
    const parameters: ModalParameters = {
      title: 'Unsaved changes',
      description: 'You have unsaved changes. Do you really want to leave?',
      okAction: () => next(),
      cancelAction: () => next(false),
    };
    appStore.showModal(parameters);
  } else {
    next();
  }
}

onBeforeRouteUpdate(beforeRouteChange);
onBeforeRouteLeave(beforeRouteChange);

onMounted(async () => {
  await getCameras();
  window.addEventListener('beforeunload', handleBeforeUnload);
});

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
});
</script>

<template>
  <div class="container-xxl">
    <AppPageHeading />
    <div class="mt-4">
      <button
        class="btn btn-primary"
        :disabled="!data.hasDirtyCameras"
        @click="saveAllDirty()"
      >
        Save All
      </button>
      <button class="btn btn-secondary ms-2" @click="newCamera()">
        New
      </button>
      <div id="camerasAccordion" class="accordion mt-4">
        <div v-for="camera in data.cameras" :key="camera.id" class="accordion-item">
          <h2 :id="`heading-${camera.id}`" class="accordion-header">
            <button
              class="accordion-button collapsed"
              type="button"
              data-bs-toggle="collapse"
              :data-bs-target="`#collapse-${camera.id}`"
              :aria-expanded="false"
              :aria-controls="`collapse-${camera.id}`"
            >
              <div class="d-flex align-items-center w-100">
                <span class="me-auto">
                  {{ camera.name || "New camera" }}
                  <span
                    v-if="isCameraDirty(camera)"
                    class="badge bg-warning text-dark ms-2"
                    role="button"
                    @click.stop="saveCamera(camera)"
                  >Unsaved</span>
                  <span v-if="camera.isHidden" class="badge bg-secondary ms-2">Hidden</span>
                </span>
              </div>
            </button>
          </h2>
          <div
            :id="`collapse-${camera.id}`"
            class="accordion-collapse collapse"
            :aria-labelledby="`heading-${camera.id}`"
            data-bs-parent="#camerasAccordion"
          >
            <div class="accordion-body">
              <div class="grid gap-sm">
                <div class="g-col-12 g-col-md-6">
                  <label :for="`name-${camera.id}`" class="form-label">Name</label>
                  <input
                    :id="`name-${camera.id}`"
                    v-model="camera.name"
                    class="form-control form-control-sm"
                    :class="{ 'is-invalid': data.errors.includes(`name-${camera.id}`) }"
                    type="text"
                    @input="onCameraInput"
                  >
                </div>
                <div class="g-col-12 g-col-md-6">
                  <label :for="`path-${camera.id}`" class="form-label">Snapshots Path</label>
                  <input
                    :id="`path-${camera.id}`"
                    v-model="camera.snapshotsPath"
                    class="form-control form-control-sm"
                    :class="{ 'is-invalid': data.errors.includes(`snapshotsPath-${camera.id}`) }"
                    type="text"
                    placeholder="/mnt/snapshots/backyard"
                    @input="onCameraInput"
                  >
                </div>
                <div class="g-col-12">
                  <div class="form-check">
                    <input
                      :id="`hidden-${camera.id}`"
                      v-model="camera.isHidden"
                      class="form-check-input"
                      :class="{ 'is-invalid': data.errors.includes(`hidden-${camera.id}`) }"
                      type="checkbox"
                      @change="onCameraInput"
                    >
                    <label :for="`hidden-${camera.id}`" class="form-check-label">Hidden</label>
                  </div>
                </div>
                <div v-if="camera.id" class="g-col-12">
                  <small class="text-body-primary">ID: {{ camera.id }}</small>
                </div>
                <div class="g-col-12">
                  <div class="btn-toolbar">
                    <button class="btn btn-sm btn-primary me-2" @click="saveCamera(camera)">
                      Save
                    </button>
                    <router-link
                      v-if="camera.id"
                      :to="{ name: 'cameraSnapshots', query: { cameraId: String(camera.id) } }"
                      class="btn btn-sm btn-secondary me-2"
                    >
                      View Timeline
                    </router-link>
                    <button class="btn btn-sm btn-danger ms-auto" @click="deleteCamera(camera)">
                      Delete
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="data.cameras.length < 1" class="text-center mt-4">
          No cameras configured.
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
