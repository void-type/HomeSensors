/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

/** Information for bootstrapping a web client. */
export interface WebClientInfo {
  /** The value of the header antiforgery token */
  antiforgeryToken?: string;
  /** The header name of the antiforgery token */
  antiforgeryTokenHeaderName?: string;
  /** The UI-friendly application name. */
  applicationName?: string;
  /** The current user */
  user?: DomainUser;
}

/** A user for use in the domain layer and model services. */
export interface DomainUser {
  /** UI-friendly name for the current user */
  login?: string;
  /** Names of the authorization policies that the user fulfills. */
  authorizedAs?: string[];
}

export interface AppVersion {
  version?: string | null;
  isPublicRelease?: boolean;
  isPrerelease?: boolean;
  gitCommitId?: string;
  /** @format date-time */
  gitCommitDate?: string;
  assemblyConfiguration?: string;
}

export interface CategoryResponse {
  /** @format int64 */
  id?: number;
  name?: string;
  /** @format int32 */
  order?: number;
}

/** A set of items. Can optionally by a page of a full set. */
export interface IItemSetOfIFailure {
  /**
   * The count of items in this set.
   * @format int32
   */
  count?: number;
  /** The items in this set. */
  items?: IFailure[];
  /** When true, this is a page of a full set. */
  isPagingEnabled?: boolean;
  /**
   * If paging is enabled, this represents the page number in the total set.
   * @format int32
   */
  page?: number;
  /**
   * If paging is enabled, the requested number of results per page.
   * @format int32
   */
  take?: number;
  /**
   * The count of all the items in the total set. If paging is enabled, the total number of results in all pages.
   * @format int32
   */
  totalCount?: number;
}

/** A domain logic failure with UI-friendly error message and optional field name or UI handle. */
export interface IFailure {
  /** The UI-friendly error message to be displayed to the user. */
  message?: string;
  /** The name of the UI field corresponding to the invalid user input. */
  uiHandle?: string | null;
  /** A A code name or identifier for the error for programmatic error discrimination. */
  code?: string | null;
}

/** A UI-friendly message and the Id of the entity that was affected during an event. */
export type EntityMessageOfLong = UserMessage & {
  /**
   * The Id of the entity affected during an event.
   * @format int64
   */
  id?: number;
};

/** A UI-friendly message. */
export interface UserMessage {
  /** The UI-friendly message. */
  message?: string;
}

export interface CategorySaveRequest {
  /** @format int64 */
  id?: number;
  name?: string;
  /** @format int32 */
  order?: number;
}

export interface EmailRecipientResponse {
  /** @format int64 */
  id?: number;
  email?: string;
}

export interface EmailRecipientSaveRequest {
  /** @format int64 */
  id?: number;
  email?: string;
}

export interface TemperatureDeviceResponse {
  /** @format int64 */
  id?: number;
  name?: string;
  mqttTopic?: string;
  /** @format int64 */
  locationId?: number | null;
  lastReading?: TemperatureReadingResponse | null;
  isRetired?: boolean;
  isLost?: boolean;
  isInactive?: boolean;
  isBatteryLevelLow?: boolean;
  excludeFromInactiveAlerts?: boolean;
  /** @format int32 */
  inactiveLimitMinutes?: number;
}

export interface TemperatureReadingResponse {
  /** @format date-time */
  time?: string;
  /** @format double */
  humidity?: number | null;
  /** @format double */
  temperatureCelsius?: number | null;
  location?: TemperatureLocationResponse | null;
  isHot?: boolean;
  isCold?: boolean;
}

export interface TemperatureLocationResponse {
  /** @format int64 */
  id?: number;
  name?: string;
  /** @format double */
  minTemperatureLimitCelsius?: number | null;
  /** @format double */
  maxTemperatureLimitCelsius?: number | null;
  isHidden?: boolean;
  color?: string;
  /** @format int64 */
  categoryId?: number | null;
}

export interface TemperatureDeviceSaveRequest {
  /** @format int64 */
  id?: number;
  name?: string;
  mqttTopic?: string;
  /** @format int64 */
  locationId?: number | null;
  isRetired?: boolean;
  excludeFromInactiveAlerts?: boolean;
  /** @format int32 */
  inactiveLimitMinutes?: number;
}

export interface TemperatureCheckLimitResponse {
  location?: TemperatureLocationResponse;
  isFailed?: boolean;
  minReading?: TemperatureReadingResponse | null;
  maxReading?: TemperatureReadingResponse | null;
}

export interface TemperatureLocationSaveRequest {
  /** @format int64 */
  id?: number;
  name?: string;
  /** @format double */
  minTemperatureLimitCelsius?: number | null;
  /** @format double */
  maxTemperatureLimitCelsius?: number | null;
  isHidden?: boolean;
  color?: string;
  /** @format int64 */
  categoryId?: number | null;
}

export interface TemperatureTimeSeriesResponse {
  hvacActions?: TemperatureTimeSeriesHvacAction[];
  locations?: TemperatureTimeSeriesLocationData[];
}

export interface TemperatureTimeSeriesHvacAction {
  action?: string;
  /** @format date-time */
  startTime?: string;
  /** @format date-time */
  endTime?: string;
  /** @format int32 */
  durationMinutes?: number;
}

export interface TemperatureTimeSeriesLocationData {
  location?: TemperatureLocationResponse;
  temperatureAggregate?: TemperatureTimeSeriesAggregate;
  humidityAggregate?: TemperatureTimeSeriesAggregate;
  points?: TemperatureTimeSeriesPoint[];
}

export interface TemperatureTimeSeriesAggregate {
  /** @format double */
  minimum?: number | null;
  /** @format double */
  maximum?: number | null;
  /** @format double */
  average?: number | null;
}

export interface TemperatureTimeSeriesPoint {
  /** @format date-time */
  time?: string;
  /** @format double */
  temperatureCelsius?: number | null;
  /** @format double */
  humidity?: number | null;
}

export interface TemperatureTimeSeriesRequest {
  /** @format date-time */
  startTime?: string;
  /** @format date-time */
  endTime?: string;
  locationIds?: number[];
  includeHvacActions?: boolean;
  trimHvacActionsToRequestedTimeRange?: boolean;
}

export interface WaterLeakDeviceResponse {
  /** @format int64 */
  id?: number;
  name?: string;
  mqttTopic?: string;
  /** @format int32 */
  inactiveLimitMinutes?: number;
}

export interface WaterLeakDeviceSaveRequest {
  /** @format int64 */
  id?: number;
  name?: string;
  mqttTopic?: string;
  /** @format int32 */
  inactiveLimitMinutes?: number;
}

export interface CategoriesDeleteParams {
  /** @format int64 */
  id: number;
}

export interface EmailRecipientsDeleteParams {
  /** @format int64 */
  id: number;
}

export interface TemperatureDevicesDeleteParams {
  /** @format int64 */
  id: number;
}

export interface TemperatureLocationsCheckLimitsParams {
  /** @format date-time */
  since?: string;
  isAveragingEnabled?: boolean;
}

export interface TemperatureLocationsDeleteParams {
  /** @format int64 */
  id: number;
}

export interface TemperatureReadingsGetCurrentReadingForLocationParams {
  /** @format int64 */
  locationId: number;
}

export interface WaterLeakDevicesDeleteParams {
  /** @format int64 */
  id: number;
}
