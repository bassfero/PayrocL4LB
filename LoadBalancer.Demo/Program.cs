using System.Net;
using System.Net.Sockets;
using System.Text;
using LoadBalancer.Configuration.Services;
using LoadBalancer.Domain.Interfaces;
using LoadBalancer.DummyService;
using LoadBalancer.Core;
using LoadBalancer.Core.Services;
using LoadBalancer.Core.Strategies;
using LoadBalancer.Demo;

public class Program
{
    private static readonly CancellationTokenSource GlobalCts = new CancellationTokenSource();
    
    private static List<DummyService> _dummyServices = new List<DummyService>();
    

    public static async Task Main(string[] args)
    {
        string? configFilePath = args.Where(arg => arg.StartsWith("-configFile=", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        var helper = new InitialiserHelper(configFilePath);
        await StartDummyServices(helper.Config);
        var loadBallancer = helper.CreateLoadBalancer(); 
        
        loadBallancer.Start();

        for (int i = 0; i < 10; i++)
        {
            await SimulateClientConnection(helper.Config.LBListeningPort(), "Data");
        }
        
        loadBallancer.Stop();
        
        // Cleanup
        await StopDummyServices();
    }

    /// <summary>
    /// Simulates a simple TCP client connecting to a backend node, sending data, and receiving a response.
    /// </summary>
    private static async Task 
        SimulateClientConnection(int port, string probeData)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[Client] Attempting connection to {port}...");
        Console.ResetColor();

        try
        {
            using (var client = new TcpClient())
            {
                // Set a timeout for the connection attempt
                var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
                if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
                {
                    throw new TimeoutException($"Connection attempt to {port} timed out.");
                }
                
                using (var stream = client.GetStream())
                {
                    // 1. Send test data
                    byte[] sendBuffer = Encoding.ASCII.GetBytes(probeData + "\r\n");
                    await stream.WriteAsync(sendBuffer.AsMemory());
                    
                    // 2. Read the response/greeting
                    byte[] readBuffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(readBuffer.AsMemory());

                    if (bytesRead > 0)
                    {
                        string response = Encoding.ASCII.GetString(readBuffer, 0, bytesRead);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[Client] SUCCESS: Received response from {port}:\n{response.Trim()}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[Client] WARNING: Connection to {port} closed without a response.");
                        Console.ResetColor();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Client] FAILURE: Could not connect to {port}. Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task StartDummyServices(IConfiguration configuration)
    {
        foreach (var node in configuration.GetInitialListOfNodes())
        {
            var ds = new DummyService(node.Port);
            _dummyServices.Add(ds);
            await ds.StartAsync();
        }
    }

    private static async Task StopDummyServices()
    {
        foreach (var ds in _dummyServices)
        {
            await ds.StopAsync();
        }
    }
    
}
