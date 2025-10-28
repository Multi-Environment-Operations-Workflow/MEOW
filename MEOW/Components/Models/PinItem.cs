using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace MEOW.Components.Models;

public class PinItem
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(10, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 50 characters long.")]
    public string Title { get; set; } = string.Empty;
    [Required(ErrorMessage = "Description is required.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 200 characters long.")]
    public string TextContext { get; set; } = string.Empty;
    public IBrowserFile? File { get; set; }
    public string? FileDataUrl { get; set; }
}