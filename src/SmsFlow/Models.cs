namespace SmsFlow;

public sealed class SendSmsRequest
{
    public string CampaignName { get; init; } = "SMSFlow API";
    public DateTimeOffset? StartDeliveryUtc { get; init; }
    public bool CheckOptOuts { get; init; } = true;
    public IReadOnlyList<SmsMessage> Messages { get; init; } = [];
}

public sealed class SmsMessage
{
    public string Destination { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public sealed class AuthResponse
{
    public string Token { get; init; } = string.Empty;
    public int ExpiresInMinutes { get; init; }
    public string Schema { get; init; } = string.Empty;
}

public sealed class SendSmsResponse
{
    public int StatusCode { get; init; }
    public SendResponse? SendResponse { get; init; }
    public IReadOnlyList<ApiError>? Errors { get; init; }
}

public sealed class SendResponse
{
    public decimal Cost { get; init; }
    public decimal RemainingBalance { get; init; }
    public int EventId { get; init; }
    public string Sample { get; init; } = string.Empty;
    public int Messages { get; init; }
    public int Parts { get; init; }
    public IReadOnlyList<CostBreakdown> CostBreakdown { get; init; } = [];
    public IReadOnlyList<ApiError> ErrorReport { get; init; } = [];
}

public sealed class CostBreakdown
{
    public int Quantity { get; init; }
    public decimal Cost { get; init; }
    public string Network { get; init; } = string.Empty;
}

public sealed class BalanceResponse
{
    public decimal Balance { get; init; }
}

public sealed class ApiError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Field { get; init; }
}
