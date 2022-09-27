<script lang="ts" setup>
import { Api } from '@/api/Api';
import useAppStore from '@/stores/appStore';
import type { LostDevice } from '@/api/data-contracts';
import { onMounted, reactive } from 'vue';
import type { HttpResponse } from '@/api/http-client';
import { storeToRefs } from 'pinia';
import { formatTemp } from '@/models/FormatHelpers';
import DateHelpers from '@/models/DateHelpers';

const appStore = useAppStore();

const { useDarkMode, useFahrenheit } = storeToRefs(appStore);

const { tempUnit } = appStore;

const data = reactive({
  lostDevices: [] as Array<LostDevice>,
});

async function getLostDevices() {
  try {
    const response = await new Api().temperaturesLostDevicesCreate();
    data.lostDevices = response.data;
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

onMounted(async () => {
  await getLostDevices();
});
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-4 mb-0">Lost devices</h1>
    <p>Devices that don't have a location.</p>
    <div class="mt-4">
      <table :class="{ table: true, 'table-dark': useDarkMode }">
        <thead>
          <tr>
            <th>Id</th>
            <th>Model</th>
            <th>Device ID</th>
            <th>Channel</th>
            <th>Last reading</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="device in data.lostDevices" :key="device.id">
            <td>{{ device.id }}</td>
            <td>{{ device.deviceModel }}</td>
            <td>{{ device.deviceId }}</td>
            <td>{{ device.deviceChannel }}</td>
            <td>
              {{ formatTemp(device.lastReadingTemperatureCelsius, useFahrenheit) }}{{ tempUnit }} on
              {{ DateHelpers.dateTimeShortForView(device.lastReadingTime) }}
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="data.lostDevices.length < 1" class="text-center">No lost devices.</div>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
