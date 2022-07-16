<script lang="ts" setup>
import { Api } from '@/api/Api';
import useAppStore from '@/stores/appStore';
import { onMounted, ref, type Ref } from 'vue';
import moment from 'moment';
import type { GraphViewModel } from '@/api/data-contracts';

const appStore = useAppStore();

const graphData: Ref<GraphViewModel> = ref({
  series: null,
  current: null,
});

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
      graphData.value = response.data;
    })
    .catch((response) => appStore.setApiFailureMessages(response));
});
</script>

<template>
  <div class="container-xxl">
    <div class="row">
      <div v-for="(currentTemp, i) in graphData.current" :key="i" class="card">
        <div class="card-body">
          <h5 class="card-title">
            {{ currentTemp.temperatureCelsius }}C {{ currentTemp.location }}
          </h5>
          <p>
            <small class="fw-light">{{ currentTemp.time }}</small>
          </p>
        </div>
      </div>

      {{ JSON.stringify(graphData) }}
    </div>
  </div>
</template>

<style lang="scss" scoped>
@import '@/styles/theme.scss';
@import 'bootstrap/scss/bootstrap';

.card {
  outline: $gray-500 1px solid;

  &:hover {
    background-color: $gray-200;
  }
}

.card-link {
  text-decoration: none;
  color: unset;

  & > img {
    max-height: 350px;
  }
}
</style>
