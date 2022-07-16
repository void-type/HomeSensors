<script lang="ts" setup>
import { Api } from '@/api/Api';
import useAppStore from '@/stores/appStore';
import { onMounted, reactive, watch, computed } from 'vue';
import moment from 'moment';
import type { GraphViewModel } from '@/api/data-contracts';
import { Chart, registerables, type ScriptableScaleContext, type TooltipItem } from 'chart.js';
import 'chartjs-adapter-moment';

Chart.register(...registerables);

const appStore = useAppStore();

const data = reactive({
  graphData: {
    series: null,
    current: null,
  } as GraphViewModel,
  useFahrenheit: false,
});

let graphChart: Chart | null = null;

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

function setGraphData(model: GraphViewModel) {
  const element = document.getElementById('tempGraph') as HTMLCanvasElement;

  if (element === null) {
    appStore.setErrorMessage('No canvas found on page for graph.');
    return;
  }

  graphChart?.destroy();

  const datasets = model.series?.map((s, si) => ({
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
  graphChart = new Chart(element, config as any);
}

onMounted(() => {
  const startTime = moment().add(-2, 'd').toDate().toISOString();
  const endTime = moment().toISOString();

  new Api()
    .tempsGraphCreate({
      startTime,
      endTime,
      intervalMinutes: 15,
    })
    .then((response) => {
      data.graphData = response.data;
      setGraphData(data.graphData);
    })
    .catch((response) => appStore.setApiFailureMessages(response));
});

watch(
  () => [data.graphData, data.useFahrenheit],
  () => setGraphData(data.graphData)
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
    <div class="row">
      <div v-for="(currentTemp, i) in data.graphData.current" :key="i" class="card col-4">
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
