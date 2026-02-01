<script lang="ts" setup>
import type { HTMLInputEvent } from '@/models/HTMLInputEvent';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { storeToRefs } from 'pinia';
import logoSvg from '@/img/logo.svg';
import useAppStore from '@/stores/appStore';

const appStore = useAppStore();
const { applicationName, user, useDarkMode, useFahrenheit, showHumidity } = storeToRefs(appStore);
</script>

<template>
  <header id="header" class="navbar navbar-expand-md navbar-dark bg-primary d-print-none">
    <nav class="container-xxl">
      <router-link :to="{ name: 'home' }" class="navbar-brand">
        <img
          :src="logoSvg"
          alt="logo"
          class="d-inline-block align-text-top"
          width="24"
          height="24"
        >
        {{ applicationName }}
      </router-link>
      <button
        type="button"
        class="navbar-toggler"
        data-bs-toggle="collapse"
        data-bs-target="#navbar-menu"
        aria-controls="navbar-menu"
        aria-expanded="false"
        aria-label="Toggle navigation"
      >
        <span class="navbar-toggler-icon" />
      </button>
      <div id="navbar-menu" class="navbar-collapse collapse">
        <slot name="navItems" />
        <ul class="navbar-nav ms-auto">
          <li class="nav-item dropdown">
            <a
              role="button"
              aria-haspopup="true"
              aria-expanded="false"
              href="#"
              class="nav-link dropdown-toggle"
              data-bs-toggle="dropdown"
            ><span>{{ user.login }}</span></a>
            <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="navbarDropdown">
              <!-- <li class="dropdown-item-text fw-bold">Roles</li>
              <li v-for="role in user.authorizedAs" :key="role" class="dropdown-item-text">
                {{ role }}
              </li>
              <li v-if="(user.authorizedAs?.length || 0) < 1" class="dropdown-item-text text-muted">
                No roles.
              </li>
              <li><hr class="dropdown-divider" /></li> -->
              <li class="dropdown-item">
                <div class="form-check form-switch">
                  <label class="form-check-label" for="useDarkMode" aria-label="Use dark mode"><FontAwesomeIcon class="me-2" icon="fa-moon" />Dark mode</label>
                  <input
                    id="useDarkMode"
                    :checked="useDarkMode"
                    class="form-check-input"
                    type="checkbox"
                    @change="
                      (e) => appStore.setDarkMode((e as HTMLInputEvent).target?.checked === true)
                    "
                  >
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
                  ><span class="me-2">🇺🇸</span> Imperial</label>
                  <input
                    id="useFahrenheitSetting"
                    :checked="useFahrenheit"
                    class="form-check-input"
                    type="checkbox"
                    @change="
                      (e) =>
                        appStore.setUseFahrenheit((e as HTMLInputEvent).target?.checked === true)
                    "
                  >
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
                  ><span class="me-2">%</span>Show humidity</label>
                  <input
                    id="showHumiditySetting"
                    :checked="showHumidity"
                    class="form-check-input"
                    type="checkbox"
                    @change="
                      (e) =>
                        appStore.setShowHumidity((e as HTMLInputEvent).target?.checked === true)
                    "
                  >
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
