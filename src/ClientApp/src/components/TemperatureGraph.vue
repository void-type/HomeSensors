<script lang="ts" setup>
import ApiHelpers from '@/models/ApiHelpers';
import useAppStore from '@/stores/appStore';
import type {
  TemperatureTimeSeriesPoint,
  TemperatureTimeSeriesResponse,
  TemperatureLocationResponse,
  CategoryResponse,
} from '@/api/data-contracts';
import { onMounted, reactive, watch, computed, watchEffect, ref } from 'vue';
import { addHours, startOfMinute } from 'date-fns';
import { Chart, registerables, type ScriptableScaleContext, type TooltipItem } from 'chart.js';
import 'chartjs-adapter-date-fns';
import type { HttpResponse } from '@/api/http-client';
import { storeToRefs } from 'pinia';
import {
  formatTemp,
  formatTempWithUnit,
  formatHumidity,
  formatHumidityWithUnit,
  tempUnit,
} from '@/models/TempFormatHelpers';
import DateHelpers from '@/models/DateHelpers';
import useMessageStore from '@/stores/messageStore';
import type { ITimeSeriesInputs } from '@/models/ITimeSeriesInputs';
import AppDateTimePicker from './AppDateTimePicker.vue';

Chart.register(...registerables);

const props = defineProps<{
  initialStart?: Date;
  initialEnd?: Date;
  initialShowHumidity?: boolean;
  initialLocationIds?: number[];
}>();

const emit = defineEmits(['inputs-change']);

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const { useFahrenheit, useDarkMode } = storeToRefs(appStore);

const initialTime = startOfMinute(new Date());

const data = reactive({
  locations: [] as Array<TemperatureLocationResponse>,
  categories: [] as Array<CategoryResponse>,
  graphSeries: [] as Array<TemperatureTimeSeriesResponse>,
  showHumidity: props.initialShowHumidity !== undefined ? props.initialShowHumidity : false,
});

const timeSeriesInputs: ITimeSeriesInputs = reactive({
  start: props.initialStart || addHours(initialTime, -48),
  end: props.initialEnd || initialTime,
  locationIds: props.initialLocationIds || ([] as Array<number>),
});

const showCurrent = ref(false);

const areAllLocationsSelected = computed(() =>
  data.locations.every((value) => timeSeriesInputs.locationIds.includes(value.id as number))
);

