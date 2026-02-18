<script lang="ts" setup>
import type { ScriptableScaleContext, TooltipItem } from 'chart.js';
import type {
  CategoryResponse,
  TemperatureLocationResponse,
  TemperatureTimeSeriesHvacAction,
  TemperatureTimeSeriesLocationData,
  TemperatureTimeSeriesPoint,
} from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import type { ITemperatureGraphInputs } from '@/models/ITemperatureGraphInputs';
import type { ITimeSeriesInputs } from '@/models/ITimeSeriesInputs';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { Chart, registerables } from 'chart.js';
import annotationPlugin from 'chartjs-plugin-annotation';
import { addHours, startOfMinute } from 'date-fns';
import { storeToRefs } from 'pinia';
import { computed, onMounted, onUnmounted, reactive, ref, watch, watchEffect } from 'vue';
import ApiHelpers from '@/models/ApiHelpers';
import DateHelpers from '@/models/DateHelpers';
import { trimAndTitleCase } from '@/models/FormatHelpers';
import { debounce } from '@/models/InputHelper';
import {
  formatHumidity,
  formatHumidityWithUnit,
  formatTemp,
  formatTempWithUnit,
  tempUnit,
} from '@/models/TempFormatHelpers';
import useAppStore from '@/stores/appStore';
import useMessageStore from '@/stores/messageStore';
import AppDateTimePicker from './AppDateTimePicker.vue';
import 'chartjs-adapter-date-fns';

const props = defineProps<{
  initialStart?: Date;
  initialEnd?: Date;
  initialShowHumidity?: boolean;
  initialLocationIds?: number[];
  initialHideHvacActions?: boolean;
}>();

const emit = defineEmits<{
  inputsChange: [value: ITemperatureGraphInputs];
}>();

Chart.register(...registerables, annotationPlugin);

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

const timeSeriesInputs = reactive(getInitialTimeSeriesInputs());

const showCurrent = ref(false);

function setTimeRange(hours: number) {
  const initialTime = startOfMinute(new Date());

  timeSeriesInputs.start = addHours(initialTime, -hours);
  timeSeriesInputs.end = initialTime;

  if (showCurrent.value) {
    showCurrent.value = false;
  }
}

const areAllLocationsSelected = computed(() =>
  data.locations.every(value => timeSeriesInputs.locationIds.includes(value.id as number)),
);

function onSelectAllClick() {
  if (areAllLocationsSelected.value) {
    timeSeriesInputs.locationIds = [];
  } else {
    timeSeriesInputs.locationIds = data.locations.map(x => x.id as number);
  }
}

let lineChart: Chart | null = null;

const colorCache: Record<string, string> = {};

function getRandomColor() {
  const letters = '0123456789ABCDEF'.split('');
  let color = '#';
  for (let i = 0; i < 6; i += 1) {
    color += letters[Math.floor(Math.random() * 16)];
  }
  return color;
}

function isValidHexColor(color: string | undefined): boolean {
  if (!color) {
    return false;
  }
  return /^#[0-9A-F]{6}$/i.test(color);
}

function getColor(location: TemperatureLocationResponse | null | undefined): string {
  if (location?.color && isValidHexColor(location.color)) {
    return location.color;
  }

  const locationName = location?.name || 'unknown';

  // Use cached color if available, otherwise generate and cache a random one
  if (!colorCache[locationName]) {
    colorCache[locationName] = getRandomColor();
  }

  return colorCache[locationName];
}

function getOrCreateLegendList(id: string): HTMLUListElement {
  const container = document.getElementById(id)!;
  let list = container.querySelector('ul');

  if (!list) {
    list = document.createElement('ul');
    list.classList.add('legend-list');
    container.appendChild(list);
  }

  return list;
}

const htmlLegendPlugin = {
  id: 'htmlLegend',
  afterUpdate(chart: Chart) {
    const containerID = (chart.options.plugins as any)?.htmlLegend?.containerID;
    if (!containerID) {
      return;
    }

    const ul = getOrCreateLegendList(containerID);
    ul.innerHTML = '';

    const items = chart.options.plugins?.legend?.labels?.generateLabels?.(chart) ?? [];

    items.forEach((item) => {
      const li = document.createElement('li');
      li.classList.add('legend-item');
      li.onclick = () => {
        chart.setDatasetVisibility(item.datasetIndex!, !chart.isDatasetVisible(item.datasetIndex!));
        chart.update();
      };

      const colorBox = document.createElement('span');
      colorBox.classList.add('legend-color-box');
      colorBox.style.backgroundColor = item.fillStyle as string;
      colorBox.style.borderColor = item.strokeStyle as string;

      const label = document.createElement('span');
      label.classList.add('legend-label');
      label.textContent = item.text;

      if (item.hidden) {
        colorBox.style.opacity = '0.3';
        label.classList.add('legend-label-hidden');
      }

      li.appendChild(colorBox);
      li.appendChild(label);
      ul.appendChild(li);
    });
  },
};

