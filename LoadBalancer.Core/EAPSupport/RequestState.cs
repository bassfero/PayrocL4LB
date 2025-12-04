using System.Net.Sockets;
using System.Text;
using LoadBalancer.Domain.Models;

namespace LoadBalancer.Core.EAPSupport;

public class RequestState
{
    public LBNode TargetNode { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public Guid RequestId { get; } = Guid.NewGuid(); 
    
    // APM/TCP specific properties:
    public Socket WorkSocket { get; set; }
    public Socket ClientSocket { get; set; }
    public const int BufferSize = 1024;
    public byte[] Buffer { get; } = new byte[BufferSize];
    public StringBuilder ResponseBuffer { get; } = new StringBuilder();

    public RequestState(LBNode targetNode, Socket clientSocket)
    {
        TargetNode = targetNode;
        ClientSocket = clientSocket;
    }
}