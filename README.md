# SMSFlow .NET SDK

The SMSFlow .NET SDK makes it easy to send SMS messages and check SMS credit balance from C#, ASP.NET Core, workers, scheduled jobs, CRM integrations, ERP integrations, and other backend .NET applications.

Documentation: https://docs.smsflow.co.za/

## Install

NuGet publishing is not enabled yet. During development, reference the project directly:

```powershell
dotnet add package SmsFlow
```

Until the public NuGet package is published, reference the project from this repository.

## Configuration

Store credentials in environment variables, user secrets, Azure Key Vault, or another secret manager.

```powershell
$env:SMSFLOW_CLIENT_ID = "YOUR_CLIENT_ID"
$env:SMSFLOW_CLIENT_SECRET = "YOUR_CLIENT_SECRET"
```

Do not put SMSFlow Client Secrets in source code, browser apps, mobile apps, logs, screenshots, or public issues.

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

## ASP.NET Core

Register the SDK through `HttpClient` so your application uses normal .NET connection management:

```csharp
builder.Services.AddHttpClient<SmsFlowClient>(client =>
{
    client.BaseAddress = new Uri("https://portal.smsflow.co.za/");
});
```

## Features

- Get and cache SMSFlow login tokens.
- Send one or more SMS messages.
- Schedule SMS messages using UTC delivery time.
- Respect opt-out checks by default.
- Check account balance.
- Raise structured exceptions when the API returns an error.

## Local test send

This command sends a real SMS and may consume test credits:

```powershell
$env:SMSFLOW_CLIENT_ID = "YOUR_CLIENT_ID"
$env:SMSFLOW_CLIENT_SECRET = "YOUR_CLIENT_SECRET"
$env:SMSFLOW_DESTINATION = "27000000000"
dotnet run --project samples/SendSmsExample
```

## Security

Never commit real credentials. Use environment variables, user secrets, Azure Key Vault, or another secret manager.

## License

MIT
