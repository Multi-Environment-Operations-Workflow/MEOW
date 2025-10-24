namespace MEOW.Components.Models
{
    public class MeshNetworkConnection(MeshNetworkNode from, MeshNetworkNode to, DateTime startedConnection, DateTime lastConfirmed)
    {
        public MeshNetworkNode From { get; private set; } = from;
        public MeshNetworkNode To { get; private set; } = to;
        public DateTime StartedConnection { get; private set; } = startedConnection;
        public DateTime LastConfirmed { get; set; } = lastConfirmed;
    }
}
