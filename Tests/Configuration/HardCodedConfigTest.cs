using LoadBalancer.Configuration.Services;
using LoadBalancer.Domain.Shared;

namespace Tests.Configuration;

public class HardCodedConfigTest
{
    [Test]
    public void HardCoded_config_returns_something()
    {
        var config = new HardCodedConfig();
        var nodes = config.GetInitialListOfNodes();
        
        Assert.That(nodes, Is.Not.Null);
        Assert.That(nodes.Count, Is.Not.Zero);
    }
}