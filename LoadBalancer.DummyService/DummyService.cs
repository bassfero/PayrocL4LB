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
                string welcomeMessage = $"{_logMessage}. You are connection #{currentCount}.\r\n";
                byte[] buffer = Encoding.ASCII.GetBytes(welcomeMessage);
                await stream.WriteAsync(buffer.AsMemory(), token);
                Console.WriteLine($"{_logMessage} Request received from {clientEndpoint}.");
                Task.Delay(_random.Next(1000, 5000), token).Wait();
            }
        }
        catch (OperationCanceledException)
        {
            // Task canceled, do nothing
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