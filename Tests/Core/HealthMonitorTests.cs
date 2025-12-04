using LoadBalancer.Core.Services;
using LoadBalancer.Domain.Interfaces;
using LoadBalancer.Domain.Models;
using Moq;


namespace Tests.Core;

public class HealthMonitorTests
{
    private readonly LBNode _nodeA = new LBNode("A", 1);
    private readonly LBNode _nodeB = new LBNode("B", 2);
    private readonly LBNode _nodeC = new LBNode("C", 3);
    
    private readonly IList<LBNode> _initialNodes;

    public HealthMonitorTests()
    {
        _initialNodes = new List<LBNode> { _nodeA, _nodeB, _nodeC };
    }

    [Test]
    public void Initialize_SetsInitialAvailableNodes()
    {
        var mockChecker = new Mock<INodeHealthChecker>();

        mockChecker.Setup(c => c.IsNodeHealthy(_nodeA)).Returns(true);
        mockChecker.Setup(c => c.IsNodeHealthy(_nodeB)).Returns(false);
        mockChecker.Setup(c => c.IsNodeHealthy(_nodeC)).Returns(true);

        using var monitor = new HealthMonitor();
        monitor.Initialize(_initialNodes.AsReadOnly(), mockChecker.Object); 

        Assert.That(monitor.GetAvailableNodes().Contains(_nodeA),Is.True);
        Assert.That(monitor.GetAvailableNodes().Contains(_nodeB),Is.False);
        Assert.That(monitor.GetAvailableNodes().Contains(_nodeC),Is.True);

        Assert.That(monitor.GetAvailableNodes().Count(), Is.EqualTo(2));
    }

    [Test]
    public void StartMonitoring_UpdatesAvailableNodesPeriodically()
    {
        var mockChecker = new Mock<INodeHealthChecker>();

        mockChecker.Setup(c => c.IsNodeHealthy(_nodeA)).Returns(true);
        mockChecker.Setup(c => c.IsNodeHealthy(_nodeB)).Returns(false);
        mockChecker.Setup(c => c.IsNodeHealthy(_nodeC)).Returns(true);

        using var monitor = new HealthMonitor();
        monitor.Initialize(_initialNodes.AsReadOnly(), mockChecker.Object);

        Assert.That(monitor.GetAvailableNodes().Contains(_nodeA), Is.True);
        Assert.That(monitor.GetAvailableNodes().Contains(_nodeB), Is.False);
        Assert.That(monitor.GetAvailableNodes().Count(), Is.EqualTo(2));

        // Change the mock behavior for the next check interval: B goes up, A goes down
        mockChecker.Setup(c => c.IsNodeHealthy(_nodeA)).Returns(false);
        mockChecker.Setup(c => c.IsNodeHealthy(_nodeB)).Returns(true);

        monitor.StartMonitoring(100);
        Thread.Sleep(50); //possibly flaky test, lack of async/await feature make it hard here

        Assert.That(monitor.GetAvailableNodes().Contains(_nodeB), Is.True);
        Assert.That(monitor.GetAvailableNodes().Contains(_nodeA), Is.False);
        Assert.That(monitor.GetAvailableNodes().Count(), Is.EqualTo(2));
    }
}