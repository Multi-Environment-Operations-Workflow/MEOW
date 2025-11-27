using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MEOW_BUSINESS.Models;
using Microsoft.Maui.Devices.Sensors;

namespace MEOW_BUSINESS.Services;

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
}