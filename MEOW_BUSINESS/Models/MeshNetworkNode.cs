using System;

namespace MEOW_BUSINESS.Models;

public class MeshNetworkNode(int id, String name)
{
    public int Id { get; set; } = id;
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public String Name { get; set; } = name;
}