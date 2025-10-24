namespace MEOW.Components.Services;
using Microsoft.JSInterop;

public class BrowserDimensions(int width, int height)
{
    public int Width { get; private set; } = width;
    public int Height { get; private set; } = height;
} 

public class BrowserDimensionService: IBrowserDimensionService
{
    private readonly IJSRuntime _js;
    
    public BrowserDimensionService(IJSRuntime js)
    {
        _js = js;
    }
    
    public async Task<BrowserDimensions> GetBrowserDimensions()
    {
        return await _js.InvokeAsync<BrowserDimensions>("getDimensions");
    }
}