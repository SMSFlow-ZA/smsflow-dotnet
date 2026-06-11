namespace SmsFlow;

public sealed class SmsFlowClientOptions
{
    public Uri BaseUri { get; init; } = new("https://portal.smsflow.co.za/");
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
