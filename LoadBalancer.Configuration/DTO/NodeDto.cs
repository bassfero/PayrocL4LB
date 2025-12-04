using LoadBalancer.Domain.Models;

namespace LoadBalancer.Configuration.DTO;

public class NodeDto
{
    public string Host { get; set; }
    public uint Port { get; set; }
    
    public LBNode ToLBNode() => new LBNode(Host, (int)Port);
}