<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import type { Device, IFailureIItemSet, Location } from '@/api/data-contracts';
import { storeToRefs } from 'pinia';
import { formatTempWithUnit } from '@/models/TempFormatHelpers';
import DateHelpers from '@/models/DateHelpers';
import { onMounted, reactive } from 'vue';
import ApiHelpers from '@/models/ApiHelpers';
import type { HttpResponse } from '@/api/http-client';

const appStore = useAppStore();
const api = ApiHelpers.client;

const { useDarkMode, useFahrenheit } = storeToRefs(appStore);

const data = reactive({
  devices: [] as Array<Device>,
  locations: [] as Array<Location>,
  errors: [] as Array<string>,
});

function getStatus(device: Device) {
  const errors: Array<string> = [];

  if (device.isLost) {
    errors.push('Lost');
  }

  if (device.isInactive) {
    errors.push('Inactive');
  }

  return errors;
}

async function getDevices() {
  try {
    const response = await api().temperaturesDevicesAllCreate();
    data.devices = response.data;
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function getLocations() {
  try {
    const response = await api().temperaturesLocationsAllCreate();
    data.locations = response.data;
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function updateDevice(device: Device) {
  data.errors = [];

  const request = {
    id: device.id,
    currentLocationId: device.currentLocationId || null,
    isRetired: device.isRetired,
  };

  try {
    const response = await api().temperaturesDevicesUpdateCreate(request);
    if (response.data.message) {
      appStore.setSuccessMessage(response.data.message);
    }
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    appStore.setApiFailureMessages(response);

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
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

onMounted(async () => {
  await getLocations();
  await getDevices();
});
</script>

<template>
  <div class="mt-4">
    <p>
      Inactive - device that hasn't saved a reading in the last 2 hours.
      <br />
      Lost - device that doesn't have a location but will acquire new readings.
      <br />
      Retired - device will not acquire new readings and statuses are suppressed.
    </p>
    <table :class="{ table: true, 'table-dark': useDarkMode }">
      <thead>
        <tr>
          <th>ID</th>
          <th>Radio Model/ID/Channel</th>
          <th>Last reading</th>
          <th>Status</th>
          <th>Retired</th>
          <th>Location</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="device in data.devices" :key="device.id">
          <td>{{ device.id }}</td>
          <td>{{ device.deviceModel }}/{{ device.deviceId }}/{{ device.deviceChannel }}</td>
          <td>
            <span v-if="device.lastReading">
              {{ formatTempWithUnit(device.lastReading?.temperatureCelsius, useFahrenheit) }} on
              {{ DateHelpers.dateTimeShortForView(device.lastReading?.time || '') }}
            </span>
          </td>
          <td>
            <div v-for="status in getStatus(device)" :key="status" class="badge bg-danger">
              {{ status }}
            </div>
          </td>
          <td>
            <label class="visually-hidden" :for="`retired-${device.id}`">Retired</label>
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
          </td>
          <td>
            <label class="visually-hidden" :for="`location-${device.id}`">Location</label>
            <select
              :id="`location-${device.id}`"
              v-model="device.currentLocationId"
              :class="{
                'form-select': true,
                'form-select-sm': true,
                'is-invalid': data.errors.includes(`location-${device.id}`),
              }"
            >
              <option value=""></option>
              <option v-for="location in data.locations" :key="location.id" :value="location.id">
                {{ location.name }}
              </option>
            </select>
          </td>
          <td>
            <button class="btn btn-sm btn-primary" @click="updateDevice(device)">Save</button>
          </td>
        </tr>
      </tbody>
    </table>
    <div v-if="data.devices.length < 1" class="text-center">No devices.</div>
  </div>
</template>

<style lang="scss" scoped>
table .btn.btn-sm {
  min-width: 0;
}
</style>
