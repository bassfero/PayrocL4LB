using LoadBalancer.Domain.Models;
using LoadBalancer.Domain.Interfaces;

namespace LoadBalancer.Core.Services;

public class HealthMonitor : IHealthMonitor
{
    private INodeHealthChecker _healthChecker;
    private IReadOnlyList<LBNode> _allNodes;
    private IList<LBNode> _availableNodes;
    
    private readonly object _lock = new object();
    
    private Timer _timer;
    private bool _isDisposed = false;

    public IReadOnlyList<LBNode> GetAvailableNodes()
    {
        return _availableNodes.AsReadOnly();
    }

    public void Initialize(IReadOnlyList<LBNode> initialNodes, INodeHealthChecker healthChecker)
    {
        if (initialNodes == null) throw new ArgumentNullException(nameof(initialNodes));
        _allNodes = initialNodes;
        
        if(healthChecker == null) throw new ArgumentNullException(nameof(healthChecker));
        _healthChecker = healthChecker;
        
        CheckAllNodes(null);
    }

    public void StartMonitoring(int checkIntervalMs)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(HealthMonitor));
        if (checkIntervalMs <= 0) throw new ArgumentOutOfRangeException(nameof(checkIntervalMs));
        
        _timer = new Timer(CheckAllNodes, null, 0, checkIntervalMs);
    }

    public void StopMonitoring()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void CheckAllNodes(object? state)
    {
        var newlyAvailable = new List<LBNode>();

        foreach (var node in _allNodes)
        {
            if (_healthChecker.IsNodeHealthy(node))
            {
                newlyAvailable.Add(node);
            }
        }

        lock (_lock)
        {
            _availableNodes = newlyAvailable.AsReadOnly();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        StopMonitoring();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}