<script lang="ts" setup>
import { storeToRefs } from 'pinia';
import DateHelpers from '@/models/DateHelpers';
import useAppStore from '@/stores/appStore';
import 'chartjs-adapter-date-fns';

const props = defineProps({
  disabled: {
    type: Boolean,
    required: false,
    default: false,
  },
  id: {
    type: String,
    required: true,
  },
});

const modelValue = defineModel<Date>();

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
  >
    <template #default="{ inputValue, inputEvents }">
      <input :id="props.id" class="form-control" :value="inputValue" v-on="inputEvents">
    </template>
  </v-date-picker>
</template>

<style lang="scss" scoped>
.disabled {
  background-color: var(--bs-secondary-bg);
  opacity: 1;
}
</style>
