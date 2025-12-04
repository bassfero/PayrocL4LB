using System.Net.Sockets;
using System.Text;
using LoadBalancer.Domain.Interfaces;
using LoadBalancer.Domain.Models;

namespace LoadBalancer.Core.Services;

public class TcpHealthChecker : INodeHealthChecker
{
    private const int TimeoutMs = 1500; 
    
    public bool IsNodeHealthy(LBNode node)
    {
        // Use a new TcpClient for each check
        using var client = new TcpClient();
        
        try
        {
            client.SendTimeout = TimeoutMs;
            client.ReceiveTimeout = TimeoutMs;
            
            // 1. Connect (Synchronous, blocking call)
            var connectTask = client.ConnectAsync(node.Host, (int)node.Port);
            if (!connectTask.Wait(TimeoutMs))
            {
                // Connection timed out
                Console.WriteLine($"Health check failed for {node}: Connection timed out after {TimeoutMs}ms.");
                client.Close();
                return false;
            }

            using (var stream = client.GetStream())
            {
                string healthRequest = "HEAD / HTTP/1.1\r\nHost: healthcheck\r\nConnection: close\r\n\r\n";
                byte[] data = Encoding.ASCII.GetBytes(healthRequest);
                stream.Write(data, 0, data.Length);

                client.Client.Shutdown(SocketShutdown.Send);
                
                byte[] buffer = new byte[1024];
                int bytesRead;
                
                // Read until the server closes the connection (Read returns 0)
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Response received, keep reading until EOF
                }
                
                // If we reached here, the full transaction completed successfully.
                return true;
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
        {
            // Explicitly handle Connection Refused
            Console.WriteLine($"Health check failed for {node}: Connection actively refused.");
            return false;
        }
        catch (Exception ex)
        {
            // Handle timeouts, DNS failures, and other general exceptions
            Console.WriteLine($"Health check failed for {node}: {ex.Message}");
            return false;
        }
    }
}
