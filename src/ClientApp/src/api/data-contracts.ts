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

export interface Device {
  /** @format int64 */
  id?: number;
  deviceModel?: string | null;
  deviceId?: string | null;
  deviceChannel?: string | null;
  /** @format int64 */
  currentLocationId?: number | null;
  lastReading?: Reading;
  isRetired?: boolean;
  isLost?: boolean;
  isInactive?: boolean;
}

export interface DomainUser {
  login?: string | null;
  authorizedAs?: string[] | null;
}

export interface GraphPoint {
  /** @format date-time */
  time?: string;
  /** @format double */
  temperatureCelsius?: number | null;
}

export interface GraphTimeSeries {
  location?: Location;
  /** @format double */
  minTemperatureCelsius?: number | null;
  /** @format double */
  maxTemperatureCelsius?: number | null;
  /** @format double */
  averageTemperatureCelsius?: number | null;
  points?: GraphPoint[] | null;
}

export interface GraphTimeSeriesRequest {
  /** @format date-time */
  startTime?: string;
  /** @format date-time */
  endTime?: string;
  locationIds?: number[] | null;
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
  minLimitTemperatureCelsius?: number | null;
  /** @format double */
  maxLimitTemperatureCelsius?: number | null;
}

export interface Reading {
  /** @format date-time */
  time?: string;
  /** @format double */
  humidity?: number | null;
  /** @format double */
  temperatureCelsius?: number | null;
  location?: Location;
}

export interface UpdateDeviceRequest {
  /** @format int64 */
  id?: number;
  /** @format int64 */
  currentLocationId?: number | null;
  isRetired?: boolean;
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