function setGraphData(
  series: Array<TemperatureTimeSeriesLocationData>,
  useF: boolean,
  showHumidity: boolean,
  showHvacActions: boolean,
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
    const lineColor = getColor(s.location);

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

  const earliestTemperatureReading
    = series.length > 0
      ? series
          .flatMap(s => s.points || [])
          .reduce(
            (earliest, current) => {
              if (!current.time) {
                return earliest;
              }
              const currentTime = new Date(current.time).getTime();
              return currentTime < earliest.getTime() ? new Date(current.time) : earliest;
            },
            new Date(series[0]?.points?.[0]?.time || Date.now()),
          )
      : new Date();

  const latestTemperatureReading
    = series.length > 0
      ? series
          .flatMap(s => s.points || [])
          .reduce(
            (latest, current) => {
              if (!current.time) {
                return latest;
              }
              const currentTime = new Date(current.time).getTime();
              return currentTime > latest.getTime() ? new Date(current.time) : latest;
            },
            new Date(series[0]?.points?.[0]?.time || 0),
          )
      : new Date();

  const hvacAnnotations = data.hvacActions.map((action, index) => {
    const startTime = new Date(action.startTime || '');
    const endTime = new Date(action.endTime || '');

    const startTimeOrEarliest
      = new Date(action.startTime || '').getTime() < earliestTemperatureReading.getTime()
        ? earliestTemperatureReading
        : startTime;

    const endTimeOrLatest
      = new Date(action.endTime || '').getTime() > latestTemperatureReading.getTime()
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
          display: false,
        },
        htmlLegend: {
          containerID: 'chart-legend-container',
        },
        title: {
          display: false,
        },
        tooltip: {
          enabled: true,
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

  lineChart = new Chart(element, { ...(config as any), plugins: [htmlLegendPlugin] });
}

async function getTimeSeries(inputs: ITimeSeriesInputs) {
  const parameters = {
    startTime: DateHelpers.dateTimeForApi(inputs.start),
    endTime: DateHelpers.dateTimeForApi(inputs.end),
    locationIds: inputs.locationIds,
    includeHvacActions: true,
    trimHvacActionsToRequestedTimeRange: true,
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
    data.locations = response.data.filter(x => !x.isHidden);
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

      const readings = data.locations.filter(location => location.categoryId === category.id);

      if (!readings.length) {
        return acc;
      }

      acc[category.name] = readings;

      return acc;
    },
    {} as Record<string, TemperatureLocationResponse[]>,
  );

  const uncategorized = data.locations.filter(location => !location.categoryId);

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
    const future = new Date();
    future.setMinutes(future.getMinutes() + 5);
    timeSeriesInputs.end = future;
    currentTimer();
  } else if (lastTimeout) {
    clearTimeout(lastTimeout);
  }
}

onMounted(async () => {
  await getLocations();
  await getCategories();

  if (timeSeriesInputs.locationIds.length === 0) {
    timeSeriesInputs.locationIds = data.locations.map(x => x.id as number);
  } else {
    getTimeSeries(timeSeriesInputs);
  }
});

function adjustDateRange(parameters: { days?: number; weeks?: number; months?: number }) {
  // Calculate total days from weeks and months (approximated)
  const totalDays
    = (parameters.days || 0) + (parameters.weeks || 0) * 7 + (parameters.months || 0) * 28;

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

const getTimeSeriesDebounced = debounce(getTimeSeries as (...args: unknown[]) => unknown, 200);

watch(timeSeriesInputs, async (inputs) => {
  emit('inputsChange', {
    ...inputs,
    showHumidity: data.showHumidity,
    hideHvacActions: !data.showHvacActions,
  });

  await getTimeSeriesDebounced(inputs);
});

watch(
  () => [data.showHumidity, data.showHvacActions],
  () => {
    emit('inputsChange', {
      ...timeSeriesInputs,
      showHumidity: data.showHumidity,
      hideHvacActions: !data.showHvacActions,
    });
  },
);

watchEffect(() => {
  setGraphData(data.graphSeries, useFahrenheit.value, data.showHumidity, data.showHvacActions);
});

function formatDuration(totalMinutes: number): string {
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours === 0) {
    return `${minutes}m`;
  }

  return `${hours}h ${minutes}m`;
}

