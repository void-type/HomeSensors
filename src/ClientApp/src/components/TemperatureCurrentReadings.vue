<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import type { Reading } from '@/api/data-contracts';
import { onMounted, reactive } from 'vue';
import { format } from 'date-fns';
import * as signalR from '@microsoft/signalr';
import { storeToRefs } from 'pinia';
import { formatTempWithUnit } from '@/models/TempFormatHelpers';
import { toNumberOrNull } from '@/models/FormatHelpers';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';

const appStore = useAppStore();

const { useFahrenheit, showHumidity } = storeToRefs(appStore);

const data = reactive({
  currentReadings: [] as Array<Reading>,
});

function getRetryMilliseconds(elapsedMilliseconds: number) {
  // Within the first minute, wait between 0 and 10 seconds.
  if (elapsedMilliseconds < 60000) {
    return Math.random() * 10000;
  }

  // Within the first hour, try between 0 and 5 minutes.
  if (elapsedMilliseconds < 3600000) {
    return Math.random() * 5 * 60000;
  }

  // Within the first day, try every 30 minutes with an offset of 0 to 3 minutes.
  if (elapsedMilliseconds < 86400000) {
    return (Math.random() * 3 + 30) * 60000;
  }

  // After a day, stop trying to reconnect.
  return null;
}

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
        const delay = getRetryMilliseconds(elapsedMilliseconds);
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
          getRetryMilliseconds(retryContext.elapsedMilliseconds),
      })
      .build();

    connection.on('updateCurrentReadings', (currentReadings) => {
      data.currentReadings = currentReadings;
    });

    connection.onreconnected(onConnectedToHub);
  }

  startConnection();
}

function isOutOfLimit(reading: Reading) {
  const current = toNumberOrNull(reading.temperatureCelsius);

  if (current === null) {
    return false;
  }

  const max = toNumberOrNull(reading.location?.maxTemperatureLimitCelsius);

  if (max !== null && current > max) {
    return true;
  }

  const min = toNumberOrNull(reading.location?.minTemperatureLimitCelsius);

  if (min !== null && current < min) {
    return true;
  }

  return false;
}

onMounted(async () => {
  await connectToHub();
});
</script>

<template>
  <div class="grid mt-4">
    <div
      v-for="(currentTemp, i) in data.currentReadings"
      :key="i"
      class="g-col-12 g-col-sm-6 g-col-md-4"
    >
      <div class="card text-center">
        <div class="card-body">
          <div class="h4 mb-2">
            <font-awesome-icon
              v-if="isOutOfLimit(currentTemp)"
              icon="fa-triangle-exclamation"
              class="text-danger blink me-2"
            />
            {{ currentTemp.location?.name }}
          </div>
          <div class="h3">
            <span class="fw-bold">{{
              formatTempWithUnit(currentTemp.temperatureCelsius, useFahrenheit)
            }}</span>
            <span v-if="currentTemp.humidity !== null && showHumidity" class="ps-3"
              >{{ currentTemp.humidity }}%</span
            >
          </div>
          <div>
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
.blink {
  animation: blink-animation 2s steps(5, start) infinite;
}

@keyframes blink-animation {
  to {
    visibility: hidden;
  }
}
</style>
