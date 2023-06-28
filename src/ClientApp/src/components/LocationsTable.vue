<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import type { IFailureIItemSet, Location } from '@/api/data-contracts';
import { storeToRefs } from 'pinia';
import { toNumberOrNull } from '@/models/FormatHelpers';
import { formatTempWithUnit } from '@/models/TempFormatHelpers';
import { onMounted, reactive } from 'vue';
import ApiHelpers from '@/models/ApiHelpers';
import type { HttpResponse } from '@/api/http-client';
import useMessageStore from '@/stores/messageStore';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const { useDarkMode, useFahrenheit } = storeToRefs(appStore);

const data = reactive({
  locations: [] as Array<Location>,
  newLocation: {
    name: '',
    min: null as number | null,
    max: null as number | null,
  },
  errors: [] as Array<string>,
});

async function getLocations() {
  try {
    const response = await api().temperaturesLocationsAllCreate();
    data.locations = response.data;
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function updateLocation(location: Location) {
  data.errors = [];

  const request = {
    id: location.id,
    name: location.name,
    minTemperatureLimitCelsius: toNumberOrNull(location.minTemperatureLimitCelsius),
    maxTemperatureLimitCelsius: toNumberOrNull(location.maxTemperatureLimitCelsius),
  };

  try {
    const response = await api().temperaturesLocationsUpdateCreate(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IFailureIItemSet).items || [];
    failures.forEach((x) => data.errors.push(`${x.uiHandle}-${location.id}`));
  }
}

async function createLocation() {
  data.errors = [];

  const location = data.newLocation;

  const request = {
    name: location.name,
    minTemperatureLimitCelsius: toNumberOrNull(location.min),
    maxTemperatureLimitCelsius: toNumberOrNull(location.max),
  };

  try {
    const response = await api().temperaturesLocationsCreateCreate(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
      await getLocations();
    }
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IFailureIItemSet).items || [];
    failures.forEach((x) => data.errors.push(`${x.uiHandle}-new`));
  }
}

onMounted(async () => {
  await getLocations();
});
</script>

<template>
  <div>
    <table :class="{ table: true, 'table-dark': useDarkMode }">
      <thead>
        <tr>
          <th>Name</th>
          <th>Min temp limit (°C)</th>
          <th>Max temp limit (°C)</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="location in data.locations" :key="location.id">
          <td>
            <label class="visually-hidden" :for="`name-${location.id}`">Name</label>
            <input
              :id="`name-${location.id}`"
              v-model="location.name"
              :class="{
                'form-control': true,
                'form-control-sm': true,
                'is-invalid': data.errors.includes(`name-${location.id}`),
              }"
              type="text"
            />
          </td>
          <td>
            <label class="visually-hidden" :for="`min-${location.id}`">Min temp limit (°C)</label>
            <input
              :id="`min-${location.id}`"
              v-model="location.minTemperatureLimitCelsius"
              :class="{
                'form-control': true,
                'form-control-sm': true,
                'is-invalid': data.errors.includes(`min-${location.id}`),
              }"
              type="number"
            />
            {{ formatTempWithUnit(location.minTemperatureLimitCelsius, useFahrenheit) }}
          </td>
          <td>
            <label class="visually-hidden" :for="`max-${location.id}`">Max temp limit (°C)</label>
            <input
              :id="`max-${location.id}`"
              v-model="location.maxTemperatureLimitCelsius"
              :class="{
                'form-control': true,
                'form-control-sm': true,
                'is-invalid': data.errors.includes(`max-${location.id}`),
              }"
              type="number"
            />
            {{ formatTempWithUnit(location.maxTemperatureLimitCelsius, useFahrenheit) }}
          </td>
          <td>
            <button class="btn btn-sm btn-primary" @click="updateLocation(location)">Save</button>
          </td>
        </tr>
      </tbody>
    </table>
    <div v-if="data.locations.length < 1" class="text-center">No locations.</div>
  </div>
  <h2 class="mt-5 mb-4">New location</h2>
  <div class="grid">
    <div class="g-col-12 g-col-md-6">
      <label for="name-new">Name</label>
      <input
        id="name-new"
        v-model="data.newLocation.name"
        type="text"
        :class="{
          'form-control': true,
          'form-control-sm': true,
          'is-invalid': data.errors.includes('name-new'),
        }"
      />
    </div>
    <div class="g-col-12 g-col-md-3">
      <label for="min-new">Min temp (°C)</label>
      <input
        id="min-new"
        v-model="data.newLocation.min"
        type="number"
        :class="{
          'form-control': true,
          'form-control-sm': true,
          'is-invalid': data.errors.includes('min-new'),
        }"
      />
      {{ formatTempWithUnit(data.newLocation.min, useFahrenheit) }}
    </div>
    <div class="g-col-12 g-col-md-3">
      <label for="max-new">Max temp (°C)</label>
      <input
        id="max-new"
        v-model="data.newLocation.max"
        type="number"
        :class="{
          'form-control': true,
          'form-control-sm': true,
          'is-invalid': data.errors.includes('max-new'),
        }"
      />
      {{ formatTempWithUnit(data.newLocation.max, useFahrenheit) }}
    </div>
    <div class="g-col-12 g-col-md-12">
      <button class="btn btn-sm btn-primary" @click="createLocation()">Add</button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
table .btn.btn-sm {
  min-width: 0;
}
</style>
