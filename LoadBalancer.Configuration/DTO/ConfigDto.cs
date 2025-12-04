namespace LoadBalancer.Configuration.DTO;

public class ConfigDto
{
    public int? ListeningPort {get; set;}
    public int? HealthCheckInterval { get; set; }
    public string Strategy { get; set; }
    public List<NodeDto> Nodes { get; set; }
}