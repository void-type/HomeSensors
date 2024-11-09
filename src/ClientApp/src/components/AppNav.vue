<script lang="ts" setup></script>

<template>
  <ul class="navbar-nav">
    <li class="nav-item">
      <router-link :to="{ name: 'home' }" class="nav-link">Home</router-link>
    </li>
    <li class="nav-item">
      <router-link :to="{ name: 'timeSeries' }" class="nav-link">Time Series</router-link>
    </li>
    <li class="nav-item">
      <router-link :to="{ name: 'discoveryMain' }" class="nav-link">Discovery</router-link>
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
        Administration
      </a>
      <ul class="dropdown-menu" aria-labelledby="administrationDropdown">
        <li class="nav-item">
          <router-link :to="{ name: 'devicesMain' }" class="nav-link">Devices</router-link>
        </li>
        <li class="nav-item">
          <router-link :to="{ name: 'locationsMain' }" class="nav-link">Locations</router-link>
        </li>
        <li class="nav-item">
          <router-link :to="{ name: 'categoriesMain' }" class="nav-link">Categories</router-link>
        </li>
      </ul>
    </li>
  </ul>
</template>

<style lang="scss" scoped></style>
