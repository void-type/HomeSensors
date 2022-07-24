<script lang="ts" setup>
import { Api } from '@/api/Api';
import useAppStore from '@/stores/appStore';
import type { GraphPoint, GraphCurrentReading, GraphTimeSeries } from '@/api/data-contracts';
import { onMounted, reactive, watch, computed } from 'vue';
import moment from 'moment';
import { Chart, registerables, type ScriptableScaleContext, type TooltipItem } from 'chart.js';
import 'chartjs-adapter-moment';
import * as signalR from '@microsoft/signalr';
import type { HttpResponse } from '@/api/http-client';

Chart.register(...registerables);

const appStore = useAppStore();

const data = reactive({
  graphRange: {
    start: moment().add(-48, 'h').toDate(),
    end: moment().toDate(),
  },
  graphSeries: [] as Array<GraphTimeSeries>,
  currentReadings: [] as Array<GraphCurrentReading>,
  useFahrenheit: true,
});

let lineChart: Chart | null = null;

const tempUnit = computed(() => (data.useFahrenheit ? 'F' : 'C'));

function getRandomColor() {
  const letters = '0123456789ABCDEF'.split('');
  let color = '#';
  for (let i = 0; i < 6; i += 1) {
    color += letters[Math.floor(Math.random() * 16)];
  }
  return color;
}

const colors = [
  '#2ac4b3',
  '#b2df8a',
  '#ff526f',
  '#73a2ef',
  '#3064cf',
  '#b180d0',
  '#feaf29',
  '#8aaec7',
  '#fe7db7',
  '#33a02c',
  '#0097fb',
  '#914bdc',
  '#915535',
  '#5d5652',
];

function formatTemp(temp: number | null | undefined, decimals = 1) {
  const convertedTemp = data.useFahrenheit ? (temp || 0) * 1.8 + 32 : temp || 0;
  return Math.round(convertedTemp * 10 ** decimals) / 10 ** decimals;
}

function setGraphData(series: Array<GraphTimeSeries>) {
  const element = document.getElementById('tempGraph') as HTMLCanvasElement;

  if (element === null) {
    return;
  }

  lineChart?.destroy();

  const datasets = series?.map((s, si) => ({
    label: s.location,
    borderColor: colors[si] || getRandomColor(),
    data: s.points
      ?.filter((p: GraphPoint) => p.temperatureCelsius)
      .map((p: GraphPoint) => ({ x: p.time, y: formatTemp(p.temperatureCelsius) })),
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
          time: {},
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
    startTime: moment(data.graphRange.start).toISOString(),
    endTime: moment(data.graphRange.end).toISOString(),
  };

  try {
    const response = await new Api().temperaturesTimeSeriesCreate(parameters);
    data.graphSeries = response.data;
    setGraphData(data.graphSeries);
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
  () => [data.graphSeries, data.useFahrenheit],
  () => setGraphData(data.graphSeries)
);
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-4 mb-0">Temps</h1>
    <div class="form-check form-switch mt-4">
      <label class="form-check-label" for="useFahrenheit">Use fahrenheit</label>
      <input
        id="useFahrenheit"
        v-model="data.useFahrenheit"
        class="form-check-input"
        type="checkbox"
      />
    </div>
    <div class="row mt-4">
      <div v-for="(currentTemp, i) in data.currentReadings" :key="i" class="card col-sm-6 col-md-4">
        <div class="card-body">
          <h5 class="card-title">
            {{ formatTemp(currentTemp.temperatureCelsius) }}{{ tempUnit }}
            {{ currentTemp.location }}
          </h5>
          <p>
            <small class="fw-light">{{ moment(currentTemp.time).format('ll HH:mm') }}</small>
          </p>
        </div>
      </div>
    </div>
    <div class="row mt-4">
      <div class="col-md-6 mb-3">
        <label for="startDate" class="form-label">Start date</label>
        <v-date-picker v-model="data.graphRange.start" mode="dateTime" is24hr
          ><template #default="{ inputValue, inputEvents }">
            <input id="startDate" class="form-control" :value="inputValue" v-on="inputEvents" />
          </template>
        </v-date-picker>
      </div>
      <div class="col-md-6 mb-3">
        <label for="endDate" class="form-label">End date</label>
        <v-date-picker v-model="data.graphRange.end" mode="dateTime" is24hr
          ><template #default="{ inputValue, inputEvents }">
            <input id="endDate" class="form-control" :value="inputValue" v-on="inputEvents" />
          </template>
        </v-date-picker>
      </div>
    </div>
    <div class="chart-container mt-3">
      <canvas id="tempGraph"></canvas>
    </div>
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
