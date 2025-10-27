using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IPinService
{
    // Send basic pin info (title, text, timestamp, etc.)
    Task<bool> SendPinMetadataAsync(PinItem pin);

    // Called when pin metadata is received from another device
    Task OnPinDataReceivedAsync(byte[] data);
}