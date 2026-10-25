namespace HomeSensors.Model.Temperature.Models;

public class TemperatureTimeSeriesResponse(List<TemperatureTimeSeriesHvacAction> hvacActions, List<TemperatureTimeSeriesLocationData> locationSeries)
{
    public List<TemperatureTimeSeriesHvacAction> HvacActions { get; } = hvacActions;

    public List<TemperatureTimeSeriesLocationData> Locations { get; } = locationSeries;
}
