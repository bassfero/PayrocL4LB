using LoadBalancer.Domain.Models;

namespace LoadBalancer.Domain.Interfaces;

public interface INodeHealthChecker
{
    bool IsNodeHealthy(LBNode node);
}