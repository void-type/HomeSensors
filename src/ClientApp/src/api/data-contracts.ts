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

export interface WebClientInfo {
  antiforgeryToken?: string;
  antiforgeryTokenHeaderName?: string;
  applicationName?: string;
  user?: DomainUser;
}

export interface DomainUser {
  login?: string;
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

export interface IItemSetOfIFailure {
  /** @format int32 */
  count?: number;
  items?: IFailure[];
  isPagingEnabled?: boolean;
  /** @format int32 */
  page?: number;
  /** @format int32 */
  take?: number;
  /** @format int32 */
  totalCount?: number;
}

export interface IFailure {
  message?: string;
  uiHandle?: string | null;
  code?: string | null;
}

export type EntityMessageOfLong = UserMessage & {
  /** @format int64 */
  id?: number;
};

export interface UserMessage {
  message?: string;
}

export interface CategorySaveRequest {
  /** @format int64 */
  id?: number;
  name?: string;
  /** @format int32 */
  order?: number;
}

export interface MqttDiscoveryClientStatus {
  topics?: string[] | null;
  isCreated?: boolean;
  isConnected?: boolean;
}

export interface MqttDiscoverySetupRequest {
  topics?: string[];
}

export interface TemperatureDeviceResponse {
  /** @format int64 */
  id?: number;
  name?: string;
  mqttTopic?: string;
  /** @format int64 */
  locationId?: number;
  lastReading?: TemperatureReadingResponse | null;
  isRetired?: boolean;
  isLost?: boolean;
  isInactive?: boolean;
  isBatteryLevelLow?: boolean;
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
  /** @format int64 */
  categoryId?: number | null;
}

export interface TemperatureDeviceSaveRequest {
  /** @format int64 */
  id?: number;
  name?: string;
  mqttTopic?: string;
  /** @format int64 */
  locationId?: number;
  isRetired?: boolean;
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
  /** @format int64 */
  categoryId?: number | null;
}

export interface TemperatureTimeSeriesResponse {
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
}

export interface TemperatureLocationsCheckLimitsParams {
  /** @format date-time */
  since?: string;
  isAveragingEnabled?: boolean;
}
