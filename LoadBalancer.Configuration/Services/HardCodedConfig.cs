using LoadBalancer.Domain.Interfaces;
using LoadBalancer.Domain.Models;
using LoadBalancer.Domain.Shared;

namespace LoadBalancer.Configuration.Services;

public class HardCodedConfig : IConfiguration
{
    private readonly IReadOnlyList<LBNode> _nodes = new List<LBNode>
    {
        new LBNode("127.0.0.1", 9500),
        new LBNode("127.0.0.1", 9501),
        new LBNode("127.0.0.1", 9502)
    };

    public IReadOnlyList<LBNode> GetInitialListOfNodes()
    {
        return _nodes;
    }

    public LoadBalancingStrategyType GetStrategyType()
    {
        return LoadBalancingStrategyType.RoundRobin;
    }

    public int LBListeningPort()
    {
        return 8080;
    }

    public int HealthCheckInterval()
    {
        return 90000;
    }
}