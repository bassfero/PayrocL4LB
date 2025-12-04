using System.Net;
using System.Net.Sockets;
using System.Text;
using LoadBalancer.Core.EAPSupport;
using LoadBalancer.Domain.Interfaces;

namespace LoadBalancer.Core;

public class LoadBalancerEAP : ILoadBalancer
{
    private readonly IConfiguration _config;
    private readonly IHealthMonitor _monitor;
    private readonly ILoadBalancerStrategy _strategy;
    private readonly IRequestForwarder _forwarder;
    private readonly int _listenPort;
    private readonly int _healthCheckInterval;
    
    private Socket _listenerSocket;
    private bool _isListening = false;

    public LoadBalancerEAP(
        IConfiguration config, 
        IHealthMonitor monitor, 
        ILoadBalancerStrategy strategy)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _forwarder = new TcpRequestForwarder();
        
        _forwarder.ForwardRequestCompleted += Forwarder_ForwardRequestCompleted;

        _listenPort = config.LBListeningPort();
        _healthCheckInterval = config.HealthCheckInterval();
    }
    
    public void Start()
    {
        _monitor.StartMonitoring(_healthCheckInterval);

        StartListener(_listenPort);
    }
    
    public void Stop()
    {
        _monitor.StopMonitoring();
        _isListening = false;
        try
        {
            _listenerSocket?.Close(); 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing listener socket: {ex.Message}");
        }
        Console.WriteLine("Load Balancer EAP stopped.");
    }
    
    private void StartListener(int listenPort)
    {
        _listenerSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
        {
            DualMode = true
        };
        //_listenerSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        
        // Bind to all network interfaces on the specified port
        _listenerSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, listenPort));
        
        // Start listening (backlog of 100)
        _listenerSocket.Listen(100);
        _isListening = true;
        
        Console.WriteLine($"Load Balancer listening on port {listenPort}.");

        // Begin the non-blocking accept loop (APM pattern)
        _listenerSocket.BeginAccept(new AsyncCallback(AcceptCallback), _listenerSocket);
    }
    
    private void AcceptCallback(IAsyncResult ar)
    {
        if (!_isListening) return;

        Socket listener = (Socket)ar.AsyncState;
        Socket clientSocket = null;
        
        try
        {
            clientSocket = listener.EndAccept(ar); 

            // IMPORTANT: Start the next accept immediately to ensure the listener is always ready.
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

            // 1. Determine a temporary target node and create the RequestState object.
            // We use a temporary node just to create the state object, as the real routing happens later.
            // In a real implementation, you would need complex HTTP parsing to know the target.
            // Here, we just choose an arbitrary node for RequestState construction.
            var tempNode = _strategy.GetNextNode(_monitor.GetAvailableNodes());
            if (tempNode == null)
            {
                // If no nodes are even registered, close socket and log error.
                Console.WriteLine("Routing Failed (Pre-Receive): No nodes registered.");
                clientSocket.Close();
                return;
            }
            
            // 2. Create RequestState and start receiving the HTTP request data from the client
            var requestState = new RequestState(tempNode, clientSocket);

            clientSocket.BeginReceive(requestState.Buffer, 0, RequestState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveRequestCallback), requestState);

        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting client connection: {ex.Message}");
            clientSocket?.Close();
        }
    }
    
    private void ReceiveRequestCallback(IAsyncResult ar)
    {
        var requestState = (RequestState)ar.AsyncState;
        Socket client = requestState.ClientSocket;

        try
        {
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // Append the received chunk to the ResponseBuffer (which we'll reuse for the inbound request text)
                requestState.ResponseBuffer.Append(Encoding.ASCII.GetString(requestState.Buffer, 0, bytesRead));
                
                // Check if we have received the end of the HTTP headers (\r\n\r\n) or if we need to continue reading.
                // For simplicity here, we assume a complete read will be performed or look for a quick end of request.
                
                if (requestState.ResponseBuffer.ToString().Contains("\r\n\r\n"))
                {
                    // Full request headers received. Stop receiving and proceed to routing.
                    RouteAndForwardRequest(requestState);
                }
                else
                {
                    // Continue reading the request data
                    client.BeginReceive(requestState.Buffer, 0, RequestState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveRequestCallback), requestState);
                }
            }
            else
            {
                // Client closed the connection gracefully before sending data.
                client.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving request from client {requestState.RequestId}: {ex.Message}");
            client.Close();
        }
    }
    
    public void RouteAndForwardRequest(RequestState requestState)
    {
        // For simplicity, we skip parsing the HTTP request body and assume the full request 
        // is now contained in requestState.ResponseBuffer.
        
        var availableNodes = _monitor.GetAvailableNodes();

        if (availableNodes.Count == 0)
        {
            Console.WriteLine("EAP Routing Failed: No healthy nodes available.");
            requestState.ClientSocket.Close();
            return;
        }

        var targetNode = _strategy.GetNextNode(availableNodes);

        if (targetNode == null)
        {
            Console.WriteLine("EAP Routing Failed: Strategy returned null.");
            requestState.ClientSocket.Close();
            return;
        }
        
        // Final routing decision updates the TargetNode in the RequestState
        requestState.TargetNode = targetNode;
        
        Console.WriteLine($"EAP Starting forward for Request ID {requestState.RequestId} to: {targetNode}");
        _forwarder.ForwardRequestAsync(targetNode, requestState);
    }
    
    private void Forwarder_ForwardRequestCompleted(object sender, RequestForwardCompletedEventArgs e)
    {
        var requestState = (RequestState)e.UserState;
        
        Console.WriteLine($"EAP Continuation triggered for Request ID {requestState.RequestId}.");

        Socket client = requestState.ClientSocket; // Retrieve the original client socket

        if (e.Error != null)
        {
            Console.WriteLine($"Error forwarding Request {requestState.RequestId} to {requestState.TargetNode}: {e.Error.Message}. Closing client connection.");
            client.Close();
            return;
        }

        if (e.Cancelled)
        {
            Console.WriteLine($"Request {requestState.RequestId} was cancelled. Closing client connection.");
            client.Close();
            return;
        }

        // --- Send final response back to client ---
        string response = e.ResponseBody;
        
        // NOTE: Assuming the response from the backend is already a complete HTTP string (headers + body)
        byte[] responseBytes = Encoding.ASCII.GetBytes(response); 
        
        client.BeginSend(responseBytes, 0, responseBytes.Length, SocketFlags.None, new AsyncCallback(SendResponseCallback), requestState);

        TimeSpan duration = DateTime.UtcNow - requestState.StartTime;
        Console.WriteLine($"Request {requestState.RequestId} handled successfully by {requestState.TargetNode} in {duration.TotalMilliseconds:F2}ms. Response snippet: {response.Substring(0, Math.Min(response.Length, 80))}");
    }
    
    private void SendResponseCallback(IAsyncResult ar)
    {
        var requestState = (RequestState)ar.AsyncState;
        Socket client = requestState.ClientSocket;
        
        try
        {
            client.EndSend(ar);
        }
        catch (Exception ex)
        {
            // Log error if client disconnects while sending
            Console.WriteLine($"Error sending response back to client {requestState.RequestId}: {ex.Message}");
        }
        finally
        {
            // Crucial: Close the client connection after the response is fully sent (as per "Connection: close" header)
            client.Close();
        }
    }
}