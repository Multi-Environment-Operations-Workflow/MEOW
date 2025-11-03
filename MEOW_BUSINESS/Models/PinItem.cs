namespace MEOW_BUSINESS.Models;

public class PinItem
{
    public string Title { get; private set; }
    public string? TextContext { get; private set; }
    public string? FileData { get; private set; }

    public PinItem(string title, string? textContext, string? fileData)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or whitespace", nameof(title));
        }
        Title = title;
        TextContext = textContext;
        FileData = fileData;
    }
}