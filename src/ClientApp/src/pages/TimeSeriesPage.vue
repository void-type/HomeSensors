<script lang="ts" setup>
import TemperatureGraph from '@/components/TemperatureGraph.vue';
import { useRouter } from 'vue-router';
import DateHelpers from '@/models/DateHelpers';
import type { ITimeSeriesInputs } from '@/models/ITimeSeriesInputs';

// Define props for query parameters
defineProps<{
  startDate?: Date;
  endDate?: Date;
  showHumidity?: boolean;
  locationIds?: string;
}>();

const router = useRouter();

// Handle the TemperatureGraph's input changes and update the URL
const onInputsChange = (inputs: ITimeSeriesInputs & { showHumidity: boolean }) => {
  const query = {
    start: inputs.start ? DateHelpers.dateTimeForApi(inputs.start) : undefined,
    end: inputs.end ? DateHelpers.dateTimeForApi(inputs.end) : undefined,
    humidity: inputs.showHumidity ? 'true' : undefined,
    locationIds:
      inputs.locationIds?.length > 0
        ? [...inputs.locationIds].sort((a, b) => a - b).join(',')
        : undefined,
  };

  // Update the URL without reloading the page
  router.replace({ query });
};
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-3">Time series</h1>
    <TemperatureGraph
      class="mt-4"
      :initial-start="startDate"
      :initial-end="endDate"
      :initial-show-humidity="showHumidity"
      :initial-location-ids="
        locationIds
          ?.split(',')
          ?.map(Number)
          ?.filter((id) => !isNaN(id)) || []
      "
      @inputs-change="onInputsChange"
    />
  </div>
</template>

<style lang="scss" scoped></style>
