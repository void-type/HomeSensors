<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import type { TemperatureReadingResponse } from '@/api/data-contracts';
import { onMounted, reactive } from 'vue';
import { addMinutes, format, isPast } from 'date-fns';
import * as signalR from '@microsoft/signalr';
import { storeToRefs } from 'pinia';
import { formatTempWithUnit, formatHumidityWithUnit } from '@/models/TempFormatHelpers';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import ApiHelpers from '@/models/ApiHelpers';

const appStore = useAppStore();

const { useFahrenheit, showHumidity, staleLimitMinutes } = storeToRefs(appStore);

const data = reactive({
  currentReadings: [] as Array<TemperatureReadingResponse>,
});

let connection: signalR.HubConnection | null = null;

async function connectToHub() {
  const startTimeMilliseconds = Date.now();

  async function onConnectedToHub() {
    const response = await connection?.invoke('getCurrentReadings');
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
        nextRetryDelayInMilliseconds: (retryContext) =>
          ApiHelpers.getRetryMilliseconds(retryContext.elapsedMilliseconds),
      })
      .build();

    connection.on('updateCurrentReadings', (currentReadings) => {
      data.currentReadings = currentReadings;
    });

    connection.onreconnected(onConnectedToHub);
  }

  startConnection();
}

function isStale(reading: TemperatureReadingResponse) {
  const readingDate = new Date(reading.time as string);

  const result = isPast(addMinutes(readingDate, staleLimitMinutes.value));

  return result;
}

onMounted(async () => {
  await connectToHub();
});
</script>

<template>
  <div class="grid">
    <div
      v-for="(currentTemp, i) in data.currentReadings"
      :key="i"
      class="g-col-6 g-col-md-4 g-col-lg-3"
    >
      <div class="card text-center">
        <div class="card-body">
          <div class="h4 mb-2">
            {{ currentTemp.location?.name }}
          </div>
          <div class="h3">
            <font-awesome-icon
              v-if="currentTemp.isHot"
              icon="fa-temperature-full"
              class="hot blink me-2"
              title="Hotter than limit."
            />
            <font-awesome-icon
              v-if="currentTemp.isCold"
              icon="fa-snowflake"
              class="cold blink me-2"
              title="Colder than limit."
            />
            <span class="fw-bold">{{
              formatTempWithUnit(currentTemp.temperatureCelsius, useFahrenheit, 0)
            }}</span>
            <span v-if="currentTemp.humidity !== null && showHumidity" class="ps-3">{{
              formatHumidityWithUnit(currentTemp.humidity)
            }}</span>
          </div>
          <div>
            <font-awesome-icon
              v-if="isStale(currentTemp)"
              icon="fa-clock"
              class="stale blink me-2"
              :title="`Reading is more than ${staleLimitMinutes} minutes old.`"
            />
            <small class="fw-light">{{
              format(new Date(currentTemp.time as string), 'HH:mm')
            }}</small>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.hot {
  color: #df2020;
}

.cold {
  color: #abc0ff;
}

.stale {
  color: inherit;
}
</style>
