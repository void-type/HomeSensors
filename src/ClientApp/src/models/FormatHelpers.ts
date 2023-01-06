export function clamp(value: number, min: number, max: number) {
  return Math.max(min, Math.min(value, max));
}

export function toNumber(value: string | number | undefined | null, defaultValue = 0) {
  const number = Number(value);
  return !Number.isNaN(number) ? number : defaultValue;
}

export function toInt(value: string | number | undefined | null, defaultValue = 0) {
  return Math.floor(toNumber(value, defaultValue));
}

export function toNumberOrNull(value: string | number | undefined | null) {
  if (value === null || typeof value === 'undefined' || value === '') {
    return null;
  }

  const number = Number(value);
  return !Number.isNaN(number) ? number : null;
}

export function trimAndTitleCase(value: string) {
  return value
    .trim()
    .split(' ')
    .filter((word) => word.length > 0)
    .map((word) => word[0].toUpperCase() + word.substring(1).toLowerCase())
    .join(' ');
}

export function isNil(value: string | null | undefined) {
  return value === null || value === undefined || value === '';
}

export function tempUnit(useFahrenheit: boolean) {
  return useFahrenheit ? '°F' : '°C';
}

export function formatTemp(tempCelsius: number | null | undefined, useFahrenheit: boolean) {
  if (tempCelsius === null || typeof tempCelsius === 'undefined') {
    return null;
  }

  const decimals = useFahrenheit ? 0 : 1;
  const convertedTemp = useFahrenheit ? (tempCelsius || 0) * 1.8 + 32 : tempCelsius || 0;
  return Math.round(convertedTemp * 10 ** decimals) / 10 ** decimals;
}

export function formatTempWithUnit(tempCelsius: number | null | undefined, useFahrenheit: boolean) {
  if (tempCelsius === null || typeof tempCelsius === 'undefined' || tempCelsius.toString() === '') {
    return null;
  }

  const decimals = useFahrenheit ? 0 : 1;
  const convertedTemp = useFahrenheit ? (tempCelsius || 0) * 1.8 + 32 : tempCelsius || 0;
  const roundedTemp = Math.round(convertedTemp * 10 ** decimals) / 10 ** decimals;

  return `${roundedTemp}${tempUnit(useFahrenheit)}`;
}
