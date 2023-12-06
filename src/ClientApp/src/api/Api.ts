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
  CheckLimitResult,
  ClientStatus,
  CreateLocationRequest,
  Device,
  GraphTimeSeries,
  GraphTimeSeriesRequest,
  IFailureIItemSet,
  Int64EntityMessage,
  Location,
  Reading,
  SetupRequest,
  TemperaturesLocationsCheckLimitsCreateParams,
  UpdateDeviceRequest,
  UpdateLocationRequest,
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
   * @tags Devices
   * @name TemperaturesDevicesAllCreate
   * @request POST:/api/temperatures/devices/all
   * @response `200` `(Device)[]` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesDevicesAllCreate = (params: RequestParams = {}) =>
    this.request<Device[], IFailureIItemSet>({
      path: `/api/temperatures/devices/all`,
      method: 'POST',
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags Devices
   * @name TemperaturesDevicesUpdateCreate
   * @request POST:/api/temperatures/devices/update
   * @response `200` `Int64EntityMessage` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesDevicesUpdateCreate = (data: UpdateDeviceRequest, params: RequestParams = {}) =>
    this.request<Int64EntityMessage, IFailureIItemSet>({
      path: `/api/temperatures/devices/update`,
      method: 'POST',
      body: data,
      type: ContentType.Json,
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags Locations
   * @name TemperaturesLocationsAllCreate
   * @request POST:/api/temperatures/locations/all
   * @response `200` `(Location)[]` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesLocationsAllCreate = (params: RequestParams = {}) =>
    this.request<Location[], IFailureIItemSet>({
      path: `/api/temperatures/locations/all`,
      method: 'POST',
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags Locations
   * @name TemperaturesLocationsCheckLimitsCreate
   * @request POST:/api/temperatures/locations/check-limits
   * @response `200` `(CheckLimitResult)[]` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesLocationsCheckLimitsCreate = (
    query: TemperaturesLocationsCheckLimitsCreateParams,
    params: RequestParams = {}
  ) =>
    this.request<CheckLimitResult[], IFailureIItemSet>({
      path: `/api/temperatures/locations/check-limits`,
      method: 'POST',
      query: query,
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags Locations
   * @name TemperaturesLocationsCreateCreate
   * @request POST:/api/temperatures/locations/create
   * @response `200` `Int64EntityMessage` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesLocationsCreateCreate = (data: CreateLocationRequest, params: RequestParams = {}) =>
    this.request<Int64EntityMessage, IFailureIItemSet>({
      path: `/api/temperatures/locations/create`,
      method: 'POST',
      body: data,
      type: ContentType.Json,
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags Locations
   * @name TemperaturesLocationsUpdateCreate
   * @request POST:/api/temperatures/locations/update
   * @response `200` `Int64EntityMessage` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesLocationsUpdateCreate = (data: UpdateLocationRequest, params: RequestParams = {}) =>
    this.request<Int64EntityMessage, IFailureIItemSet>({
      path: `/api/temperatures/locations/update`,
      method: 'POST',
      body: data,
      type: ContentType.Json,
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags MqttFeedDiscovery
   * @name TemperaturesMqttFeedDiscoveryStatusList
   * @request GET:/api/temperatures/mqtt-feed-discovery/status
   * @response `200` `ClientStatus` Success
   */
  temperaturesMqttFeedDiscoveryStatusList = (params: RequestParams = {}) =>
    this.request<ClientStatus, any>({
      path: `/api/temperatures/mqtt-feed-discovery/status`,
      method: 'GET',
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags MqttFeedDiscovery
   * @name TemperaturesMqttFeedDiscoverySetupCreate
   * @request POST:/api/temperatures/mqtt-feed-discovery/setup
   * @response `200` `ClientStatus` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesMqttFeedDiscoverySetupCreate = (data: SetupRequest, params: RequestParams = {}) =>
    this.request<ClientStatus, IFailureIItemSet>({
      path: `/api/temperatures/mqtt-feed-discovery/setup`,
      method: 'POST',
      body: data,
      type: ContentType.Json,
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags MqttFeedDiscovery
   * @name TemperaturesMqttFeedDiscoveryTeardownCreate
   * @request POST:/api/temperatures/mqtt-feed-discovery/teardown
   * @response `200` `ClientStatus` Success
   */
  temperaturesMqttFeedDiscoveryTeardownCreate = (params: RequestParams = {}) =>
    this.request<ClientStatus, any>({
      path: `/api/temperatures/mqtt-feed-discovery/teardown`,
      method: 'POST',
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags Readings
   * @name TemperaturesReadingsCurrentCreate
   * @request POST:/api/temperatures/readings/current
   * @response `200` `(Reading)[]` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesReadingsCurrentCreate = (params: RequestParams = {}) =>
    this.request<Reading[], IFailureIItemSet>({
      path: `/api/temperatures/readings/current`,
      method: 'POST',
      format: 'json',
      ...params,
    });
  /**
   * No description
   *
   * @tags Readings
   * @name TemperaturesReadingsTimeSeriesCreate
   * @request POST:/api/temperatures/readings/time-series
   * @response `200` `(GraphTimeSeries)[]` Success
   * @response `400` `IFailureIItemSet` Bad Request
   */
  temperaturesReadingsTimeSeriesCreate = (
    data: GraphTimeSeriesRequest,
    params: RequestParams = {}
  ) =>
    this.request<GraphTimeSeries[], IFailureIItemSet>({
      path: `/api/temperatures/readings/time-series`,
      method: 'POST',
      body: data,
      type: ContentType.Json,
      format: 'json',
      ...params,
    });
}
