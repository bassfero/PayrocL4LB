using LoadBalancer.Configuration.Services;
using LoadBalancer.Core;
using LoadBalancer.Core.Services;
using LoadBalancer.Core.Strategies;
using LoadBalancer.Domain.Interfaces;
using LoadBalancer.Domain.Shared;

namespace LoadBalancer.Demo;

public class InitialiserHelper
{
    public IConfiguration Config { get; set; }
    public IHealthMonitor HealthMonitor { get; set; }
    public ILoadBalancerStrategy LbStrategy { get; set; }
    public INodeHealthChecker NodeHealthChecker { get; set; }
    

    public InitialiserHelper(string jsonFilePath)
    {
        try
        {
            Config = new JsonBasedConfig(File.ReadAllText(jsonFilePath));
        }
        catch (Exception e)
        {
            Config = new HardCodedConfig();
        }

        NodeHealthChecker = new TcpHealthChecker();
        HealthMonitor = new HealthMonitor();
        
        if (Config.GetStrategyType() == LoadBalancingStrategyType.Random)
        {
            LbStrategy = new RandomStrategy();
        }
        else
        {
            LbStrategy = new RoundRobinStrategy();
        }
    }

    public LoadBalancerEAP CreateLoadBalancer()
    {
        HealthMonitor.Initialize(Config.GetInitialListOfNodes(), NodeHealthChecker);
        return new LoadBalancerEAP(Config, HealthMonitor, LbStrategy);
    }
}