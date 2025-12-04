using LoadBalancer.Domain.Models;

namespace LoadBalancer.Core.EAPSupport;

public interface IRequestForwarder
{
    void ForwardRequestAsync(LBNode targetNode, object userToken);

    event EventHandler<RequestForwardCompletedEventArgs> ForwardRequestCompleted;
}