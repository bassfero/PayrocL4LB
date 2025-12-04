using System.Reflection;
using LoadBalancer.Core.Strategies;
using LoadBalancer.Domain.Models;

namespace Tests.Core;

public class RandomStrategyTests
{
    [Test]
    public void NextNode_Is_Selected_Based_On_Seed()
    {
        var nodes = new List<LBNode>
        {
            new LBNode("A", 1), 
            new LBNode("B", 2), 
            new LBNode("C", 3)
        }.AsReadOnly();
        
        var strategy = new RandomStrategy();
        const int testSeed = 42;
        
        //using reflection to get random field and force test seed
        var rngOfStrategy = typeof(RandomStrategy).GetField("_random", BindingFlags.NonPublic | BindingFlags.Instance);
        rngOfStrategy.SetValue(strategy, new Random(testSeed));
        
        var selectedNode = strategy.GetNextNode(nodes);
        int expectedIndex = new Random(testSeed).Next(0, nodes.Count);
        
        Assert.That(nodes[expectedIndex], Is.EqualTo(selectedNode));
    }
    
    [Test]
    public void GetNextNode_EmptyList_ReturnsNull()
    {
        var strategy = new RandomStrategy();
        var emptyList = new List<LBNode>().AsReadOnly();
        
        Assert.That(strategy.GetNextNode(emptyList), Is.Null);
    }
}