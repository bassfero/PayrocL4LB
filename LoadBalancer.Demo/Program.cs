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

        await SimulateClientConnection(9502, "Data");
        for (int i = 0; i < 10; i++)
        {
            await SimulateClientConnection(helper.Config.LBListeningPort(), "Data");
        }

        loadBallancer.Stop();
        
        // Cleanup
        await StopDummyServices();
    }

    public static async Task<string> SimulateClientConnection(int port, string dataProbe)
    {
        var host = "127.0.0.1";
        Console.WriteLine($"\n--- Simulating Client Connection to {host}:{port} ---");

        try
        {
            using (var client = new TcpClient())
            {
                // Set reasonable timeouts for synchronous operations
                client.SendTimeout = 3000;
                client.ReceiveTimeout = 3000;

                // 1. Connect
                client.Connect(host, port);
                
                Console.WriteLine($"Connection successful to {host}:{port}.");

                using (NetworkStream stream = client.GetStream())
                {
                    // 2. Send Payload (A simple PING)
                    string payload = "GET / HTTP/1.1\r\nHost: test\r\nConnection: close\r\n\r\nPING";
                    byte[] data = Encoding.ASCII.GetBytes(payload);
                    stream.Write(data, 0, data.Length);
                    
                    Console.WriteLine($"Sent {data.Length} bytes.");

                    // 3. Perform Half-Close (Crucial step that mimics the Load Balancer)
                    // This signals the server that the client is done sending data, 
                    // but is still ready to receive.
                    client.Client.Shutdown(SocketShutdown.Send);
                    Console.WriteLine("Performed SocketShutdown.Send (Half-Close).");
                    
                    // 4. Read Response until the server closes the connection
                    var allStreamData = ReadAllStreamData(stream);
                    Console.WriteLine($"Received {allStreamData.Length} bytes, data: {allStreamData}");
                    return allStreamData;
                }
            }
        }
        catch (SocketException ex)
        {
            return $"Error connecting to or communicating with backend: {ex.Message}";
        }
        catch (IOException ex)
        {
            return $"IO Error during communication: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred: {ex.Message}";
        }
    }

    /// <summary>
    /// Reads all data from the stream until the server closes the connection (bytesRead == 0).
    /// </summary>
    private static string ReadAllStreamData(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int bytesRead;
        var responseBuilder = new StringBuilder();
        int totalBytes = 0;

        // Loop until Read returns 0 (server closed the connection) or timeout occurs
        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            responseBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            totalBytes += bytesRead;
        }

        Console.WriteLine($"Finished reading response. Total bytes received: {totalBytes}");
        return responseBuilder.ToString();
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
