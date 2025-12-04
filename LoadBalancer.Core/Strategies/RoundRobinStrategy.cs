using LoadBalancer.Domain.Interfaces;
using LoadBalancer.Domain.Models;

namespace LoadBalancer.Core.Strategies;

public class RoundRobinStrategy : ILoadBalancerStrategy
{
    private readonly object _lock = new object();
    private int _currentIndex = -1; 
    
    public LBNode GetNextNode(IReadOnlyList<LBNode> availableNodes)
    {
        if (availableNodes == null || availableNodes.Count == 0)
        {
            return null;
        }

        int selectedNodeIndex;
        lock (_lock)
        {
            _currentIndex++;
            _currentIndex = _currentIndex % availableNodes.Count;
            
            selectedNodeIndex = _currentIndex;
        }
        return availableNodes[selectedNodeIndex];
    }
}