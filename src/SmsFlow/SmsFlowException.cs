using System.Net;

namespace SmsFlow;

public class SmsFlowException : Exception
{
    public SmsFlowException(HttpStatusCode statusCode, string responseBody, string? errorCode = null, bool retryable = false)
        : base($"SMSFlow API request failed with status {(int)statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        ErrorCode = errorCode ?? "SMSFLOW_ERROR";
        Retryable = retryable;
    }

    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }
    public string ErrorCode { get; }
    public bool Retryable { get; }
}

public sealed class SmsFlowAuthenticationException : SmsFlowException
{
    public SmsFlowAuthenticationException(HttpStatusCode statusCode, string responseBody, string? errorCode = null)
        : base(statusCode, responseBody, errorCode ?? "AUTHENTICATION_FAILED")
    {
    }
}

public sealed class SmsFlowValidationException : SmsFlowException
{
    public SmsFlowValidationException(HttpStatusCode statusCode, string responseBody, string? errorCode = null, bool retryable = false)
        : base(statusCode, responseBody, errorCode ?? "VALIDATION_FAILED", retryable)
    {
    }
}

public sealed class SmsFlowServerException : SmsFlowException
{
    public SmsFlowServerException(HttpStatusCode statusCode, string responseBody, string? errorCode = null)
        : base(statusCode, responseBody, errorCode ?? "SERVER_ERROR", retryable: true)
    {
    }
}

public sealed class SmsFlowNetworkException : Exception
{
    public SmsFlowNetworkException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public bool Retryable => true;
}
