<script lang="ts" setup>
import { Api } from '@/api/Api';
import useAppStore from '@/stores/appStore';
import type { GraphPoint } from '@/api/data-contracts';
import { onMounted, reactive, watch, computed } from 'vue';
import moment from 'moment';
import type { GraphCurrentReading, GraphTimeSeries } from '@/api/data-contracts';
import { Chart, registerables, type ScriptableScaleContext, type TooltipItem } from 'chart.js';
import 'chartjs-adapter-moment';
import * as signalR from '@microsoft/signalr';
import type { HttpResponse } from '@/api/http-client';

Chart.register(...registerables);

const appStore = useAppStore();

const data = reactive({
  startTime: moment().add(-48, 'h').toDate().toISOString(),
  endTime: moment().toISOString(),
  intervalMinutes: 15,
  series: [] as Array<GraphTimeSeries>,
  current: [] as Array<GraphCurrentReading>,
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
  '#feaf29',
  '#ff617b',
  '#73a2ef',
  '#b180d0',
  '#3064cf',
  '#d0a45f',
  '#8aaec7',
  '#ef65a2',
  '#8aaec7',
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
    startTime: data.startTime,
    endTime: data.endTime,
    intervalMinutes: data.intervalMinutes,
  };

  try {
    const response = await new Api().temperaturesTimeSeriesCreate(parameters);
    data.series = response.data;
    setGraphData(data.series);
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
        data.current = response;
      } catch {
        setTimeout(connectInternal, 2000);
      }
    }
  }

  if (connection === null) {
    connection = new signalR.HubConnectionBuilder().withUrl('/hub/temperatures').build();

    connection.on('updateCurrentReadings', (currentReadings) => {
      data.current = currentReadings;
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
  () => [data.startTime, data.endTime, data.intervalMinutes],
  () => getTimeSeries()
);

watch(
  () => [data.series, data.useFahrenheit],
  () => setGraphData(data.series)
);
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-4 mb-0">Temps</h1>
    <div class="form-check form-switch">
      <label class="form-check-label" for="useFahrenheit">Use fahrenheit</label>
      <input
        id="useFahrenheit"
        v-model="data.useFahrenheit"
        class="form-check-input"
        type="checkbox"
      />
    </div>
    <div class="row mt-3">
      <div v-for="(currentTemp, i) in data.current" :key="i" class="card col-sm-6 col-md-4">
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
    <div class="mt-3">
      <canvas id="tempGraph"></canvas>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
