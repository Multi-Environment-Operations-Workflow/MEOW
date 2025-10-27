using Microsoft.AspNetCore.Components.Forms;

namespace MEOW.Components.Models;

public class PinItem
{
    public string Title { get; set; } = string.Empty;
    public string TextContext { get; set; } = string.Empty;
    public IBrowserFile? File { get; set; }
    public string? FileDataUrl { get; set; }
}