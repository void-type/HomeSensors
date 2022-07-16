<script lang="ts" setup>
import { Api } from '@/api/Api';
import useAppStore from '@/stores/appStore';
import { onMounted, reactive, watch, computed } from 'vue';
import moment from 'moment';
import type { GraphCurrentReading, GraphTimeSeries } from '@/api/data-contracts';
import { Chart, registerables, type ScriptableScaleContext, type TooltipItem } from 'chart.js';
import 'chartjs-adapter-moment';
import * as signalR from '@microsoft/signalr';

Chart.register(...registerables);

const appStore = useAppStore();

const data = reactive({
  startTime: moment().add(-2, 'd').toDate().toISOString(),
  endTime: moment().toISOString(),
  intervalMinutes: 15,
  series: [] as Array<GraphTimeSeries>,
  current: [] as Array<GraphCurrentReading>,
  useFahrenheit: true,

  counter: 0,
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
    appStore.setErrorMessage('No canvas found on page for graph.');
    return;
  }

  lineChart?.destroy();

  const datasets = series?.map((s, si) => ({
    label: s.location,
    borderColor: colors[si] || getRandomColor(),
    data: s.points
      ?.filter((p) => p.temperatureCelsius)
      .map((p) => ({ x: p.time, y: formatTemp(p.temperatureCelsius) })),
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

function getTimeSeries() {
  const parameters = {
    startTime: data.startTime,
    endTime: data.endTime,
    intervalMinutes: data.intervalMinutes,
  };

  new Api()
    .temperaturesTimeSeriesCreate(parameters)
    .then((response) => {
      data.series = response.data;
      setGraphData(data.series);
    })
    .catch((response) => appStore.setApiFailureMessages(response));
}

onMounted(() => {
  getTimeSeries();

  new Api()
    .temperaturesCurrentCreate()
    .then((response) => {
      data.current = response.data;
      setGraphData(data.current);
    })
    .catch((response) => appStore.setApiFailureMessages(response));

  const connection = new signalR.HubConnectionBuilder().withUrl('/hub/temperatures').build();

  connection.on('updateCurrentReadings', (currentReadings) => {
    data.counter += 1;
    data.current = currentReadings;
  });

  function connect() {
    connection.start().catch((error) => {
      appStore.setErrorMessage(error);
      setTimeout(connect, 2000);
    });
  }

  connection.onclose(connect);

  connect();
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
    <div class="form-check form-switch mt-2">
      <label class="form-check-label" for="useFahrenheit">Use fahrenheit</label>
      <input
        id="useFahrenheit"
        v-model="data.useFahrenheit"
        class="form-check-input"
        type="checkbox"
      />
    </div>
    <div>
      <canvas id="tempGraph"></canvas>
    </div>
    <p>Updates from server: {{ data.counter }}</p>
    <div class="row">
      <div v-for="(currentTemp, i) in data.current" :key="i" class="card col-4">
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
  </div>
</template>

<style lang="scss" scoped></style>
