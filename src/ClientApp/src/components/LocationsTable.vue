<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import type { IItemSetOfIFailure, TemperatureLocationResponse } from '@/api/data-contracts';
import { storeToRefs } from 'pinia';
import { toNumberOrNull } from '@/models/FormatHelpers';
import { formatTempWithUnitOrEmpty } from '@/models/TempFormatHelpers';
import { onMounted, reactive } from 'vue';
import ApiHelpers from '@/models/ApiHelpers';
import type { HttpResponse } from '@/api/http-client';
import useMessageStore from '@/stores/messageStore';
import type { ModalParameters } from '@/models/ModalParameters';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const { useFahrenheit } = storeToRefs(appStore);

const data = reactive({
  locations: [] as Array<TemperatureLocationResponse>,
  newLocation: {
    name: '',
    min: null as number | null,
    max: null as number | null,
  },
  errors: [] as Array<string>,
});

async function getLocations() {
  try {
    const response = await api().temperatureLocationsGetAll();
    data.locations = response.data;
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function newLocation() {
  if (data.locations.findIndex((x) => (x.id || 0) < 1) > -1) {
    return;
  }

  data.locations.unshift({
    id: 0,
    name: '',
    minTemperatureLimitCelsius: null,
    maxTemperatureLimitCelsius: null,
  });
}

async function reallyDeleteLocation(location: TemperatureLocationResponse) {
  if (location.id === null || typeof location.id === 'undefined') {
    return;
  }

  try {
    const response = await api().temperatureLocationsDelete(location.id);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    await getLocations();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function deleteLocation(location: TemperatureLocationResponse) {
  const parameters: ModalParameters = {
    title: 'Delete location',
    description:
      'Do you really want to delete this location? Only locations with no related readings or devices can be deleted.',
    okAction: () => reallyDeleteLocation(location),
  };

  appStore.showModal(parameters);
}

async function saveLocation(location: TemperatureLocationResponse) {
  data.errors = [];

  const request = {
    id: location.id,
    name: location.name,
    minTemperatureLimitCelsius: toNumberOrNull(location.minTemperatureLimitCelsius),
    maxTemperatureLimitCelsius: toNumberOrNull(location.maxTemperatureLimitCelsius),
  };

  try {
    const response = await api().temperatureLocationsSave(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    await getLocations();
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IItemSetOfIFailure).items || [];
    failures.forEach((x) => data.errors.push(`${x.uiHandle}-${location.id}`));
  }
}

onMounted(async () => {
  await getLocations();
});
</script>

<template>
  <button class="btn btn-primary" @click="newLocation()">New</button>
  <div class="grid mt-4">
    <div v-for="location in data.locations" :key="location.id" class="card g-col-12">
      <div class="card-body">
        <div class="grid gap-sm">
          <div v-if="!location.id" class="g-col-12">New location</div>
          <div class="g-col-12 g-col-md-6 g-col-lg-4">
            <label :for="`name-${location.id}`">Name</label>
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
          </div>
          <div class="g-col-12 g-col-md-6 g-col-lg-4">
            <label :for="`min-${location.id}`">Min temp limit (°C)</label>
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
            {{ formatTempWithUnitOrEmpty(location.minTemperatureLimitCelsius, useFahrenheit) }}
          </div>
          <div class="g-col-12 g-col-md-6 g-col-lg-4">
            <label :for="`max-${location.id}`">Max temp limit (°C)</label>
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
            {{ formatTempWithUnitOrEmpty(location.maxTemperatureLimitCelsius, useFahrenheit) }}
          </div>
          <div v-if="location.id" class="g-col-12">
            <div>
              <small class="text-body-secondary me-2">ID: {{ location.id }}</small>
            </div>
          </div>
          <div class="g-col-12">
            <div class="btn-toolbar">
              <button class="btn btn-sm btn-primary me-2" @click="saveLocation(location)">
                Save
              </button>
              <button class="btn btn-sm btn-danger ms-auto" @click="deleteLocation(location)">
                Delete
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div v-if="data.locations.length < 1" class="g-col-12 text-center">No locations.</div>
  </div>
</template>

<style lang="scss" scoped></style>
