import type { ITimeSeriesInputs } from './ITimeSeriesInputs';

export interface ITemperatureGraphInputs extends ITimeSeriesInputs {
  showHumidity: boolean;
  hideHvacActions: boolean;
}
