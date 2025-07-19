<script lang="ts" setup>
import ApiHelpers from '@/models/ApiHelpers';
import useAppStore from '@/stores/appStore';
import type {
  TemperatureTimeSeriesPoint,
  TemperatureTimeSeriesLocationData,
  TemperatureLocationResponse,
  CategoryResponse,
  TemperatureTimeSeriesHvacAction,
} from '@/api/data-contracts';
import { onMounted, reactive, watch, computed, watchEffect, ref, onUnmounted } from 'vue';
import { addHours, startOfMinute } from 'date-fns';
import { Chart, registerables, type ScriptableScaleContext, type TooltipItem } from 'chart.js';
import 'chartjs-adapter-date-fns';
import annotationPlugin from 'chartjs-plugin-annotation';
import type { HttpResponse } from '@/api/http-client';
import { storeToRefs } from 'pinia';
import {
  formatTemp,
  formatTempWithUnit,
  formatHumidity,
  formatHumidityWithUnit,
  tempUnit,
} from '@/models/TempFormatHelpers';
import { trimAndTitleCase } from '@/models/FormatHelpers';
import DateHelpers from '@/models/DateHelpers';
import useMessageStore from '@/stores/messageStore';
import type { ITimeSeriesInputs } from '@/models/ITimeSeriesInputs';
import AppDateTimePicker from './AppDateTimePicker.vue';

Chart.register(...registerables, annotationPlugin);

const props = defineProps<{
  initialStart?: Date;
  initialEnd?: Date;
  initialShowHumidity?: boolean;
  initialLocationIds?: number[];
  initialHideHvacActions?: boolean;
}>();

const emit = defineEmits(['inputs-change']);

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const { useFahrenheit, useDarkMode } = storeToRefs(appStore);

const data = reactive({
  locations: [] as Array<TemperatureLocationResponse>,
  categories: [] as Array<CategoryResponse>,
  graphSeries: [] as Array<TemperatureTimeSeriesLocationData>,
  hvacActions: [] as Array<TemperatureTimeSeriesHvacAction>,
  showHumidity: props.initialShowHumidity !== undefined ? props.initialShowHumidity : false,
  showHvacActions:
    props.initialHideHvacActions !== undefined ? !props.initialHideHvacActions : true,
});

function getInitialTimeSeriesInputs(): ITimeSeriesInputs {
  const initialTime = startOfMinute(new Date());

  return {
    start: props.initialStart || addHours(initialTime, -48),
    end: props.initialEnd || initialTime,
    locationIds: props.initialLocationIds || ([] as Array<number>),
  };
}

const timeSeriesInputs: ITimeSeriesInputs = reactive(getInitialTimeSeriesInputs());

const showCurrent = ref(false);