const coolingMinutes = computed(() =>
  data.hvacActions
    .filter(x => x.action === 'cooling')
    .reduce((acc, x) => acc + (x.durationMinutes || 0), 0),
);

const heatingMinutes = computed(() =>
  data.hvacActions
    .filter(x => x.action === 'heating')
    .reduce((acc, x) => acc + (x.durationMinutes || 0), 0),
);

onUnmounted(() => {
  if (lastTimeout) {
    clearTimeout(lastTimeout);
  }
});
</script>

<template>
  <div class="grid">
    <!-- Left sidebar - desktop only -->
    <div class="g-col-12 g-col-lg-3 d-none d-lg-block">
      <div id="controlsAccordionDesktop" class="accordion">
        <!-- Locations accordion item -->
        <div class="accordion-item">
          <div class="accordion-header">
            <button
              class="accordion-button"
              type="button"
              data-bs-toggle="collapse"
              data-bs-target="#locationsCollapseDesktop"
              aria-expanded="true"
              aria-controls="locationsCollapseDesktop"
            >
              Locations
            </button>
          </div>
          <div
            id="locationsCollapseDesktop"
            class="accordion-collapse collapse show"
            data-bs-parent="#controlsAccordionDesktop"
          >
            <div class="accordion-body">
              <button
                class="btn btn-sm btn-outline-secondary mb-2"
                @click="onSelectAllClick"
              >
                {{ !areAllLocationsSelected ? "Select" : "Deselect" }} all
              </button>
              <div
                v-for="(values, categoryName) in categorizedLocations"
                :key="categoryName"
                class="mb-2"
              >
                <div class="fw-bold mb-1">
                  {{ categoryName }}
                </div>
                <div class="ps-2">
                  <div v-for="location in values" :key="location.id" class="form-check">
                    <input
                      :id="`locationSelectDesktop-${location.id}`"
                      v-model="timeSeriesInputs.locationIds"
                      :value="location.id"
                      class="form-check-input"
                      type="checkbox"
                    >
                    <label class="form-check-label" :for="`locationSelectDesktop-${location.id}`">
                      {{ location.name }}
                    </label>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Display options accordion item -->
        <div class="accordion-item">
          <div class="accordion-header">
            <button
              class="accordion-button collapsed"
              type="button"
              data-bs-toggle="collapse"
              data-bs-target="#displayCollapseDesktop"
              aria-expanded="false"
              aria-controls="displayCollapseDesktop"
            >
              Display Options
            </button>
          </div>
          <div
            id="displayCollapseDesktop"
            class="accordion-collapse collapse"
            data-bs-parent="#controlsAccordionDesktop"
          >
            <div class="accordion-body">
              <div class="form-check form-switch">
                <input
                  id="showHumidityDesktop"
                  v-model="data.showHumidity"
                  class="form-check-input"
                  type="checkbox"
                >
                <label class="form-check-label" for="showHumidityDesktop">Show humidity</label>
              </div>
              <div class="form-check form-switch">
                <input
                  id="showHvacActionsDesktop"
                  v-model="data.showHvacActions"
                  class="form-check-input"
                  type="checkbox"
                >
                <label class="form-check-label" for="showHvacActionsDesktop">Show HVAC actions</label>
              </div>
            </div>
          </div>
        </div>

        <!-- Date range accordion item -->
        <div class="accordion-item">
          <div class="accordion-header">
            <button
              class="accordion-button collapsed"
              type="button"
              data-bs-toggle="collapse"
              data-bs-target="#dateRangeCollapseDesktop"
              aria-expanded="false"
              aria-controls="dateRangeCollapseDesktop"
            >
              Date Range
            </button>
          </div>
          <div
            id="dateRangeCollapseDesktop"
            class="accordion-collapse collapse"
            data-bs-parent="#controlsAccordionDesktop"
          >
            <div class="accordion-body">
              <div class="mb-3">
                <label for="startDateDesktop" class="form-label">Start date</label>
                <AppDateTimePicker id="startDateDesktop" v-model="timeSeriesInputs.start" />
              </div>
              <div class="mb-3">
                <label for="endDateDesktop" class="form-label">End date</label>
                <AppDateTimePicker
                  id="endDateDesktop"
                  v-model="timeSeriesInputs.end"
                  :disabled="showCurrent"
                />
                <div class="form-check mt-2">
                  <input
                    id="showCurrentDesktop"
                    v-model="showCurrent"
                    class="form-check-input"
                    type="checkbox"
                    @change="setCurrentTimer()"
                  >
                  <label class="form-check-label" for="showCurrentDesktop">Show current</label>
                </div>
              </div>
              <div class="d-flex flex-column gap-2">
                <div class="btn-group btn-group-sm">
                  <button
                    class="btn btn-outline-secondary"
                    title="Back 1 Month"
                    @click="adjustDateRange({ months: -1 })"
                  >
                    &laquo; Month
                  </button>
                  <button
                    class="btn btn-outline-secondary"
                    title="Forward 1 Month"
                    @click="adjustDateRange({ months: 1 })"
                  >
                    Month &raquo;
                  </button>
                </div>
                <div class="btn-group btn-group-sm">
                  <button
                    class="btn btn-outline-secondary"
                    title="Back 1 Week"
                    @click="adjustDateRange({ weeks: -1 })"
                  >
                    &laquo; Week
                  </button>
                  <button
                    class="btn btn-outline-secondary"
                    title="Forward 1 Week"
                    @click="adjustDateRange({ weeks: 1 })"
                  >
                    Week &raquo;
                  </button>
                </div>
                <div class="btn-group btn-group-sm">
                  <button
                    class="btn btn-outline-secondary"
                    title="Back 1 Day"
                    @click="adjustDateRange({ days: -1 })"
                  >
                    &laquo; Day
                  </button>
                  <button
                    class="btn btn-outline-secondary"
                    title="Forward 1 Day"
                    @click="adjustDateRange({ days: 1 })"
                  >
                    Day &raquo;
                  </button>
                </div>
                <div class="btn-group btn-group-sm">
                  <button
                    class="btn btn-outline-secondary"
                    title="Last 48 hours"
                    @click="setTimeRange(48)"
                  >
                    48h
                  </button>
                  <button
                    class="btn btn-outline-secondary"
                    title="Last 24 hours"
                    @click="setTimeRange(24)"
                  >
                    24h
                  </button>
                  <button
                    class="btn btn-outline-secondary"
                    title="Last 12 hours"
                    @click="setTimeRange(12)"
                  >
                    12h
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Main content area -->
    <div class="g-col-12 g-col-lg-9">
      <!-- Mobile controls - only visible on screens smaller than lg -->
      <div class="d-lg-none mb-3">
        <div id="controlsAccordionMobile" class="accordion">
          <!-- Locations accordion item -->
          <div class="accordion-item">
            <div class="accordion-header">
              <button
                class="accordion-button collapsed"
                type="button"
                data-bs-toggle="collapse"
                data-bs-target="#locationsCollapseMobile"
                aria-expanded="false"
                aria-controls="locationsCollapseMobile"
              >
                Locations
              </button>
            </div>
            <div
              id="locationsCollapseMobile"
              class="accordion-collapse collapse"
              data-bs-parent="#controlsAccordionMobile"
            >
              <div class="accordion-body">
                <button
                  class="btn btn-sm btn-outline-secondary mb-2"
                  @click="onSelectAllClick"
                >
                  {{ !areAllLocationsSelected ? "Select" : "Deselect" }} all
                </button>
                <div class="grid">
                  <div
                    v-for="(values, categoryName) in categorizedLocations"
                    :key="categoryName"
                    class="g-col-12 g-col-sm-4"
                  >
                    <div class="fw-bold mb-1">
                      {{ categoryName }}
                    </div>
                    <div class="ps-2">
                      <div v-for="location in values" :key="location.id" class="form-check">
                        <input
                          :id="`locationSelectMobile-${location.id}`"
                          v-model="timeSeriesInputs.locationIds"
                          :value="location.id"
                          class="form-check-input"
                          type="checkbox"
                        >
                        <label class="form-check-label" :for="`locationSelectMobile-${location.id}`">
                          {{ location.name }}
                        </label>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Display options accordion item -->
          <div class="accordion-item">
            <div class="accordion-header">
              <button
                class="accordion-button collapsed"
                type="button"
                data-bs-toggle="collapse"
                data-bs-target="#displayCollapseMobile"
                aria-expanded="false"
                aria-controls="displayCollapseMobile"
              >
                Display Options
              </button>
            </div>
            <div
              id="displayCollapseMobile"
              class="accordion-collapse collapse"
              data-bs-parent="#controlsAccordionMobile"
            >
              <div class="accordion-body">
                <div class="form-check form-switch">
                  <input
                    id="showHumidityMobile"
                    v-model="data.showHumidity"
                    class="form-check-input"
                    type="checkbox"
                  >
                  <label class="form-check-label" for="showHumidityMobile">Show humidity</label>
                </div>
                <div class="form-check form-switch">
                  <input
                    id="showHvacActionsMobile"
                    v-model="data.showHvacActions"
                    class="form-check-input"
                    type="checkbox"
                  >
                  <label class="form-check-label" for="showHvacActionsMobile">Show HVAC actions</label>
                </div>
              </div>
            </div>
          </div>

          <!-- Date range accordion item -->
          <div class="accordion-item">
            <div class="accordion-header">
              <button
                class="accordion-button collapsed"
                type="button"
                data-bs-toggle="collapse"
                data-bs-target="#dateRangeCollapseMobile"
                aria-expanded="false"
                aria-controls="dateRangeCollapseMobile"
              >
                Date Range
              </button>
            </div>
            <div
              id="dateRangeCollapseMobile"
              class="accordion-collapse collapse"
              data-bs-parent="#controlsAccordionMobile"
            >
              <div class="accordion-body">
                <div class="grid mb-3">
                  <div class="g-col-12 g-col-md-6">
                    <label for="startDateMobile" class="form-label">Start date</label>
                    <AppDateTimePicker id="startDateMobile" v-model="timeSeriesInputs.start" />
                  </div>
                  <div class="g-col-12 g-col-md-6">
                    <label for="endDateMobile" class="form-label">End date</label>
                    <AppDateTimePicker
                      id="endDateMobile"
                      v-model="timeSeriesInputs.end"
                      :disabled="showCurrent"
                    />
                    <div class="form-check form-check-inline mt-2">
                      <input
                        id="showCurrentMobile"
                        v-model="showCurrent"
                        class="form-check-input"
                        type="checkbox"
                        @change="setCurrentTimer()"
                      >
                      <label class="form-check-label" for="showCurrentMobile">Show current</label>
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
                      title="Last 48 hours"
                      @click="setTimeRange(48)"
                    >
                      48h
                    </button>
                    <button
                      class="btn btn-outline-secondary"
                      title="Last 24 hours"
                      @click="setTimeRange(24)"
                    >
                      24h
                    </button>
                    <button
                      class="btn btn-outline-secondary"
                      title="Last 12 hours"
                      @click="setTimeRange(12)"
                    >
                      12h
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Chart -->
      <div class="chart-container-wrapper position-relative">
        <div class="chart-container">
          <canvas id="tempGraph" />
        </div>
        <div id="chart-legend-container" />
        <div id="chartjs-tooltip" class="position-absolute d-none">
          <div class="tooltip-body" />
        </div>
      </div>

      <!-- HVAC stats -->
      <div class="mt-4 d-flex gap-4">
        <div class="cold">
          <FontAwesomeIcon icon="fa-snowflake" aria-hidden="true" class="me-1" />
          <span class="visually-hidden">Cooling:</span>
          {{ formatDuration(coolingMinutes) }}
        </div>
        <div class="hot">
          <FontAwesomeIcon icon="fa-fire" aria-hidden="true" class="me-1" />
          <span class="visually-hidden">Heating:</span>
          {{ formatDuration(heatingMinutes) }}
        </div>
      </div>

      <!-- Stats table -->
      <table class="mt-3 table" :class="{ 'table-dark': useDarkMode }">
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
            <td>
              <span
                class="color-dot me-2"
                :style="{ backgroundColor: getColor(series.location) }"
              />
              {{ series.location?.name }}
            </td>
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
  </div>
</template>

<style lang="scss" scoped>
.chart-container {
  position: relative;
  height: 400px;
}

.color-dot {
  display: inline-block;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  vertical-align: middle;
}

.hot {
  color: #d74040;
}

.cold {
  color: #5e83f3;
}

#chart-legend-container {
  display: flex;
  justify-content: center;
  padding-top: 0.5rem;
}

:deep(.legend-list) {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  list-style: none;
  margin: 0;
  padding: 0;
  gap: 0.5rem 1rem;
}

:deep(.legend-item) {
  display: flex;
  align-items: center;
  cursor: pointer;
  font-size: 0.85rem;
  gap: 0.35rem;
}

:deep(.legend-color-box) {
  display: inline-block;
  width: 14px;
  height: 14px;
  border-radius: 50%;
}

:deep(.legend-label) {
  color: var(--bs-body-color);
}

:deep(.legend-label-hidden) {
  text-decoration: line-through;
  opacity: 0.5;
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
