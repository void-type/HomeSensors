<script lang="ts" setup>
import logoSvg from '@/img/logo.svg';
import { storeToRefs } from 'pinia';
import { computed } from 'vue';
import useAppStore from '@/stores/appStore';
import type { HTMLInputEvent } from '@/models/HTMLInputEvent';

const appStore = useAppStore();
const { applicationName, user, useDarkMode, useFahrenheit, showHumidity } = storeToRefs(appStore);
const userRoles = computed(() => (user.value?.authorizedAs || []).join(', '));
</script>

<template>
  <header id="header" class="navbar navbar-expand-md navbar-dark bg-primary shadow">
    <nav class="container-xxl">
      <router-link :to="{ name: 'home' }" class="navbar-brand text-light">
        <img
          :src="logoSvg"
          alt="logo"
          class="d-inline-block align-text-top"
          width="24"
          height="24"
        />
        {{ applicationName }}
      </router-link>
      <button
        class="navbar-toggler"
        type="button"
        data-bs-toggle="collapse"
        data-bs-target="#navbar-menu"
        aria-controls="navbar-menu"
        aria-expanded="false"
        aria-label="Toggle navigation"
      >
        <span class="navbar-toggler-icon"></span>
      </button>
      <div id="navbar-menu" class="navbar-collapse collapse">
        <slot name="navItems"></slot>
        <ul class="navbar-nav ms-auto">
          <li class="nav-item dropdown">
            <a
              role="button"
              aria-haspopup="true"
              aria-expanded="false"
              href="#"
              class="nav-link dropdown-toggle"
              data-bs-toggle="dropdown"
              ><span>{{ user.login }}</span></a
            >
            <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="navbarDropdown">
              <li class="dropdown-item">Roles: {{ userRoles }}</li>
              <li class="dropdown-item">
                <div class="form-check form-switch" title="Use dark mode">
                  <label
                    class="form-check-label"
                    for="useDarkModeSetting"
                    title="Use dark mode"
                    aria-label="Use dark mode"
                    @click.stop
                    >🌙</label
                  >
                  <input
                    id="useDarkModeSetting"
                    :checked="useDarkMode"
                    class="form-check-input"
                    type="checkbox"
                    @change="
                      (e) => appStore.setDarkMode((e as HTMLInputEvent).target?.checked === true)
                    "
                  />
                </div>
              </li>
              <li class="dropdown-item">
                <div class="form-check form-switch" title="Use Fahrenheit">
                  <label
                    class="form-check-label"
                    for="useFahrenheitSetting"
                    title="Use Fahrenheit"
                    aria-label="Use Fahrenheit"
                    @click.stop
                    >🇺🇸</label
                  >
                  <input
                    id="useFahrenheitSetting"
                    :checked="useFahrenheit"
                    class="form-check-input"
                    type="checkbox"
                    @change="
                      (e) =>
                        appStore.setUseFahrenheit((e as HTMLInputEvent).target?.checked === true)
                    "
                  />
                </div>
              </li>
              <li class="dropdown-item">
                <div class="form-check form-switch" title="Show Humidity">
                  <label
                    class="form-check-label"
                    for="showHumiditySetting"
                    title="Show Humidity"
                    aria-label="Show Humidity"
                    @click.stop
                    >%</label
                  >
                  <input
                    id="showHumiditySetting"
                    :checked="showHumidity"
                    class="form-check-input"
                    type="checkbox"
                    @change="
                      (e) =>
                        appStore.setShowHumidity((e as HTMLInputEvent).target?.checked === true)
                    "
                  />
                </div>
              </li>
            </ul>
          </li>
        </ul>
      </div>
    </nav>
  </header>
</template>

<style lang="scss" scoped>
.form-check-label {
  width: 100%;
}
</style>
