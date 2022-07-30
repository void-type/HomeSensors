<script lang="ts" setup>
import { Api } from '@/api/Api';
import useAppStore from '@/stores/appStore';
import type { InactiveDevice } from '@/api/data-contracts';
import { onMounted, reactive } from 'vue';
import type { HttpResponse } from '@/api/http-client';

const appStore = useAppStore();

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
    <div class="row mt-4">
      <table class="table">
        <thead>
          <tr>
            <th>Id</th>
            <th>Model</th>
            <th>Device ID</th>
            <th>Channel</th>
            <th>Location</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="device in data.inactiveDevices" :key="device.id">
            <td>{{ device.id }}</td>
            <td>{{ device.deviceModel }}</td>
            <td>{{ device.deviceId }}</td>
            <td>{{ device.deviceChannel }}</td>
            <td>{{ device.locationName }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
