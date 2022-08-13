import { createRouter, createWebHistory } from 'vue-router';
import useAppStore from '@/stores/appStore';
import RouterHelpers from '@/models/RouterHelpers';

const router = createRouter({
  scrollBehavior: (to) => {
    if (to.hash) {
      document.getElementById(to.hash.slice(1))?.focus();
      return { el: to.hash };
    }

    document.getElementById('app-template')?.focus();
    return { left: 0, top: 0 };
  },
  history: createWebHistory(import.meta.env.BASE_URL),
  linkActiveClass: 'active',
  linkExactActiveClass: 'active',
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/pages/AppHome.vue'),
      meta: { title: 'Home' },
    },
    {
      path: '/inactive-devices',
      name: 'inactiveDevices',
      component: () => import('@/pages/InactiveDevices.vue'),
      meta: { title: 'Inactive devices' },
    },
    {
      path: '/lost-devices',
      name: 'lostDevices',
      component: () => import('@/pages/LostDevices.vue'),
      meta: { title: 'Lost devices' },
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: { name: 'home' },
      meta: { title: 'Home' },
    },
  ],
});

router.beforeEach((to, from, next) => {
  const appStore = useAppStore();
  appStore.clearMessages();
  next();
});

router.afterEach((to) => {
  RouterHelpers.setTitle(to);
});

export default router;
