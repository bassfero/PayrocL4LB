using System.ComponentModel;

namespace LoadBalancer.Core.EAPSupport;

public class RequestForwardCompletedEventArgs : AsyncCompletedEventArgs
{
    public string ResponseBody { get; }

    public RequestForwardCompletedEventArgs(string responseBody, Exception error, bool cancelled, object userState)
        : base(error, cancelled, userState)
    {
        this.ResponseBody = responseBody;
    }
}