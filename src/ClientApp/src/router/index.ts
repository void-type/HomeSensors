import { Collapse } from 'bootstrap';
import { createRouter, createWebHistory } from 'vue-router';
import RouterHelpers from '@/models/RouterHelpers';
import useMessageStore from '@/stores/messageStore';

const router = createRouter({
  scrollBehavior(to, from, savedPosition) {
    if (to.hash) {
      document.getElementById(to.hash.slice(1))?.focus();
      return {
        el: to.hash,
      };
    }

    if (savedPosition) {
      return savedPosition;
    }

    if (from.path === to.path) {
      return undefined;
    }

    document.getElementById('app')?.focus();

    return {
      el: '#app',
    };
  },
  history: createWebHistory(import.meta.env.BASE_URL),
  linkActiveClass: 'active',
  linkExactActiveClass: 'active',
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/pages/CurrentTemperaturesPage.vue'),
      meta: { title: 'Home' },
    },
    {
      path: '/time-series',
      name: 'timeSeries',
      component: () => import('@/pages/TimeSeriesPage.vue'),
      props: (route) => {
        return {
          startDate: route.query.start ? new Date(route.query.start as string) : undefined,
          endDate: route.query.end ? new Date(route.query.end as string) : undefined,
          showHumidity: route.query.humidity === 'true',
          locationIds: route.query.locationIds,
          hideHvacActions: route.query.hideHvacActions === 'true',
        };
      },
      meta: { title: 'Time Series' },
    },
    {
      path: '/devices',
      name: 'devicesMain',
      component: () => import('@/pages/DevicesPage.vue'),
      meta: { title: 'Devices' },
    },
    {
      path: '/locations',
      name: 'locationsMain',
      component: () => import('@/pages/LocationsPage.vue'),
      meta: { title: 'Locations' },
    },
    {
      path: '/categories',
      name: 'categoriesMain',
      component: () => import('@/pages/CategoriesPage.vue'),
      meta: { title: 'Categories' },
    },
    {
      path: '/mqtt-discovery',
      name: 'mqttDiscovery',
      component: () => import('@/pages/MqttDiscoveryPage.vue'),
      props: (route) => {
        const topics = route.query.topic;

        if (!topics) {
          return {};
        }

        return {
          topics: Array.isArray(topics) ? topics : [topics],
        };
      },
      meta: { title: 'MQTT Discovery' },
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: { name: 'home' },
      meta: { title: 'Home' },
    },
  ],
});

router.beforeEach((to, from, next) => {
  const messageStore = useMessageStore();
  messageStore.clearMessages();
  next();
});

router.afterEach((to) => {
  Collapse.getOrCreateInstance('#navbar-menu', { toggle: false }).hide();

  RouterHelpers.setTitle(to);
});

export default router;
