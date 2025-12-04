using LoadBalancer.Domain.Models;

namespace LoadBalancer.Domain.Interfaces;

public interface IHealthMonitor : IDisposable
{
    IReadOnlyList<LBNode> GetAvailableNodes();
    void Initialize(IReadOnlyList<LBNode> initialNodes, INodeHealthChecker healthChecker);
    void StartMonitoring(int checkIntervalMs);
    void StopMonitoring();
}