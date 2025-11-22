<script lang="ts" setup>
import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router';
import type {
  IItemSetOfIFailure,
  TemperatureDeviceResponse,
  TemperatureLocationResponse,
} from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import type { ModalParameters } from '@/models/ModalParameters';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { storeToRefs } from 'pinia';
import { onBeforeUnmount, onMounted, reactive } from 'vue';
import { onBeforeRouteLeave, onBeforeRouteUpdate } from 'vue-router';
import ApiHelpers from '@/models/ApiHelpers';
import DateHelpers from '@/models/DateHelpers';
import { formatTempWithUnit } from '@/models/TempFormatHelpers';
import useAppStore from '@/stores/appStore';
import useMessageStore from '@/stores/messageStore';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const { useFahrenheit, staleLimitMinutes } = storeToRefs(appStore);

const data = reactive({
  devices: [] as Array<TemperatureDeviceResponse>,
  locations: [] as Array<TemperatureLocationResponse>,
  errors: [] as Array<string>,
  originalDevices: new Map<number, string>(),
  hasDirtyDevices: false,
});

function trackOriginalState(device: TemperatureDeviceResponse) {
  if (device.id !== undefined) {
    data.originalDevices.set(
      device.id,
      JSON.stringify({
        name: device.name,
        mqttTopic: device.mqttTopic,
        locationId: device.locationId,
        isRetired: device.isRetired,
      }),
    );
  }
}

function isDeviceDirty(device: TemperatureDeviceResponse): boolean {
  if (device.id === 0) {
    return true;
  } // New devices are always dirty
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
    locationId: device.locationId,
    isRetired: device.isRetired,
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

async function getDevices() {
  try {
    const response = await api().temperatureDevicesGetAll();
    data.devices = response.data;
    // Track original state for dirty checking
    data.devices.forEach(device => trackOriginalState(device));
    updateDirtyState();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function getLocations() {
  try {
    const response = await api().temperatureLocationsGetAll();
    data.locations = response.data;
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
    locationId: 0,
    isRetired: false,
  };

  data.devices.unshift(newDev);
  updateDirtyState();
}

function onDeviceInput() {
  updateDirtyState();
}

async function reallyDeleteDevice(device: TemperatureDeviceResponse) {
  if (device.id === null || typeof device.id === 'undefined') {
    return;
  }

  try {
    const response = await api().temperatureDevicesDelete({ id: device.id });
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    await getLocations();
    await getDevices();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function deleteDevice(device: TemperatureDeviceResponse) {
  const parameters: ModalParameters = {
    title: 'Delete device',
    description: 'Do you really want to delete this device? All related readings will be removed!',
    okAction: () => reallyDeleteDevice(device),
  };

  appStore.showModal(parameters);
}

async function saveDevice(device: TemperatureDeviceResponse) {
  data.errors = [];

  const request = {
    id: device.id,
    name: device.name,
    mqttTopic: device.mqttTopic,
    locationId: device.locationId,
    isRetired: device.isRetired,
  };

  try {
    const response = await api().temperatureDevicesSave(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    // Update the device in place
    const isNewDevice = device.id === 0;
    if (isNewDevice) {
      // For new devices, we need to refetch to get the complete data with the new ID
      // But we'll replace just this entry in the list
      const tempIndex = data.devices.findIndex(d => d.id === 0);
      if (tempIndex >= 0) {
        // Remove the temporary entry
        data.devices.splice(tempIndex, 1);
        // Refresh the full list to get the new device with proper ID and relationships
        await getDevices();
      }
    } else {
      // Update existing device in place with the form data
      const existingIndex = data.devices.findIndex(d => d.id === device.id);
      if (existingIndex >= 0) {
        // Merge the updated data while preserving other properties
        data.devices[existingIndex] = {
          ...data.devices[existingIndex],
          ...request,
        };
        // Update original state tracking
        trackOriginalState(data.devices[existingIndex]);
      }
    }

    updateDirtyState();

    // Only refresh locations if needed
    await getLocations();
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IItemSetOfIFailure).items || [];
    failures.forEach(x => data.errors.push(`${x.uiHandle}-${device.id}`));
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
  await getLocations();
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
      Devices
    </h1>
    <div class="mt-4">
      <button class="btn btn-primary" @click="newDevice()">
        New
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
                  <span v-if="isDeviceDirty(device)" class="badge bg-warning text-dark ms-2">Unsaved</span>
                  <span v-if="device.isRetired" class="badge bg-secondary ms-2">Retired</span>
                  <FontAwesomeIcon
                    v-if="device.isInactive"
                    icon="fa-clock"
                    class="text-danger ms-2"
                    :title="`Inactive. Hasn't been seen in ${staleLimitMinutes} minutes.`"
                  />
                  <FontAwesomeIcon
                    v-if="device.isLost"
                    icon="fa-battery-quarter"
                    class="text-danger ms-2"
                    title="Lost. Doesn't have location."
                  />
                  <FontAwesomeIcon
                    v-if="device.isBatteryLevelLow"
                    icon="fa-battery-quarter"
                    class="text-danger ms-2"
                    title="Battery low."
                  />
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
                  <label :for="`mqttTopic-${device.id}`" class="form-label">Topic</label>
                  <div class="input-group input-group-sm">
                    <input
                      :id="`mqttTopic-${device.id}`"
                      v-model="device.mqttTopic"
                      required
                      type="text"
                      class="form-control"
                      :class="{
                        'is-invalid': data.errors.includes(`mqttTopic-${device.id}`),
                      }"
                      @input="onDeviceInput"
                    >
                    <router-link
                      :to="{ name: 'discoveryMain', query: { topic: device.mqttTopic } }"
                      class="btn btn-secondary"
                      title="View MQTT"
                    >
                      <FontAwesomeIcon icon="fa-search" />
                    </router-link>
                  </div>
                </div>
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`location-${device.id}`" class="form-label">Location</label>
                  <select
                    :id="`location-${device.id}`"
                    v-model="device.locationId"
                    class="form-select form-select-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`location-${device.id}`),
                    }"
                    @change="onDeviceInput"
                  >
                    <option :value="0" />
                    <option
                      v-for="location in data.locations"
                      :key="location.id"
                      :value="location.id"
                    >
                      {{ location.name }}
                    </option>
                  </select>
                </div>
                <div class="g-col-12">
                  <div class="form-check">
                    <input
                      :id="`retired-${device.id}`"
                      v-model="device.isRetired"
                      class="form-check-input form-check-input"
                      :class="{
                        'is-invalid': data.errors.includes(`retired-${device.id}`),
                      }"
                      type="checkbox"
                      @change="onDeviceInput"
                    >
                    <label :for="`retired-${device.id}`" class="form-check-label">Retired</label>
                  </div>
                </div>
                <div v-if="device.id" class="g-col-12">
                  <div>
                    <small class="text-body-secondary me-2">ID: {{ device.id }}</small>
                  </div>
                  <div>
                    <small v-if="device.lastReading">Last reading:
                      {{
                        formatTempWithUnit(device.lastReading?.temperatureCelsius, useFahrenheit)
                      }}
                      on
                      {{ DateHelpers.dateTimeShortForView(device.lastReading?.time || "") }}
                    </small>
                  </div>
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
          No devices.
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
