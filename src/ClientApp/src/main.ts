import { createApp } from 'vue';
import { createPinia } from 'pinia';
import 'bootstrap';
import router from '@/router';
import { library, config as fontAwesomeConfig } from '@fortawesome/fontawesome-svg-core';
import {
  faMoon,
  faThumbtack,
  faClock,
  faBatteryQuarter,
  faTemperatureFull,
  faSnowflake,
} from '@fortawesome/free-solid-svg-icons';
import { setupCalendar, DatePicker } from 'v-calendar';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
import App from './App.vue';

// Prevents inline styling to appease CSP.
fontAwesomeConfig.autoAddCss = false;

library.add(faMoon, faThumbtack, faClock, faBatteryQuarter, faTemperatureFull, faSnowflake);

const app = createApp(App);

app
  .use(setupCalendar, {})
  .component('VDatePicker', DatePicker)
  .use(createPinia())
  .use(router)
  .mount('#app');
