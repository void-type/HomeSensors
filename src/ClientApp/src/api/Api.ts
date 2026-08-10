/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

import type {AppVersion,
  CameraResponse,
  CameraSaveRequest,
  CamerasDeleteParams,
  CameraSnapshot,
  CameraSnapshotsGetOriginalParams,
  CameraSnapshotsGetThumbnailParams,
  CameraSnapshotsGetTimelineParams,
  CameraSnapshotsUploadOriginalParams,
  CategoriesDeleteParams,
  CategoryResponse,
  CategorySaveRequest,
  EmailRecipientResponse,
  EmailRecipientSaveRequest,
  EmailRecipientsDeleteParams,
  EntityMessageOfLong,
  EntityMessageOfString,
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
  WaterLeakDeviceResponse,
  WaterLeakDeviceSaveRequest,
  WaterLeakDevicesDeleteParams,
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
   * @tags Cameras
   * @name CamerasGetAll
   * @request GET:/api/cameras/all
   * @response `200` `(CameraResponse)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  camerasGetAll = (params: RequestParams = {}) =>
    this.request<CameraResponse[], IItemSetOfIFailure>({
      path: `/api/cameras/all`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Cameras
   * @name CamerasSave
   * @request POST:/api/cameras
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  camerasSave = (data: CameraSaveRequest, params: RequestParams = {}) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/cameras`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Cameras
   * @name CamerasDelete
   * @request DELETE:/api/cameras/{id}
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  camerasDelete = ({ id }: CamerasDeleteParams, params: RequestParams = {}) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/cameras/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CameraSnapshots
   * @name CameraSnapshotsGetTimeline
   * @request GET:/api/camera-snapshots/{cameraId}/timeline
   * @response `200` `(CameraSnapshot)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  cameraSnapshotsGetTimeline = (
    { cameraId, ...query }: CameraSnapshotsGetTimelineParams,
    params: RequestParams = {},
  ) =>
    this.request<CameraSnapshot[], IItemSetOfIFailure>({
      path: `/api/camera-snapshots/${cameraId}/timeline`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CameraSnapshots
   * @name CameraSnapshotsGetThumbnail
   * @request GET:/api/camera-snapshots/{cameraId}/thumbnail/{fileName}
   * @response `200` `File`
   * @response `400` `IItemSetOfIFailure`
   * @response `404` `void`
   */
  cameraSnapshotsGetThumbnail = (
    { cameraId, fileName, ...query }: CameraSnapshotsGetThumbnailParams,
    params: RequestParams = {},
  ) =>
    this.request<Blob, IItemSetOfIFailure | void>({
      path: `/api/camera-snapshots/${cameraId}/thumbnail/${fileName}`,
      method: "GET",
      query: query,
      ...params,
    });
  /**
   * No description
   *
   * @tags CameraSnapshots
   * @name CameraSnapshotsGetOriginal
   * @request GET:/api/camera-snapshots/{cameraId}/original/{fileName}
   * @response `200` `File`
   * @response `400` `IItemSetOfIFailure`
   * @response `404` `void`
   */
  cameraSnapshotsGetOriginal = (
    { cameraId, fileName }: CameraSnapshotsGetOriginalParams,
    params: RequestParams = {},
  ) =>
    this.request<Blob, IItemSetOfIFailure | void>({
      path: `/api/camera-snapshots/${cameraId}/original/${fileName}`,
      method: "GET",
      ...params,
    });
  /**
   * No description
   *
   * @tags CameraSnapshots
   * @name CameraSnapshotsUploadOriginal
   * @request POST:/api/camera-snapshots/{cameraId}/upload
   * @response `200` `EntityMessageOfString`
   * @response `400` `IItemSetOfIFailure`
   * @response `404` `IItemSetOfIFailure`
   * @response `409` `IItemSetOfIFailure`
   */
  cameraSnapshotsUploadOriginal = (
    { cameraId }: CameraSnapshotsUploadOriginalParams,
    data: {
      /** @format binary */
      file?: File | null;
      timestamp?: string | null;
    },
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfString, IItemSetOfIFailure>({
      path: `/api/camera-snapshots/${cameraId}/upload`,
      method: "POST",
      body: data,
      type: ContentType.FormData,
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
    { id }: CategoriesDeleteParams,
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
   * @tags EmailRecipients
   * @name EmailRecipientsGetAll
   * @request GET:/api/email-recipients/all
   * @response `200` `(EmailRecipientResponse)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  emailRecipientsGetAll = (params: RequestParams = {}) =>
    this.request<EmailRecipientResponse[], IItemSetOfIFailure>({
      path: `/api/email-recipients/all`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags EmailRecipients
   * @name EmailRecipientsSave
   * @request POST:/api/email-recipients
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  emailRecipientsSave = (
    data: EmailRecipientSaveRequest,
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/email-recipients`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags EmailRecipients
   * @name EmailRecipientsDelete
   * @request DELETE:/api/email-recipients/{id}
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  emailRecipientsDelete = (
    { id }: EmailRecipientsDeleteParams,
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/email-recipients/${id}`,
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
    { id }: TemperatureDevicesDeleteParams,
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
    query: TemperatureLocationsCheckLimitsParams = {},
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
    { id }: TemperatureLocationsDeleteParams,
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
    { locationId }: TemperatureReadingsGetCurrentReadingForLocationParams,
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
  /**
   * No description
   *
   * @tags WaterLeakDevices
   * @name WaterLeakDevicesGetAll
   * @request GET:/api/water-leak-devices/all
   * @response `200` `(WaterLeakDeviceResponse)[]`
   * @response `400` `IItemSetOfIFailure`
   */
  waterLeakDevicesGetAll = (params: RequestParams = {}) =>
    this.request<WaterLeakDeviceResponse[], IItemSetOfIFailure>({
      path: `/api/water-leak-devices/all`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaterLeakDevices
   * @name WaterLeakDevicesSave
   * @request POST:/api/water-leak-devices
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  waterLeakDevicesSave = (
    data: WaterLeakDeviceSaveRequest,
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/water-leak-devices`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaterLeakDevices
   * @name WaterLeakDevicesDelete
   * @request DELETE:/api/water-leak-devices/{id}
   * @response `200` `EntityMessageOfLong`
   * @response `400` `IItemSetOfIFailure`
   */
  waterLeakDevicesDelete = (
    { id }: WaterLeakDevicesDeleteParams,
    params: RequestParams = {},
  ) =>
    this.request<EntityMessageOfLong, IItemSetOfIFailure>({
      path: `/api/water-leak-devices/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
}
