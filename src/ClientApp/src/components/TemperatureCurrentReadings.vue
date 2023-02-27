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

let connection: signalR.HubConnection | null = null;

async function connectToHub() {
  async function connectInternal() {
    if (connection !== null) {
      try {
        await connection.start();
        const response = await connection?.invoke('getCurrentReadings');
        data.currentReadings = response;
      } catch {
        setTimeout(connectInternal, 2000);
      }
    }
  }

  if (connection === null) {
    connection = new signalR.HubConnectionBuilder().withUrl('/hub/temperatures').build();

    connection.on('updateCurrentReadings', (currentReadings) => {
      data.currentReadings = currentReadings;
    });

    connection.onclose(connectInternal);
  }

  connectInternal();
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
