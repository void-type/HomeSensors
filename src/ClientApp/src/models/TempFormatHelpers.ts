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
