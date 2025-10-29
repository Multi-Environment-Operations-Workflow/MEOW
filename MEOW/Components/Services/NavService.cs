namespace MEOW.Components.Services;

public class NavService
{
    public event EventHandler<CompassData>? CompassReadingChanged;

    public event EventHandler<List<NavPoint>> NavPointsChanged;

    private readonly List<NavPoint>
        _navPoints = []; // ~Lattitude:57.0155703 Longitude: 9.9777961

    public event EventHandler<NavPoint>? LocationChanged;
    private CancellationTokenSource? _cts;

    public static (int, int) NavPointToVector(NavPoint loc, NavPoint point)
    {
        const int radius = 50; // Radius is 50, Diameter is 100. Treated as percent

        // convert to radians
        double lat1 = loc.Latitude * Math.PI / 180.0;
        double lat2 = point.Latitude * Math.PI / 180.0;
        double dLon = (point.Longitude - loc.Longitude) * Math.PI / 180.0;

        // great-circle / atan2(y, x) bearing
        double atY = Math.Sin(dLon) * Math.Cos(lat2);
        double atX = Math.Cos(lat1) * Math.Sin(lat2) -
                     Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

        double brng = Math.Atan2(atY, atX);
        brng = brng * 180.0 / Math.PI; // → degrees
        brng = (brng + 360.0) % 360.0; // normalize to 0..360

        double radians = brng * Math.PI / 180.0;
        double x = radius + radius * Math.Cos(radians);
        double y = radius + radius * Math.Sin(radians);

        return ((int)y, (int)x);
    }
    
    public async Task StartAsync()
    {
        if (!Compass.Default.IsSupported)
            throw new NotSupportedException("Compass not supported on this device");

        Compass.Default.ReadingChanged += (s, e) => { CompassReadingChanged?.Invoke(this, e.Reading); };

        Compass.Default.Start(SensorSpeed.UI);
    }

    public void AddNavPoint(NavPoint navPoint)
    {
        _navPoints.Add(navPoint);
        NavPointsChanged.Invoke(this, GetNavPoints());
    }

    public void RemoveNavPoint(int id)
    {
        var index = _navPoints.FindIndex(point => id == point.Id);
        if (index != -1) _navPoints.RemoveAt(index);
        NavPointsChanged.Invoke(this, GetNavPoints());
    }

    public List<NavPoint> GetNavPoints()
    {
        return new List<NavPoint>().Concat(_navPoints).ToList();
    }

    public void AddDebugPoints(NavPoint pos, string dir)
    {
        var north = new NavPoint(pos.Latitude + 1f, pos.Longitude);
        var south = new NavPoint(pos.Latitude - 1f, pos.Longitude);
        var east = new NavPoint(pos.Latitude, pos.Longitude + 1f);
        var west = new NavPoint(pos.Latitude, pos.Longitude - 1f);

        switch (dir)
        {
            case "west":
                AddNavPoint(west);
                break;
            case "east":
                AddNavPoint(east);
                break;
            case "north":
                AddNavPoint(north);
                break;
            case "south":
                AddNavPoint(south);
                break;
        }
    }

    public async Task StartTrackingAsync()
    {
        _cts = new CancellationTokenSource();

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var location = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium,
                        TimeSpan.FromSeconds(10)), // ToDo add option for this to change
                    _cts.Token
                );

                if (location != null)
                {
                    NavPoint pos = new NavPoint((float)location.Latitude, (float)location.Longitude, -1);
                    LocationChanged?.Invoke(this, pos);
                }

                await Task.Delay(5000); // poll every 5 seconds
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPS tracking error: {ex.Message}");
        }
    }

    public void StopCompass() => Compass.Default.Stop();
}

public record NavPoint(float Latitude, float Longitude, int Id = 0);