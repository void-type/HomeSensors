<script lang="ts" setup>
import ApiHelpers from '@/models/ApiHelpers';
import useAppStore from '@/stores/appStore';
import type { GraphPoint, GraphTimeSeries, Location } from '@/api/data-contracts';
import { onMounted, reactive, watch, computed } from 'vue';
import { addHours, startOfMinute } from 'date-fns';
import { Chart, registerables, type ScriptableScaleContext, type TooltipItem } from 'chart.js';
import 'chartjs-adapter-date-fns';
import type { HttpResponse } from '@/api/http-client';
import { storeToRefs } from 'pinia';
import { formatTemp, formatTempWithUnit, tempUnit } from '@/models/TempFormatHelpers';
import DateHelpers from '@/models/DateHelpers';

Chart.register(...registerables);

const appStore = useAppStore();
const api = ApiHelpers.client;

const { useFahrenheit, useDarkMode } = storeToRefs(appStore);

const initialTime = startOfMinute(new Date());

const data = reactive({
  graphRange: {
    start: addHours(initialTime, -48),
    end: initialTime,
    locationIds: [] as Array<number>,
  },
  locations: [] as Array<Location>,
  graphSeries: [] as Array<GraphTimeSeries>,
});

const tempUnitComputed = computed(() => tempUnit(useFahrenheit.value));

const areAllLocationsSelected = computed(() =>
  data.locations.every((value) => data.graphRange.locationIds.includes(value.id as number))
);

function onSelectAllClick() {
  if (areAllLocationsSelected.value) {
    data.graphRange.locationIds = [];
  } else {
    data.graphRange.locationIds = data.locations.map((x) => x.id as number);
  }
}

let lineChart: Chart | null = null;

const categoryColors: Record<string, string> = {
  Basement: '#2ac4b3',
  Bedroom: '#b2df8a',
  Garage: '#ff526f',
  'Garage Freezer': '#73a2ef',
  'Garage Fridge': '#3064cf',
  "Jeff's Office": '#b180d0',
  Outside: '#feaf29',
};

const predefinedColors = [
  '#8aaec7',
  '#fe7db7',
  '#33a02c',
  '#0097fb',
  '#914bdc',
  '#915535',
  '#5d5652',
];

function getRandomColor() {
  const letters = '0123456789ABCDEF'.split('');
  let color = '#';
  for (let i = 0; i < 6; i += 1) {
    color += letters[Math.floor(Math.random() * 16)];
  }
  return color;
}

function getColor(categoryName: string) {
  const existing = categoryColors[categoryName];

  if (existing) {
    return existing;
  }

  const predefinedColor = predefinedColors.find((x) => !Object.values(categoryColors).includes(x));

  if (predefinedColor) {
    categoryColors[categoryName] = predefinedColor;
    return predefinedColor;
  }

  const randomColor = getRandomColor();

  categoryColors[categoryName] = randomColor;
  return randomColor;
}

function setGraphData(series: Array<GraphTimeSeries>) {
  const element = document.getElementById('tempGraph') as HTMLCanvasElement;

  if (element === null) {
    return;
  }

  const oldHiddenCategories: Array<string | null | undefined> = [];

  if (lineChart != null) {
    const oldCategoryCount = lineChart?.data.datasets.length || 0;

    for (let i = 0; i < oldCategoryCount; i += 1) {
      const datasetMeta = lineChart.getDatasetMeta(i);
      const isHidden = datasetMeta.visible === false;

      if (isHidden && datasetMeta) {
        oldHiddenCategories.push(datasetMeta.label);
      }
    }

    lineChart?.destroy();
  }

  const datasets = series?.map((s) => ({
    label: s.location?.name,
    borderColor: getColor(s.location?.name || 'unknown'),
    data: s.points
      ?.filter((p: GraphPoint) => p.temperatureCelsius)
      .map((p: GraphPoint) => ({
        x: p.time,
        y: formatTemp(p.temperatureCelsius, useFahrenheit.value),
      })),
    hidden: oldHiddenCategories.includes(s.location?.name),
  }));

  const config = {
    type: 'line',
    data: {
      datasets,
    },
    options: {
      // 12 hours
      spanGaps: 1000 * 60 * 60 * 12,
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: 'bottom',
        },
        title: {
          display: false,
        },
        tooltip: {
          callbacks: {
            label: (item: TooltipItem<'line'>) =>
              `${item.dataset.label}: ${item.formattedValue}${tempUnitComputed.value}`,
          },
        },
      },

      scales: {
        x: {
          type: 'time',
          time: {
            displayFormats: {
              millisecond: 'HH:mm:ss',
              second: 'HH:mm:ss',
              minute: 'HH:mm',
              hour: 'HH',
              day: 'MMM d',
              week: 'PP',
              month: 'MMM yyyy',
              quarter: 'QQQ yyyy',
              year: 'yyyy',
            },
            tooltipFormat: 'yyyy-MM-dd HH:mm:ss',
          },
          ticks: {
            autoSkip: false,
            major: {
              enabled: true,
            },
            font(context: ScriptableScaleContext) {
              if (context.tick && context.tick.major) {
                return {
                  weight: 'bold',
                };
              }
              return {};
            },
          },
        },
      },
    },
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  lineChart = new Chart(element, config as any);
}

