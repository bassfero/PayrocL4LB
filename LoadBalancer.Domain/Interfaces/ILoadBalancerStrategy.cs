using LoadBalancer.Domain.Models;

namespace LoadBalancer.Domain.Interfaces;

public interface ILoadBalancerStrategy
{
    LBNode GetNextNode(IReadOnlyList<LBNode> availableNodes);
}