function onSelectAllClick() {
  if (areAllLocationsSelected.value) {
    timeSeriesInputs.locationIds = [];
  } else {
    timeSeriesInputs.locationIds = data.locations.map((x) => x.id as number);
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

function setGraphData(
  series: Array<TemperatureTimeSeriesResponse>,
  useF: boolean,
  showHumidity: boolean
) {
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
      ?.filter((p: TemperatureTimeSeriesPoint) => p.temperatureCelsius)
      .map((p: TemperatureTimeSeriesPoint) => ({
        x: p.time,
        y: showHumidity
          ? formatHumidity(p.humidity)
          : formatTemp(p.temperatureCelsius, useF, useF ? 1 : 2),
      })),
    hidden: oldHiddenCategories.includes(s.location?.name),
  }));

  const config = {
    type: 'line',
    data: {
      datasets,
    },
    options: {
      animation: !showCurrent.value,
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
              `${item.dataset.label}: ${item.formattedValue}${showHumidity ? '%' : tempUnit(useF)}`,
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

async function getTimeSeries(inputs: ITimeSeriesInputs) {
  const parameters = {
    startTime: DateHelpers.dateTimeForApi(inputs.start),
    endTime: DateHelpers.dateTimeForApi(inputs.end),
    locationIds: inputs.locationIds,
  };

  try {
    const response = await api().temperatureReadingsGetTimeSeries(parameters);
    data.graphSeries = response.data;
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function getLocations() {
  try {
    const response = await api().temperatureLocationsGetAll();
    data.locations = response.data.filter((x) => !x.isHidden);
    timeSeriesInputs.locationIds = data.locations.map((x) => x.id as number);
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function getCategories() {
  try {
    const response = await api().categoriesGetAll();
    data.categories = response.data;
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

const categorizedLocations = computed(() => {
  const sortedCategories = data.categories.slice().sort((a, b) => (a.order ?? 0) - (b.order ?? 0));

  const groupedReadings = sortedCategories.reduce(
    (acc, category) => {
      if (!category.name) {
        return acc;
      }

      const readings = data.locations.filter((location) => location.categoryId === category.id);

      if (!readings.length) {
        return acc;
      }

      acc[category.name] = readings;

      return acc;
    },
    {} as Record<string, TemperatureLocationResponse[]>
  );

  const uncategorized = data.locations.filter((location) => !location.categoryId);

  if (uncategorized.length) {
    groupedReadings.Uncategorized = uncategorized;
  }

  return groupedReadings;
});

let lastTimeout: number | null = null;
const timeoutSeconds = 5;

function currentTimer() {
  lastTimeout = setTimeout(() => {
    const future = new Date();
    future.setMinutes(future.getMinutes() + 5);
    timeSeriesInputs.end = future;
    currentTimer();
  }, timeoutSeconds * 1000);
}

function setCurrentTimer() {
  if (showCurrent.value) {
    currentTimer();
  } else if (lastTimeout) {
    clearTimeout(lastTimeout);
  }
}

onMounted(async () => {
  await getLocations();
  await getCategories();
});

watch(timeSeriesInputs, (inputs) => {
  emit('inputs-change', {
    ...inputs,
    showHumidity: data.showHumidity,
  });

  getTimeSeries(inputs);
});

watch(
  () => data.showHumidity,
  () => {
    emit('inputs-change', {
      ...timeSeriesInputs,
      showHumidity: data.showHumidity,
    });
  }
);

watchEffect(() => setGraphData(data.graphSeries, useFahrenheit.value, data.showHumidity));
</script>

<template>
  <div>
    <div class="grid">
      <div class="g-col-12 g-col-md-6">
        <label for="startDate" class="form-label">Start date</label>
        <app-date-time-picker id="startDate" v-model="timeSeriesInputs.start" />
      </div>
      <div class="g-col-12 g-col-md-6">
        <label for="endDate" class="form-label">End date</label>
        <app-date-time-picker id="endDate" v-model="timeSeriesInputs.end" :disabled="showCurrent" />
        <div class="form-check form-check-inline mt-2">
          <input
            id="showCurrent"
            v-model="showCurrent"
            class="form-check-input"
            type="checkbox"
            @change="setCurrentTimer()"
          />
          <label class="form-check-label" for="showCurrent">Show current</label>
        </div>
      </div>
      <div class="g-col-12">
        <button id="selectAllButton" class="btn btn-sm btn-secondary" @click="onSelectAllClick">
          {{ !areAllLocationsSelected ? 'Select' : 'Deselect' }} all
        </button>
      </div>
      <div class="g-col-12">
        <div
          v-for="(values, categoryName) in categorizedLocations"
          :key="categoryName"
          class="g-col-12"
        >
          <div>{{ categoryName }}</div>
          <div v-for="location in values" :key="location.id" class="form-check form-check-inline">
            <input
              :id="`locationSelect-${location.id}`"
              v-model="timeSeriesInputs.locationIds"
              :value="location.id"
              class="form-check-input"
              type="checkbox"
            />
            <label class="form-check-label" :for="`locationSelect-${location.id}`">{{
              location.name
            }}</label>
          </div>
        </div>
      </div>
      <div class="g-col-12">
        <div class="form-check form-switch">
          <label class="form-check-label" for="showHumidity" @click.stop>Humidity</label>
          <input
            id="showHumidity"
            v-model="data.showHumidity"
            class="form-check-input"
            type="checkbox"
          />
        </div>
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
        <tr v-for="(series, i) in data.graphSeries" :key="i">
          <td>{{ series.location?.name }}</td>
          <td>
            {{
              data.showHumidity
                ? formatHumidityWithUnit(series.humidityAggregate?.minimum)
                : formatTempWithUnit(series.temperatureAggregate?.minimum, useFahrenheit, 1)
            }}
          </td>
          <td>
            {{
              data.showHumidity
                ? formatHumidityWithUnit(series.humidityAggregate?.maximum)
                : formatTempWithUnit(series.temperatureAggregate?.maximum, useFahrenheit, 1)
            }}
          </td>
          <td>
            {{
              data.showHumidity
                ? formatHumidityWithUnit(series.humidityAggregate?.average)
                : formatTempWithUnit(series.temperatureAggregate?.average, useFahrenheit, 1)
            }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style lang="scss" scoped>
.chart-container {
  position: relative;
  min-height: 600px;
}

#tempGraph {
  height: 400px;
}
</style>
