<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import type { GraphCurrentReading } from '@/api/data-contracts';
import { onMounted, reactive } from 'vue';
import { format } from 'date-fns';
import * as signalR from '@microsoft/signalr';
import { storeToRefs } from 'pinia';
import { formatTemp } from '@/models/FormatHelpers';

const appStore = useAppStore();

const { useFahrenheit, tempUnit, showHumidity } = storeToRefs(appStore);

const data = reactive({
  currentReadings: [] as Array<GraphCurrentReading>,
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

onMounted(async () => {
  await connectToHub();
});
</script>

<template>
  <div class="row mt-4 px-2">
    <div
      v-for="(currentTemp, i) in data.currentReadings"
      :key="i"
      class="col-sm-6 col-md-4 mb-3 px-2"
    >
      <div class="card text-center">
        <div class="card-body">
          <div class="h4 mb-2">
            {{ currentTemp.location }}
          </div>
          <div class="h3">
            <span class="fw-bold"
              >{{ formatTemp(currentTemp.temperatureCelsius, useFahrenheit) }}{{ tempUnit }}</span
            >
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

<style lang="scss" scoped></style>
