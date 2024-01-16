import { isNil } from './FormatHelpers';

export function tempUnit(useFahrenheit: boolean) {
  return useFahrenheit ? '°F' : '°C';
}

export function formatTemp(tempCelsius: number | null | undefined, useFahrenheit: boolean) {
  if (tempCelsius === null || typeof tempCelsius === 'undefined' || tempCelsius.toString() === '') {
    return null;
  }

  const decimals = useFahrenheit ? 0 : 1;
  const convertedTemp = useFahrenheit ? (tempCelsius || 0) * 1.8 + 32 : tempCelsius || 0;
  return Math.round(convertedTemp * 10 ** decimals) / 10 ** decimals;
}

export function formatTempWithUnit(tempCelsius: number | null | undefined, useFahrenheit: boolean) {
  return `${formatTemp(tempCelsius, useFahrenheit)}${tempUnit(useFahrenheit)}`;
}

export function formatTempWithUnitOrEmpty(tempCelsius: number | null | undefined, useFah: boolean) {
  if (isNil(tempCelsius)) {
    return '';
  }

  return formatTempWithUnit(tempCelsius, useFah);
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
