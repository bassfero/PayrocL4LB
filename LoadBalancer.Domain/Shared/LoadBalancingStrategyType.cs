namespace LoadBalancer.Domain.Shared;

public enum LoadBalancingStrategyType
{
    RoundRobin,
    Random,
    LeastConnections
}