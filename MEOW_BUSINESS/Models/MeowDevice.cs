using Plugin.BLE.Abstractions.Contracts;

namespace MEOW_BUSINESS.Models;

public class MeowDevice(string name, Guid id, IDevice nativeDevice)
{
    public string Name { get; set; } = name;
    public Guid Id { get; set; } = id;
    public IDevice NativeDevice { get; set; } = nativeDevice;
}