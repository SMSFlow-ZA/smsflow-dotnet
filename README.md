# SMSFlow .NET SDK

[![NuGet version](https://img.shields.io/nuget/v/SmsFlow.svg)](https://www.nuget.org/packages/SmsFlow)
[![CI](https://github.com/SMSFlow-ZA/smsflow-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/SMSFlow-ZA/smsflow-dotnet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

The SMSFlow .NET SDK makes it easy to send SMS messages and check SMS credit balance from C#, ASP.NET Core, workers, scheduled jobs, CRM integrations, ERP integrations, and other backend .NET applications.

Documentation: https://docs.smsflow.co.za/

## Install

```powershell
dotnet add package SmsFlow
```

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
    ClientSecret = Environment.GetEnvironmentVariable("SMSFLOW_CLIENT_SECRET")!,
    Timeout = TimeSpan.FromSeconds(30)
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

## Bulk send

```csharp
await client.SendSmsAsync(new SendSmsRequest
{
    CampaignName = "Order dispatch alerts",
    Messages =
    [
        new SmsMessage { Destination = "27000000000", Content = "Order 1001 has shipped." },
        new SmsMessage { Destination = "27000000001", Content = "Order 1002 has shipped." }
    ]
});
```

## Check balance

```csharp
var balance = await client.GetBalanceAsync();
Console.WriteLine(balance.Balance);
```

## Error handling

```csharp
try
{
    await client.SendSmsAsync(new SendSmsRequest
    {
        CampaignName = "Transactional SMS",
        Messages = [new SmsMessage { Destination = "27000000000", Content = "Hello from SMSFlow." }]
    });
}
catch (SmsFlowAuthenticationException)
{
    Console.Error.WriteLine("Check your SMSFlow Client ID and Client Secret.");
    throw;
}
catch (SmsFlowValidationException ex)
{
    Console.Error.WriteLine($"Fix the request before retrying. {ex.ErrorCode}: {ex.ResponseBody}");
    throw;
}
catch (SmsFlowException ex)
{
    Console.Error.WriteLine($"{(int)ex.StatusCode}: {ex.ErrorCode}; retryable={ex.Retryable}; {ex.ResponseBody}");
    throw;
}
```

## ASP.NET Core

Register the SDK through `HttpClient` so your application uses normal .NET connection management:

```csharp
builder.Services.AddHttpClient<SmsFlowClient>(client =>
{
    client.BaseAddress = new Uri("https://portal.smsflow.co.za/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

## Timeouts and retries

```csharp
var client = new SmsFlowClient(new SmsFlowClientOptions
{
    ClientId = Environment.GetEnvironmentVariable("SMSFLOW_CLIENT_ID")!,
    ClientSecret = Environment.GetEnvironmentVariable("SMSFLOW_CLIENT_SECRET")!,
    Timeout = TimeSpan.FromSeconds(30),
    RetryCount = 2,
    RetryBaseDelay = TimeSpan.FromMilliseconds(250),
    RetryMaxDelay = TimeSpan.FromSeconds(2)
});

var balance = await client.GetBalanceAsync(); // Safe to retry temporary failures.

await client.SendSmsAsync(new SendSmsRequest
{
    CampaignName = "Transactional SMS",
    RetryTemporaryFailures = true, // Use only with your own idempotency or duplicate-send guard.
    Messages = [new SmsMessage { Destination = "27000000000", Content = "Hello from SMSFlow." }]
});
```

Retry only temporary network failures, `408`, `429`, and `5xx` responses. Do not retry validation errors, authentication failures, or insufficient-balance responses until the underlying issue has been fixed. Store the returned `eventId` against your own transaction or notification record.

## Delivery status

The public HTTPS API currently exposes authentication, send, and balance endpoints. Delivery-status helper methods will be added when a public delivery-status endpoint is available.

## Features

- Get and cache SMSFlow login tokens.
- Send one or more SMS messages.
- Schedule SMS messages using UTC delivery time.
- Respect opt-out checks by default.
- Check account balance.
- Raise typed structured exceptions when the API returns an error.
- Configure timeouts and opt-in retries for temporary failures.

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
