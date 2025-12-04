namespace LoadBalancer.Domain.Models;

public class LBNode
{
    public string Host { get; init; }
    public int Port { get; init; }
    public int ActiveConnections { get; set; }

    public LBNode(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host cannot be null or whitespace.", nameof(host));
        
        if (port <= 0)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be greater than zero.");
        
        Host = host;
        Port = port;
        ActiveConnections = 0;
    }
    
    public override bool Equals(object obj)
    {
        return obj is LBNode node &&
               Host.Equals(node.Host, StringComparison.OrdinalIgnoreCase) && 
               Port == node.Port;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Host.ToLowerInvariant(), Port); 
    }

    public override string ToString() => $"{Host}:{Port}";
    
}