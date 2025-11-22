import { isNil } from './FormatHelpers';

export function tempUnit(useFahrenheit: boolean) {
  return useFahrenheit ? '°F' : '°C';
}

export function formatTemp(
  tempCelsius: number | null | undefined,
  useFahrenheit: boolean,
  decimalsOverride = -1,
) {
  if (tempCelsius === null || typeof tempCelsius === 'undefined' || tempCelsius.toString() === '') {
    return null;
  }

  const tempDecimals = useFahrenheit ? 0 : 1;

  const decimals = decimalsOverride > -1 ? decimalsOverride : tempDecimals;
  const convertedTemp = useFahrenheit ? (tempCelsius || 0) * 1.8 + 32 : tempCelsius || 0;
  return Math.round(convertedTemp * 10 ** decimals) / 10 ** decimals;
}

export function formatTempWithUnit(
  tempCelsius: number | null | undefined,
  useFahrenheit: boolean,
  decimalsOverride = -1,
) {
  return `${formatTemp(tempCelsius, useFahrenheit, decimalsOverride)}${tempUnit(useFahrenheit)}`;
}

export function formatTempWithUnitOrEmpty(
  tempCelsius: number | null | undefined,
  useFahrenheit: boolean,
  decimalsOverride = -1,
) {
  if (isNil(tempCelsius)) {
    return '';
  }

  return formatTempWithUnit(tempCelsius, useFahrenheit, decimalsOverride);
}

export function formatHumidity(humidity: number | null | undefined) {
  if (humidity === null || typeof humidity === 'undefined' || humidity.toString() === '') {
    return null;
  }

  return Math.round(humidity);
}

export function formatHumidityWithUnit(humidity: number | null | undefined) {
  return `${formatHumidity(humidity)}%`;
}
