namespace HomeSensors.Model.Temperature.Models;

public class TemperatureTimeSeriesResponse
{
    public TemperatureTimeSeriesResponse(List<TemperatureTimeSeriesHvacAction> hvacActions, List<TemperatureTimeSeriesLocationData> locationSeries)
    {
        HvacActions = hvacActions;
        Locations = locationSeries;
    }

    public List<TemperatureTimeSeriesHvacAction> HvacActions { get; }

    public List<TemperatureTimeSeriesLocationData> Locations { get; }
}
