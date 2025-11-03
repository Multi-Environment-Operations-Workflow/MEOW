using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public interface IPinService
{
    Task<bool> SendPinMetadataAsync(PinItem pin);

    Task OnPinDataReceivedAsync(byte[] data);
    
    void AddPin(PinItem pin);
    void RemovePin(PinItem pin);
    List<PinItem> GetPins();
}