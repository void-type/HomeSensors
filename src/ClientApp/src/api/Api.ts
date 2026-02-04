/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

import type {AppVersion,
  CategoriesDeleteParams,
  CategoryResponse,
  CategorySaveRequest,
  EntityMessageOfLong,
  IItemSetOfIFailure,
  TemperatureCheckLimitResponse,
  TemperatureDeviceResponse,
  TemperatureDeviceSaveRequest,
  TemperatureDevicesDeleteParams,
  TemperatureLocationResponse,
  TemperatureLocationSaveRequest,
  TemperatureLocationsCheckLimitsParams,
  TemperatureLocationsDeleteParams,
  TemperatureReadingResponse,
  TemperatureReadingsGetCurrentReadingForLocationParams,
  TemperatureTimeSeriesRequest,
  TemperatureTimeSeriesResponse,
  WebClientInfo,} from "./data-contracts";
import { ContentType, HttpClient } from "./http-client";
import type { RequestParams } from "./http-client";

export class Api<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags App
   * @name AppGetInfo
   * @summary Get information to bootstrap the SPA client like application name and user data.
   * @request GET:/api/app/info
   * @response `200` `WebClientInfo`
   */
  appGetInfo = (params: RequestParams = {}) =>
    this.request<WebClientInfo, any>({
      path: `/api/app/info`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags App
   * @name AppGetVersion
   * @summary Get the version of the application.
   * @request GET:/api/app/version
   * @response `200` `AppVersion`
   */
  appGetVersion = (params: RequestParams = {}) =>
    this.request<AppVersion, any>({
      path: `/api/app/version`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Categories
   * @name CategoriesGetAll
   * @request GET:/api/categories/all
   * @response `200` `(CategoryResponse)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  categoriesGetAll = (params: RequestParams = {}) =>
    this.request<CategoryResponse[], IItemSetOfIFailure>({
      path: `/api/categories/all`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Categories
   * @name CategoriesSave
   * @request POST:/api/categories
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  categoriesSave = (data: CategorySaveRequest, params: RequestParams = {}) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/categories`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Categories
   * @name CategoriesDelete
   * @request DELETE:/api/categories/{id}
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  categoriesDelete = (
    { id, ...query }: CategoriesDeleteParams,
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/categories/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureDevices
   * @name TemperatureDevicesGetAll
   * @request GET:/api/temperature-devices/all
   * @response `200` `(TemperatureDeviceResponse)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureDevicesGetAll = (params: RequestParams = {}) =>
    this.request<TemperatureDeviceResponse[], IItemSetOfIFailure>({
      path: `/api/temperature-devices/all`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureDevices
   * @name TemperatureDevicesSave
   * @request POST:/api/temperature-devices
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureDevicesSave = (
    data: TemperatureDeviceSaveRequest,
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/temperature-devices`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureDevices
   * @name TemperatureDevicesDelete
   * @request DELETE:/api/temperature-devices/{id}
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureDevicesDelete = (
    { id, ...query }: TemperatureDevicesDeleteParams,
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/temperature-devices/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureLocations
   * @name TemperatureLocationsGetAll
   * @request GET:/api/temperature-locations/all
   * @response `200` `(TemperatureLocationResponse)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureLocationsGetAll = (params: RequestParams = {}) =>
    this.request<TemperatureLocationResponse[], IItemSetOfIFailure>({
      path: `/api/temperature-locations/all`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureLocations
   * @name TemperatureLocationsCheckLimits
   * @request GET:/api/temperature-locations/check-limits
   * @response `200` `(TemperatureCheckLimitResponse)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureLocationsCheckLimits = (
    query: TemperatureLocationsCheckLimitsParams,
    params: RequestParams = {},
  ) =>
    this.request<TemperatureCheckLimitResponse[], IItemSetOfIFailure>({
      path: `/api/temperature-locations/check-limits`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureLocations
   * @name TemperatureLocationsSave
   * @request POST:/api/temperature-locations
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureLocationsSave = (
    data: TemperatureLocationSaveRequest,
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/temperature-locations`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureLocations
   * @name TemperatureLocationsDelete
   * @request DELETE:/api/temperature-locations/{id}
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureLocationsDelete = (
    { id, ...query }: TemperatureLocationsDeleteParams,
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/temperature-locations/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureReadings
   * @name TemperatureReadingsGetCurrentReadings
   * @request GET:/api/temperature-readings/current
   * @response `200` `(TemperatureReadingResponse)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureReadingsGetCurrentReadings = (params: RequestParams = {}) =>
    this.request<TemperatureReadingResponse[], IItemSetOfIFailure>({
      path: `/api/temperature-readings/current`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureReadings
   * @name TemperatureReadingsGetCurrentReadingForLocation
   * @request GET:/api/temperature-readings/location/{locationId}
   * @response `200` `(TemperatureReadingResponse)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureReadingsGetCurrentReadingForLocation = (
    {
      locationId,
      ...query
    }: TemperatureReadingsGetCurrentReadingForLocationParams,
    params: RequestParams = {},
  ) =>
    this.request<TemperatureReadingResponse[], IItemSetOfIFailure>({
      path: `/api/temperature-readings/location/${locationId}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TemperatureReadings
   * @name TemperatureReadingsGetTimeSeries
   * @request POST:/api/temperature-readings/time-series
   * @response `200` `TemperatureTimeSeriesResponse`
   * @response `400` `IItemSetOfIFailure`
   */
  temperatureReadingsGetTimeSeries = (
    data: TemperatureTimeSeriesRequest,
    params: RequestParams = {},
  ) =>
    this.request<TemperatureTimeSeriesResponse, IItemSetOfIFailure>({
      path: `/api/temperature-readings/time-series`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
