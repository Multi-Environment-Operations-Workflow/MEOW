using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class PinService : IPinService
{
    private readonly List<PinItem> _pins = new();

    public void AddPinAsync(PinItem pin)
    {
        _pins.Add(pin);
    }

    public void RemovePinAsync(PinItem pin)
    {
        _pins.Remove(pin);
    }

    public async Task<List<PinItem>> GetPinsAsync()
    {
        await Task.Yield();
        return _pins.ToList();
    }

    public Task<bool> SendPinMetadataAsync(PinItem pin) => throw new NotImplementedException();
    public Task OnPinDataReceivedAsync(byte[] data) => throw new NotImplementedException();
}