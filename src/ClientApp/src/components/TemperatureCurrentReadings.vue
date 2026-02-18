<script lang="ts" setup>
import type { CategoryResponse, TemperatureReadingResponse } from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import * as signalR from '@microsoft/signalr';
import { addMinutes, format, isPast } from 'date-fns';
import { storeToRefs } from 'pinia';
import { computed, onMounted, reactive } from 'vue';
import ApiHelpers from '@/models/ApiHelpers';
import { formatHumidityWithUnit, formatTempWithUnit } from '@/models/TempFormatHelpers';
import useAppStore from '@/stores/appStore';
import useMessageStore from '@/stores/messageStore';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const { useFahrenheit, showHumidity, staleLimitMinutes } = storeToRefs(appStore);

const data = reactive({
  currentReadings: [] as Array<TemperatureReadingResponse>,
  categories: [] as Array<CategoryResponse>,
});

async function getCategories() {
  try {
    const response = await api().categoriesGetAll();
    data.categories = response.data;
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

let connection: signalR.HubConnection | null = null;

async function connectToHub() {
  const startTimeMilliseconds = Date.now();

  async function onConnectedToHub() {
    if (connection === null || connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }
    const response = await connection.invoke('getCurrentReadings');
    data.currentReadings = response;
  }

  async function startConnection() {
    if (connection !== null) {
      try {
        await connection.start();
        await onConnectedToHub();
      } catch {
        // If we fail to start the connection, retry after delay.
        const elapsedMilliseconds = Date.now() - startTimeMilliseconds;
        const delay = ApiHelpers.getRetryMilliseconds(elapsedMilliseconds);
        if (delay !== null) {
          setTimeout(startConnection, delay);
        }
      }
    }
  }

  if (connection === null) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hub/temperatures')
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext =>
          ApiHelpers.getRetryMilliseconds(retryContext.elapsedMilliseconds),
      })
      .build();

    connection.on('updateCurrentReadings', (currentReadings) => {
      data.currentReadings = currentReadings;
    });

    connection.on('updateCategories', (categories) => {
      data.categories = categories;
    });

    connection.onreconnected(onConnectedToHub);
  }

  startConnection();
}

const categorizedReadings = computed(() => {
  const visibleReadings = data.currentReadings.filter(x => !x.location?.isHidden);

  const sortedCategories = data.categories.slice().sort((a, b) => (a.order ?? 0) - (b.order ?? 0));

  const groupedReadings = sortedCategories.reduce(
    (acc, category) => {
      if (!category.name) {
        return acc;
      }

      const readings = visibleReadings.filter(
        reading => reading.location?.categoryId === category.id,
      );

      if (!readings.length) {
        return acc;
      }

      acc[category.name] = readings;

      return acc;
    },
    {} as Record<string, TemperatureReadingResponse[]>,
  );

  const uncategorized = visibleReadings.filter(reading => !reading.location?.categoryId);

  if (uncategorized.length) {
    groupedReadings.Uncategorized = uncategorized;
  }

  return groupedReadings;
});

function isStale(reading: TemperatureReadingResponse) {
  const readingDate = new Date(reading.time as string);

  const result = isPast(addMinutes(readingDate, staleLimitMinutes.value));

  return result;
}

function formatReadingTime(reading: TemperatureReadingResponse) {
  const readingDate = new Date(reading.time as string);
  const hoursDiff = (Date.now() - readingDate.getTime()) / (1000 * 60 * 60);

  if (hoursDiff > 24) {
    return format(readingDate, 'MMM d, HH:mm');
  }
  return format(readingDate, 'HH:mm');
}

onMounted(async () => {
  await connectToHub();
  await getCategories();
});
</script>

<template>
  <div class="grid">
    <div v-for="(values, categoryName) in categorizedReadings" :key="categoryName" class="g-col-12">
      <h4>{{ categoryName }}</h4>
      <div class="grid">
        <div v-for="(currentTemp, i) in values" :key="i" class="g-col-6 g-col-md-4 g-col-lg-3">
          <div class="card text-center">
            <div class="card-body">
              <div class="h5 mb-2">
                {{ currentTemp.location?.name }}
              </div>
              <div class="h3">
                <span v-if="currentTemp.isHot" title="Hotter than limit.">
                  <FontAwesomeIcon
                    icon="fa-temperature-full"
                    class="hot blink me-2"
                    aria-label="Hotter than limit."
                  />
                </span>
                <span v-if="currentTemp.isCold" title="Colder than limit.">
                  <FontAwesomeIcon
                    icon="fa-snowflake"
                    class="cold blink me-2"
                    aria-label="Colder than limit."
                  />
                </span>
                <span class="fw-bold">{{
                  formatTempWithUnit(currentTemp.temperatureCelsius, useFahrenheit, 0)
                }}</span>
                <span v-if="currentTemp.humidity !== null && showHumidity" class="ps-3">{{
                  formatHumidityWithUnit(currentTemp.humidity)
                }}</span>
              </div>
              <div>
                <span
                  v-if="isStale(currentTemp)"
                  :title="`Reading is more than ${staleLimitMinutes} minutes old.`"
                >
                  <FontAwesomeIcon
                    icon="fa-clock"
                    class="stale blink me-2"
                    :aria-label="`Reading is more than ${staleLimitMinutes} minutes old.`"
                  />
                </span>
                <router-link
                  :to="{ name: 'timeSeries', query: { locationIds: currentTemp.location?.id! } }"
                  :title="`Last reading at ${formatReadingTime(currentTemp)}. Click to view time series for this location.`"
                  :aria-label="`Last reading at ${formatReadingTime(currentTemp)}. Click to view time series for this location.`"
                >
                  <small>{{
                    formatReadingTime(currentTemp)
                  }}</small>
                </router-link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.hot {
  color: #d74040;
}

.cold {
  color: #5e83f3;
}

.stale {
  color: inherit;
}
</style>
