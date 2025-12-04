using System.Net;
using System.Net.Sockets;
using System.Text;
using LoadBalancer.Domain.Models;

namespace LoadBalancer.DummyService;

public class DummyService
{
    private readonly int _port;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private Task _listenerTask;
    private int _connectionCount = 0;
    private Random _random = new Random();

    private readonly string _logMessage;

    public DummyService(int port)
    {
        IPAddress ipAddress = IPAddress.Any;
        _port = port;
        _logMessage = $"[Dummy Service port: {port}] ";
        
        try
        {
            _listener = new TcpListener(IPAddress.Loopback, _port);
            Console.WriteLine($"{_logMessage} Initialized listener");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_logMessage} Failed to initialize TCP listener: {ex.Message}");
            throw;
        }
    }
    
    public Task StartAsync()
    {
        if (_listenerTask != null && !_listenerTask.IsCompleted)
        {
            return Task.CompletedTask;
        }

        _listener.Start();
        _listenerTask = Task.Run(() => ListenerLoop(_cts.Token), _cts.Token);
        Console.WriteLine($"{_logMessage} Started listening.");
        return Task.CompletedTask;
    }
    
    public async Task StopAsync()
    {
        if (_listenerTask == null || _listenerTask.IsCompleted)
        {
            return;
        }

        _cts.Cancel();
        _listener.Stop();
        
        try
        {
            await _listenerTask;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"{_logMessage} Was cancelled");
        }
        Console.WriteLine($"{_logMessage} Listener stopped.");
    }
    
    private async Task ListenerLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                _ = HandleConnectionAsync(client, token); 
            }
            catch (OperationCanceledException)
            {
                break; 
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    Console.WriteLine($"{_logMessage} Error accepting client: {ex.Message}");
                    await Task.Delay(100, token); 
                }
            }
        }
    }
    
    private async Task HandleConnectionAsync(TcpClient client, CancellationToken token)
    {
        int currentCount = Interlocked.Increment(ref _connectionCount);
        string clientEndpoint = client.Client.RemoteEndPoint.ToString();
        Console.WriteLine($"{_logMessage} Connection accepted from {clientEndpoint}. Total: {currentCount}");

        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                // 1. Read request data from LB
                byte[] readBuffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(readBuffer.AsMemory(), token);

                string requestData = bytesRead > 0
                    ? Encoding.ASCII.GetString(readBuffer, 0, bytesRead)
                    : "";
                if (!string.IsNullOrEmpty(requestData))
                {
                    Console.WriteLine($"[DummyService {_port}] Received: {requestData.Trim().Split('\r')[0]}...");
                }

                // 2. Simulate some processing
                await Task.Delay(new Random().Next(50, 150), token);

                // 3. Build HTTP response
                string body = $"<h1>Response from {_port}</h1>" +
                              $"<p>Connection Index: {currentCount}</p>" +
                              $"<p>Time: {DateTime.Now:HH:mm:ss.fff}</p>";

                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

                string headers = $"HTTP/1.1 200 OK\r\n" +
                                 $"Content-Type: text/html; charset=utf-8\r\n" +
                                 $"Content-Length: {bodyBytes.Length}\r\n" +
                                 $"Connection: close\r\n\r\n";

                byte[] headerBytes = Encoding.ASCII.GetBytes(headers);

                // 4. Send complete response
                await stream.WriteAsync(headerBytes.AsMemory(), token);
                await stream.WriteAsync(bodyBytes.AsMemory(), token);
                await stream.FlushAsync(token); // ensure LB sees EOF

                // 5. Explicitly close the connection
                client.Close();
            }
        }
        catch (OperationCanceledException)
        {
            // expected if cancellation token triggered
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_logMessage} Error handling connection from {clientEndpoint}: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref _connectionCount);
            Console.WriteLine($"{_logMessage} Connection closed. Active: {_connectionCount}");
        }
    }
}
