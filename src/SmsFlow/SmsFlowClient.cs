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
        : this(new HttpClient { BaseAddress = options.BaseUri }, options)
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/integration/authentication");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicValue);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
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

        var token = await GetTokenAsync(cancellationToken).ConfigureAwait(false);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/integration/BulkMessages");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(new
        {
            SendOptions = new
            {
                startDeliveryUtc = request.StartDeliveryUtc?.UtcDateTime,
                campaignName = request.CampaignName,
                checkOptOuts = request.CheckOptOuts
            },
            messages = request.Messages.Select(message => new
            {
                content = message.Content,
                destination = message.Destination
            })
        }, options: JsonOptions);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        return await response.Content.ReadFromJsonAsync<SendSmsResponse>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("SMSFlow send returned an empty response.");
    }

    public async Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/integration/Balance");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
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

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new SmsFlowException(response.StatusCode, body);
    }
}
