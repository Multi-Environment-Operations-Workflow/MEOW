namespace MEOW.Components.Models
{
    public class ConnectionPair
    {
        public string Name1 { get; set; } = "";
        public string Name2 { get; set; } = "";
        public string StartConnection { get; set; } = "";
        public DateTime LastConfirmed { get; set; }

        // Returnerer listen af nodes (uden duplikater)
        public static object ToNodes(IEnumerable<ConnectionPair> connections)
        {
            return connections
                .SelectMany(c => new[] { c.Name1, c.Name2 })
                .Distinct()
                .Select(n => new { label = n })
                .ToList();
        }

        // Returnerer listen af links
        public static object ToLinks(IEnumerable<ConnectionPair> connections)
        {
            return connections.Select(c => new {
                name1 = c.Name1,
                name2 = c.Name2,
                start = c.StartConnection,
                last = c.LastConfirmed.ToString("HH:mm")
            }).ToList();
        }
    }
}
