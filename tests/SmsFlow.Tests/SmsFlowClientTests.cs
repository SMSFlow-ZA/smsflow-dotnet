using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SmsFlow.Tests;

public sealed class SmsFlowClientTests
{
    [Fact]
    public async Task SendSmsAsync_AuthenticatesAndPostsBulkMessageShape()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/integration/authentication")
            {
                Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
                return JsonResponse(new { token = "token", expiresInMinutes = 120, schema = "Basic" });
            }

            if (request.RequestUri?.PathAndQuery == "/api/integration/BulkMessages")
            {
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("token", request.Headers.Authorization?.Parameter);
                return JsonResponse(new
                {
                    statusCode = 200,
                    sendResponse = new
                    {
                        cost = 1.25m,
                        remainingBalance = 98.75m,
                        eventId = 123,
                        sample = "Hello",
                        messages = 1,
                        parts = 1,
                        costBreakdown = Array.Empty<object>(),
                        errorReport = Array.Empty<object>()
                    }
                });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://portal.smsflow.co.za/")
        };
        var client = new SmsFlowClient(httpClient, new SmsFlowClientOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret"
        });

        var response = await client.SendSmsAsync(new SendSmsRequest
        {
            CampaignName = "Integration Test",
            Messages =
            [
                new SmsMessage
                {
                    Destination = "27000000000",
                    Content = "Hello from SMSFlow"
                }
            ]
        });

        Assert.Equal(123, response.SendResponse?.EventId);

        var body = handler.PostedBodies.Single();
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("SendOptions", out var sendOptions));
        Assert.Equal("Integration Test", sendOptions.GetProperty("campaignName").GetString());
        Assert.True(document.RootElement.TryGetProperty("messages", out var messages));
        Assert.Equal("27000000000", messages[0].GetProperty("destination").GetString());
    }

    [Fact]
    public async Task GetBalanceAsync_UsesBearerToken()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/integration/authentication")
            {
                return JsonResponse(new { token = "token", expiresInMinutes = 120, schema = "Basic" });
            }

            if (request.RequestUri?.PathAndQuery == "/api/integration/Balance")
            {
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("token", request.Headers.Authorization?.Parameter);
                return JsonResponse(new { balance = 42.50m });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = new SmsFlowClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://portal.smsflow.co.za/") },
            new SmsFlowClientOptions { ClientId = "client-id", ClientSecret = "client-secret" });

        var balance = await client.GetBalanceAsync();

        Assert.Equal(42.50m, balance.Balance);
    }

    [Fact]
    public async Task SendSmsAsync_ThrowsTypedValidationError()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/integration/authentication")
            {
                return JsonResponse(new { token = "token", expiresInMinutes = 120, schema = "Basic" });
            }

            if (request.RequestUri?.PathAndQuery == "/api/integration/BulkMessages")
            {
                return JsonResponse(
                    new { statusCode = 400, errors = new[] { new { code = "INVALID_DESTINATION", message = "Invalid destination." } } },
                    HttpStatusCode.BadRequest);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = new SmsFlowClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://portal.smsflow.co.za/") },
            new SmsFlowClientOptions { ClientId = "client-id", ClientSecret = "client-secret" });

        var exception = await Assert.ThrowsAsync<SmsFlowValidationException>(() => client.SendSmsAsync(new SendSmsRequest
        {
            CampaignName = "Integration Test",
            Messages =
            [
                new SmsMessage
                {
                    Destination = "not-a-number",
                    Content = "Hello from SMSFlow"
                }
            ]
        }));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("INVALID_DESTINATION", exception.ErrorCode);
        Assert.False(exception.Retryable);
    }

    [Fact]
    public async Task AuthenticateAsync_RetriesTemporaryServerFailures()
    {
        var calls = 0;
        var handler = new RecordingHandler(_ =>
        {
            calls++;
            return calls == 1
                ? JsonResponse(new { errors = new[] { new { code = "SERVICE_UNAVAILABLE" } } }, HttpStatusCode.ServiceUnavailable)
                : JsonResponse(new { token = "token", expiresInMinutes = 120, schema = "Basic" });
        });

        var client = new SmsFlowClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://portal.smsflow.co.za/") },
            new SmsFlowClientOptions
            {
                ClientId = "client-id",
                ClientSecret = "client-secret",
                RetryCount = 1,
                RetryBaseDelay = TimeSpan.Zero
            });

        var auth = await client.AuthenticateAsync();

        Assert.Equal("token", auth.Token);
        Assert.Equal(2, calls);
    }

    private static HttpResponseMessage JsonResponse(object body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(body)
        };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<string> PostedBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                PostedBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return responder(request);
        }
    }
}
