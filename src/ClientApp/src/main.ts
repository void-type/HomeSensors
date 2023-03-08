import { createApp } from 'vue';
import { createPinia } from 'pinia';
import 'bootstrap';
import router from '@/router';
import { library } from '@fortawesome/fontawesome-svg-core';
import { faClock, faSnowflake, faTemperatureFull } from '@fortawesome/free-solid-svg-icons';
import SetupCalendar, { DatePicker } from 'v-calendar';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
import App from './App.vue';

library.add(faTemperatureFull);
library.add(faSnowflake);
library.add(faClock);

const app = createApp(App);

app
  .use(SetupCalendar, {})
  .component('VDatePicker', DatePicker)
  .use(createPinia())
  .use(router)
  .mount('#app');