function resetTimeSeriesInputs() {
  const initialTime = startOfMinute(new Date());

  timeSeriesInputs.start = addHours(initialTime, -48);
  timeSeriesInputs.end = initialTime;

  if (showCurrent.value) {
    showCurrent.value = false;
  }
}

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
  series: Array<TemperatureTimeSeriesLocationData>,
  useF: boolean,
  showHumidity: boolean,
  showHvacActions: boolean
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

  const datasets = series?.map((s) => {
    const lineColor = getColor(s.location?.name || 'unknown');

    return {
      label: s.location?.name,
      borderColor: lineColor,
      backgroundColor: lineColor,
      data: s.points
        ?.filter((p: TemperatureTimeSeriesPoint) => p.temperatureCelsius)
        .map((p: TemperatureTimeSeriesPoint) => ({
          x: p.time,
          y: showHumidity
            ? formatHumidity(p.humidity)
            : formatTemp(p.temperatureCelsius, useF, useF ? 1 : 2),
        })),
      hidden: oldHiddenCategories.includes(s.location?.name),
    };
  });

  const earliestTemperatureReading =
    series.length > 0
      ? series
          .flatMap((s) => s.points || [])
          .reduce(
            (earliest, current) => {
              if (!current.time) return earliest;
              const currentTime = new Date(current.time).getTime();
              return currentTime < earliest.getTime() ? new Date(current.time) : earliest;
            },
            new Date(series[0].points?.[0]?.time || Date.now())
          )
      : new Date();

  const latestTemperatureReading =
    series.length > 0
      ? series
          .flatMap((s) => s.points || [])
          .reduce(
            (latest, current) => {
              if (!current.time) return latest;
              const currentTime = new Date(current.time).getTime();
              return currentTime > latest.getTime() ? new Date(current.time) : latest;
            },
            new Date(series[0].points?.[0]?.time || 0)
          )
      : new Date();

  const hvacAnnotations = data.hvacActions.map((action, index) => {
    const startTime = new Date(action.startTime || '');
    const endTime = new Date(action.endTime || '');

    const startTimeOrEarliest =
      new Date(action.startTime || '').getTime() < earliestTemperatureReading.getTime()
        ? earliestTemperatureReading
        : startTime;

    const endTimeOrLatest =
      new Date(action.endTime || '').getTime() > latestTemperatureReading.getTime()
        ? latestTemperatureReading
        : endTime;

    return {
      type: 'box',
      xMin: startTimeOrEarliest,
      xMax: endTimeOrLatest,
      backgroundColor:
        action.action === 'heating' ? 'rgba(255, 100, 100, 0.2)' : 'rgba(100, 150, 255, 0.2)',
      borderColor: 'transparent',
      drawTime: 'beforeDatasetsDraw',
      display: true,
      label: {
        display: false,
      },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      enter(context: any) {
        if (context.chart.tooltip) {
          const tooltipContent = {
            title: `${trimAndTitleCase(action.action || 'unknown')}: ${action.durationMinutes} min`,
            body: [
              `Start: ${DateHelpers.dateTimeForView(startTime)}`,
              `End: ${DateHelpers.dateTimeForView(endTime)}`,
            ],
          };

          context.chart.$hoveredAnnotation = {
            content: tooltipContent,
          };

          context.chart.update('none');
        }
        return true;
      },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      leave(context: any) {
        if (context.chart.tooltip) {
          context.chart.$hoveredAnnotation = null;

          context.chart.update('none');

          document.getElementById('chartjs-tooltip')?.classList.add('d-none');
        }
        return true;
      },
      id: `hvac-${index}`,
    };
  });

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
          enabled: true,
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          external(context: any) {
            if (!context.chart.$hoveredAnnotation) {
              return;
            }

            const tooltipEl = document.getElementById('chartjs-tooltip')!;

            const { content } = context.chart.$hoveredAnnotation;
            const cardBody = tooltipEl.querySelector('.tooltip-body');

            if (cardBody && content) {
              let innerHtml = '';

              if (content.title) {
                innerHtml += `<div class="fw-bold">${content.title}</div>`;
              }

              content.body.forEach((line: string) => {
                innerHtml += `<div class="card-text">${line}</div>`;
              });

              cardBody.innerHTML = innerHtml;

              document.getElementById('chartjs-tooltip')?.classList.remove('d-none');
            }
          },
          callbacks: {
            label: (item: TooltipItem<'line'>) =>
              `${item.dataset.label}: ${item.formattedValue}${showHumidity ? '%' : tempUnit(useF)}`,
          },
        },
        annotation: {
          annotations: showHvacActions ? hvacAnnotations : [],
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
    includeHvacActions: true,
  };

  try {
    const response = await api().temperatureReadingsGetTimeSeries(parameters);
    data.graphSeries = response.data.locations || [];
    data.hvacActions = response.data.hvacActions || [];
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function getLocations() {
  try {
    const response = await api().temperatureLocationsGetAll();
    data.locations = response.data.filter((x) => !x.isHidden);
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

  if (timeSeriesInputs.locationIds.length === 0) {
    timeSeriesInputs.locationIds = data.locations.map((x) => x.id as number);
  } else {
    getTimeSeries(timeSeriesInputs);
  }
});

function adjustDateRange(parameters: { days?: number; weeks?: number; months?: number }) {
  // Calculate total days from weeks and months (approximated)
  const totalDays =
    (parameters.days || 0) + (parameters.weeks || 0) * 7 + (parameters.months || 0) * 28;

  // Calculate the current range span in milliseconds
  const currentRangeMs = timeSeriesInputs.end.getTime() - timeSeriesInputs.start.getTime();

  // Calculate the new start and end dates
  const newStart = new Date(timeSeriesInputs.start.getTime() + totalDays * 24 * 60 * 60 * 1000);
  const newEnd = new Date(newStart.getTime() + currentRangeMs);

  // Update the inputs
  timeSeriesInputs.start = newStart;
  timeSeriesInputs.end = newEnd;

  // If showing current, turn it off when manually adjusting dates
  if (showCurrent.value) {
    showCurrent.value = false;
  }
}

watch(timeSeriesInputs, (inputs) => {
  emit('inputs-change', {
    ...inputs,
    showHumidity: data.showHumidity,
    hideHvacActions: !data.showHvacActions,
  });

  getTimeSeries(inputs);
});

watch(
  () => [data.showHumidity, data.showHvacActions],
  () => {
    emit('inputs-change', {
      ...timeSeriesInputs,
      showHumidity: data.showHumidity,
      hideHvacActions: !data.showHvacActions,
    });
  }
);

watchEffect(() => {
  setGraphData(data.graphSeries, useFahrenheit.value, data.showHumidity, data.showHvacActions);
});

onUnmounted(() => {
  if (lastTimeout) {
    clearTimeout(lastTimeout);
  }
});
</script>

<template>
  <div>
    <div>
      <div class="mb-3">
        <div class="d-flex justify-content-between align-items-center mb-2">
          <h5 class="mb-0">Locations</h5>
          <button
            id="selectAllButton"
            class="btn btn-sm btn-outline-secondary ms-auto"
            @click="onSelectAllClick"
          >
            {{ !areAllLocationsSelected ? 'Select' : 'Deselect' }} all
          </button>
        </div>
        <div class="grid">
          <div
            v-for="(values, categoryName) in categorizedLocations"
            :key="categoryName"
            class="g-col-12 g-col-sm-4"
          >
            <div class="fw-bold mb-1">{{ categoryName }}</div>
            <div class="ps-2">
              <div v-for="location in values" :key="location.id" class="form-check">
                <input
                  :id="`locationSelect-${location.id}`"
                  v-model="timeSeriesInputs.locationIds"
                  :value="location.id"
                  class="form-check-input"
                  type="checkbox"
                />
                <label class="form-check-label" :for="`locationSelect-${location.id}`">
                  {{ location.name }}
                </label>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div class="mb-3">
        <div class="form-check form-switch">
          <input
            id="showHumidity"
            v-model="data.showHumidity"
            class="form-check-input"
            type="checkbox"
          />
          <label class="form-check-label" for="showHumidity">Show humidity</label>
        </div>
        <div class="form-check form-switch">
          <input
            id="showHvacActions"
            v-model="data.showHvacActions"
            class="form-check-input"
            type="checkbox"
          />
          <label class="form-check-label" for="showHvacActions">Show HVAC actions</label>
        </div>
      </div>
      <div class="mb-3">
        <div class="grid mb-3">
          <div class="g-col-12 g-col-md-6">
            <label for="startDate" class="form-label">Start date</label>
            <app-date-time-picker id="startDate" v-model="timeSeriesInputs.start" />
          </div>
          <div class="g-col-12 g-col-md-6">
            <label for="endDate" class="form-label">End date</label>
            <app-date-time-picker
              id="endDate"
              v-model="timeSeriesInputs.end"
              :disabled="showCurrent"
            />
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
        </div>
        <div class="d-flex justify-content-center flex-wrap gap-2">
          <div class="btn-group btn-group-sm mb-2">
            <button
              class="btn btn-outline-secondary"
              title="Back 1 Month"
              @click="adjustDateRange({ months: -1 })"
            >
              <span>&laquo;&nbsp;</span>
              <span>Month</span>
            </button>
            <button
              class="btn btn-outline-secondary"
              title="Back 1 Week"
              @click="adjustDateRange({ weeks: -1 })"
            >
              <span>&laquo;&nbsp;</span>
              <span>Week</span>
            </button>
            <button
              class="btn btn-outline-secondary"
              title="Back 1 Day"
              @click="adjustDateRange({ days: -1 })"
            >
              <span>&laquo;&nbsp;</span>
              <span>Day</span>
            </button>
            <button
              class="btn btn-outline-secondary"
              title="Forward 1 Day"
              @click="adjustDateRange({ days: 1 })"
            >
              <span>Day</span>
              <span>&nbsp;&raquo;</span>
            </button>
            <button
              class="btn btn-outline-secondary"
              title="Forward 1 Week"
              @click="adjustDateRange({ weeks: 1 })"
            >
              <span>Week</span>
              <span>&nbsp;&raquo;</span>
            </button>
            <button
              class="btn btn-outline-secondary"
              title="Forward 1 Month"
              @click="adjustDateRange({ months: 1 })"
            >
              <span>Month</span>
              <span>&nbsp;&raquo;</span>
            </button>
          </div>
          <div class="btn-group btn-group-sm mb-2">
            <button
              class="btn btn-outline-secondary"
              title="Reset to last 48 hours"
              @click="resetTimeSeriesInputs()"
            >
              <span>Reset</span>
            </button>
          </div>
        </div>
      </div>
    </div>
    <div class="chart-container-wrapper mt-3 position-relative">
      <div class="chart-container">
        <canvas id="tempGraph"></canvas>
      </div>
      <div id="chartjs-tooltip" class="position-absolute d-none">
        <div class="tooltip-body"></div>
      </div>
    </div>
    <div class="mt-3 grid">
      <div class="g-col-6 cold">
        Cooling:
        {{
          data.hvacActions
            .filter((x) => x.action === 'cooling')
            .reduce((acc, x) => acc + (x.durationMinutes || 0), 0)
        }}
        min
      </div>
      <div class="g-col-6 hot">
        Heating:
        {{
          data.hvacActions
            .filter((x) => x.action === 'heating')
            .reduce((acc, x) => acc + (x.durationMinutes || 0), 0)
        }}
        min
      </div>
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
  height: 400px;
}

.hot {
  color: #d74040;
}

.cold {
  color: #5e83f3;
}

#chartjs-tooltip {
  pointer-events: none;
  z-index: 100;
  bottom: 0;
  left: 0;
  outline: none;
  background-color: black;
  color: white;
  border-radius: 5px;
  padding: 5px;

  .tooltip-body {
    font-size: 0.8rem;
  }
}
</style>
