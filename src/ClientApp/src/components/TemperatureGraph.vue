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

const initialTime = startOfMinute(new Date());

const data = reactive({
  locations: [] as Array<TemperatureLocationResponse>,
  categories: [] as Array<CategoryResponse>,
  graphSeries: [] as Array<TemperatureTimeSeriesLocationData>,
  hvacActions: [] as Array<TemperatureTimeSeriesHvacAction>,
  showHumidity: props.initialShowHumidity !== undefined ? props.initialShowHumidity : false,
  showHvacActions:
    props.initialHideHvacActions !== undefined ? !props.initialHideHvacActions : true,
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

  // TODO: cut off first and last actions to match data range
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

  // Create annotations from HVAC actions
  const hvacAnnotations = data.hvacActions.map((action, index) => {
    const durationMin = Math.round(
      (new Date(action.endTime || '').getTime() - new Date(action.startTime || '').getTime()) /
        (1000 * 60)
    );

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
        display: false, // Don't show a permanent label
      },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      enter(context: any) {
        if (context.chart.tooltip) {
          // Create custom tooltip content
          const tooltipContent = {
            title: trimAndTitleCase(action.action || 'unknown'),
            body: [
              `Duration: ${durationMin} min`,
              `Start: ${DateHelpers.dateTimeForView(startTime)}`,
              `End: ${DateHelpers.dateTimeForView(endTime)}`,
            ],
          };
          // Get mouse position from chart
          const { canvas } = context.chart;
          const rect = canvas.getBoundingClientRect();
          const position = {
            x: rect.left + rect.width / 2, // Default to middle of chart
            y: rect.top + 20, // Position near the top
          };

          // Store the tooltip data on the chart instance for later use
          context.chart.$hoveredAnnotation = {
            content: tooltipContent,
            position,
          };

          // Force tooltip to show
          context.chart.tooltip.setActiveElements([], { x: position.x, y: position.y });
          context.chart.update('none');
        }
        return true;
      },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      leave(context: any) {
        if (context.chart.tooltip) {
          // Clear the stored annotation tooltip data
          context.chart.$hoveredAnnotation = null;

          // Hide the tooltip
          context.chart.tooltip.setActiveElements([], { x: 0, y: 0 });
          context.chart.update('none');

          document.getElementById('chartjs-tooltip')?.remove();
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
            // Only use custom tooltip for annotation hovers
            if (!context.chart.$hoveredAnnotation) {
              return;
            }

            // Get or create tooltip element
            let tooltipEl = document.getElementById('chartjs-tooltip');
            if (!tooltipEl) {
              tooltipEl = document.createElement('div');
              tooltipEl.id = 'chartjs-tooltip';
              tooltipEl.innerHTML = '<table class="tooltip-table"></table>';
              document.body.appendChild(tooltipEl);

              // Style the tooltip
              tooltipEl.style.background = useDarkMode.value
                ? 'rgba(28, 30, 31, 0.9)'
                : 'rgba(255, 255, 255, 0.9)';
              tooltipEl.style.color = useDarkMode.value ? '#e6e6e6' : '#1c1e1f';
              tooltipEl.style.borderRadius = '5px';
              tooltipEl.style.padding = '10px';
              tooltipEl.style.position = 'absolute';
              tooltipEl.style.pointerEvents = 'none';
              tooltipEl.style.zIndex = '100';
              tooltipEl.style.boxShadow = '0 2px 5px rgba(0,0,0,0.25)';
              tooltipEl.style.fontSize = '14px';
            }

            // Get the data from the chart
            const { content } = context.chart.$hoveredAnnotation;
            const tableRoot = tooltipEl.querySelector('table');

            // Display tooltip content
            if (tableRoot && content) {
              let innerHtml = '<thead>';
              innerHtml += `<tr><th style="text-align:center;font-weight:bold;">${content.title}</th></tr>`;
              innerHtml += '</thead><tbody>';

              content.body.forEach((line: string) => {
                innerHtml += `<tr><td>${line}</td></tr>`;
              });

              innerHtml += '</tbody>';
              tableRoot.innerHTML = innerHtml;
            }

            // Position tooltip near the mouse
            // TODO: Tooltip seems to be positioned mirrored across the X axis
            const { canvas } = context.chart;
            const rect = canvas.getBoundingClientRect();

            // Show tooltip
            tooltipEl.style.opacity = '1';
            tooltipEl.style.left = `${rect.left + rect.width / 2}px`;
            tooltipEl.style.top = `${rect.top + 20}px`;
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
  const tooltipEl = document.getElementById('chartjs-tooltip');
  if (tooltipEl) {
    tooltipEl.remove();
  }

  if (lastTimeout) {
    clearTimeout(lastTimeout);
  }
});
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
        <div class="form-check form-switch">
          <label class="form-check-label" for="showHvacActions" @click.stop>
            Show HVAC Actions
          </label>
          <input
            id="showHvacActions"
            v-model="data.showHvacActions"
            class="form-check-input"
            type="checkbox"
          />
        </div>
      </div>
    </div>
    <div class="chart-container-wrapper mt-3">
      <div class="chart-container">
        <canvas id="tempGraph"></canvas>
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
</style>
