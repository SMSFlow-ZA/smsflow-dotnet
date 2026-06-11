# SMSFlow .NET SDK

Draft public .NET SDK for the SMSFlow HTTPS API.

## Install

Package publishing is not enabled yet. During development, reference the project directly.

## Usage

```csharp
using SmsFlow;

var client = new SmsFlowClient(new SmsFlowClientOptions
{
    ClientId = Environment.GetEnvironmentVariable("SMSFLOW_CLIENT_ID")!,
    ClientSecret = Environment.GetEnvironmentVariable("SMSFLOW_CLIENT_SECRET")!
});

var response = await client.SendSmsAsync(new SendSmsRequest
{
    CampaignName = "SDK example",
    Messages =
    [
        new SmsMessage
        {
            Destination = "27000000000",
            Content = "Your SMSFlow .NET test message was sent successfully."
        }
    ]
});

Console.WriteLine(response.SendResponse?.EventId);
```

## Safety

Never commit real credentials. Use environment variables, user secrets, Azure Key Vault, or another secret manager.
