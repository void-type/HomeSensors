<script lang="ts" setup></script>

<template>
  <ul class="navbar-nav">
    <li class="nav-item">
      <router-link :to="{ name: 'inactiveDevices' }" class="nav-link">Inactive devices</router-link>
    </li>
    <li class="nav-item">
      <router-link :to="{ name: 'lostDevices' }" class="nav-link">Lost devices</router-link>
    </li>
  </ul>
</template>

<style lang="scss" scoped></style>
