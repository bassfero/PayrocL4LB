using System.Net.Sockets;
using LoadBalancer.Domain.Interfaces;
using LoadBalancer.Domain.Models;

namespace LoadBalancer.Core.Services;

public class TcpHealthChecker : INodeHealthChecker
{
    private const int TimeoutMs = 500;
    
    public bool IsNodeHealthy(LBNode node)
    {
        using var client = new TcpClient();
        
        try
        {
            client.SendTimeout = TimeoutMs;
            client.ReceiveTimeout = TimeoutMs;
            client.Connect(node.Host, (int)node.Port);
            
            return true;
        }
        catch (Exception ex)
        {
            // Catch other general exceptions
            Console.WriteLine($"Health check failed for {node}: {ex.Message}");
            return false;
        }
    }
}