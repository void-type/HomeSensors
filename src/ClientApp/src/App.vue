<script lang="ts" setup>
import { onMounted } from 'vue';
import { RouterView, useRoute } from 'vue-router';
import useAppStore from '@/stores/appStore';
import AppHeader from '@/components/AppHeader.vue';
import AppNav from '@/components/AppNav.vue';
import AppFooter from '@/components/AppFooter.vue';
import AppMessageCenter from '@/components/AppMessageCenter.vue';
import RouterHelpers from '@/models/RouterHelpers';
import AppModal from '@/components/AppModal.vue';
import ApiHelpers from '@/models/ApiHelpers';
import DarkModeHelpers from '@/models/DarkModeHelpers';
import UserSettingHelpers from '@/models/UserSettingHelpers';
import useMessageStore from '@/stores/messageStore';

const appStore = useAppStore();
const messageStore = useMessageStore();
const route = useRoute();
const api = ApiHelpers.client;

onMounted(() => {
  appStore.setDarkMode(DarkModeHelpers.getInitialDarkModeSetting());

  appStore.setUseFahrenheit(UserSettingHelpers.getInitialFahrenheitSetting());

  appStore.setShowHumidity(UserSettingHelpers.getInitialHumiditySetting());

  api()
    .appGetInfo()
    .then((response) => {
      appStore.setApplicationInfo(response.data);
      RouterHelpers.setTitle(route);

      if (response.data.antiforgeryToken) {
        ApiHelpers.setHeader(
          response.data.antiforgeryTokenHeaderName || 'X-Csrf-Token',
          response.data.antiforgeryToken
        );
      }
    })
    .catch((response) => messageStore.setApiFailureMessages(response));

  api()
    .appGetVersion()
    .then((response) => appStore.setVersionInfo(response.data));
});
</script>

<template>
  <div id="skip-nav" class="container-xxl visually-hidden-focusable">
    <router-link class="d-inline-flex p-2 m-1" :to="{ hash: '#main', query: route.query }"
      >Skip to main content</router-link
    >
  </div>
  <AppHeader class="d-print-none">
    <template #navItems>
      <AppNav />
    </template>
  </AppHeader>
  <AppMessageCenter class="d-print-none" />
  <main id="main" tabindex="-1">
    <RouterView />
  </main>
  <AppModal />
  <AppFooter class="mt-4" />
</template>

<style lang="scss" scoped></style>
