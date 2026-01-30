<script lang="ts" setup>
import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router';
import type {
  CategoryResponse,
  IItemSetOfIFailure,
  TemperatureLocationResponse,
} from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import type { ModalParameters } from '@/models/ModalParameters';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { onBeforeUnmount, onMounted, reactive } from 'vue';
import { ChromePicker, tinycolor } from 'vue-color';
import { onBeforeRouteLeave, onBeforeRouteUpdate } from 'vue-router';
import ApiHelpers from '@/models/ApiHelpers';
import { toNumberOrNull } from '@/models/FormatHelpers';
import { formatTempWithUnitOrEmpty } from '@/models/TempFormatHelpers';
import useAppStore from '@/stores/appStore';
import useMessageStore from '@/stores/messageStore';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const data = reactive({
  locations: [] as Array<TemperatureLocationResponse>,
  categories: [] as Array<CategoryResponse>,
  newLocation: {
    name: '',
    min: null as number | null,
    max: null as number | null,
  },
  errors: [] as Array<string>,
  originalLocations: new Map<number, string>(),
  hasDirtyLocations: false,
});

function trackOriginalState(location: TemperatureLocationResponse) {
  if (location.id !== undefined) {
    data.originalLocations.set(
      location.id,
      JSON.stringify({
        name: location.name,
        minTemperatureLimitCelsius: location.minTemperatureLimitCelsius,
        maxTemperatureLimitCelsius: location.maxTemperatureLimitCelsius,
        isHidden: location.isHidden,
        color: location.color,
        categoryId: location.categoryId,
      }),
    );
  }
}

function isLocationDirty(location: TemperatureLocationResponse): boolean {
  if (location.id === 0) {
    return true;
  } // New locations are always dirty
  if (location.id === undefined) {
    return false;
  }

  const original = data.originalLocations.get(location.id);
  if (!original) {
    return false;
  }

  const current = JSON.stringify({
    name: location.name,
    minTemperatureLimitCelsius: location.minTemperatureLimitCelsius,
    maxTemperatureLimitCelsius: location.maxTemperatureLimitCelsius,
    isHidden: location.isHidden,
    color: location.color,
    categoryId: location.categoryId,
  });

  return original !== current;
}

function updateDirtyState() {
  data.hasDirtyLocations = data.locations.some(location => isLocationDirty(location));
}

function handleBeforeUnload(event: BeforeUnloadEvent) {
  if (data.hasDirtyLocations) {
    event.preventDefault();

    event.returnValue = '';
    return '';
  }
  return null;
}

function onLocationInput() {
  updateDirtyState();
}

function onColorPickerChange(location: TemperatureLocationResponse, colorValue: unknown) {
  const color = tinycolor(colorValue as string);
  location.color = color.isValid() ? color.toHexString() : '';
  updateDirtyState();
}

