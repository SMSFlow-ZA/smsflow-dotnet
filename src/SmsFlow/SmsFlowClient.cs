using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SmsFlow;

public sealed class SmsFlowClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly SmsFlowClientOptions _options;
    private AuthResponse? _cachedAuth;
    private DateTimeOffset _tokenRefreshAtUtc;

    public SmsFlowClient(SmsFlowClientOptions options)
        : this(new HttpClient { BaseAddress = options.BaseUri, Timeout = options.Timeout }, options)
    {
    }

    public SmsFlowClient(HttpClient httpClient, SmsFlowClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _httpClient.BaseAddress ??= options.BaseUri;
    }

    public async Task<AuthResponse> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException("SMSFlow ClientId and ClientSecret are required.");
        }

        var basicValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        using var response = await SendWithRetryAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/integration/authentication");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicValue);
            return request;
        }, allowRetry: true, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("SMSFlow authentication returned an empty response.");

        _cachedAuth = auth;
        _tokenRefreshAtUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Max(auth.ExpiresInMinutes - 5, 1));
        return auth;
    }

    public async Task<SendSmsResponse> SendSmsAsync(SendSmsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Messages.Count == 0)
        {
            throw new SmsFlowValidationException(HttpStatusCode.BadRequest, "At least one SMS message is required.", "MESSAGES_REQUIRED");
        }

        var token = await GetTokenAsync(cancellationToken).ConfigureAwait(false);
        using var response = await SendWithRetryAsync(() =>
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/integration/BulkMessages");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = JsonContent.Create(new Dictionary<string, object?>
            {
                ["SendOptions"] = new
                {
                    startDeliveryUtc = request.StartDeliveryUtc?.UtcDateTime,
                    campaignName = request.CampaignName,
                    checkOptOuts = request.CheckOptOuts
                },
                ["messages"] = request.Messages.Select(message => new
                {
                    content = message.Content,
                    destination = message.Destination
                })
            }, options: JsonOptions);
            return httpRequest;
        }, request.RetryTemporaryFailures, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        return await response.Content.ReadFromJsonAsync<SendSmsResponse>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("SMSFlow send returned an empty response.");
    }

    public async Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken).ConfigureAwait(false);
        using var response = await SendWithRetryAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/integration/Balance");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }, allowRetry: true, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        return await response.Content.ReadFromJsonAsync<BalanceResponse>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("SMSFlow balance returned an empty response.");
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedAuth is not null && DateTimeOffset.UtcNow < _tokenRefreshAtUtc)
        {
            return _cachedAuth.Token;
        }

        var auth = await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
        return auth.Token;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> createRequest,
        bool allowRetry,
        CancellationToken cancellationToken)
    {
        var attempts = allowRetry ? _options.RetryCount + 1 : 1;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                var response = await _httpClient.SendAsync(createRequest(), cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode || !IsRetryableStatus(response.StatusCode) || attempt == attempts - 1)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (Exception ex) when ((ex is HttpRequestException || ex is TaskCanceledException) && !cancellationToken.IsCancellationRequested)
            {
                if (attempt == attempts - 1)
                {
                    throw new SmsFlowNetworkException("SMSFlow request failed before a response was received.", ex);
                }
            }

            await Task.Delay(RetryDelay(attempt), cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("SMSFlow request retry loop exited unexpectedly.");
    }

    private TimeSpan RetryDelay(int attempt)
    {
        var delayMs = _options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt);
        return TimeSpan.FromMilliseconds(Math.Min(delayMs, _options.RetryMaxDelay.TotalMilliseconds));
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var errorCode = ExtractErrorCode(body);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new SmsFlowAuthenticationException(response.StatusCode, body, errorCode);
        }

        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
        {
            throw new SmsFlowValidationException(response.StatusCode, body, errorCode, IsRetryableStatus(response.StatusCode));
        }

        throw new SmsFlowServerException(response.StatusCode, body, errorCode);
    }

    private static bool IsRetryableStatus(HttpStatusCode statusCode)
    {
        var status = (int)statusCode;
        return status is 408 or 429 || status >= 500;
    }

    private static string? ExtractErrorCode(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Array
                && errors.GetArrayLength() > 0
                && errors[0].TryGetProperty("code", out var code))
            {
                return code.GetString();
            }

            if (document.RootElement.TryGetProperty("code", out var rootCode))
            {
                return rootCode.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