async function getTimeSeries() {
  const parameters = {
    startTime: DateHelpers.dateTimeForApi(data.graphRange.start),
    endTime: DateHelpers.dateTimeForApi(data.graphRange.end),
    locationIds: data.graphRange.locationIds,
  };

  try {
    const response = await api().temperaturesReadingsTimeSeriesCreate(parameters);
    data.graphSeries = response.data;
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function getLocations() {
  try {
    const response = await api().temperaturesLocationsAllCreate();
    data.locations = response.data;
    data.graphRange.locationIds = data.locations.map((x) => x.id as number);
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

onMounted(async () => {
  await getTimeSeries();
  await getLocations();
});

watch(
  () => [data.graphRange.start, data.graphRange.end, data.graphRange.locationIds],
  () => getTimeSeries()
);

watch(
  () => [data.graphSeries, useFahrenheit.value],
  () => setGraphData(data.graphSeries)
);
</script>

<template>
  <div class="grid mt-4">
    <div class="g-col-12 g-col-md-6 mb-3">
      <label for="startDate" class="form-label">Start date</label>
      <v-date-picker
        v-model="data.graphRange.start"
        mode="dateTime"
        :masks="{ inputDateTime24hr: 'YYYY-MM-DD HH:MM' }"
        :update-on-input="false"
        is24hr
        is-required
        ><template #default="{ inputValue, inputEvents }">
          <input id="startDate" class="form-control" :value="inputValue" v-on="inputEvents" />
        </template>
      </v-date-picker>
    </div>
    <div class="g-col-12 g-col-md-6 mb-3">
      <label for="endDate" class="form-label">End date</label>
      <v-date-picker
        v-model="data.graphRange.end"
        mode="dateTime"
        :masks="{ inputDateTime24hr: 'YYYY-MM-DD HH:MM' }"
        :update-on-input="false"
        is24hr
        is-required
        ><template #default="{ inputValue, inputEvents }">
          <input id="endDate" class="form-control" :value="inputValue" v-on="inputEvents" />
        </template>
      </v-date-picker>
    </div>
  </div>
  <div class="text-center mb-1">
    <button id="selectAllButton" class="btn btn-sm btn-secondary" @click="onSelectAllClick">
      {{ !areAllLocationsSelected ? 'Select' : 'Deselect' }} all
    </button>
  </div>
  <div class="text-center">
    <div v-for="location in data.locations" :key="location.id" class="form-check form-check-inline">
      <input
        :id="`locationSelect-${location.id}`"
        v-model="data.graphRange.locationIds"
        :value="location.id"
        class="form-check-input"
        type="checkbox"
      />
      <label class="form-check-label" :for="`locationSelect-${location.id}`">{{
        location.name
      }}</label>
    </div>
  </div>
  <div class="chart-container mt-3">
    <canvas id="tempGraph"></canvas>
  </div>
  <table :class="{ 'mt-3': true, table: true, 'table-dark': useDarkMode }">
    <thead>
      <tr>
        <th>Location</th>
        <th>Low</th>
        <th>High</th>
        <th>Avg</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="(point, i) in data.graphSeries" :key="i">
        <td>{{ point.location?.name }}</td>
        <td>{{ formatTempWithUnit(point.minTemperatureCelsius, useFahrenheit) }}</td>
        <td>{{ formatTempWithUnit(point.maxTemperatureCelsius, useFahrenheit) }}</td>
        <td>{{ formatTempWithUnit(point.averageTemperatureCelsius, useFahrenheit) }}</td>
      </tr>
    </tbody>
  </table>
</template>

<style lang="scss" scoped>
.chart-container {
  position: relative;
}

#tempGraph {
  height: 400px;
}
</style>
