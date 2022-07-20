/* eslint-disable */
/* tslint:disable */
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

import type {
  AppVersion,
  GraphCurrentReading,
  GraphTimeSeries,
  GraphTimeSeriesRequest,
  IFailureIItemSet,
  WebClientInfo,
} from './data-contracts';
import { ContentType, HttpClient, type RequestParams } from './http-client';

export class Api<SecurityDataType = unknown> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Application
   * @name AppInfoList
   * @summary Get information to bootstrap the SPA client like application name and user data.
   * @request GET:/api/app/info
   * @response `200` `WebClientInfo` Success
   */
  appInfoList = (params: RequestParams = {}) =>
    this.request<WebClientInfo, any>({
      path: `/api/app/info`,
      method: 'GET',
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags Application
   * @name AppVersionList
   * @summary Get the version of the application.
   * @request GET:/api/app/version
   * @response `200` `AppVersion` Success
   */
  appVersionList = (params: RequestParams = {}) =>
    this.request<AppVersion, any>({
      path: `/api/app/version`,
      method: 'GET',
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureApi
   * @name TemperaturesTimeSeriesCreate
   * @request POST:/api/temperatures/time-series
   * @response `200` `(GraphTimeSeries)[]` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesTimeSeriesCreate = (data: GraphTimeSeriesRequest, params: RequestParams = {}) =>
    this.request<GraphTimeSeries[], IFailureIItemSet>({
      path: `/api/temperatures/time-series`,
      method: 'POST',
      body: data,
      type: ContentType.Json,
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureApi
   * @name TemperaturesCurrentCreate
   * @request POST:/api/temperatures/current
   * @response `200` `(GraphCurrentReading)[]` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesCurrentCreate = (params: RequestParams = {}) =>
    this.request<GraphCurrentReading[], IFailureIItemSet>({
      path: `/api/temperatures/current`,
      method: 'POST',
      format: 'json',
      ...params,
    });
}
