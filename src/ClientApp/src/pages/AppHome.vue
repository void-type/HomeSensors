<script lang="ts" setup>
import TemperatureCurrentReadings from '@/components/TemperatureCurrentReadings.vue';
import TemperatureGraph from '@/components/TemperatureGraph.vue';
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-4 mb-4">Temperatures</h1>
    <TemperatureCurrentReadings />
    <h2 class="mt-5 mb-4">Time series</h2>
    <TemperatureGraph />
  </div>
</template>

<style lang="scss" scoped></style>
