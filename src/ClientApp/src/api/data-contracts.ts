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

export interface AppVersion {
  version?: string | null;
  isPublicRelease?: boolean;
  isPrerelease?: boolean;
  gitCommitId?: string | null;
  /** @format date-time */
  gitCommitDate?: string;
  assemblyConfiguration?: string | null;
}

export interface CheckLimitResult {
  location?: Location;
  isFailed?: boolean;
  minReading?: Reading;
  maxReading?: Reading;
}

export interface ClientStatus {
  topics?: string[] | null;
  isCreated?: boolean;
  isConnected?: boolean;
}

export interface Device {
  /** @format int64 */
  id?: number;
  name?: string | null;
  mqttTopic?: string | null;
  /** @format int64 */
  locationId?: number | null;
  lastReading?: Reading;
  isRetired?: boolean;
  isLost?: boolean;
  isInactive?: boolean;
  isBatteryLevelLow?: boolean;
}

export interface DeviceUpdateRequest {
  /** @format int64 */
  id?: number;
  name?: string | null;
  mqttTopic?: string | null;
  /** @format int64 */
  locationId?: number | null;
  isRetired?: boolean;
}

export interface DomainUser {
  login?: string | null;
  authorizedAs?: string[] | null;
}

export interface IFailure {
  message?: string | null;
  uiHandle?: string | null;
}

export interface IFailureIItemSet {
  /** @format int32 */
  count?: number;
  items?: IFailure[] | null;
  isPagingEnabled?: boolean;
  /** @format int32 */
  page?: number;
  /** @format int32 */
  take?: number;
  /** @format int32 */
  totalCount?: number;
}

export interface Int64EntityMessage {
  message?: string | null;
  /** @format int64 */
  id?: number;
}

export interface Location {
  /** @format int64 */
  id?: number;
  name?: string | null;
  /** @format double */
  minTemperatureLimitCelsius?: number | null;
  /** @format double */
  maxTemperatureLimitCelsius?: number | null;
}

export interface LocationCreateRequest {
  name?: string | null;
  /** @format double */
  minTemperatureLimitCelsius?: number | null;
  /** @format double */
  maxTemperatureLimitCelsius?: number | null;
}

export interface LocationUpdateRequest {
  /** @format int64 */
  id?: number;
  name?: string | null;
  /** @format double */
  minTemperatureLimitCelsius?: number | null;
  /** @format double */
  maxTemperatureLimitCelsius?: number | null;
}

export interface Reading {
  /** @format date-time */
  time?: string;
  /** @format double */
  humidity?: number | null;
  /** @format double */
  temperatureCelsius?: number | null;
  location?: Location;
  isHot?: boolean;
  isCold?: boolean;
}

export interface SetupRequest {
  topics?: string[] | null;
}

export interface TimeSeries {
  location?: Location;
  temperatureAggregate?: TimeSeriesAggregate;
  humidityAggregate?: TimeSeriesAggregate;
  points?: TimeSeriesPoint[] | null;
}

export interface TimeSeriesAggregate {
  /** @format double */
  minimum?: number | null;
  /** @format double */
  maximum?: number | null;
  /** @format double */
  average?: number | null;
}

export interface TimeSeriesPoint {
  /** @format date-time */
  time?: string;
  /** @format double */
  temperatureCelsius?: number | null;
  /** @format double */
  humidity?: number | null;
}

export interface TimeSeriesRequest {
  /** @format date-time */
  startTime?: string;
  /** @format date-time */
  endTime?: string;
  locationIds?: number[] | null;
}

export interface WebClientInfo {
  antiforgeryToken?: string | null;
  antiforgeryTokenHeaderName?: string | null;
  applicationName?: string | null;
  user?: DomainUser;
}

export interface TemperaturesLocationsCheckLimitsCreateParams {
  /** @format date-time */
  lastCheck?: string;
}
