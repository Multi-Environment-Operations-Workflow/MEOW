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
        const int radius = 60;

        var deg2rad = Math.PI / 180;
        var latA = loc.Latitude * deg2rad;
        var lonA = loc.Longitude * deg2rad;
        var latB = point.Latitude * deg2rad;
        var lonB = point.Longitude * deg2rad;

        var deltaRatio = MathF.Tan((float)(latB / 2 + MathF.PI / 4))
                         / MathF.Tan((float)(latA / 2 + MathF.PI / 4));
        var deltaLon = MathF.Abs((float)(lonA - lonB)) ;

        deltaLon %= MathF.PI;
        var bearing = MathF.Atan2(deltaLon, deltaRatio) / deg2rad;

        var radians = (float)((bearing % 360 - 90) * deg2rad);
        var left = radius + radius * MathF.Cos(radians);
        var top = radius - radius * MathF.Sin(radians);


        Console.WriteLine("Point: " + point.Longitude + ", " + point.Latitude + ": Bearing: " + bearing);

        return ((int)left, (int)top);
    }

    /*
     * South: 9;56
     * Center: 8:56
     * West: 8;57
     */

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