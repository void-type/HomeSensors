<script lang="ts" setup>
import TemperatureGraph from '@/components/TemperatureGraph.vue';
import { useRouter } from 'vue-router';
import DateHelpers from '@/models/DateHelpers';
import type { ITimeSeriesInputs } from '@/models/ITimeSeriesInputs';

defineProps<{
  startDate?: Date;
  endDate?: Date;
  showHumidity?: boolean;
  locationIds?: string;
  hideHvacActions?: boolean;
}>();

const router = useRouter();

const onInputsChange = (
  inputs: ITimeSeriesInputs & { hideHvacActions: boolean; showHumidity: boolean }
) => {
  const query = {
    start: inputs.start ? DateHelpers.dateTimeForApi(inputs.start) : undefined,
    end: inputs.end ? DateHelpers.dateTimeForApi(inputs.end) : undefined,
    humidity: inputs.showHumidity ? 'true' : undefined,
    locationIds:
      inputs.locationIds?.length > 0
        ? [...inputs.locationIds].sort((a, b) => a - b).join(',')
        : undefined,
    hideHvacActions: inputs.hideHvacActions ? 'true' : undefined,
  };

  router.replace({
    query,
  });
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
      :initial-hide-hvac-actions="hideHvacActions"
      @inputs-change="onInputsChange"
    />
  </div>
</template>

<style lang="scss" scoped></style>
