import type { RouteLocationNormalizedLoaded } from 'vue-router';
import useAppStore from '@/stores/appStore';
import { isNil } from './FormatHelper';

export default class RouterHelper {
  static setTitle(
    route: RouteLocationNormalizedLoaded,
    additionalTitle: string | null | undefined = null,
  ) {
    const appStore = useAppStore();
    const title = [additionalTitle, `${route.meta.title}`, appStore.applicationName]
      .filter(x => !isNil(x))
      .join(' | ');

    document.title = title;
  }

  static scrollToTop(behavior: ScrollBehavior = 'smooth') {
    document.getElementById('main')?.focus();

    window.scrollTo({
      top: 0,
      behavior,
    });
  }
}
