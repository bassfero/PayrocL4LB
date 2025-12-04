using LoadBalancer.Domain.Interfaces;
using LoadBalancer.Domain.Models;

namespace LoadBalancer.Core.Strategies;

public class RandomStrategy : ILoadBalancerStrategy
{
    private readonly Random _random = new Random();
    
    public LBNode GetNextNode(IReadOnlyList<LBNode> availableNodes)
    {
        if (availableNodes == null || availableNodes.Count == 0)
        {
            return null;
        }

        lock (_random)
        {
            int index = _random.Next(0, availableNodes.Count);
            return availableNodes[index];
        }
    }
}