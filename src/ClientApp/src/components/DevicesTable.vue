<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import type { Device, IFailureIItemSet, Location } from '@/api/data-contracts';
import { storeToRefs } from 'pinia';
import { formatTempWithUnit } from '@/models/TempFormatHelpers';
import DateHelpers from '@/models/DateHelpers';
import { onMounted, reactive } from 'vue';
import ApiHelpers from '@/models/ApiHelpers';
import type { HttpResponse } from '@/api/http-client';
import useMessageStore from '@/stores/messageStore';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import type { ModalParameters } from '@/models/ModalParameters';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const { isFieldInError } = appStore;
const { useFahrenheit, staleLimitMinutes } = storeToRefs(appStore);

const data = reactive({
  devices: [] as Array<Device>,
  locations: [] as Array<Location>,
  errors: [] as Array<string>,
});

async function getDevices() {
  try {
    const response = await api().temperaturesDevicesAllCreate();
    data.devices = response.data;
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function getLocations() {
  try {
    const response = await api().temperaturesLocationsAllCreate();
    data.locations = response.data;
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function reallyDeleteDevice(device: Device) {
  if (device.id === null || typeof device.id === 'undefined') {
    return;
  }

  try {
    const response = await api().temperaturesDevicesDelete(device.id);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    await getLocations();
    await getDevices();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function newDevice() {
  if (data.devices.findIndex((x) => (x.id || 0) < 1) > -1) {
    return;
  }

  data.devices.unshift({
    id: 0,
    name: null,
    mqttTopic: null,
    locationId: null,
    isRetired: false,
  });
}

async function deleteDevice(device: Device) {
  const parameters: ModalParameters = {
    title: 'Delete device',
    description: 'Do you really want to delete this device?',
    okAction: () => reallyDeleteDevice(device),
  };

  appStore.showModal(parameters);
}

async function updateDevice(device: Device) {
  data.errors = [];

  const request = {
    id: device.id,
    name: device.name,
    mqttTopic: device.mqttTopic,
    locationId: device.locationId || null,
    isRetired: device.isRetired,
  };

  try {
    const response = await api().temperaturesDevicesUpdateCreate(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    await getLocations();
    await getDevices();
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IFailureIItemSet).items || [];
    failures.forEach((x) => data.errors.push(`${x.uiHandle}-${device.id}`));
  }

  // Update statuses
  try {
    const response = await api().temperaturesDevicesAllCreate();
    const newDevices = response.data;

    data.devices.forEach((x) => {
      const d = newDevices.filter((y) => y.id === x.id)[0];

      if (d) {
        // eslint-disable-next-line no-param-reassign
        x.isInactive = d.isInactive;
        // eslint-disable-next-line no-param-reassign
        x.isLost = d.isLost;
      }
    });
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

onMounted(async () => {
  await getLocations();
  await getDevices();
});
</script>

<template>
  <button class="btn btn-primary" @click="newDevice()">New</button>
  <div class="grid mt-4">
    <div v-for="device in data.devices" :key="device.id" class="card g-col-12">
      <div class="card-body">
        <div class="grid">
          <div v-if="!device.id" class="g-col-12">New device</div>
          <div class="g-col-12 g-col-md-6">
            <label :for="`name-${device.id}`" class="form-label">Name</label>
            <input
              :id="`name-${device.id}`"
              v-model="device.name"
              required
              type="text"
              :class="{ 'form-control': true, 'is-invalid': isFieldInError('name') }"
            />
          </div>
          <div class="g-col-12 g-col-md-6">
            <label :for="`mqttTopic-${device.id}`" class="form-label">Topic</label>
            <input
              :id="`mqttTopic-${device.id}`"
              v-model="device.mqttTopic"
              required
              type="text"
              :class="{ 'form-control': true, 'is-invalid': isFieldInError('mqttTopic') }"
            />
          </div>
          <div class="g-col-12 g-col-md-6">
            <label :for="`location-${device.id}`" class="form-label">Location</label>
            <select
              :id="`location-${device.id}`"
              v-model="device.locationId"
              :class="{
                'form-select': true,
                'is-invalid': data.errors.includes(`location-${device.id}`),
              }"
            >
              <option value=""></option>
              <option v-for="location in data.locations" :key="location.id" :value="location.id">
                {{ location.name }}
              </option>
            </select>
          </div>
          <div class="g-col-12">
            <div class="form-check">
              <input
                :id="`retired-${device.id}`"
                v-model="device.isRetired"
                class="form-check-input"
                :class="{
                  'form-check-input': true,
                  'is-invalid': data.errors.includes(`retired-${device.id}`),
                }"
                type="checkbox"
              />
              <label :for="`retired-${device.id}`" class="form-check-label">Retired</label>
            </div>
          </div>
          <div class="g-col-12">
            <div class="btn-toolbar">
              <button class="btn btn-primary me-2" @click="updateDevice(device)">Save</button>
              <button v-if="device.id" class="btn btn-danger ms-auto" @click="deleteDevice(device)">
                Delete
              </button>
            </div>
          </div>
        </div>
      </div>
      <div v-if="device.id" class="ms-3 mb-1">
        <div>
          <small class="text-body-secondary me-2">ID: {{ device.id }}</small>
          <font-awesome-icon
            v-if="device.isInactive"
            icon="fa-clock"
            class="text-danger me-2"
            :title="`Inactive. Hasn't been seen in ${staleLimitMinutes} minutes.`"
          />
          <font-awesome-icon
            v-if="device.isLost"
            icon="fa-battery-quarter"
            class="text-danger me-2"
            title="Lost. Doesn't have location."
          />
          <font-awesome-icon
            v-if="device.isBatteryLevelLow"
            icon="fa-battery-quarter"
            class="text-danger me-2"
            title="Battery low."
          />
        </div>
        <div>
          <small v-if="device.lastReading"
            >Last reading:
            {{ formatTempWithUnit(device.lastReading?.temperatureCelsius, useFahrenheit) }} on
            {{ DateHelpers.dateTimeShortForView(device.lastReading?.time || '') }}
          </small>
        </div>
      </div>
    </div>
    <div v-if="data.devices.length < 1" class="text-center">No devices.</div>
  </div>
</template>

<style lang="scss" scoped>
table .btn.btn-sm {
  min-width: 0;
}

.badge.d-block:not(:last-child) {
  margin-bottom: 0.25rem;
}
</style>
