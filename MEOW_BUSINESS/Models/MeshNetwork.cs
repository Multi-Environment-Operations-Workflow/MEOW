using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MEOW_BUSINESS.Services;
using SVGCreator;
using Color = System.Drawing.Color;

namespace MEOW_BUSINESS.Models;

public class MeshNetwork
{
    List<MeshNetworkNode> Nodes { get; } = new();
    List<MeshNetworkConnection> Connections { get; } = new();

    public void AddNodesAndConnections(List<MeshNetworkNode> nodes, List<MeshNetworkConnection> connections)
    {
        Nodes.AddRange(nodes);
        Connections.AddRange(connections);
    }

    public async Task<string> ToSVG()
    {
        var document = new SvgDocument();
        document.SetWidth(800).SetHeight(800);

        // Step 1: Calculate positions
        CalculateNodePositions(800, 800);

        // Step 2: Draw connections first (so lines are behind nodes)
        DrawConnections(document);

        // Step 3: Draw nodes
        DrawNodes(document);

        return document.ToString();
    }

    private void CalculateNodePositions(int width, int height)
    {
        if (Nodes.Count == 0) return;

        const int topY = 60;
        const int verticalSpacing = 120;
        const int horizontalSpacing = 160;
        const int perLayer = 3;

        // Root node at top center
        Nodes[0].X = width / 2;
        Nodes[0].Y = topY;

        // Split remaining nodes into layers
        var layers = new List<List<MeshNetworkNode>>();
        for (int i = 1; i < Nodes.Count; i += perLayer)
        {
            layers.Add(Nodes.Skip(i).Take(perLayer).ToList());
        }

        // Position each layer
        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            var layer = layers[layerIndex];
            var y = topY + (layerIndex + 1) * verticalSpacing;

            var totalWidth = (layer.Count - 1) * horizontalSpacing;
            var startX = (width / 2) - totalWidth / 2;

            for (int i = 0; i < layer.Count; i++)
            {
                layer[i].X = startX + i * horizontalSpacing;
                layer[i].Y = y;
            }
        }
    }

    private void DrawNodes(SvgDocument document)
    {
        foreach (var node in Nodes)
        {
            document.AddCircle((node.X, node.Y), 25, Color.LightBlue, Color.Black, 2);
            document.AddText(node.Name, (node.X, node.Y), 12, "Arial", 0, Color.Black, Color.Black, 1);
        }
    }

    private void DrawConnections(SvgDocument document)
    {
        foreach (var conn in Connections)
        {
            var from = conn.From;
            var to = conn.To;

            // Simple straight line for now
            document.AddLine((from.X, from.Y), (to.X, to.Y), Color.Gray, 2);

            // Label with start / last
            var midX = (from.X + to.X) / 2;
            var midY = (from.Y + to.Y) / 2 - 12;
            document.AddText($"{conn.StartedConnection.ToString("HH:mm")} / {conn.LastConfirmed.ToString("HH:mm")}", (midX, midY), 10, "Arial", 0 ,Color.Black, Color.Black, 1);
        }
    }
}
