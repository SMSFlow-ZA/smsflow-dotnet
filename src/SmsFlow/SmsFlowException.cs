using System.Net;

namespace SmsFlow;

public sealed class SmsFlowException : Exception
{
    public SmsFlowException(HttpStatusCode statusCode, string responseBody)
        : base($"SMSFlow API request failed with status {(int)statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }
}
