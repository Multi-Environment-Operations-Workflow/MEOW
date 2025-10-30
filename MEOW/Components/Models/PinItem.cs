using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace MEOW.Components.Models;

public class PinItem
{
    public string Title { get; set; }
    public string TextContext { get; set; }
    public string? FileDataUrl { get; set; }

    public PinItem(string title, string textContext)
    {
        if (!string.IsNullOrWhiteSpace(title)) { Title = title; }
        if (!string.IsNullOrWhiteSpace(textContext)) { TextContext = textContext; }
    }
}