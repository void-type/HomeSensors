<script lang="ts" setup>
import { Api } from '@/api/Api';
import useAppStore from '@/stores/appStore';
import type { GraphPoint, GraphCurrentReading, GraphTimeSeries } from '@/api/data-contracts';
import { onMounted, reactive, watch } from 'vue';
import { addHours, startOfMinute, format } from 'date-fns';
import { Chart, registerables, type ScriptableScaleContext, type TooltipItem } from 'chart.js';
import 'chartjs-adapter-date-fns';
import * as signalR from '@microsoft/signalr';
import type { HttpResponse } from '@/api/http-client';
import { storeToRefs } from 'pinia';
import { formatTemp } from '@/models/FormatHelpers';
import DateHelpers from '@/models/DateHelpers';

Chart.register(...registerables);

const appStore = useAppStore();

const { useFahrenheit, useDarkMode, tempUnit, showHumidity } = storeToRefs(appStore);

const initialTime = startOfMinute(new Date());

const data = reactive({
  graphRange: {
    start: addHours(initialTime, -48),
    end: initialTime,
  },
  graphSeries: [] as Array<GraphTimeSeries>,
  currentReadings: [] as Array<GraphCurrentReading>,
});

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
    label: s.location,
    borderColor: getColor(s.location || 'unknown'),
    data: s.points
      ?.filter((p: GraphPoint) => p.temperatureCelsius)
      .map((p: GraphPoint) => ({
        x: p.time,
        y: formatTemp(p.temperatureCelsius, useFahrenheit.value),
      })),
    hidden: oldHiddenCategories.includes(s.location),
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
              `${item.dataset.label}: ${item.formattedValue}${tempUnit.value}`,
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
    locationIds: [
      1,
      2,
      3
    ],
  };

  try {
    const response = await new Api().temperaturesTimeSeriesCreate(parameters);
    data.graphSeries = response.data;
  } catch (error) {
    appStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

let connection: signalR.HubConnection | null = null;

async function connectToHub() {
  async function connectInternal() {
    if (connection !== null) {
      try {
        await connection.start();
        const response = await connection?.invoke('getCurrentReadings');
        data.currentReadings = response;
      } catch {
        setTimeout(connectInternal, 2000);
      }
    }
  }

  if (connection === null) {
    connection = new signalR.HubConnectionBuilder().withUrl('/hub/temperatures').build();

    connection.on('updateCurrentReadings', (currentReadings) => {
      data.currentReadings = currentReadings;
    });

    connection.onclose(connectInternal);
  }

  connectInternal();
}

onMounted(async () => {
  await connectToHub();
  await getTimeSeries();
});

watch(
  () => [data.graphRange.start, data.graphRange.end],
  () => getTimeSeries()
);

watch(
  () => [data.graphSeries, useFahrenheit.value],
  () => setGraphData(data.graphSeries)
);
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-4 mb-0">Temperatures</h1>
    <div class="row mt-4 px-2">
      <div
        v-for="(currentTemp, i) in data.currentReadings"
        :key="i"
        class="col-sm-6 col-md-4 mb-3 px-2"
      >
        <div class="card text-center">
          <div class="card-body">
            <div class="h4 mb-2">
              {{ currentTemp.location }}
            </div>
            <div class="h3">
              <span class="fw-bold"
                >{{ formatTemp(currentTemp.temperatureCelsius, useFahrenheit) }}{{ tempUnit }}</span
              >
              <span v-if="currentTemp.humidity !== null && showHumidity" class="ps-3"
                >{{ currentTemp.humidity }}%</span
              >
            </div>
            <div>
              <small class="fw-light">{{
                format(new Date(currentTemp.time as string), 'HH:mm')
              }}</small>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div class="row mt-4">
      <div class="col-md-6 mb-3">
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
      <div class="col-md-6 mb-3">
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
    <div class="chart-container mt-3">
      <canvas id="tempGraph"></canvas>
    </div>
    <table :class="{ 'mt-3': true, table: true, 'table-dark': useDarkMode }">
      <thead>
        <tr>
          <th>Location</th>
          <th>Min</th>
          <th>Max</th>
          <th>Avg</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(point, i) in data.graphSeries" :key="i">
          <td>{{ point.location }}</td>
          <td>{{ formatTemp(point.min, useFahrenheit) }}{{ tempUnit }}</td>
          <td>{{ formatTemp(point.max, useFahrenheit) }}{{ tempUnit }}</td>
          <td>{{ formatTemp(point.average, useFahrenheit) }}{{ tempUnit }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style lang="scss" scoped>
.chart-container {
  position: relative;
}

#tempGraph {
  height: 400px;
}
</style>
