using LoadBalancer.Domain.Models;

namespace Tests.Domain;

public class LBNodeTests
{
    private LBNode _testNode;

    [SetUp]
    public void Setup()
    {
        _testNode = new LBNode("192.168.1.1", 8080);
    }

    [Test]
    public void Constructor_fails_if_host_is_null()
    {
        Assert.Throws<ArgumentException>(() => new LBNode(null, 2));
    }
    
    [Test]
    public void Constructor_fails_if_host_is_empty()
    {
        Assert.Throws<ArgumentException>(() => new LBNode(string.Empty, 2));
    }
    
    [Test]
    public void Constructor_fails_if_port_is_zero_or_less()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LBNode("127.0.0.1", -2));
    }

    [Test]
    public void Nodes_Equals_If_Host_and_port_are_equal()
    {
        var nodeA = new LBNode("someHOST", 8080);
        var nodeB = new LBNode("SOMEhost", 8080);
        Assert.That(nodeA, Is.EqualTo(nodeB));
        Assert.That(nodeA.Equals(nodeB), Is.True);
    }
    
    [Test]
    public void Nodes_Not_Equals_If_Host_and_port_are_Not_equal()
    {
        var nodeA = new LBNode("127.0.0.1", 8080);
        var nodeB = new LBNode("127.0.0.1", 8081);
        Assert.That(nodeA, Is.Not.EqualTo(nodeB));
        Assert.That(nodeA.Equals(nodeB), Is.False);
    }
}