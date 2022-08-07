<script lang="ts" setup>
import { storeToRefs } from 'pinia';
import useAppStore from '@/stores/appStore';

const appStore = useAppStore();
const { version } = storeToRefs(appStore);
</script>

<template>
  <footer class="d-print-none">
    <div>
      <a href="https://github.com/void-type/HomeSensors">
        HomeSensors {{ version }} is open source!</a
      >
    </div>
    <div>
      Logo from <a href="https://www.svgrepo.com/svg/230960/home-real-estate">svgrepo.com</a>
    </div>
  </footer>
</template>

<style lang="scss" scoped>
footer {
  border-top: 1px solid;
  padding: 1em 0rem;
  text-align: center;
}
</style>
