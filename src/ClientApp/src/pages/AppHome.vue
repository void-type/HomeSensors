<script lang="ts" setup>
import TemperatureCurrentReadings from '@/components/TemperatureCurrentReadings.vue';
import TemperatureGraph from '@/components/TemperatureGraph.vue';
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-4 mb-0">Temperatures</h1>
    <TemperatureCurrentReadings />
    <TemperatureGraph />
  </div>
</template>

<style lang="scss" scoped></style>
