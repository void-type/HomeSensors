<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import DateHelpers from '@/models/DateHelpers';
import 'chartjs-adapter-date-fns';
import { storeToRefs } from 'pinia';

const modelValue = defineModel<Date>();

const props = defineProps({
  disabled: {
    type: Boolean,
    required: false,
    default: false,
  },
});

const appStore = useAppStore();

const { useDarkMode } = storeToRefs(appStore);
</script>

<template>
  <div v-if="props.disabled" class="form-control disabled" type="text" disabled="true">
    {{ DateHelpers.dateTimeShortForView(modelValue) }}
  </div>
  <v-date-picker
    v-else
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

<style lang="scss" scoped>
.disabled {
  background-color: var(--bs-secondary-bg);
  opacity: 1;
}
</style>
