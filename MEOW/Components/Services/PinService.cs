using MEOW.Components.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace MEOW.Components.Services;

public class PinService : IPinService
{

    public List<PinItem> PinList = new();
   
    
    // First we gotta see if there is a bluetooth connection or if they are connected to any devices    

    public async Task<string> ProcessedValueAsync(string input)
    {
        await Task.Delay(100);
        return $"Processed value: {input}";
    }
    
    public async Task<string> SetPinList(PinItem pin) 
    {
        await Task.Delay(1000);
        PinList.Add(pin);
        Console.WriteLine(pin);
        return $"Processed value: {Pin}";
    }

    public async Task<List<PinItem>> GetPinList()
    {
        await Task.Delay(1000);
        return PinList;
    }

    public Task<bool> SendPinMetadataAsync(PinItem pin)
    {
        throw new NotImplementedException();
    }
    
    public Task OnPinDataReceivedAsync(byte[] data)
    {
        throw new NotImplementedException();
    }
}