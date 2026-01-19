import { config as fontAwesomeConfig, library } from '@fortawesome/fontawesome-svg-core';
import {
  faBatteryQuarter,
  faBellSlash,
  faClock,
  faMoon,
  faSearch,
  faSnowflake,
  faTemperatureFull,
  faThumbtack,
} from '@fortawesome/free-solid-svg-icons';
import { createPinia } from 'pinia';
import { DatePicker, setupCalendar } from 'v-calendar';
import { createApp } from 'vue';
import router from '@/router';
import App from './App.vue';
import 'bootstrap';

// Prevents inline styling to appease CSP.
fontAwesomeConfig.autoAddCss = false;

library.add(
  faBatteryQuarter,
  faBellSlash,
  faClock,
  faMoon,
  faSearch,
  faSnowflake,
  faTemperatureFull,
  faThumbtack,
);

const app = createApp(App);

app
  .use(setupCalendar, {})
  .component('VDatePicker', DatePicker)
  .use(createPinia())
  .use(router)
  .mount('#app');
