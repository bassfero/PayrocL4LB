using LoadBalancer.Configuration.Services;
using LoadBalancer.Domain.Shared;

namespace Tests.Configuration;

public class JsonBasedConfigTests
{
    [Test]
    public void Constructor_Fails_if_json_not_provided()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonBasedConfig(null));
    }
    
    [Test]
    public void GetInitialListOfNodes_ValidJson_ReturnsCorrectNodes()
    {
        const string json = @"{
            ""Strategy"": ""Random"",
            ""Nodes"": [
                { ""host"": ""10.0.0.1"", ""port"": 80 },
                { ""host"": ""10.0.0.2"", ""port"": 81 }
            ]
        }";
        var config = new JsonBasedConfig(json);
        var nodes = config.GetInitialListOfNodes();
        
        Assert.That(nodes.Count, Is.EqualTo(2));
    }
    
    [Test]
    public void GetInitialListOfNodes_MissingNodes_ReturnsEmptyList()
    {
        const string json = @"{ ""Strategy"": ""RoundRobin"" }";
        var config = new JsonBasedConfig(json);
        var nodes = config.GetInitialListOfNodes();
        Assert. That(nodes, Is.Empty);
    }

    [Test]
    public void GetStrategyType_ValidStrategy_ReturnsCorrectEnum()
    {
        const string json = @"{ ""Strategy"": ""LeastConnections"", ""Nodes"": [] }";
        var config = new JsonBasedConfig(json);
        Assert.That(config.GetStrategyType(), Is.EqualTo(LoadBalancingStrategyType.LeastConnections));
    }
}