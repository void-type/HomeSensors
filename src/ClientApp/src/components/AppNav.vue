<script lang="ts" setup></script>

<template>
  <ul class="navbar-nav">
    <li class="nav-item">
      <router-link :to="{ name: 'home' }" class="nav-link">
        Home
      </router-link>
    </li>
    <li class="nav-item">
      <router-link :to="{ name: 'timeSeries' }" class="nav-link">
        Time Series
      </router-link>
    </li>
    <li class="nav-item">
      <router-link :to="{ name: 'mqttDiscovery' }" class="nav-link">
        MQTT
      </router-link>
    </li>
    <li class="nav-item dropdown">
      <a
        id="administrationDropdown"
        class="nav-link dropdown-toggle"
        href="#"
        role="button"
        data-bs-toggle="dropdown"
        aria-expanded="false"
      >
        Admin
      </a>
      <ul class="dropdown-menu" aria-labelledby="administrationDropdown">
        <li>
          <router-link :to="{ name: 'devicesMain' }" class="dropdown-item">
            Devices
          </router-link>
        </li>
        <li><hr class="dropdown-divider"></li>
        <li>
          <router-link :to="{ name: 'locationsMain' }" class="dropdown-item">
            Locations
          </router-link>
        </li>
        <li><hr class="dropdown-divider"></li>
        <li>
          <router-link :to="{ name: 'categoriesMain' }" class="dropdown-item">
            Categories
          </router-link>
        </li>
      </ul>
    </li>
  </ul>
</template>

<style lang="scss" scoped></style>
