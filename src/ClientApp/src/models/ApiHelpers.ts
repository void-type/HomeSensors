import type { ApiConfig } from '../api/http-client';
import { Api } from '../api/Api';

const defaultHeaders: Record<string, string> = {};

export default class ApiHelpers {
  static setHeader(headerName: string, headerValue: string) {
    defaultHeaders[headerName] = headerValue;
  }

  static client(apiConfig?: ApiConfig<unknown>) {
    if (apiConfig) {
      return new Api(apiConfig);
    }

    return new Api({
      baseApiParams: {
        headers: defaultHeaders,
      },
    });
  }

  static imageUrl(id: number | string) {
    return `/api/images/${id}`;
  }

  static getRetryMilliseconds(elapsedMilliseconds: number) {
    // Within the first minute, wait between 0 and 10 seconds.
    if (elapsedMilliseconds < 60000) {
      return Math.random() * 10000;
    }

    // Within the first hour, try between 0 and 5 minutes.
    if (elapsedMilliseconds < 3600000) {
      return Math.random() * 5 * 60000;
    }

    // Within the first day, try every 30 minutes with an offset of 0 to 3 minutes.
    if (elapsedMilliseconds < 86400000) {
      return (Math.random() * 3 + 30) * 60000;
    }

    // After a day, stop trying to reconnect.
    return null;
  }
}
