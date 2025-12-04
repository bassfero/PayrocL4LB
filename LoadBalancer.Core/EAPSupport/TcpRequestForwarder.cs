using System.Net;
using System.Net.Sockets;
using System.Text;
using LoadBalancer.Domain.Models;

namespace LoadBalancer.Core.EAPSupport;

public class TcpRequestForwarder : IRequestForwarder
{
    public event EventHandler<RequestForwardCompletedEventArgs> ForwardRequestCompleted;
    private const int SocketTimeoutMs = 5000;

    public void ForwardRequestAsync(LBNode targetNode, object userToken)
    {
        var state = (RequestState)userToken; // UserToken is now RequestState object
        state.WorkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Use the IPEndPoint for the target node
        IPEndPoint remoteEP;
        try
        {
            var hostEntry = Dns.GetHostEntry(targetNode.Host);
            if (hostEntry.AddressList.Length == 0)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }
            var ipv4Address = hostEntry.AddressList
                .First(a => a.AddressFamily == AddressFamily.InterNetwork);

            remoteEP = new IPEndPoint(ipv4Address, (int)targetNode.Port);
        }
        catch (Exception ex)
        {
            // Fail fast if DNS resolution fails
            OnForwardRequestCompleted(new RequestForwardCompletedEventArgs(
                null, 
                new SocketException((int)SocketError.HostNotFound, $"Failed to resolve host {targetNode.Host}: {ex.Message}"), 
                false, 
                userToken));
            return;
        }

        // Store the user token (the original request context) in the IAsyncResult state object
        state.WorkSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), state);

        // NOTE: The thread returns immediately here. The connection will happen in the background.
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        var state = (RequestState)ar.AsyncState;
        Socket client = state.WorkSocket;
        
        try
        {
            // Finish the connection attempt
            client.EndConnect(ar);

            if (!client.Connected)
            {
                throw new SocketException((int)SocketError.ConnectionRefused);
            }

            // Connection successful: Now send the HTTP request
            Send(client, state);
        }
        catch (Exception ex)
        {
            // Connection failed
            client.Close();
            OnForwardRequestCompleted(new RequestForwardCompletedEventArgs(
                null, 
                new HttpRequestException($"Connection failed to {state.TargetNode}: {ex.Message}", ex), 
                false, 
                state));
        }
    }

    private void Send(Socket client, RequestState state)
    {
        // 1. Construct the minimal HTTP GET request
        string httpRequest = $"GET / HTTP/1.1\r\nHost: {state.TargetNode.Host}:{state.TargetNode.Port}\r\nConnection: close\r\n\r\n";
        byte[] byteData = Encoding.ASCII.GetBytes(httpRequest);

        // 2. Begin sending the data (non-blocking)
        client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), state);
    }

    private void SendCallback(IAsyncResult ar)
    {
        var state = (RequestState)ar.AsyncState;
        Socket client = state.WorkSocket;

        try
        {
            // Finish the sending operation and get the number of bytes sent
            int bytesSent = client.EndSend(ar);

            // 3. Begin receiving the response (non-blocking)
            client.BeginReceive(state.Buffer, 0, RequestState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception ex)
        {
            // Send failed
            client.Close();
            OnForwardRequestCompleted(new RequestForwardCompletedEventArgs(
                null, 
                new HttpRequestException($"Send failed to {state.TargetNode}: {ex.Message}", ex), 
                false, 
                state));
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        var state = (RequestState)ar.AsyncState;
        Socket client = state.WorkSocket;

        try
        {
            // Finish the receiving operation and get the number of bytes received
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // Append received data to the response buffer
                state.ResponseBuffer.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));

                // Continue reading until the socket is closed by the server (end of response)
                client.BeginReceive(state.Buffer, 0, RequestState.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
            }
            else
            {
                // 4. Response complete: Disconnect, close socket, and raise event
                client.Shutdown(SocketShutdown.Both);
                client.Close();

                OnForwardRequestCompleted(new RequestForwardCompletedEventArgs(
                    state.ResponseBuffer.ToString(), 
                    null, 
                    false, 
                    state));
            }
        }
        catch (Exception ex)
        {
            // Receive failed
            client.Close();
            OnForwardRequestCompleted(new RequestForwardCompletedEventArgs(
                null, 
                new HttpRequestException($"Receive failed from {state.TargetNode}: {ex.Message}", ex), 
                false, 
                state));
        }
    }

    protected virtual void OnForwardRequestCompleted(RequestForwardCompletedEventArgs e)
    {
        ForwardRequestCompleted?.Invoke(this, e);
    }
}