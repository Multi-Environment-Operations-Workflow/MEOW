namespace MEOW.Components.Models;

public class MeowDevice(string name, Guid id)
{
    public string Name { get; set; } = name;
    public Guid Id { get; set; } = id;
}