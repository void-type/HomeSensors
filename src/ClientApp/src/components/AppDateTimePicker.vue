<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import 'chartjs-adapter-date-fns';
import { storeToRefs } from 'pinia';

const modelValue = defineModel<Date>();

const appStore = useAppStore();

const { useDarkMode } = storeToRefs(appStore);
</script>

<template>
  <v-date-picker
    v-model="modelValue"
    is-required
    mode="dateTime"
    :masks="{ inputDateTime24hr: 'YYYY-MM-DD HH:mm' }"
    :update-on-input="false"
    is24hr
    hide-time-header
    :is-dark="useDarkMode"
    color="primary"
    ><template #default="{ inputValue, inputEvents }">
      <input id="endDate" class="form-control" :value="inputValue" v-on="inputEvents" />
    </template>
  </v-date-picker>
</template>
