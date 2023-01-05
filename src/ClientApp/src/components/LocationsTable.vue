<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import type { Device, Location } from '@/api/data-contracts';
import { storeToRefs } from 'pinia';
import { formatTempWithUnit } from '@/models/FormatHelpers';
import { onMounted, reactive } from 'vue';
import { Api } from '@/api/Api';
import type { HttpResponse } from '@/api/http-client';

const appStore = useAppStore();

const { useDarkMode, useFahrenheit } = storeToRefs(appStore);

const data = reactive({
  locations: [] as Array<Location>,
  newLocation: {
    name: '',
    min: null as number | null,
    max: null as number | null,
  },
});

async function getLocations() {
  try {
    const response = await new Api().temperaturesLocationsAllCreate();
    data.locations = response.data;
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

// TODO: finish these API calls
async function updateLocation(location: Location) {
  // const request = {
  //   name: location.name,
  //   minLimitTemperatureCelsius: location.minLimitTemperatureCelsius,
  //   maxLimitTemperatureCelsius: location.maxLimitTemperatureCelsius,
  // };

  try {
    // const response = await new Api().temperaturesLocationsUpdateCreate(request);
    // if (response.data.message) {
    //   appStore.setSuccessMessage(response.data.message);
    // }
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function createLocation() {
  // const request = data.newLocation;

  try {
    // const response = await new Api().temperaturesLocationsCreateCreate(request);
    // if (response.data.message) {
    //   appStore.setSuccessMessage(response.data.message);
    // }
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

onMounted(async () => {
  await getLocations();
});
</script>

<template>
  <div class="mt-4">
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
              class="form-control form-control-sm"
              type="text"
            />
          </td>
          <td>
            <label class="visually-hidden" :for="`min-${location.id}`">Min temp limit (°C)</label>
            <input
              :id="`min-${location.id}`"
              v-model="location.minLimitTemperatureCelsius"
              class="form-control form-control-sm"
              type="number"
            />
            {{ formatTempWithUnit(location.minLimitTemperatureCelsius, useFahrenheit) }}
          </td>
          <td>
            <label class="visually-hidden" :for="`max-${location.id}`">Max temp limit (°C)</label>
            <input
              :id="`max-${location.id}`"
              v-model="location.maxLimitTemperatureCelsius"
              class="form-control form-control-sm"
              type="number"
            />
            {{ formatTempWithUnit(location.maxLimitTemperatureCelsius, useFahrenheit) }}
          </td>
          <td>
            <button class="btn btn-sm btn-primary" disabled @click="updateLocation(location)">
              Save
            </button>
          </td>
        </tr>
      </tbody>
    </table>
    <div v-if="data.locations.length < 1" class="text-center">No locations.</div>
  </div>
  <div class="mt-4 row g-3">
    <div class="col-md-6">
      <label for="newLocationName">Name</label>
      <input
        id="newLocationName"
        v-model="data.newLocation.name"
        type="text"
        class="form-control"
      />
    </div>
    <div class="col-md-3">
      <label for="newLocationMin">Min temp (°C)</label>
      <input
        id="newLocationMin"
        v-model="data.newLocation.min"
        type="number"
        class="form-control"
      />
      {{ formatTempWithUnit(data.newLocation.min, useFahrenheit) }}
    </div>
    <div class="col-md-3">
      <label for="newLocationMax">Max temp (°C)</label>
      <input
        id="newLocationMax"
        v-model="data.newLocation.max"
        type="number"
        class="form-control"
      />
      {{ formatTempWithUnit(data.newLocation.max, useFahrenheit) }}
    </div>
    <div class="col-md-12">
      <button class="btn btn-primary" disabled @click="createLocation()">Add</button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
table .btn.btn-sm {
  min-width: 0;
}
</style>
