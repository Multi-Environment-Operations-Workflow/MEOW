namespace MEOW.Components.Services;
using Microsoft.JSInterop;

public class BrowserDimensions(int width, int height)
{
    public int Width { get; private set; } = width;
    public int Height { get; private set; } = height;
} 

public class BrowserDimensionService(IJSRuntime js) : IBrowserDimensionService
{
    public async Task<BrowserDimensions> GetBrowserDimensions()
    {
        return new BrowserDimensions(800, 800);
    }
}