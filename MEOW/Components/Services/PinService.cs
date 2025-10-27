using MEOW.Components.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace MEOW.Components.Services;

public class PinService : IPinService
{

    public async Task<string> ProcessedValueAsync(string input)
    {
        await Task.Delay(100);
        return $"Processed value: {input}";
    }
    
    public Task<bool> SendPinMetadataAsync(PinItem pin)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SendPinImageAsync(PinItem pin)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SendPinChunkAsync(byte[] data, int sequenceNumber)
    {
        throw new NotImplementedException();
    }

    public Task OnPinDataReceivedAsync(byte[] data)
    {
        throw new NotImplementedException();
    }

    public Task OnPinImageChunkReceivedAsync(byte[] chunk, int sequenceNumber)
    {
        throw new NotImplementedException();
    }

    public void AssembleCompletePin(Guid pinId)
    {
        throw new NotImplementedException();
    }
}