import { createApp } from 'vue';
import { createPinia } from 'pinia';
import 'bootstrap';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
import App from '@/App.vue';
import router from '@/router';
import { library } from '@fortawesome/fontawesome-svg-core';
import { faTriangleExclamation } from '@fortawesome/free-solid-svg-icons';
import SetupCalendar, { DatePicker } from 'v-calendar';

library.add(faTriangleExclamation);

const app = createApp(App);

app
  .use(SetupCalendar, {})
  .component('VDatePicker', DatePicker)
  .use(createPinia())
  .use(router)
  .mount('#app');
