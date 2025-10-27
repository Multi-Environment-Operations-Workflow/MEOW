using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IPinService
{
    // Send basic pin info (title, text, timestamp, etc.)
    Task<bool> SendPinMetadataAsync(PinItem pin);

    // Send the image portion of the pin (if available)
    Task<bool> SendPinImageAsync(PinItem pin);

    // Send a chunk of image data for large transfers
    Task<bool> SendPinChunkAsync(byte[] data, int sequenceNumber);

    // Called when pin metadata is received from another device
    Task OnPinDataReceivedAsync(byte[] data);

    // Called when an image chunk is received from another device
    Task OnPinImageChunkReceivedAsync(byte[] chunk, int sequenceNumber);

    // Combine all received chunks into a complete pin
    void AssembleCompletePin(Guid pinId);
}