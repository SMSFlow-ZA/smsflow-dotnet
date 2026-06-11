using SmsFlow;

var clientId = Environment.GetEnvironmentVariable("SMSFLOW_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("SMSFLOW_CLIENT_SECRET");
var destination = Environment.GetEnvironmentVariable("SMSFLOW_DESTINATION");

if (string.IsNullOrWhiteSpace(clientId) ||
    string.IsNullOrWhiteSpace(clientSecret) ||
    string.IsNullOrWhiteSpace(destination))
{
    Console.Error.WriteLine("Set SMSFLOW_CLIENT_ID, SMSFLOW_CLIENT_SECRET, and SMSFLOW_DESTINATION before running.");
    return 1;
}

var client = new SmsFlowClient(new SmsFlowClientOptions
{
    ClientId = clientId,
    ClientSecret = clientSecret
});

var result = await client.SendSmsAsync(new SendSmsRequest
{
    CampaignName = ".NET SDK sample",
    Messages =
    [
        new SmsMessage
        {
            Destination = destination,
            Content = "Your SMSFlow .NET SDK test message was sent successfully."
        }
    ]
});

Console.WriteLine($"Status: {result.StatusCode}");
Console.WriteLine($"Event ID: {result.SendResponse?.EventId}");
return 0;
