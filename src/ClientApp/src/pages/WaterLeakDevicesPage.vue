<script lang="ts" setup>
import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router';
import type {
  IItemSetOfIFailure,
  WaterLeakDeviceResponse,
} from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import type { ModalParameters } from '@/models/ModalParameters';
import { Collapse } from 'bootstrap';
import { nextTick, onBeforeUnmount, onMounted, reactive } from 'vue';
import { onBeforeRouteLeave, onBeforeRouteUpdate } from 'vue-router';
import ApiHelpers from '@/models/ApiHelpers';
import useAppStore from '@/stores/appStore';
import useMessageStore from '@/stores/messageStore';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const data = reactive({
  devices: [] as Array<WaterLeakDeviceResponse>,
  errors: [] as Array<string>,
  originalDevices: new Map<number, string>(),
  hasDirtyDevices: false,
});

function trackOriginalState(device: WaterLeakDeviceResponse) {
  if (device.id !== undefined) {
    data.originalDevices.set(
      device.id,
      JSON.stringify({
        name: device.name,
        mqttTopic: device.mqttTopic,
      }),
    );
  }
}

function isDeviceDirty(device: WaterLeakDeviceResponse): boolean {
  if (device.id === 0) {
    return true;
  }
  if (device.id === undefined) {
    return false;
  }

  const original = data.originalDevices.get(device.id);
  if (!original) {
    return false;
  }

  const current = JSON.stringify({
    name: device.name,
    mqttTopic: device.mqttTopic,
  });

  return original !== current;
}

function updateDirtyState() {
  data.hasDirtyDevices = data.devices.some(device => isDeviceDirty(device));
}

function handleBeforeUnload(event: BeforeUnloadEvent) {
  if (data.hasDirtyDevices) {
    event.preventDefault();

    event.returnValue = '';
    return '';
  }
  return null;
}

function onDeviceInput() {
  updateDirtyState();
}

async function getDevices() {
  try {
    const response = await api().waterLeakDevicesGetAll();
    data.devices = response.data.slice().sort((a, b) => (a.name || '').localeCompare(b.name || ''));
    data.devices.forEach(device => trackOriginalState(device));
    updateDirtyState();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function newDevice() {
  if (data.devices.findIndex(x => (x.id || 0) < 1) > -1) {
    return;
  }

  const newDev = {
    id: 0,
    name: '',
    mqttTopic: '',
  };

  data.devices.unshift(newDev);
  updateDirtyState();

  await nextTick();
  const collapseEl = document.getElementById('collapse-0');
  if (collapseEl) {
    const collapse = new Collapse(collapseEl, { toggle: false });
    collapse.show();
  }
}

async function reallyDeleteDevice(device: WaterLeakDeviceResponse) {
  if (device.id === null || typeof device.id === 'undefined') {
    return;
  }

  try {
    const response = await api().waterLeakDevicesDelete({ id: device.id });
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    await getDevices();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function deleteDevice(device: WaterLeakDeviceResponse) {
  const parameters: ModalParameters = {
    title: 'Delete device',
    description: 'Do you really want to delete this water leak device?',
    okAction: () => reallyDeleteDevice(device),
  };

  appStore.showModal(parameters);
}

async function saveDevice(device: WaterLeakDeviceResponse): Promise<boolean> {
  data.errors = [];

  const request = {
    id: device.id,
    name: device.name,
    mqttTopic: device.mqttTopic,
  };

  try {
    const response = await api().waterLeakDevicesSave(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    const isNewDevice = device.id === 0;
    if (isNewDevice) {
      const newItem = data.devices.find(d => d.id === 0);
      if (newItem) {
        newItem.id = response.data.id;
        trackOriginalState(newItem);
        data.devices.sort((a, b) => (a.name || '').localeCompare(b.name || ''));
      }
    } else {
      const existingIndex = data.devices.findIndex(d => d.id === device.id);
      if (existingIndex >= 0) {
        data.devices[existingIndex] = {
          ...data.devices[existingIndex],
          ...request,
        };
        trackOriginalState(data.devices[existingIndex]);
        data.devices.sort((a, b) => (a.name || '').localeCompare(b.name || ''));
      }
    }

    updateDirtyState();
    return true;
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IItemSetOfIFailure).items || [];
    failures.forEach(x => data.errors.push(`${x.uiHandle}-${device.id}`));
    return false;
  }
}

async function saveAllDirty() {
  const dirtyItems = data.devices.filter(item => isDeviceDirty(item));
  for (const item of dirtyItems) {
    if (!await saveDevice(item)) {
      break;
    }
  }
}

function beforeRouteChange(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) {
  if (data.hasDirtyDevices) {
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
  await getDevices();
  window.addEventListener('beforeunload', handleBeforeUnload);
});

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
});
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-3">
      Water Leak Devices
    </h1>
    <div class="mt-4">
      <button class="btn btn-primary" @click="newDevice()">
        New
      </button>
      <button
        class="btn btn-secondary ms-2"
        :disabled="!data.hasDirtyDevices"
        @click="saveAllDirty()"
      >
        Save All
      </button>
      <div id="devicesAccordion" class="accordion mt-4">
        <div v-for="device in data.devices" :key="device.id" class="accordion-item">
          <h2 :id="`heading-${device.id}`" class="accordion-header">
            <button
              class="accordion-button collapsed"
              type="button"
              data-bs-toggle="collapse"
              :data-bs-target="`#collapse-${device.id}`"
              :aria-expanded="false"
              :aria-controls="`collapse-${device.id}`"
            >
              <div class="d-flex align-items-center w-100">
                <span class="me-auto">
                  {{ device.name || "New device" }}
                  <span
                    v-if="isDeviceDirty(device)"
                    class="badge bg-warning text-dark ms-2"
                    role="button"
                    @click.stop="saveDevice(device)"
                  >Unsaved</span>
                </span>
              </div>
            </button>
          </h2>
          <div
            :id="`collapse-${device.id}`"
            class="accordion-collapse collapse"
            :aria-labelledby="`heading-${device.id}`"
            data-bs-parent="#devicesAccordion"
          >
            <div class="accordion-body">
              <div class="grid gap-sm">
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`name-${device.id}`" class="form-label">Name</label>
                  <input
                    :id="`name-${device.id}`"
                    v-model="device.name"
                    required
                    type="text"
                    class="form-control form-control-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`name-${device.id}`),
                    }"
                    @input="onDeviceInput"
                  >
                </div>
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`mqttTopic-${device.id}`" class="form-label">MQTT Topic</label>
                  <input
                    :id="`mqttTopic-${device.id}`"
                    v-model="device.mqttTopic"
                    required
                    type="text"
                    class="form-control form-control-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`mqttTopic-${device.id}`),
                    }"
                    @input="onDeviceInput"
                  >
                </div>
                <div class="g-col-12">
                  <div class="btn-toolbar">
                    <button class="btn btn-sm btn-primary me-2" @click="saveDevice(device)">
                      Save
                    </button>
                    <button
                      v-if="device.id"
                      class="btn btn-sm btn-danger ms-auto"
                      @click="deleteDevice(device)"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="data.devices.length < 1" class="text-center mt-4">
          No water leak devices.
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
