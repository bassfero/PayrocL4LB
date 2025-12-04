using LoadBalancer.Domain.Models;
using LoadBalancer.Domain.Shared;

namespace LoadBalancer.Domain.Interfaces;

public interface IConfiguration
{
    IReadOnlyList<LBNode> GetInitialListOfNodes();
    LoadBalancingStrategyType GetStrategyType();
    int LBListeningPort();
    int HealthCheckInterval();
}