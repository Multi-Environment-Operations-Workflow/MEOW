using MEOW.Components.Enums;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class NavService : INavService
{
    public event EventHandler<NavPoint>? LocationChanged;
    public event EventHandler<CompassData>? CompassChanged;
    public event EventHandler<List<NavPoint>>? NavPointsChanged;

    private readonly object _navLock = new();
    private readonly List<NavPoint> _navPoints = []; // ~Latitude:57.0155703 Longitude: 9.9777961
    private CancellationTokenSource? _cts;


    private void CompassChangedFnc(object? s, CompassChangedEventArgs e)
    {
        CompassChanged?.Invoke(this, e.Reading);
    }

    public async Task StartCompass()
    {
        if (!Compass.IsSupported) return;
        Compass.Default.ReadingChanged += CompassChangedFnc;
        Compass.Default.Start(SensorSpeed.UI);
    }

    public async Task StopCompass()
    {
        if (!Compass.IsSupported) return;
        Compass.Default.ReadingChanged -= CompassChangedFnc;
        Compass.Default.Stop();
    }

    public async Task TryStartGps()
    {
        if (_cts is not null) return;
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
                    NavPoint pos = new NavPoint((float)location.Longitude, (float)location.Latitude,
                        NavPointType.OtherDevice, -1);
                    LocationChanged?.Invoke(this, pos);
                }

                await Task.Delay(5000, _cts.Token); // poll every 5 seconds
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPS tracking error: {ex.Message}");
        }
    }

    public async Task StopGps()
    {
        _cts?.CancelAsync();
        _cts?.Dispose();
    }

    public List<NavPoint> GetNavPoints() => [.._navPoints];

    public void AddNavPoint(NavPoint navPoint)
    {
        lock (_navLock)
        {
            _navPoints.Add(navPoint);
            NavPointsChanged?.Invoke(this, GetNavPoints());
        }
    }

    public void RemoveNavPoint(int id)
    {
        var index = _navPoints.FindIndex(point => id == point.Id);
        if (index != -1) _navPoints.RemoveAt(index);
        NavPointsChanged?.Invoke(this, GetNavPoints());
    }
}