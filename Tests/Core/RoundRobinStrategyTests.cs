using LoadBalancer.Core.Strategies;
using LoadBalancer.Domain.Models;

namespace Tests.Core;

public class RoundRobinStrategyTests
{

    [Test]
    public void Multiple_Requests_Select_Correct_Node_In_Order()
    {
        var nodes = new List<LBNode>
        {
            new LBNode("A", 1),
            new LBNode("B", 2),
            new LBNode("C", 3)
        }.AsReadOnly();
        var strategy = new RoundRobinStrategy();
        
        // Cycle 1
        Assert.That(strategy.GetNextNode(nodes), Is.EqualTo(nodes[0])); 
        Assert.That(strategy.GetNextNode(nodes), Is.EqualTo(nodes[1])); 
        Assert.That(strategy.GetNextNode(nodes), Is.EqualTo(nodes[2]));
        // Cycle 2
        Assert.That(strategy.GetNextNode(nodes), Is.EqualTo(nodes[0])); 
    }
    
    [Test]
    public void GetNextNode_EmptyList_ReturnsNull()
    {
        var strategy = new RoundRobinStrategy();
        var emptyList = new List<LBNode>().AsReadOnly();
        
        Assert.That(strategy.GetNextNode(emptyList), Is.Null);
    }
    
}