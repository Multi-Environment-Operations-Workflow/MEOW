using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface INavService
{
    public event EventHandler<NavPoint> LocationChanged;
    public event EventHandler<CompassData> CompassChanged;
    public event EventHandler<List<NavPoint>> NavPointsChanged;

    public Task StartCompass();
    public Task StopCompass();
    public Task TryStartGps();
    public Task StopGps();
    public List<NavPoint> GetNavPoints();
    public void AddNavPoint(NavPoint navPoint);
    public void RemoveNavPoint(int id);
    public void OnUserPoint(MeowMessageGps msg);
    public Task<NavPoint?> TryGetLastPosition();
}