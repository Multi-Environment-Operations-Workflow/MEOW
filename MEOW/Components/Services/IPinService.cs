using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IPinService
{
    void Initialize();

    Task<(bool, List<Exception>)> SendMessage(PinItem pin);

    Task<bool> SendPinMetadataAsync(PinItem pin);

    Task OnPinDataReceivedAsync(byte[] data);

    void AddPin(PinItem pin);
    void RemovePin(PinItem pin);
    List<PinItem> GetPins();
}