async function getLocations() {
  try {
    const response = await api().temperatureLocationsGetAll();
    data.locations = response.data;
    // Track original state for dirty checking
    data.locations.forEach(location => trackOriginalState(location));
    updateDirtyState();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function getCategories() {
  try {
    const response = await api().categoriesGetAll();
    data.categories = response.data;
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function newLocation() {
  if (data.locations.findIndex(x => (x.id || 0) < 1) > -1) {
    return;
  }

  const newLoc = {
    id: 0,
    name: '',
    minTemperatureLimitCelsius: null,
    maxTemperatureLimitCelsius: null,
    isHidden: false,
    color: '',
    categoryId: null,
  };

  data.locations.unshift(newLoc);
  updateDirtyState();
}

async function reallyDeleteLocation(location: TemperatureLocationResponse) {
  if (location.id === null || typeof location.id === 'undefined') {
    return;
  }

  try {
    const response = await api().temperatureLocationsDelete({ id: location.id });
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    await getLocations();
    await getCategories();
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
    isHidden: location.isHidden,
    color: location.color || '',
    categoryId: toNumberOrNull(location.categoryId),
  };

  try {
    const response = await api().temperatureLocationsSave(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    // Update the location in place
    const isNewLocation = location.id === 0;
    if (isNewLocation) {
      // For new locations, we need to refetch to get the complete data with the new ID
      const tempIndex = data.locations.findIndex(l => l.id === 0);
      if (tempIndex >= 0) {
        // Remove the temporary entry
        data.locations.splice(tempIndex, 1);
        // Refresh the full list to get the new location with proper ID and relationships
        await getLocations();
      }
    } else {
      // Update existing location in place with the form data
      const existingIndex = data.locations.findIndex(l => l.id === location.id);
      if (existingIndex >= 0) {
        // Merge the updated data while preserving other properties
        data.locations[existingIndex] = {
          ...data.locations[existingIndex],
          ...request,
        };
        // Update original state tracking
        trackOriginalState(data.locations[existingIndex]);
      }
    }

    updateDirtyState();

    // Only refresh categories if needed
    await getCategories();
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IItemSetOfIFailure).items || [];
    failures.forEach(x => data.errors.push(`${x.uiHandle}-${location.id}`));
  }
}

function beforeRouteChange(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) {
  if (data.hasDirtyLocations) {
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
  await getCategories();
  window.addEventListener('beforeunload', handleBeforeUnload);
});

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
});
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-3">
      Locations
    </h1>
    <div class="mt-4">
      <button class="btn btn-primary" @click="newLocation()">
        New
      </button>
      <div id="locationsAccordion" class="accordion mt-4">
        <div v-for="location in data.locations" :key="location.id" class="accordion-item">
          <h2 :id="`heading-${location.id}`" class="accordion-header">
            <button
              class="accordion-button collapsed"
              type="button"
              data-bs-toggle="collapse"
              :data-bs-target="`#collapse-${location.id}`"
              :aria-expanded="false"
              :aria-controls="`collapse-${location.id}`"
            >
              <div class="d-flex align-items-center w-100">
                <span class="me-auto">
                  <span
                    class="color-dot me-2"
                    :class="{ 'color-dot-empty': !location.color }"
                    :style="location.color ? { backgroundColor: location.color } : {}"
                  />
                  {{ location.name || "New location" }}
                  <span v-if="isLocationDirty(location)" class="badge bg-warning text-dark ms-2">Unsaved</span>
                  <span v-if="location.isHidden" class="badge bg-secondary ms-2">Hidden</span>
                </span>
              </div>
            </button>
          </h2>
          <div
            :id="`collapse-${location.id}`"
            class="accordion-collapse collapse"
            :aria-labelledby="`heading-${location.id}`"
            data-bs-parent="#locationsAccordion"
          >
            <div class="accordion-body">
              <div class="grid gap-sm">
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`name-${location.id}`" class="form-label">Name</label>
                  <input
                    :id="`name-${location.id}`"
                    v-model="location.name"
                    class="form-control form-control-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`name-${location.id}`),
                    }"
                    type="text"
                    @input="onLocationInput"
                  >
                </div>
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`category-${location.id}`" class="form-label">Category</label>
                  <select
                    :id="`category-${location.id}`"
                    v-model="location.categoryId"
                    class="form-select form-select-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`category-${location.id}`),
                    }"
                    @change="onLocationInput"
                  >
                    <option :value="0" />
                    <option
                      v-for="category in data.categories"
                      :key="category.id"
                      :value="category.id"
                    >
                      {{ category.name }}
                    </option>
                  </select>
                </div>
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`min-${location.id}`" class="form-label">Min temp limit (°C)</label>
                  <input
                    :id="`min-${location.id}`"
                    v-model="location.minTemperatureLimitCelsius"
                    class="form-control form-control-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`min-${location.id}`),
                    }"
                    type="number"
                    @input="onLocationInput"
                  >
                  {{ formatTempWithUnitOrEmpty(location.minTemperatureLimitCelsius, true) }}
                </div>
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`max-${location.id}`" class="form-label">Max temp limit (°C)</label>
                  <input
                    :id="`max-${location.id}`"
                    v-model="location.maxTemperatureLimitCelsius"
                    class="form-control form-control-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`max-${location.id}`),
                    }"
                    type="number"
                    @input="onLocationInput"
                  >
                  {{ formatTempWithUnitOrEmpty(location.maxTemperatureLimitCelsius, true) }}
                </div>
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`color-${location.id}`" class="form-label">Color</label>
                  <div class="input-group input-group-sm">
                    <input
                      :id="`color-${location.id}`"
                      v-model="location.color"
                      type="text"
                      class="form-control"
                      :class="{
                        'is-invalid': data.errors.includes(`color-${location.id}`),
                      }"
                      title="Enter color hex code"
                      @input="onLocationInput"
                    >
                    <button
                      class="btn btn-outline-secondary dropdown-toggle"
                      type="button"
                      data-bs-toggle="dropdown"
                      data-bs-auto-close="outside"
                      aria-expanded="false"
                      aria-label="Open color picker"
                    >
                      <FontAwesomeIcon icon="fa-palette" :style="{ color: location.color || '#000' }" />
                    </button>
                    <div class="dropdown-menu dropdown-menu-end p-3">
                      <div class="mb-2">
                        <small class="text-muted">Use black for default color.</small>
                      </div>
                      <ChromePicker
                        :model-value="location.color"
                        :disable-alpha="true"
                        :disable-fields="true"
                        @update:model-value="(color: unknown) => onColorPickerChange(location, color)"
                      />
                    </div>
                  </div>
                </div>
                <div class="g-col-12">
                  <div class="form-check">
                    <input
                      :id="`hidden-${location.id}`"
                      v-model="location.isHidden"
                      class="form-check-input form-check-input"
                      :class="{
                        'is-invalid': data.errors.includes(`hidden-${location.id}`),
                      }"
                      type="checkbox"
                      @change="onLocationInput"
                    >
                    <label :for="`hidden-${location.id}`" class="form-check-label">Hidden</label>
                  </div>
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
        </div>
        <div v-if="data.locations.length < 1" class="text-center mt-4">
          No locations.
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.color-dot {
  display: inline-block;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  vertical-align: middle;
}

.color-dot-empty {
  border: 2px solid var(--bs-border-color);
}
</style>
