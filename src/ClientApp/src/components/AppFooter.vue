<script lang="ts" setup>
import { storeToRefs } from 'pinia';
import RouterHelpers from '@/models/RouterHelpers';
import useAppStore from '@/stores/appStore';

const appStore = useAppStore();
const { version } = storeToRefs(appStore);
</script>

<template>
  <footer id="footer" class="mt-5 d-print-none border-top text-center py-3 px-0">
    <div class="m-0 mb-4 text-center">
      <button class="btn btn-outline-secondary" type="button" @click="RouterHelpers.scrollToTop()">
        Back to top
        <FontAwesomeIcon icon="fa-arrow-up" class="ms-1" />
      </button>
    </div>
    <div>
      <a href="https://github.com/void-type/HomeSensors">
        HomeSensors {{ version }} is open source!</a>
    </div>
    <div>
      Logo from <a href="https://www.svgrepo.com/svg/230960/home-real-estate">svgrepo.com</a>
    </div>
  </footer>
</template>

<style lang="scss" scoped>
footer {
  padding: 1em 0rem;
  text-align: center;
  border-top: var(--bs-border-width) solid var(--bs-border-color);
}
</style>
