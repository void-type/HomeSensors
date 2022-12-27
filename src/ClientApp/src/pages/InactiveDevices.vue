<script lang="ts" setup>
import { Api } from '@/api/Api';
import useAppStore from '@/stores/appStore';
import type { InactiveDevice } from '@/api/data-contracts';
import { onMounted, reactive } from 'vue';
import type { HttpResponse } from '@/api/http-client';
import { storeToRefs } from 'pinia';
import { formatTemp } from '@/models/FormatHelpers';
import DateHelpers from '@/models/DateHelpers';

const appStore = useAppStore();

const { useDarkMode, useFahrenheit } = storeToRefs(appStore);
const { tempUnit } = appStore;

const data = reactive({
  inactiveDevices: [] as Array<InactiveDevice>,
});

async function getInactiveDevices() {
  try {
    const response = await new Api().temperaturesInactiveDevicesCreate();
    data.inactiveDevices = response.data;
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

onMounted(async () => {
  await getInactiveDevices();
});
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-4 mb-0">Inactive devices</h1>
    <p>Devices that haven't saved data within the last 2 hours.</p>
    <div class="mt-4">
      <table :class="{ table: true, 'table-dark': useDarkMode }">
        <thead>
          <tr>
            <th>Id</th>
            <th>Model</th>
            <th>Device ID</th>
            <th>Channel</th>
            <th>Location</th>
            <th>Last reading</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="device in data.inactiveDevices" :key="device.id">
            <td>{{ device.id }}</td>
            <td>{{ device.deviceModel }}</td>
            <td>{{ device.deviceId }}</td>
            <td>{{ device.deviceChannel }}</td>
            <td>{{ device.location?.name }}</td>
            <td>
              {{ formatTemp(device.lastReadingTemperatureCelsius, useFahrenheit) }}{{ tempUnit }} on
              {{ DateHelpers.dateTimeShortForView(device.lastReadingTime) }}
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="data.inactiveDevices.length < 1" class="text-center">No inactive devices.</div>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
