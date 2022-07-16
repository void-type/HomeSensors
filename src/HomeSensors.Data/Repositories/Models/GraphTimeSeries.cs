namespace HomeSensors.Data.Repositories.Models;

public class GraphTimeSeries
{
    public GraphTimeSeries(string location, IEnumerable<GraphPoint> points)
    {
        Location = location;
        Points = points;
    }

    public string Location { get; }
    public IEnumerable<GraphPoint> Points { get; }
}
