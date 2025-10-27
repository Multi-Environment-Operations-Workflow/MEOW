namespace MEOW.Components.Services;

public interface IBrowserDimensionService
{
    Task<BrowserDimensions> GetBrowserDimensions();
}