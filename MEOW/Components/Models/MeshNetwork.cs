using Aspose.Svg;
using Aspose.Svg.Builder;
using MEOW.Components.Services;

namespace MEOW.Components.Models;

public class MeshNetwork(IBrowserDimensionService browserDimensionService)
{
    public List<MeshNetworkNode> Nodes { get; private set; } = new();
    
    public List<MeshNetworkConnection> Connections { get; private set; } = new();

    
    public void AddNode(MeshNetworkNode node, List<MeshNetworkConnection>? connections)
    {
        Nodes.Add(node);
        
        if (connections == null) return;
        
        if(!connections.Any(c => !c.From.Equals(node) && !c.To.Equals(node))) throw new Exception("Connections must be related to the node being added.");
        
        foreach (var connection in connections)
        {
            Connections.Add(connection);
        }
    }
    
    public void AddConnection(MeshNetworkConnection connection)
    {
        Connections.Add(connection);
    }
    
    public void AddNodesAndConnections(List<MeshNetworkNode> nodes, List<MeshNetworkConnection> connections)
    {
        Nodes.AddRange(nodes);
        Connections.AddRange(connections);
    }

    public async Task<string> ToSVG()
    {
        var document = new SVGDocument();
        var dimensions = await browserDimensionService.GetBrowserDimensions();
        var svgElementBuilder = new SVGSVGElementBuilder()
            .Width(dimensions.Width)
            .Height(dimensions.Height)
            .ViewBox(0, 0, dimensions.Width, dimensions.Height);
        svgElementBuilder = DrawNodes(svgElementBuilder);
        svgElementBuilder.Build(document.FirstChild as SVGSVGElement);
        return 
    }

    private SVGSVGElementBuilder DrawNodes(SVGSVGElementBuilder svgElementBuilder)
    {
        foreach (var node in Nodes)
        {
            svgElementBuilder.AddCircle(cx: node.X, cy: node.Y, r: 25, fill: "#1976d2", stroke: "black");
            svgElementBuilder.AddText(x: node.X, y: node.Y + 4, fontSize: 12, fill: "white", content: String.Concat(node.Id  + ": ", node.Name));
        }
        
        return svgElementBuilder;
    }
    
}